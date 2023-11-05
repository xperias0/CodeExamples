/*
 * Document:#GridSystem.cpp#
 * Author: Yuyang Qiu
 * Function:Build cube place grid system.
 */

// Fill out your copyright notice in the Description page of Project Settings.
#include "GridSystem.h"
#include "GameFramework/PlayerController.h"
#include "AI/NavigationSystemBase.h"
#include "Blueprint/WidgetLayoutLibrary.h"
#include "Kismet/GameplayStatics.h"


// Sets default values
AGridSystem::AGridSystem()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;
 
}

// Called when the game starts or when spawned
void AGridSystem::BeginPlay()
{
	Super::BeginPlay();
	
	PlayerController = UGameplayStatics::GetPlayerController(this, 0);
	generateGridSystem();
	
	curName = actorToPlace -> GetName();
	TArray<FString> subStrings;
	
	curName.ParseIntoArray(subStrings, TEXT("_"), true);
	FString state = subStrings[1];

	for(int i=0;i<4;i++)
	{
		shapeArray.Add(FVector2d(0,0));
	}
	UE_LOG(LogTemp,Display,TEXT("state: %s"),*state);

	switch (state)
	{
	case "Square":
		curShape = shape::Square;
		break;

	case "Line":
		curShape = shape::Line;
		break;

	default:
		curShape = shape::Single;
	}
	
	
}

// Called every frame
void AGridSystem::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
	objController();

	int curI = static_cast<int>(curRNC.X);
	int curJ = static_cast<int>(curRNC.Y);

	if(PlayerController ->IsInputKeyDown(EKeys::LeftMouseButton))
	{
	
		if(isPlaceable() )
		{
			
			spawnActor();	
			if(curShape == Single)
			{
				 
				placeableArray[curI][curJ] = false;
				plotArray[curI][curJ] = Default;
			}else{
				
				for(FVector2d f : shapeArray)
				{
					int i = static_cast<int>(f.X);
					int j = static_cast<int>(f.Y);
					placeableArray[i][j] = false;
					plotArray[i][j] = Default;
				}
			}
			
		}
		
	}
	
}

/// <summary>
/// Initialize grid system.
/// </summary>
void AGridSystem::generateGridSystem()
{
	gridArray = new FVector3d*[gridSize];
	placeableArray = new bool*[gridSize];
	worldOffset = (gridSize * worldGridSize) * 0.5 - (worldGridSize * 0.5);

	for (int i = 0; i < gridSize; i++)
	{
		gridArray[i] = new FVector3d[gridSize]; 
		placeableArray[i] = new bool[gridSize];
		
		for (int j = 0; j < gridSize; j++) 
			{
			
			float curX = (j * worldGridSize) - worldOffset;
			float curY = (i * worldGridSize) - worldOffset;
			FVector3d curPosition = FVector3d(curX, curY, 0);
			gridArray[i][j] = curPosition;
			placeableArray[i][j] = true;

			}
		
	}

	plotArray.Init(TArray<enum plotType>(),gridSize);
	
	for(int i = 0;i<gridSize;i++)
	{
		for(int j = 0;j<gridSize;j++)
		{
			plotArray[i].Add(Empty);
		}
		
	}
}


/// <summary>
/// Get closest gird position of current mouse position.
/// </summary>
/// <param name="inPosition"></param>
/// <returns>A closest gird position</returns>
FVector AGridSystem::getClosestPosition(FVector3d inPosition)
{
	FVector3d closestPosition = inPosition;
	float closestDistance = defaultClosestDis;
	
	
	for(int i=0;i<gridSize;i++)
	{
		for(int j=0;j<gridSize;j++)
		{
			FVector3d curPositon = gridArray[i][j];
			float curDistance = FVector3d::Distance(closestPosition,curPositon);		

			if(curDistance < closestDistance)
			{
				closestPosition = curPositon;
				closestDistance = curDistance;
				curRNC = FVector2d(i,j);
			}
			
		}
		
	}
	
	
	
	return closestPosition;
}

