using HarmonyLib;

namespace RepoValuableParry.Patches
{
    /// <summary>
    /// Intentionally empty of gameplay changes. Vanilla valuable physics and
    /// ordinary swings are never modified. This patch exists so a future
    /// lock/visual hook has a dedicated home without touching PhysGrabObject.
    /// </summary>
    [HarmonyPatch]
    static class ValuablePatch
    {
        [HarmonyPatch(typeof(ValuableObject), "Start")]
        [HarmonyPostfix]
        static void StartPostfix(ValuableObject __instance)
        {
            // No gameplay mutation. Debug overlay can still discover valuables.
        }
    }
}
