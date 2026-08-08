using MoreBetterDeepDrill.Utils;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MoreBetterDeepDrill.Jobs
{
    /// <summary>
    /// 深钻井工作提供者。为 pawn 分配钻井任务。
    /// ShouldSkip 使用 tick 级缓存，同一 tick 内多次调用只扫描一次建筑列表。
    /// </summary>
    public class MBDD_WorkGiver_DeepDrill : WorkGiver_Scanner
    {
        /// <summary>ShouldSkip 的 tick 级缓存：避免同一 tick 内对每个 pawn 重复扫描建筑列表</summary>
        private static int cachedShouldSkipTick = -1;
        private static int cachedShouldSkipMapId = -1;
        private static bool cachedShouldSkipResult;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForUndefined();

        // Touch is the broad scanner mode needed by the 3x3 drill. HasJobOnThing performs the
        // exact per-def reachability check before a target can be selected.
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return DrillsOnMap(pawn.Map);
        }

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        /// <summary>
        /// 快速跳过检查。同一 tick 内仅扫描一次建筑列表判断是否存在可用钻机。
        /// 机械体需 EnableMechdroids 开启后才可操作。
        /// </summary>
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (pawn.IsColonyMech && !StaticValues.ModSetting.EnableMechdroids)
                return true;

            int mapId = pawn.Map.uniqueID;
            int tick = Find.TickManager.TicksGame;
            if (cachedShouldSkipMapId == mapId && cachedShouldSkipTick == tick)
                return cachedShouldSkipResult;

            cachedShouldSkipMapId = mapId;
            cachedShouldSkipTick = tick;

            foreach (Thing thing in DrillsOnMap(pawn.Map))
            {
                if (thing is Building building && building.Faction == pawn.Faction)
                {
                    var comp = building.GetComp<Comp.MBDD_CompDeepDrill>();
                    if (comp != null && comp.CanDrillNow
                        && building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) == null)
                    {
                        cachedShouldSkipResult = false;
                        return false;
                    }
                }
            }

            cachedShouldSkipResult = true;
            return true;
        }

        private static IEnumerable<Thing> DrillsOnMap(Map map)
        {
            foreach (Thing thing in map.listerThings.ThingsOfDef(Defs.ThingDefOf.MBDD_RangedDeepDrill))
                yield return thing;
            foreach (Thing thing in map.listerThings.ThingsOfDef(Defs.ThingDefOf.MBDD_LargeDeepDrill))
                yield return thing;
            foreach (Thing thing in map.listerThings.ThingsOfDef(Defs.ThingDefOf.MBDD_ArchotechDeepDrill))
                yield return thing;
        }

        /// <summary>判断 pawn 是否可以在指定建筑上工作</summary>
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // 最廉价的检查放最前：本 WorkGiver 只关心三种钻机
            bool isLargeDrill = t.def == Defs.ThingDefOf.MBDD_LargeDeepDrill;
            if (!isLargeDrill
                && t.def != Defs.ThingDefOf.MBDD_RangedDeepDrill
                && t.def != Defs.ThingDefOf.MBDD_ArchotechDeepDrill)
                return false;

            if (t.Faction != pawn.Faction)
                return false;

            if (!(t is Building building))
                return false;

            if (building.IsForbidden(pawn))
                return false;

            var comp = building.GetComp<Comp.MBDD_CompDeepDrill>();
            if (comp == null || !comp.CanDrillNow)
                return false;

            if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) != null)
                return false;

            if (building.IsBurning())
                return false;

            PathEndMode requiredPathEndMode = isLargeDrill ? PathEndMode.Touch : PathEndMode.InteractionCell;
            if (!pawn.CanReach(building, requiredPathEndMode, MaxPathDanger(pawn)))
                return false;

            // 预约检查放最后（开销最大）
            if (isLargeDrill)
                return pawn.CanReserve(building, 12, 0, null, forced);
            return pawn.CanReserve(building, 1, -1, null, forced);
        }

        /// <summary>创建钻井 Job：大型钻机多 pawn，普通钻机单 pawn</summary>
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.def == Defs.ThingDefOf.MBDD_LargeDeepDrill)
                return JobMaker.MakeJob(Defs.JobDefOf.MBDD_MultiOperateDeepDrill, t, 1500, checkOverrideOnExpiry: false);
            else
                return JobMaker.MakeJob(Defs.JobDefOf.MBDD_SingleOperateDeepDrill, t, 1500, checkOverrideOnExpiry: false);
        }
    }
}
