using HarmonyLib;
using MBDD_PRF_Support.TargetBench;
using MoreBetterDeepDrill.Comp;
using ProjectRimFactory.AutoMachineTool;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace MBDD_PRF_Support.Patch
{
    /// <summary>
    /// 关于Building_AutoMachineTool.GetTarget的补丁（PRF兼容）
    /// </summary>
    [HarmonyPatch(typeof(Building_AutoMachineTool), nameof(Building_AutoMachineTool.GetTarget),[typeof(IntVec3), typeof(Rot4), typeof(Map), typeof(bool)])]
    public class Patch_Building_AutoMachineTool_GetTarget
    {
        /// <summary>
        /// Prefix
        /// </summary>
        private static bool Prefix(Building_AutoMachineTool __instance, ref bool __result, IntVec3 pos, Rot4 rot, Map map, bool spawned = false)
        {
            IEnumerable<Thing> source = from t in (pos + rot.FacingCell).GetThingList(map)
                                        where t.def.category == ThingCategory.Building
                                        where t.InteractionCell == pos
                                        select t;
            Building building_deepDrill = (Building)source.Where((Thing t) => t is Building && t.TryGetComp<MBDD_CompDeepDrill>() != null).FirstOrDefault();
            if (building_deepDrill != null)
            {
                var fieldInfo0 = typeof(Building_AutoMachineTool).GetField("salTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                SAL_TargetBench salTarget_Get = (SAL_TargetBench)fieldInfo0.GetValue(__instance);

                if (spawned && (salTarget_Get is MBDD_SAL_TargetDeepDrill && building_deepDrill == null))
                {
                    salTarget_Get.Free();
                    fieldInfo0.SetValue(__instance, salTarget_Get);
                }

                if (building_deepDrill != null)
                    salTarget_Get = new MBDD_SAL_TargetDeepDrill(__instance, __instance.Position, __instance.Map, __instance.Rotation, building_deepDrill);
                else
                    salTarget_Get = null;
                fieldInfo0.SetValue(__instance, salTarget_Get);

                if (spawned && salTarget_Get != null)
                {
                    salTarget_Get.Reserve();
                    fieldInfo0.SetValue(__instance, salTarget_Get);
                }

                __result = salTarget_Get != null;
                return false;
            }
            else
                return true;
        }
    }
}
