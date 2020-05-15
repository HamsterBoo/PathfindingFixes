using System;
using System.Collections.Generic;
using BattleTech;

namespace PathfindingFixes {
  public static class PathNodeHeapQueueUtil {
    public static void Push(List<PathNode> heap, PathNode element) {
      heap.Add(element);
      bubbleUp(heap, heap.Count - 1);
    }

    public static PathNode PopMinimum(List<PathNode> heap) {
      PathNode result = heap[0];
      heap[0] = heap[heap.Count - 1];
      heap.RemoveAt(heap.Count - 1);
      bubbleDown(heap, 0);
      return result;
    }

    private static void bubbleUp(List<PathNode> heap, int index) {
      while (index > 0) {
        int parentIndex = index - 1 >> 1;
        PathNode pathNode = heap[parentIndex];
        if (pathNode.CostToThisNode.CompareTo(heap[index].CostToThisNode) < 0) {
          break;
        }
        PathNode value = heap[index];
        heap[index] = heap[parentIndex];
        heap[parentIndex] = value;
        index = parentIndex;
      }
    }

    private static void bubbleDown(List<PathNode> heap, int index) {
      for (; ; ) {
        int leftIndex = 2 * index + 1;
        int rightIndex = 2 * index + 2;
        if (leftIndex >= heap.Count) {
          break;
        }
        int nextIndex = leftIndex;
        PathNode pathNode;
        if (rightIndex < heap.Count) {
          pathNode = heap[rightIndex];
          if (pathNode.CostToThisNode.CompareTo(heap[leftIndex].CostToThisNode) < 0) {
            nextIndex = rightIndex;
          }
        }
        pathNode = heap[index];
        if (pathNode.CostToThisNode.CompareTo(heap[nextIndex].CostToThisNode) <= 0) {
          break;
        }
        PathNode value = heap[nextIndex];
        heap[nextIndex] = heap[index];
        heap[index] = value;
        index = nextIndex;
      }
    }
  }
}