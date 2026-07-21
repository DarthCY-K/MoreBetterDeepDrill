using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MoreBetterDeepDrill.Jobs
{
    /// <summary>
    /// 深钻井 JobDriver 基类。管理 pawn 在钻机上的工作循环。
    /// initAction：进入工作 toil 时加入钻井；tickAction：每 tick 积累技能经验；
    /// AddFinishAction：pawn 离开时调 DrillLeaveWork 清理状态。
    /// drillComp 缓存为字段避免每 tick GetComp 调用。
    /// </summary>
    public abstract class MBDD_JobDriver_OperateDeepDrill : JobDriver
    {
        private Comp.MBDD_CompDeepDrill drillComp;

        /// <summary>寻路终点模式。单人钻机使用交互格；多人钻机围绕机体站位</summary>
        protected virtual PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public abstract override bool TryMakePreToilReservations(bool errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Building drill = (Building)job.targetA.Thing;
            drillComp = drill.GetComp<Comp.MBDD_CompDeepDrill>();

            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOnThingHavingDesignation(TargetIndex.A, DesignationDefOf.Uninstall);
            this.FailOn(() => !drillComp.CanDrillNow);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode);
            Toil work = ToilMaker.MakeToil("MakeNewToils");
            work.initAction = delegate
            {
                drillComp.DrillJoinWork(work.actor);
            };
            work.tickAction = delegate
            {
                this.pawn.rotationTracker.FaceCell(GenAdj.OccupiedRect(this.TargetA.Thing).ClosestCellTo(this.pawn.Position));
                Pawn actor = work.actor;

                if (actor.skills != null)
                    actor.skills.Learn(SkillDefOf.Mining, 0.065f);
            };
            work.AddFinishAction(delegate
            {
                Pawn actor = work.actor;
                drillComp.DrillLeaveWork(actor);
            });
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.WithEffect(EffecterDefOf.Drill, TargetIndex.A);
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode);
            work.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            work.activeSkill = () => SkillDefOf.Mining;
            yield return work;
        }
    }
}
