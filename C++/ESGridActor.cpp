// Fill out your copyright notice in the Description page of Project Settings.

/*
 * Document:#ESGridActor.cpp#
 * Author: Yuyang Qiu
 * Function:The preview actor before place on each grid.
 */

#include "Core/Grid/ESGridActor.h"

#include "Components/InstancedStaticMeshComponent.h"
#include "Core/Game/ESDefaultGameMode.h"
#include "Core/Grid/ESGridHelper.h"
#include "Core/Grid/ESGridSystem.h"


 // Sets default values
AESGridActor::AESGridActor()
{
	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	GroundPlane = CreateDefaultSubobject<UBoxComponent>(TEXT("Ground Plane"));
	GroundPlane->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
	GroundPlane->SetCollisionResponseToAllChannels(ECR_Ignore);
	GroundPlane->SetCollisionResponseToChannel(ECC_GameTraceChannel1, ECR_Block);
	GroundPlane->SetRelativeTransform(
		FTransform(FRotator(0, 0, 0),
			FVector(0, 0, -1),
			FVector(1, 1, 1)));
	SetRootComponent(GroundPlane);

	GridScene = CreateDefaultSubobject<USceneComponent>(TEXT("Grid Scene"));
	GridScene->SetupAttachment(RootComponent);

	// GridMeshes = CreateDefaultSubobject<UInstancedStaticMeshComponent>(TEXT("Grid Meshes"));
	// GridMeshes->SetupAttachment(RootComponent);
	// GridMeshes->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
	// GridMeshes->SetCollisionResponseToAllChannels(ECR_Ignore);
	// GridMeshes->SetCollisionResponseToChannel(ECC_WorldStatic, ECR_Block);
	// GridMeshes->SetCollisionResponseToChannel(ECC_GameTraceChannel2, ECR_Block);

	TilePreviewMesh = CreateDefaultSubobject<UInstancedStaticMeshComponent>(TEXT("Preview Meshes"));
	TilePreviewMesh->SetupAttachment(RootComponent);
	TilePreviewMesh->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
	TilePreviewMesh->SetCollisionResponseToAllChannels(ECR_Ignore);
	TilePreviewMesh->SetCollisionResponseToChannel(ECC_WorldStatic, ECR_Block);
}

// Called when the game starts or when spawned
void AESGridActor::BeginPlay()
{
	Super::BeginPlay();

	TilePreviewMesh->SetStaticMesh(TileMesh);
	TilePreviewMesh->SetMaterial(0, PreviewMaterials);

	const auto GameMode = Cast<AESDefaultGameMode>(GetWorld()->GetAuthGameMode());
	if (GameMode)
	{
		GridSystem = GameMode->GridSystem;
		GridSystem->OnTilePlaced().AddUObject(this, &AESGridActor::OnTilePlaced);
		GridSystem->OnTileRemoved().AddUObject(this, &AESGridActor::OnTileRemoved);
		GridSystem->OnTileChanged().AddUObject(this, &AESGridActor::OnTileUpdate);
	}

	GroundPlane->SetBoxExtent(FVector(GridSize * GridSystem->GridSizeX / 2, GridSize * GridSystem->GridSizeY / 2, 1));
	GridScene->SetRelativeLocation(FVector(-(GridSize * GridSystem->GridSizeX / 2), -(GridSize * GridSystem->GridSizeY / 2), 1));
	// GridMeshes->SetRelativeLocation(FVector(-(GridSize * GridSystem->GridSizeX / 2), -(GridSize * GridSystem->GridSizeY / 2), 1));
	TilePreviewMesh->SetRelativeLocation(FVector(-(GridSize * GridSystem->GridSizeX / 2), -(GridSize * GridSystem->GridSizeY / 2), 1));

	PreviewMaterial = UMaterialInstanceDynamic::Create(TilePreviewMesh->GetMaterial(0), this);
	TilePreviewMesh->SetMaterial(0, PreviewMaterial);

	// Initial Ground Meshes
	for (int i = 1; i < 6; ++i)
	{
		EGroundType Type = static_cast<EGroundType>(i);
		MeshIndex.Emplace(Type, TArray<FIntPoint>());
		UInstancedStaticMeshComponent* MeshInstance = NewObject<UInstancedStaticMeshComponent>(this, *FString("Grid Mesh" + FString::FromInt(i)));
		MeshInstance->SetupAttachment(GridScene);
		MeshInstance->RegisterComponent();
		// MeshInstance->AttachToComponent(GridScene, FAttachmentTransformRules::KeepRelativeTransform);
		MeshInstance->SetCollisionEnabled(ECollisionEnabled::QueryOnly);
		MeshInstance->SetCollisionResponseToAllChannels(ECR_Ignore);
		MeshInstance->SetCollisionResponseToChannel(ECC_WorldStatic, ECR_Block);
		MeshInstance->SetCollisionResponseToChannel(ECC_GameTraceChannel2, ECR_Block);
		MeshInstance->SetStaticMesh(TileMesh);
		switch (i)
		{
		case 1:
			MeshInstance->ComponentTags.Add(FName("Rock"));
			break;
		case 2:
			MeshInstance->ComponentTags.Add(FName("Sand"));
			break;
		case 3:
			MeshInstance->ComponentTags.Add(FName("Grass"));
			break;
		}

		if (GroundMaterials.Contains(Type))
		{
			for (int j = 0; j < GroundMaterials[Type].Materials.Num(); ++j)
			{
				MeshInstance->SetMaterial(j, GroundMaterials[Type].Materials[j]);
			}
		}
		GridMeshes.Add(Type, MeshInstance);
	}
}

