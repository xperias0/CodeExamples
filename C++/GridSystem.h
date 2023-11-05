// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "hhPlot.h"
#include "Building.h"	
#include "GameFramework/Actor.h"
#include "GameFramework/PlayerController.h"
#include "GridSystem.generated.h"


UCLASS()
class STORM_API AGridSystem :public APlayerController
{
	GENERATED_BODY()
	
	enum plotType
	{
		Empty,
		Default,
		Stone,
		Sand,
		Grass,
		Lake,
		Montain
	};
public:	
	
	AGridSystem();

	enum type
	{
		Null,
		Plot,
		Building
		
	};
	enum shape
	{
		Single,
		Square,
		Line
	};

	FVector3d curMousePos;

	AActor* curActor;
	UPROPERTY(EditAnywhere)
	int gridSize = 20;

	UPROPERTY(EditAnywhere)
	AActor* testBall ;
	
	UPROPERTY(EditAnywhere)
	int defaultClosestDis = 50;
	
	UPROPERTY(EditAnywhere)
	int	worldGridSize = 100;

	UPROPERTY(EditAnywhere)
	AActor* actorToPlace ;

	UPROPERTY(EditAnywhere)
	UMaterialInterface* defaultMaterial;

	UPROPERTY(EditAnywhere)
	UMaterialInterface* transparentMaterial;
	
	float worldOffset;	
	FVector3d** gridArray ;

	FString curName ; 

	bool** placeableArray;

	TArray<TArray<plotType>> plotArray;
	
	FVector2d curRNC = FVector2d(0,0);

	APlayerController* PlayerController;

	TArray<FVector2d> shapeArray;

	plotType  curPlotType = Default;
	shape curShape = Single;
	type curType = Plot;

	int plotType = curPlotType;
	int shape    = curShape; 
	void generateGridSystem();	

	//void spawnNewActor(FVector position,TSubclassOf<AActor> actorToSpawn);

	FVector getClosestPosition(FVector3d inPosition);

	FVector GetMouseHitLocation();
	
	bool isPlaceable();

	void SetupInputComponent() override;

	void OnLeftMouseButtonPressed();

	void objController();

	void switchMaterial(UMaterialInterface* material);

	void switchStaticMesh(AActor* actor);

	UFUNCTION(BlueprintCallable)
	void setCurShape(AActor* actor);
	
	UFUNCTION(BlueprintCallable)
	void setBuilding(AActor* actor);

	TArray<FVector2d> buildShapeArray(TArray<FVector2d> inArray);

	void spawnActor();
	
protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

	bool swithced = false;

public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

};
