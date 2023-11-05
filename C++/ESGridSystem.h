// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "UObject/Object.h"
#include "Core/Types/GroundType.h"
#include "Core/Grid/ESGridType.h"
#include "ESGridSystem.generated.h"

USTRUCT(BlueprintType)
struct FGridTile
{
	GENERATED_BODY()

		UPROPERTY(BlueprintReadWrite, EditAnywhere)
		TArray<FIntPoint> Shape;

	UPROPERTY(BlueprintReadWrite, EditAnywhere)
		EGroundType Type = EGroundType::None;

	FIntPoint GetSize();
};

/**
 *
 */
UCLASS(BlueprintType)
class EVEOFTHESTORM_API UESGridSystem : public UObject
{
	GENERATED_BODY()

public:
	UESGridSystem();

	UPROPERTY(BlueprintReadOnly, Transient)
		int32 GridSizeX = 256;

	UPROPERTY(BlueprintReadOnly, Transient)
		int32 GridSizeY = 256;

	void InitializeGrid(int32 Width, int32 Height);

	UFUNCTION(BlueprintCallable)
		void Initialize();

	UFUNCTION(BlueprintCallable)
		void PlaceInitialTile(TArray<FGridTile> Tiles, EGridDirection Direction);

	UFUNCTION(BlueprintCallable)
		EGroundType GetTileType(int X, int Y) const;

	UFUNCTION(BlueprintCallable)
		bool HasTile(int X, int Y, const FGridTile Tile, EGridDirection Direction) const;

	UFUNCTION(BlueprintCallable)
		bool IsPointNearGround(int X, int Y) const;

	UFUNCTION(BlueprintCallable)
		bool IsTileNearGround(int X, int Y, const TArray<FIntPoint>& Shape) const;

	UFUNCTION(BlueprintCallable)
		bool CanPlaceTile(int X, int Y, const FGridTile Tile, EGridDirection Direction) const;

	UFUNCTION(BlueprintCallable)
		bool PlaceTile(int X, int Y, FGridTile Tile, EGridDirection Direction);

	UFUNCTION(BlueprintCallable)
		void RemoveTile(int X, int Y, FGridTile Tile, EGridDirection Direction);

	UFUNCTION(BlueprintCallable)
		void RemoveOneTile(int X, int Y);

	UFUNCTION(BlueprintCallable)
		void UpdateOneTile(int X, int Y, EGroundType Type);

	UFUNCTION(BlueprintCallable)
		void UpdateTile(int X, int Y, FGridTile Tile, EGridDirection Direction);

	UFUNCTION(BlueprintCallable)
		TArray<FIntPoint> GetPoints(int Count);

	DECLARE_EVENT_ThreeParams(UESGridSystem, FOnTilePlacedEvent, int, int, EGroundType)
		FOnTilePlacedEvent& OnTilePlaced() { return OnTilePlacedEvent; }

	DECLARE_EVENT_ThreeParams(UESGridSystem, FOnTileRemovedEvent, int, int, EGroundType)
		FOnTileRemovedEvent& OnTileRemoved() { return OnTileRemovedEvent; }

	DECLARE_EVENT_FourParams(UESGridSystem, FOnTileChangedEvent, int, int, EGroundType, EGroundType)
		FOnTileChangedEvent& OnTileChanged() { return OnTileChangeEvent; }

	UFUNCTION(BlueprintCallable)
		bool ValidPosition(int X, int Y) const;

	UFUNCTION(BlueprintCallable, Category = "ES|Building")
		bool IsGroundValid(int X, int Y, const TArray<FIntPoint>& Shape, EGroundType InGroundType = EGroundType::None);

protected:
	void SetTile(int X, int Y, const TArray<FIntPoint> Tile, const EGroundType Type, EGridDirection Direction);

	TArray<TArray<EGroundType>> Grid;

private:
	FOnTilePlacedEvent OnTilePlacedEvent;

	FOnTileRemovedEvent OnTileRemovedEvent;

	FOnTileChangedEvent OnTileChangeEvent;
};
