using MoreBetterDeepDrill.Utils;
using RimWorld;
using Verse;

namespace MoreBetterDeepDrill.PlaceWorkers
{
    public class MBDD_PlaceWorker_DeepDrill : PlaceWorker_ShowDeepResources
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            return true;
        }
    }
}
