using UnityEngine;
using BattleTech;

namespace PathfindingFixes {
  public class CacheNode {
    public Point CellIndex;
    public MapTerrainDataCell[] cells;
    public float LocalGrade;
    public float Steepness;
    public bool IsImpassibleTerrain;
    //public bool IsValidHex;
    public CacheNodeLink[] NeighborLinks;
    //public AbstractActor OccupyingActor { get; set; }

    private static int[] xDiff = { 0, 1, 0, -1, 0, 1, 1, -1, -1 };
    private static int[] zDiff = { 0, 0, 1, 0, -1, 1, -1, 1, -1 };
    private static float cellDelta = (float)MapMetaDataExporter.cellSize;
    private static float cellDeltaDiag = (float)MapMetaDataExporter.cellSize * 1.414f;

    public CacheNode(Vector3 worldPosition, MapMetaData mapMetaData) {
      this.CellIndex = mapMetaData.GetIndex(worldPosition);
      this.cells = new MapTerrainDataCell[9];
      for (int i = 0; i < 9; i++) {
        this.cells[i] = mapMetaData.GetCellAt(this.CellIndex.X + xDiff[i], this.CellIndex.Z + zDiff[i]);
      }
      this.UpdateLocalGrade();
      this.UpdateSteepness();
      this.UpdateIsPassableTerrain();

      this.NeighborLinks = new CacheNodeLink[8];
    }

    public void UpdateLocalGrade() {
      float centerHeight = this.cells[0].cachedHeight;
      this.LocalGrade = 0f;
      for (int i = 1; i <= 4; i++) {
        float height = this.cells[i].cachedHeight;
        this.LocalGrade = Mathf.Max(this.LocalGrade, Mathf.Abs(PathingUtil.GetGrade(centerHeight, height, cellDelta)));
      }
      for (int i = 5; i <= 8; i++) {
        float height = this.cells[i].cachedHeight;
        this.LocalGrade = Mathf.Max(this.LocalGrade, Mathf.Abs(PathingUtil.GetGrade(centerHeight, height, cellDeltaDiag)));
      }
    }

    public void UpdateSteepness() {
      this.Steepness = this.cells[0].cachedSteepness;
    }

    public void UpdateIsPassableTerrain() {
      switch (MapMetaData.GetPriorityTerrainMaskFlags(this.cells[0])) {
        case (TerrainMaskFlags.DeepWater):
        case (TerrainMaskFlags.Impassable):
        case (TerrainMaskFlags.MapBoundary):
          this.IsImpassibleTerrain = true;
          break;
        default:
          this.IsImpassibleTerrain = false;
          break;
      }
    }

    public float GetHeight() {
      return this.cells[0].cachedHeight;
    }
  }
}
