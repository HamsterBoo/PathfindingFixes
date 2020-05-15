using BattleTech;

using Harmony;

namespace PathfindingFixes.Patches {
  [HarmonyPatch(typeof(EncounterLayerParent), "InitializeFromSave")]
  public class InitializeFromSavePatch {
    public static void Prefix() {
      Main.Logger.Log($"[InitializeFromSavePatch Postfix] Building cache for map");
      PathCache.Instance.ResetCache(UnityGameInstance.BattleTechGame.Combat);
      Main.Logger.Log($"[InitializeFromSavePatch Postfix] Done building cache");
    }
  }
}