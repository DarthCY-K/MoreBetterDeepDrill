using Verse.AI;

namespace MoreBetterDeepDrill.Jobs
{
    public class MBDD_JobDriver_MultiOperateDeepDrill : MBDD_JobDriver_OperateDeepDrill
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 12, 0, null, errorOnFailed);
        }
    }
}