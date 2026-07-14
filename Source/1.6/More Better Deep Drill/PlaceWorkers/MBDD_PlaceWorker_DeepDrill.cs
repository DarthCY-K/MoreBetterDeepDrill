using RimWorld;
using Verse;

namespace MoreBetterDeepDrill.PlaceWorkers
{
    /// <summary>
    /// 深钻井放置检测器。继承 ShowDeepResources 以在放置时显示资源覆盖层，
    /// 始终允许放置。
    /// </summary>
    public class MBDD_PlaceWorker_DeepDrill : PlaceWorker_ShowDeepResources
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            return true;
        }
    }
}
