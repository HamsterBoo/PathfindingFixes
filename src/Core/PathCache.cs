using BattleTech;
using HBS.Math;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace PathfindingFixes {
  public class PathCache {
    private static PathCache instance;
    public static PathCache Instance {
      get {
        if (instance == null) instance = new PathCache();
        return instance;
      }
    }
    private CacheNode[,] cacheNodes;

    public float OneXUnit { get; private set; }
    public float OneZUnit { get; private set; }
    public float OneUnitDiag { get; private set; }
    public int CenterX { get; private set; }
    public int CenterZ { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public float WorldXFromNodeX(int X) {
      return (float)(X - this.CenterX) * this.OneXUnit;
    }
    public float WorldZFromNodeZ(int Z) {
      return (float)(Z - this.CenterZ) * this.OneZUnit;
    }

    public Vector3 WorldPosFromNode(int X, int Z) {
      return new Vector3(this.WorldXFromNodeX(X), 0f, this.WorldZFromNodeZ(Z));
    }

    public bool IsValidPosition(int X, int Z) {
      switch ((Z - this.CenterZ) % 4) {
        case (0):
          return this.CenterX % 2 == X % 2;
        case (2):
          return this.CenterX % 2 != X % 2;
        default:
          return false;
      }
    }

    public CacheNode GetNodeAt(int X, int Z) {
      if (X >= 0 && X < this.Width && Z >= 0 && Z < this.Height) {
        return this.cacheNodes[X, Z];
      }
      return default(CacheNode);
    }

    public float DistFromAngle(int angle) {
      switch (angle) {
        case 0:
        case 4:
          return this.OneZUnit;
        case 2:
        case 6:
          return this.OneXUnit;
        default:
          return this.OneUnitDiag;
      }
    }

    public int NodeXFromWorldX(float x) {
      return Mathf.RoundToInt(x / this.OneXUnit) + this.CenterX;
    }

    public int NodeZFromWorldZ(float z) {
      return Mathf.RoundToInt(z / this.OneZUnit) + this.CenterZ;
    }

    public Point NodePosFromWorldPos(Vector3 pos) {
      return new Point(this.NodeXFromWorldX(pos.x), this.NodeZFromWorldZ(pos.z));
    }

    public void SetDimensions(MapMetaData mapMetaData, HexGrid hexGrid) {
      // Pathfinding Nodes are on a rectangular catesian grid thats twice as dense as the hex grid
      this.OneXUnit = hexGrid.HexWidth / 2f;
      this.OneZUnit = hexGrid.HexWidth * HexGrid.SQRT_3 / 4f;
      this.OneUnitDiag = Mathf.Sqrt(this.OneXUnit * this.OneXUnit + this.OneZUnit * this.OneZUnit);

      // invert MapMetaData.GetIndex for the first and last cell (do not count for MapMetaData.IsWithinBounds)
      float halfCell = (float)MapMetaDataExporter.cellSize * 0.5f;
      float minWorldX = -1024f - halfCell;
      float maxWorldX = 1024f - 2048f / (float)mapMetaData.mapTerrainDataCells.GetLength(1) - halfCell;
      float minWorldZ = -1024f - halfCell;
      float maxWorldZ = 1024f - 2048f / (float)mapMetaData.mapTerrainDataCells.GetLength(0) - halfCell;

      // find center, ensuring (0,0) isn't in the 1-cell border
      this.CenterX = (int)Mathf.Abs(minWorldX / this.OneXUnit);
      if (!mapMetaData.IsWithinBounds(new Vector3(this.WorldXFromNodeX(0), 0f, 0f))) {
        this.CenterX -= 1;
      }
      this.CenterZ = (int)Mathf.Abs(minWorldZ / this.OneZUnit);
      if (!mapMetaData.IsWithinBounds(new Vector3(0f, 0f, this.WorldXFromNodeX(0)))) {
        this.CenterZ -= 1;
      }

      // find width and height, ensuring (width-1,height-1) isn't in the 1-cell border
      this.Width = 1 + this.CenterX + (int)(maxWorldX / this.OneXUnit);
      if (!mapMetaData.IsWithinBounds(new Vector3(this.WorldXFromNodeX(this.Width - 1), 0f, 0f))) {
        this.Width -= 1;
      }
      this.Height = 1 + this.CenterZ + (int)(maxWorldZ / this.OneZUnit);
      if (!mapMetaData.IsWithinBounds(new Vector3(0f, 0f, this.WorldXFromNodeX(this.Height - 1)))) {
        this.Height -= 1;
      }
    }

    public void ResetCache(CombatGameState combat) {
      MapMetaData mapMetaData = combat.MapMetaData;
      HexGrid hexGrid = combat.HexGrid;
      CacheNodeLink.PathBlockerGradeMultiplier = combat.Constants.MoveConstants.PathBlockerGradeMultiplier;

      this.SetDimensions(mapMetaData, hexGrid);

      this.cacheNodes = new CacheNode[this.Width, this.Height];
      for (int x = 0; x < this.Width; x++) {
        for (int z = 0; z < this.Height; z++) {
          Vector3 pos = this.WorldPosFromNode(x, z);
          this.cacheNodes[x, z] = new CacheNode(pos, mapMetaData);
        }
      }

      for (int x = 0; x < this.Width; x++) {
        for (int z = 0; z < this.Height; z++) {
          CacheNode from = this.cacheNodes[x, z];
          for (int k = 0; k < 8; k++) {
            Point diff = PathingUtil.GetForwardDelta(k);
            CacheNode to = this.GetNodeAt(x + diff.X, z + diff.Z);
            if (to != null) {
              from.NeighborLinks[k] = new CacheNodeLink(from, to, this.DistFromAngle(k), k, mapMetaData);
            }
          }
        }
      }
    }

    public CacheNodeLink GetCacheNodeLink(Point pos, int angle) {
      return this.cacheNodes[pos.X, pos.Z].NeighborLinks[angle];
    }

    /*
    public static float GetTerrainCost(MapTerrainDataCell cell, AbstractActor unit, MoveType moveType)
    {
      float cost = unit.Pathing.getGrid(moveType).Capabilities.MoveCostNormal;
      if (cell == null)
      {
          return cost;
      }
      DesignMaskDef mask = unit.Combat.MapMetaData.GetPriorityDesignMask(cell);
      if (mask == null)
      {
          return cost;
      }
      Mech mech = (unit as Mech);
      Vehicle vehicle = (unit as Vehicle);
      if (mech != null)
      {
        WeightClass weightClass = mech.weightClass;
        switch (weightClass) {
          case (WeightClass.LIGHT):
            cost = mask.moveCostMechLight;
            break;
          case (WeightClass.MEDIUM):
            cost = mask.moveCostMechMedium;
            break;
          case (WeightClass.ASSAULT):
            cost = mask.moveCostMechAssault;
            break;
          default:
            cost = mask.moveCostMechHeavy;
            break;
        }
      }
      else if (vehicle != null)
      {
        VehicleMovementType movementType = vehicle.movementType;
        if (movementType == VehicleMovementType.Wheeled) {
          WeightClass weightClass = vehicle.weightClass;
          switch (weightClass) {
            case (WeightClass.LIGHT):
              cost = mask.moveCostWheeledLight;
              break;
            case (WeightClass.MEDIUM):
              cost = mask.moveCostWheeledMedium;
              break;
            case (WeightClass.ASSAULT):
              cost = mask.moveCostWheeledAssault;
              break;
            default:
              cost = mask.moveCostWheeledHeavy;
              break;
          }
        }
        else if (movementType == VehicleMovementType.Tracked)
        {
          WeightClass weightClass = vehicle.weightClass;
          switch (weightClass) {
            case (WeightClass.LIGHT):
              cost = mask.moveCostTrackedLight;
              break;
            case (WeightClass.MEDIUM):
              cost = mask.moveCostTrackedMedium;
              break;
            case (WeightClass.ASSAULT):
              cost = mask.moveCostTrackedAssault;
              break;
            default:
              cost = mask.moveCostTrackedHeavy;
              break;
          }
        }
      }
      if (moveType == MoveType.Sprinting)
      {
          cost *= mask.moveCostSprintMultiplier;
      }
      return cost;
    }
    */
  }
}
