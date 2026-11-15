using Verse.AI;

namespace MoreBetterDeepDrill.Jobs
{
    public class MBDD_JobDriver_SingleOperateDeepDrill : MBDD_JobDriver_OperateDeepDrill
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }
    }
}