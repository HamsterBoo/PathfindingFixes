using BattleTech;

using Harmony;

namespace PathfindingFixes.Patches {
  [HarmonyPatch(typeof(EncounterLayerParent), "FirstTimeInitialization")]
  public class FirstTimeInitializationPatch {
    public static void Prefix() {
      Main.Logger.Log($"[FirstTimeInitializationPatch Postfix] Building cache for map");
      PathCache.Instance.ResetCache(UnityGameInstance.BattleTechGame.Combat);
      Main.Logger.Log($"[FirstTimeInitializationPatch Postfix] Done building cache");
    }
  }
}