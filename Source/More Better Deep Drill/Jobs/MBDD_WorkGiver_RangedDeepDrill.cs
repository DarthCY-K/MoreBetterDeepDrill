using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MoreBetterDeepDrill.Jobs
{
    public class MBDD_WorkGiver_RangedDeepDrill : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(Defs.ThingDefOf.MBDD_RangedDeepDrill);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return Find.StudyManager.GetStudiableThingsAndPlatforms(pawn.Map);
        }

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction)
                return false;

            if (!(t is Building building))
                return false;

            if (building.IsForbidden(pawn))
                return false;

            if (!pawn.CanReserve(building, 1, -1, null, forced))
                return false;

            if (!building.TryGetComp<Comp.MBDD_CompDeepDrill>().CanDrillNow)
                return false;

            if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) != null)
                return false;

            if (building.IsBurning())
                return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(Defs.JobDefOf.MBDD_SingleOperateDeepDrill, t, 1500, checkOverrideOnExpiry: false);
        }
    }
}