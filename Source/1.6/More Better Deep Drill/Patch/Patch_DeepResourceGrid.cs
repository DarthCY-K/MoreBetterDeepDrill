using HarmonyLib;
using MoreBetterDeepDrill.Utils;
using Verse;

namespace MoreBetterDeepDrill.Patch
{
    /// <summary>
    /// 关于DeepResourceGrid.DeepResourcesOnGUI的补丁（为了提供选中显示矿脉的功能）
    /// </summary>
    [HarmonyPatch(typeof(DeepResourceGrid), nameof(DeepResourceGrid.DeepResourcesOnGUI))]
    public class Patch_DeepResourceGrid_DeepResourcesOnGUI
    {
        /// <summary>
        /// Postfix
        /// </summary>
        private static void Postfix(DeepResourceGrid __instance)
        {
            Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
            if (singleSelectedThing != null && singleSelectedThing.TryGetComp<Comp.MBDD_CompRangedDeepDrill>() != null)
                DeepDrillUtil.RenderMouseAttachments(singleSelectedThing.MapHeld);
        }
    }

    /// <summary>
    /// 关于DeepResourceGrid.DrawPlacingMouseAttachments的补丁（为了提供选中显示矿脉的功能）
    /// </summary>
    [HarmonyPatch(typeof(DeepResourceGrid), nameof(DeepResourceGrid.DrawPlacingMouseAttachments))]
    public class Patch_DeepResourceGrid_DrawPlacingMouseAttachments
    {
        /// <summary>
        /// Postfix
        /// </summary>
        private static void Postfix(DeepResourceGrid __instance, BuildableDef placingDef)
        {
            var map = Find.CurrentMap;
            if (placingDef is ThingDef thingDef && thingDef.CompDefFor<Comp.MBDD_CompRangedDeepDrill>() != null && DeepDrillUtil.AnyActiveDeepScannersOnMap(map))
                DeepDrillUtil.RenderMouseAttachments(map);
        }
    }
}