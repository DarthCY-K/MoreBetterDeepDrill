using System.Collections.Generic;
using HarmonyLib;
using MoreBetterDeepDrill.Comp;
using MoreBetterDeepDrill.Settings;
using RimWorld;
using Verse;

namespace MoreBetterDeepDrill.Patch
{
    /// <summary>
    /// 原版深钻虫害继续处理概率、冷却、生成和信件；这里只过滤 MBDD 钻机。
    /// 原版钻机不进入本补丁的过滤分支，因此不受 MBDD 设置影响。
    /// </summary>
    [HarmonyPatch(typeof(DeepDrillInfestationIncidentUtility), nameof(DeepDrillInfestationIncidentUtility.GetUsableDeepDrills))]
    public static class Patch_DeepDrillInfestationIncidentUtility_GetUsableDeepDrills
    {
        private static void Postfix(List<Thing> outDrills)
        {
            bool enabled = MBDD_Mod.ModSetting?.EnableInsectoids ?? true;
            for (int i = outDrills.Count - 1; i >= 0; i--)
            {
                MBDD_CompDeepDrill drillComp = outDrills[i].TryGetComp<MBDD_CompDeepDrill>();
                if (drillComp != null && (!enabled || !drillComp.IsDrillingNow))
                    outDrills.RemoveAt(i);
            }
        }
    }
}
