// Fill out your copyright notice in the Description page of Project Settings.

/*
 * Document:#ESGridSystem.cpp#
 * Author: Yuyang Qiu
 * Function:Build the grid system that allows to place grid on.
 */

#include "Core/Grid/ESGridSystem.h"

#include "Core/Grid/ESGridHelper.h"
#include "Core/Grid/ESGridType.h"
#include "Core/Types/GroundType.h"

FIntPoint FGridTile::GetSize()
{
	FIntPoint Size = FIntPoint::ZeroValue;
	for (const auto p : Shape)
	{
		Size.X = FMath::Max(Size.X, p.X);
		Size.Y = FMath::Max(Size.Y, p.Y);
	}
	return Size;
}

UESGridSystem::UESGridSystem() : UObject()
{
	Grid = TArray<TArray<EGroundType>>();
}

void UESGridSystem::InitializeGrid(int32 Width, int32 Height)
{
	GridSizeX = Width;
	GridSizeY = Height;

	Initialize();
}

void UESGridSystem::Initialize()
{
	Grid.Empty();
	Grid.SetNum(GridSizeY);
	for (int i = 0; i < GridSizeY; i++)
	{
		Grid[i].SetNum(GridSizeX);
	}
}

void UESGridSystem::PlaceInitialTile(TArray<FGridTile> Tiles, EGridDirection Direction)
{
	const int32 X = GridSizeX / 2;
	const int32 Y = GridSizeY / 2;

	for (auto& Tile : Tiles)
	{
		const FIntPoint Size = Tile.GetSize();
		// const int32 X = GridSizeX / 2 - Size.X / 2;
		// const int32 Y = GridSizeY / 2 - Size.Y / 2;
		SetTile(X, Y, Tile.Shape, Tile.Type, Direction);
		// PlaceTile(X, Y, Tile, Direction);
	}
}

EGroundType UESGridSystem::GetTileType(int X, int Y) const
{
	if (ValidPosition(X, Y))
		return Grid[X][Y];

	return EGroundType::None;
}

bool UESGridSystem::HasTile(int X, int Y, const FGridTile Tile, EGridDirection Direction) const
{
	TArray<FIntPoint> RotatedShape = UESGridHelper::RotateShape(Tile.Shape, Direction);

	if (!IsTileNearGround(X, Y, RotatedShape))
	{
		return true;
	}

	for (const auto p : RotatedShape)
	{
		if (ValidPosition(X + p.X, Y + p.Y) &&
			Grid[X + p.X][Y + p.Y] != EGroundType::None)
		{
			return true;
		}
	}
	return false;
}

bool UESGridSystem::IsPointNearGround(int X, int Y) const
{
	if (!ValidPosition(X, Y)) return false;

	if (Grid[X + 1][Y] != EGroundType::None || Grid[X - 1][Y] != EGroundType::None ||
		Grid[X][Y + 1] != EGroundType::None || Grid[X][Y - 1] != EGroundType::None)
		return true;
	return false;
}

bool UESGridSystem::IsTileNearGround(int X, int Y, const TArray<FIntPoint>& Shape) const
{
	for (auto& Point : Shape)
	{
		if (IsPointNearGround(X + Point.X, Y + Point.Y))
			return true;
	}
	return false;
}

bool UESGridSystem::CanPlaceTile(int X, int Y, const FGridTile Tile, EGridDirection Direction) const
{
	TArray<FIntPoint> RotatedShape = UESGridHelper::RotateShape(Tile.Shape, Direction);
	if (!IsTileNearGround(X, Y, RotatedShape))
	{
		return false;
	}

	for (const auto p : RotatedShape)
	{
		if (!ValidPosition(X + p.X, Y + p.Y))
		{
			return false;
		}
	}
	return true;
}

bool UESGridSystem::PlaceTile(int X, int Y, FGridTile Tile, EGridDirection Direction)
{
	if (!HasTile(X, Y, Tile, Direction))
	{
		SetTile(X, Y, Tile.Shape, Tile.Type, Direction);
		return true;
	}
	return false;
}

void UESGridSystem::RemoveTile(int X, int Y, FGridTile Tile, EGridDirection Direction)
{
	if (CanPlaceTile(X, Y, Tile, Direction))
	{
		SetTile(X, Y, Tile.Shape, EGroundType::None, Direction);
	}
}

void UESGridSystem::RemoveOneTile(int X, int Y)
{
	if (ValidPosition(X, Y))
	{
		EGroundType OldType = Grid[X][Y];
		Grid[X][Y] = EGroundType::None;
		OnTileRemovedEvent.Broadcast(X, Y, OldType);
	}
}

void UESGridSystem::UpdateOneTile(int X, int Y, EGroundType Type)
{
	if (ValidPosition(X, Y))
	{
		EGroundType OldType = Grid[X][Y];
		Grid[X][Y] = Type;
		OnTileChangeEvent.Broadcast(X, Y, OldType, Type);
	}
}

void UESGridSystem::UpdateTile(int X, int Y, FGridTile Tile, EGridDirection Direction)
{
	if (CanPlaceTile(X, Y, Tile, Direction))
	{
		SetTile(X, Y, Tile.Shape, Tile.Type, Direction);
	}
}

TArray<FIntPoint> UESGridSystem::GetPoints(int Count)
{
	TArray<FIntPoint> Result;
	for (int i = 0; i < GridSizeY; i++)
	{
		for (int j = 0; j < GridSizeX; ++j)
		{
			if (Grid[j][i] == EGroundType::None) continue;

			Result.Add(FIntPoint(j, i));
		}
	}
	return Result;
}

bool UESGridSystem::ValidPosition(int X, int Y)  const
{
	return !(X < 0 || X >= GridSizeX || Y < 0 || Y >= GridSizeY);
}

bool UESGridSystem::IsGroundValid(int X, int Y, const TArray<FIntPoint>& Shape, EGroundType InGroundType)
{
	for (const auto p : Shape)
	{
		if (InGroundType == EGroundType::None)
		{
			if (Grid[X + p.X][Y + p.Y] == EGroundType::None)
			{
				return false;
			}
		}
		else if (Grid[X + p.X][Y + p.Y] != InGroundType)
		{
			return false;
		}
	}
	return true;
}

void UESGridSystem::SetTile(int X, int Y, const TArray<FIntPoint> Tile, const EGroundType Type,
	EGridDirection Direction)
{
	TArray<FIntPoint> RotatedShape = UESGridHelper::RotateShape(Tile, Direction);
	for (const auto p : RotatedShape)
	{
		EGroundType OldGround = Grid[X + p.X][Y + p.Y];
		Grid[X + p.X][Y + p.Y] = Type;
		// If not remove
		if (OldGround != EGroundType::None && Type != EGroundType::None)
		{
			OnTileChangeEvent.Broadcast(X + p.X, Y + p.Y, OldGround, Type);
		}
		// if removing
		else if (Type == EGroundType::None)
		{
			OnTileRemovedEvent.Broadcast(X + p.X, Y + p.Y, OldGround);
		}
		// If placing
		else
		{
			OnTilePlacedEvent.Broadcast(X + p.X, Y + p.Y, Type);
		}
	}
}
