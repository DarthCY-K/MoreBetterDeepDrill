using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace MoreBetterDeepDrill.Jobs
{
    public class MBDD_JobDriver_OperateDeepDrill : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOnThingHavingDesignation(TargetIndex.A, DesignationDefOf.Uninstall);
            this.FailOn(() => !job.targetA.Thing.TryGetComp<Comp.MBDD_CompDeepDrill>().CanDrillNow);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil work = ToilMaker.MakeToil("MakeNewToils");
            work.tickAction = delegate
            {
                this.pawn.rotationTracker.FaceCell(GenAdj.OccupiedRect(this.TargetA.Thing).ClosestCellTo(this.pawn.Position));
                Pawn actor = work.actor;
                ((Building)actor.CurJob.targetA.Thing).GetComp<Comp.MBDD_CompDeepDrill>().DrillJoinWork(actor);

                //机械族没有技能，所以不能给他们加经验
                if (actor.skills != null)
                    actor.skills.Learn(SkillDefOf.Mining, 0.065f);
            };
            work.AddFinishAction(delegate
            {
                Pawn actor = work.actor;
                ((Building)actor.CurJob.targetA.Thing).GetComp<Comp.MBDD_CompDeepDrill>().DrillLeaveWork(actor);
            });
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.WithEffect(EffecterDefOf.Drill, TargetIndex.A);
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            work.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            work.activeSkill = () => SkillDefOf.Mining;
            yield return work;
        }
    }
}