FVector AGridSystem::GetMouseHitLocation()
{
	
	
	if (PlayerController)
	{
		
		float MouseX, MouseY;
		PlayerController->GetMousePosition(MouseX, MouseY);

		
		FVector WorldLocation, WorldDirection;
		PlayerController->DeprojectScreenPositionToWorld(MouseX, MouseY, WorldLocation, WorldDirection);

		
		FCollisionQueryParams QueryParams;
		QueryParams.bTraceComplex = true;
		QueryParams.bReturnPhysicalMaterial = false;

		FCollisionObjectQueryParams ObjectQueryParams;
		ObjectQueryParams.AddObjectTypesToQuery(ECollisionChannel::ECC_GameTraceChannel1);
		FHitResult HitResult;
		GetWorld()->LineTraceSingleByObjectType(HitResult, WorldLocation, WorldLocation + WorldDirection * 4000, ObjectQueryParams, QueryParams);

		if (HitResult.IsValidBlockingHit())
		{
			//GEngine->AddOnScreenDebugMessage(-1, 3.0f, FColor::Red, TEXT("hit"));
			return HitResult.Location;
		}
	}

	return FHitResult().Location;
}


/// <summary>
/// Determine wheather it can place a cube on the grid or not.
/// </summary>
/// <returns>True if it is placeable, false if not</returns>
bool AGridSystem::isPlaceable()
{
	
		bool isInGrid = false;
		for(int i=0;i<gridSize;i++)
		{
			for(int j=0;j<gridSize;j++)
			{
				FVector3d curPos = gridArray[i][j];
				if(curPos == curMousePos)
				{
					isInGrid = true;
				}
			
			}
		
		}

		bool allTrue = true;
		
		if(isInGrid)
		{
			if(curType == Plot)
			{
				shapeArray = buildShapeArray(shapeArray);

				for(FVector2d i : shapeArray)
				{
					int curI = static_cast<int>(i.X);
					int curJ = static_cast<int>(i.Y);
				
					if(placeableArray[curI][curJ] == false)
					{
						allTrue = false;
						break;
					}
			
				}
			}else
			{
				int curI = static_cast<int>(curRNC.X);
				int curJ = static_cast<int>(curRNC.Y);


				if(plotArray[curI][curJ] == Empty)
				{
					allTrue = false;
				}
				
			}
			

			
		}
	
		if(allTrue && isInGrid)
		{
			return true;
		}

		return false;
	
	
}

void AGridSystem::SetupInputComponent()
{
	Super::SetupInputComponent();

	
	FInputKeyBinding LeftMouseButtonBinding(EKeys::LeftMouseButton, EInputEvent::IE_Pressed);
	LeftMouseButtonBinding.KeyDelegate.BindDelegate(this, &AGridSystem::OnLeftMouseButtonPressed);
	InputComponent->KeyBindings.Add(LeftMouseButtonBinding);
}

void AGridSystem::OnLeftMouseButtonPressed()
{
	
	UE_LOG(LogTemp,Display,TEXT("Left Mouse Button Pressed"));
}


/// <summary>
/// Preview cube, normal color if it's placeable, and red color if 
/// it's not.
/// </summary>
void AGridSystem::objController()
{
	FVector3d mousePosition = GetMouseHitLocation();
//	FVector3d newPosition   = getClosestPosition(mousePosition,actorToPlace ->GetClass());

	curMousePos = getClosestPosition(mousePosition);
	actorToPlace -> SetActorLocation(curMousePos);

	
	bool Placeable = isPlaceable();
	
	if(Placeable && !swithced)
	{
		
		switchMaterial(defaultMaterial);
		swithced = true;
	}
	if(!Placeable && swithced){
		
		switchMaterial(transparentMaterial);
		swithced = false;
	}
}


void  AGridSystem::switchStaticMesh(AActor* actor)
{

	UStaticMeshComponent* meshComponent = actorToPlace -> FindComponentByClass<UStaticMeshComponent>();
	UStaticMesh* meshToSet = meshComponent -> GetStaticMesh();
	
	UStaticMeshComponent* meshCom = NewObject<UStaticMeshComponent>();
	meshCom -> SetStaticMesh(meshToSet);
	
	
}

