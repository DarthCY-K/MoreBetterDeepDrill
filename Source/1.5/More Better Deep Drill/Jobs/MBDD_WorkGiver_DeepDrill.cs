using MoreBetterDeepDrill.Utils;
using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MoreBetterDeepDrill.Jobs
{
    public class MBDD_WorkGiver_DeepDrill : WorkGiver_Scanner
    {
        private static int cachedShouldSkipTick = -1;
        private static int cachedShouldSkipMapId = -1;
        private static bool cachedShouldSkipResult;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

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

            List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                Building building = allBuildingsColonist[i];
                if (building.def == Defs.ThingDefOf.MBDD_RangedDeepDrill 
                    || building.def == Defs.ThingDefOf.MBDD_LargeDeepDrill
                    || building.def == Defs.ThingDefOf.MBDD_ArchotechDeepDrill)
                {
                    CompPowerTrader comp = building.GetComp<CompPowerTrader>();
                    if ((comp == null || comp.PowerOn) && building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) == null)
                    {
                        cachedShouldSkipResult = false;
                        return false;
                    }
                }
            }

            cachedShouldSkipResult = true;
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction)
                return false;

            if (!(t is Building building))
                return false;

            if (building.IsForbidden(pawn))
                return false;

            //根据人数分开处理
            if (t.def == Defs.ThingDefOf.MBDD_LargeDeepDrill)
            {
                if (!pawn.CanReserve(building, 12, 0, null, forced))
                    return false;
            }
            else
            {
                if (!pawn.CanReserve(building, 1, -1, null, forced))
                    return false;
            }

            var comp = building.GetComp<Comp.MBDD_CompDeepDrill>();
            if (comp == null || !comp.CanDrillNow)
                return false;

            if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) != null)
                return false;

            if (building.IsBurning())
                return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.def == Defs.ThingDefOf.MBDD_LargeDeepDrill)
                return JobMaker.MakeJob(Defs.JobDefOf.MBDD_MultiOperateDeepDrill, t, 1500, checkOverrideOnExpiry: false);
            else
                return JobMaker.MakeJob(Defs.JobDefOf.MBDD_SingleOperateDeepDrill, t, 1500, checkOverrideOnExpiry: false);
        }
    }
}