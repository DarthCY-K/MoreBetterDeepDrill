using Verse.AI;

namespace MoreBetterDeepDrill.Jobs
{
    /// <summary>
    /// 大型钻机 JobDriver。允许多 pawn 围绕 3×3 机体站位工作（Touch 模式），
    /// 避免所有人挤向同一个交互格导致后续 pawn 永远够不到而空转。
    /// </summary>
    public class MBDD_JobDriver_MultiOperateDeepDrill : MBDD_JobDriver_OperateDeepDrill
    {
        protected override PathEndMode PathEndMode => PathEndMode.Touch;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 12, 0, null, errorOnFailed);
        }
    }
}