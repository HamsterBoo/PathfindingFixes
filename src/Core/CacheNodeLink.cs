using System.Collections.Generic;
using UnityEngine;
using BattleTech;

namespace PathfindingFixes {
  public class CacheNodeLink {
    public struct DistCell {
      public MapTerrainDataCell cell;
      public float dist;
      public DistCell(float dist, MapTerrainDataCell cell) {
        this.dist = dist;
        this.cell = cell;
      }
    }

    public CacheNode From;
    public CacheNode To;
    public CacheNodeLink Reciprocal;
    public List<MapTerrainDataCell> PathCells;
    public List<DistCell[]> TargetDistCells;

    public float Distance;
    public float Grade;
    public float MaxGrade;

    public static float PathBlockerGradeMultiplier;
    private static float cellDelta = (float)MapMetaDataExporter.cellSize;
    private static float cellDeltaDiag = (float)MapMetaDataExporter.cellSize * 1.414f;

    public CacheNodeLink(CacheNode from, CacheNode to, float distance, int angle, MapMetaData mapMetaData) {
      this.From = from;
      this.To = to;
      this.Reciprocal = to.NeighborLinks[(angle + 4) % 8];
      if (this.Reciprocal != null) {
        this.Reciprocal.Reciprocal = this;
      }

      this.PathCells = new List<MapTerrainDataCell>();
      this.TargetDistCells = new List<DistCell[]>();
      List<Point> indexLine = BresenhamLineUtil.BresenhamLine(from.CellIndex, to.CellIndex);
      for (int i = 0; i < indexLine.Count - 1; i++) {
        // Targets are indexLine[i + 1] and the cells in the adjacent cardinal directions
        DistCell[] targets = new DistCell[3];
        Point current = indexLine[i];
        int xDiff = indexLine[i + 1].X - current.X;
        int zDiff = indexLine[i + 1].Z - current.Z;

        if (xDiff >= 0) {
          targets[0] = new DistCell(cellDelta, mapMetaData.GetCellAt(current.X + 1, current.Z));
        } else {
          targets[0] = new DistCell(cellDelta, mapMetaData.GetCellAt(current.X - 1, current.Z));
        }

        if (zDiff >= 0) {
          targets[1] = new DistCell(cellDelta, mapMetaData.GetCellAt(current.X, current.Z + 1));
        } else {
          targets[1] = new DistCell(cellDelta, mapMetaData.GetCellAt(current.X, current.Z - 1));
        }

        if (xDiff == 0) {
          targets[2] = new DistCell(cellDelta, mapMetaData.GetCellAt(current.X - 1, current.Z));
        } else if (zDiff == 0) {
          targets[2] = new DistCell(cellDelta, mapMetaData.GetCellAt(current.X, current.Z - 1));
        } else {
          // this should be impossible with the modified bresenham they're using
          targets[2] = new DistCell(cellDeltaDiag, mapMetaData.GetCellAt(indexLine[i]));
        }

        this.PathCells.Add(mapMetaData.GetCellAt(current));
        this.TargetDistCells.Add(targets);
      }
      this.Distance = distance;
      this.UpdateGrade();
      this.UpdateMaxGrade();
    }

    public void UpdateGrade() {
      this.Grade = PathingUtil.GetGrade(this.From.GetHeight(), this.To.GetHeight(), this.Distance);
    }

    public void UpdateMaxGrade() {
      this.MaxGrade = Mathf.Abs(this.Grade); // average grade also matters for blocking
      for (int i = 0; i < this.PathCells.Count; i++) {
        float height1 = this.PathCells[i].cachedHeight;
        for (int j = 0; j < 3; j++) {
          float height2 = this.TargetDistCells[i][j].cell.cachedHeight;
          float delta = this.TargetDistCells[i][j].dist;
          this.MaxGrade = Mathf.Max(this.MaxGrade, Mathf.Abs(PathingUtil.GetGrade(height1, height2, delta)));
        }
      }
      this.MaxGrade = Mathf.Max(this.MaxGrade, this.To.LocalGrade);
    }

    public bool IsBlocked(PathingCapabilitiesDef capabilities) {
      float maxGrade = capabilities.MaxGrade * CacheNodeLink.PathBlockerGradeMultiplier;
      return this.To.Steepness > capabilities.MaxSteepness || this.MaxGrade > maxGrade || this.Reciprocal.MaxGrade > maxGrade || this.To.IsImpassibleTerrain;
    }

    public float GetTerrainModifiedCost(PathingCapabilitiesDef capabilities, float terrainCost) {
      float gradeModifier = (this.Grade > capabilities.MinGrade) ? 1f + this.Grade * capabilities.GradeMultiplier : 1f;
      return this.Distance * gradeModifier * terrainCost;
    }
  }
}
