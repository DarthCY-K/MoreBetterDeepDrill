using HarmonyLib;
using MoreBetterDeepDrill.Utils;
using Verse;

namespace MoreBetterDeepDrill.Patch
{
    /// <summary>
    /// DeepResourcesOnGUI 补丁。选中 Ranged 钻机时显示鼠标所在格子的深钻井资源。
    /// </summary>
    [HarmonyPatch(typeof(DeepResourceGrid), nameof(DeepResourceGrid.DeepResourcesOnGUI))]
    public static class Patch_DeepResourceGrid_DeepResourcesOnGUI
    {
        private static void Postfix(DeepResourceGrid __instance)
        {
            Thing singleSelectedThing = Find.Selector.SingleSelectedThing;
            Map map = singleSelectedThing?.MapHeld;
            if (map != null && singleSelectedThing.TryGetComp<Comp.MBDD_CompRangedDeepDrill>() != null
                && map.deepResourceGrid.AnyActiveDeepScannersOnMap())
                DeepDrillUtil.RenderMouseAttachments(map);
        }
    }

    /// <summary>
    /// DrawPlacingMouseAttachments 补丁。放置 Ranged 钻机时显示鼠标所在格子的深钻井资源。
    /// </summary>
    [HarmonyPatch(typeof(DeepResourceGrid), nameof(DeepResourceGrid.DrawPlacingMouseAttachments))]
    public static class Patch_DeepResourceGrid_DrawPlacingMouseAttachments
    {
        private static void Postfix(DeepResourceGrid __instance, BuildableDef placingDef)
        {
            var map = Find.CurrentMap;
            if (map != null && placingDef is ThingDef thingDef && thingDef.CompDefFor<Comp.MBDD_CompRangedDeepDrill>() != null
                && map.deepResourceGrid.AnyActiveDeepScannersOnMap())
                DeepDrillUtil.RenderMouseAttachments(map);
        }
    }

    /// <summary>
    /// SetAt 补丁。原方法完成后根据最终网格值同步全局资源索引。
    /// </summary>
    [HarmonyPatch(typeof(DeepResourceGrid), nameof(DeepResourceGrid.SetAt))]
    public static class Patch_DeepResourceGrid_SetAt
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(Map ___map, IntVec3 c)
        {
            MapResourceCache.ForMap(___map).NotifySetAt(___map, c);
        }
    }

    [HarmonyPatch(typeof(DeepResourceGrid), nameof(DeepResourceGrid.ExposeData))]
    public static class Patch_DeepResourceGrid_ExposeData
    {
        private static void Postfix(Map ___map)
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
                MapResourceCache.ForMap(___map).Invalidate();
        }
    }
}