void AGridSystem::switchMaterial(UMaterialInterface* material)
{
	UStaticMeshComponent* CurMeshComponent = actorToPlace -> FindComponentByClass<UStaticMeshComponent>();
	CurMeshComponent -> SetMaterial(0,material);
	
	if(CurMeshComponent)
	{
		
		for(UActorComponent* component :CurMeshComponent -> GetAttachChildren())
		{
			UStaticMeshComponent* meshComponent = Cast<UStaticMeshComponent>(component);
			if(meshComponent)
			{
				meshComponent -> SetMaterial(0,material);
			}
	
		}
		
	}
}


 TArray<FVector2d> AGridSystem::buildShapeArray(TArray<FVector2d> inArray)
{	
	
	int curI = static_cast<int>(curRNC.X);
	
	int curJ = static_cast<int>(curRNC.Y);

	FVector2d cur = FVector2d(curI,curJ);
	inArray[0] = cur;
	if(curShape == Line)
	{
		int addI = curI - 1 < 0 ? 0 : curI - 1;
		for(int i=1;i<3;i++)
		{
			inArray[i] = cur;
		}
		inArray[3] = FVector2d(addI,curJ);
		
	}else if (curShape == Square)
	{
		
		int addJ = curJ + 1 > gridSize - 1 ? gridSize - 1 : curJ + 1;
		int minI = curI - 1 < 0 ? 0 : curI - 1;

		inArray[1] = FVector2d(curI,addJ);
		inArray[2] = FVector2d(minI,addJ);
		inArray[3] = FVector2d(minI,curJ);
		
		
	}
	return inArray;
}


/// <summary>
/// Change current mesh component of an actor.
/// </summary>
/// <param name="actor"></param>
void AGridSystem::setCurShape(AActor* actor)
{
	FString s = actor -> GetName();
	curName = actor -> GetName();
	actorToPlace -> SetActorLocation(FVector3d(100,100,-400));
	actorToPlace = actor;
	curType = Plot;
	TArray<FString> subStrings;

	UStaticMeshComponent* meshComponent = actorToPlace -> FindComponentByClass<UStaticMeshComponent>();
	UMaterialInterface* mat = meshComponent -> GetMaterial(0);
	
	defaultMaterial = mat;
	
	curName.ParseIntoArray(subStrings, TEXT("_"), true);
	FString state = subStrings[1];

	if(state == "Square")
	{
		curShape = shape::Square;
	}else if(state == "Line")
	{
		curShape = shape::Line;
	}else
	{
		curShape = shape::Single;
	}
	
	UE_LOG(LogTemp,Display,TEXT("SET:%s"),*s);
}


void AGridSystem::spawnActor( )
{

	if(isPlaceable() && curType == Plot)
	{
	
		
		FActorSpawnParameters Parameters;
		
			FTransform3d trans;
			
			trans.SetLocation(curMousePos);
			trans.SetScale3D((FVector(1,1,1)));
		    GetWorld() ->SpawnActor<AActor>(actorToPlace->GetClass(),trans,Parameters);
		
	}
	
	if( curType == Building)
	{
		FActorSpawnParameters Parameters;
		FTransform3d trans;
		trans.SetLocation(curMousePos);
		trans.SetScale3D((FVector(1,1,1)));
		GetWorld() ->SpawnActor<AActor>(actorToPlace->GetClass(),trans,Parameters);
	
		
		
	}

		
	}
	


void AGridSystem::setBuilding(AActor* actor)
{
	curName = "Single";
	actorToPlace -> SetActorLocation(FVector3d(100,100,-400));
	actorToPlace = actor;
	
	UStaticMeshComponent* meshComponent = actorToPlace -> FindComponentByClass<UStaticMeshComponent>();
	UMaterialInterface* mat = meshComponent -> GetMaterial(0);
	defaultMaterial = mat;
	
	
	
	curType  = Building;
	curShape = Single;
}