void AESGridActor::OnTilePlaced(int X, int Y, EGroundType Type)
{
	const FTransform Transform = GetTileTransform(X, Y);
	const int32 i = GridMeshes[Type]->AddInstance(Transform);
	if (i != MeshIndex[Type].Add(FIntPoint(X, Y)))
	{
		UE_LOG(LogTemp, Warning, TEXT("Adding wrong"))
	}
	OnTileSpawned(Type, i);
}

void AESGridActor::OnTileRemoved(int X, int Y, EGroundType Type)
{
	FIntPoint Point = FIntPoint(X, Y);
	for (int i = 0; i < MeshIndex[Type].Num(); ++i)
	{
		if (Point == MeshIndex[Type][i])
		{
			MeshIndex[Type].RemoveAt(i);
			GridMeshes[Type]->RemoveInstance(i);
			break;
		}
	}
	OnTileDestroyed(Type, X, Y);
}

void AESGridActor::OnTileUpdate(int X, int Y, EGroundType OldType, EGroundType NewType)
{
	OnTileRemoved(X, Y, OldType);
	OnTilePlaced(X, Y, NewType);
}

void AESGridActor::SetPreviewMesh(const FGridTile Tile, EGridDirection Direction)
{
	HidePreviewMesh();

	CurrentTile = Tile;
	CurrentDirection = Direction;

	// auto TextureRen
	TArray<FIntPoint> RotatedShape = UESGridHelper::RotateShape(Tile.Shape, Direction);
	PreviewMaterial->SetTextureParameterValue(FName("Color"), PreviewGroundMaterials[Tile.Type]);
	for (auto p : RotatedShape)
	{
		FTransform Transform = GetTileTransform(p.X, p.Y);
		TilePreviewMesh->AddInstance(Transform);
	}

	// auto Type = Tile.Type;
	// if (PreviewGroundMaterials.Contains(Type))
	// {
	// 	TilePreviewMesh->SetVectorParameterValueOnMaterials()
	// 	TilePreviewMesh->SetMaterial(0, GroundMaterials[Type]);
	// }
}

void AESGridActor::UpdatePreviewMesh(int X, int Y)
{
	// Calculate Correct Transform
	FTransform Transform = GetTileTransform(X, Y);
	CurrentTilePreviewLocation = FIntPoint(X, Y);
	const FVector Location = Transform.GetLocation();
	Transform.SetLocation(FVector(Location.X - (GridSize * GridSystem->GridSizeX / 2), Location.Y - (GridSize * GridSystem->GridSizeY / 2), 1));
	Transform.SetScale3D(FVector(1));
	TilePreviewMesh->SetRelativeTransform(Transform);

	// Check if Tile is Valid
	if (GridSystem->HasTile(X, Y, CurrentTile, CurrentDirection))
	{
		TilePreviewMesh->SetScalarParameterValueOnMaterials(MaterialIsErrorParamName, 1);
	}
	else
	{
		TilePreviewMesh->SetScalarParameterValueOnMaterials(MaterialIsErrorParamName, 0);
	}
}

void AESGridActor::UpdatePreviewMeshByWorldLocation(const FVector Location)
{
	const FVector GroundPosition = GetActorTransform().InverseTransformPosition(Location);
	const FVector GridPosition = GridScene->GetRelativeTransform().InverseTransformPosition(GroundPosition);

	const int X = FMath::FloorToInt(GridPosition.X / GridSize);
	const int Y = FMath::FloorToInt(GridPosition.Y / GridSize);
	UpdatePreviewMesh(X, Y);
	// UE_LOG(LogTemp, Warning, TEXT("GridPosition: %s Pos: %d, %d"), *GridPosition.ToString(), X, Y);
}

void AESGridActor::HidePreviewMesh()
{
	TilePreviewMesh->ClearInstances();
	TilePreviewMesh->SetRelativeTransform(FTransform());
}

bool AESGridActor::ConfirmPlacement()
{
	if (GridSystem->CanPlaceTile(CurrentTilePreviewLocation.X, CurrentTilePreviewLocation.Y, CurrentTile, CurrentDirection)
		&& !GridSystem->HasTile(CurrentTilePreviewLocation.X, CurrentTilePreviewLocation.Y, CurrentTile, CurrentDirection))
	{
		GridSystem->PlaceTile(CurrentTilePreviewLocation.X, CurrentTilePreviewLocation.Y, CurrentTile, CurrentDirection);
		HidePreviewMesh();
		return true;
	}
	return false;
}

FVector AESGridActor::GetCenterLocation()
{
	return FVector(GridSize * GridSystem->GridSizeX / 2, GridSize * GridSystem->GridSizeY / 2, 1);
}

// Called every frame
void AESGridActor::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);
}

FTransform AESGridActor::GetTileTransform(int X, int Y)
{
	float OffsetX = (X * GridSize) + GridOffset;
	float OffsetY = (Y * GridSize) + GridOffset;
	return FTransform(FRotator::ZeroRotator, FVector(OffsetX, OffsetY, 0), TileScale);
}

