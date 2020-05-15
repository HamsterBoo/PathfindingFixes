using BattleTech;
using HBS.Math;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

using Harmony;

namespace PathfindingFixes.Patches {
  [HarmonyPatch(typeof(PathNodeGrid), "BuildPathNetwork")]
  public class BuildPathNetworkPatch {
    private static MethodInfo GetBehaviorVariableValueInfo;
    private static MethodInfo BuildAllNeighborsOfNodeInfo;
    private static MethodInfo GetBackwardMultiplierInfo;

    public static void Prepare() {
      GetBehaviorVariableValueInfo = AccessTools.Method(typeof(BehaviorTree), "GetBehaviorVariableValue");
      BuildAllNeighborsOfNodeInfo = AccessTools.Method(typeof(PathNodeGrid), "BuildAllNeighborsOfNode");
      GetBackwardMultiplierInfo = AccessTools.Method(typeof(PathNodeGrid), "GetBackwardMultiplier");
    }

    public static bool Prefix(PathNodeGrid __instance, CombatGameState ___combat, AbstractActor ___owningActor, List<PathNode> ___open, PathNode[] ___neighbors, PathNode[,] ___pathNodes, MoveType ___moveType, int numThisFrame, ref int __result) {
      //Main.Logger.Log($"[BuildPathNetworkPatch Prefix] Starting Path Network");

      __result = BuildPathNetwork(__instance, ___combat.AllActors, ___owningActor, ___open, ___neighbors, ___pathNodes, ___moveType, numThisFrame);

      return false;
    }

    private static int BuildPathNetwork(PathNodeGrid grid, List<AbstractActor> bpnActors, AbstractActor owningActor, List<PathNode> open, PathNode[] neighbors, PathNode[,] pathNodes, MoveType moveType, int numThisFrame) {
      bpnActors.Remove(owningActor);
      float num2 = grid.MaxDistance * 2f;
      bool flag = false;
      if (owningActor.BehaviorTree != null) {
        string stringVal = ((BehaviorVariableValue)GetBehaviorVariableValueInfo.Invoke(owningActor.BehaviorTree, new object[] { BehaviorVariableName.String_RouteGUID })).StringVal;
        if (stringVal != null && stringVal.Length > 0) {
          flag = true;
        }
      }
      for (int i = bpnActors.Count - 1; i >= 0; i--) {
        if (bpnActors[i].IsDead || Vector3.Distance(bpnActors[i].CurrentIntendedPosition, owningActor.CurrentPosition) > num2) {
          bpnActors.RemoveAt(i);
        }
      }
      if (open != null) {
        return ProcessOpen(grid, bpnActors, owningActor, open, neighbors, pathNodes, moveType, flag, numThisFrame);
      }
      return 0;
    }

    private static int ProcessOpen(PathNodeGrid grid, List<AbstractActor> bpnActors, AbstractActor owningActor, List<PathNode> open, PathNode[] neighbors, PathNode[,] pathNodes, MoveType moveType, bool flag, int numThisFrame) {
      Point gridOffset = PathCache.Instance.NodePosFromWorldPos(grid.StartNode.Position) - grid.StartNode.Index;
      for (int i = 0; i < numThisFrame; i++) {
        if (open.Count == 0) {
          //Main.Logger.Log($"[BuildPathNetworkPatch Prefix] Build finished {i} nodes");
          return i;
        }
        PathNode pathNode = PathNodeHeapQueueUtil.PopMinimum(open);
        pathNode.IsOpen = false;
        BuildAllNeighborsOfNodeInfo.Invoke(grid, new object[] { pathNode, bpnActors }); // will populate the grid with bad nodes, but that's fine
        for (int j = 0; j < neighbors.Length; j++) {
          PathNode neighbor = neighbors[j];
          if (neighbor != null && (!neighbor.HasCollision || (flag && (neighbor.OccupyingActor == null || owningActor.IsFriendly(neighbor.OccupyingActor))))) {
            CacheNodeLink link = PathCache.Instance.GetCacheNodeLink(pathNode.Index + gridOffset, j);
            if (link != null && !link.IsBlocked(grid.Capabilities)) {
              //float cost = pathNode.CostToThisNode + grid.GetTerrainModifiedCost(pathNode, neighbor, grid.MaxDistance - pathNode.CostToThisNode) * (float)GetBackwardMultiplierInfo.Invoke(grid, new object[] { });
              float terrainCost = link.GetTerrainModifiedCost(grid.Capabilities, PathNodeGrid.GetTerrainCost(link.To.cells[0], owningActor, moveType));
              float cost = pathNode.CostToThisNode + terrainCost * (float)GetBackwardMultiplierInfo.Invoke(grid, new object[] { });
              if (cost < grid.MaxDistance && (neighbor.CostToThisNode <= -1f || cost < neighbor.CostToThisNode)) {
                neighbor.Parent = pathNode;
                neighbor.CostToThisNode = cost;
                neighbor.DepthInPath = pathNode.DepthInPath + 1;
                neighbor.Angle = j;
                neighbor.IsOpen = true;
                neighbor.IsClosed = false;
                PathNodeHeapQueueUtil.Push(open, neighbor);
              }
            }
          }
        }
      }
      //Main.Logger.Log($"[BuildPathNetworkPatch Prefix] Frame finished {numThisFrame} nodes");
      return numThisFrame;
    }
  }
}