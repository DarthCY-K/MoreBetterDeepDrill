using HarmonyLib;
using MBDD_PRF_Support.TargetBench;
using MoreBetterDeepDrill.Comp;
using ProjectRimFactory.AutoMachineTool;
using ProjectRimFactory.SAL3;
using RimWorld;
using System.Linq;
using System.Reflection;
using Verse;

namespace MBDD_PRF_Support.Patch
{
    /// <summary>
    /// 关于Building_AutoMachineTool.GetTarget的补丁（PRF兼容）
    /// 让 PRF 的 AutoMachineTool 可以把 MBDD 钻机识别为工作台目标。
    /// </summary>
    [HarmonyPatch(typeof(Building_AutoMachineTool), nameof(Building_AutoMachineTool.GetTarget), [typeof(IntVec3), typeof(Rot4), typeof(Map), typeof(bool)])]
    public class Patch_Building_AutoMachineTool_GetTarget
    {
        /// <summary>缓存的 salTarget 字段引用，避免每次 GetTarget 都执行反射查找</summary>
        private static readonly FieldInfo SalTargetField =
            typeof(Building_AutoMachineTool).GetField("salTarget", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Prefix
        /// </summary>
        private static bool Prefix(Building_AutoMachineTool __instance, ref bool __result, IntVec3 pos, Rot4 rot, Map map, bool spawned = false)
        {
            Building building_deepDrill = (pos + rot.FacingCell).GetThingList(map)
                .OfType<Building>()
                .Where(t => t.InteractionCell == pos && t.TryGetComp<MBDD_CompDeepDrill>() != null)
                .FirstOrDefault();

            // 目标位置不是 MBDD 钻机：交还给 PRF 原版逻辑
            if (building_deepDrill == null)
                return true;

            // 旧目标已指向同一钻机：直接复用，避免反复重建和预约抖动
            if (SalTargetField.GetValue(__instance) is MBDD_SAL_TargetDeepDrill oldTarget)
            {
                if (oldTarget.TargetDrill == building_deepDrill)
                {
                    __result = true;
                    return false;
                }

                // 替换目标前释放旧的钻机目标，避免预约泄漏
                oldTarget.Free();
            }

            SAL_TargetBench salTarget = new MBDD_SAL_TargetDeepDrill(__instance, __instance.Position, __instance.Map, __instance.Rotation, building_deepDrill);
            SalTargetField.SetValue(__instance, salTarget);

            if (spawned)
                salTarget.Reserve();

            __result = true;
            return false;
        }
    }
}
