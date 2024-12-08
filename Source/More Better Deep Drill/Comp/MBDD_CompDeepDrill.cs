using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MoreBetterDeepDrill.Comp
{
    public class MBDD_CompDeepDrill : ThingComp
    {
        protected CompPowerTrader powerComp;

        protected float portionProgress = 0;

        public float PortionYieldPct
        {
            get => portionYieldPct;
            protected set
            {
                if (value > 0)
                    portionYieldPct = value;
                else
                    portionYieldPct = 0;
            }
        }

        protected float portionYieldPct = 0;

        public float DrillPower
        {
            get => drillPower;
            protected set
            {
                if (value > 0)
                    drillPower = value;
                else
                    drillPower = 0;
            }
        }

        protected float drillPower = 0;

        protected int lastUsedTick = -99999;

        protected const float WorkPerPortionBase = 10000f;

        public float ProgressToNextPortionPercent => portionProgress / 10000f;

        protected List<Pawn> drillers = new List<Pawn>();

        public bool canDrillNow;

        public override void CompTick()
        {
            if (canDrillNow)
            {
                base.CompTick();

                if (drillers.Count > 0)
                    DrillWork();
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();

            UpdateCanDrillState();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            powerComp = parent.TryGetComp<CompPowerTrader>();
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref portionProgress, "portionProgress", 0f);
            Scribe_Values.Look(ref portionYieldPct, "portionYieldPct", 0f);
            Scribe_Values.Look(ref lastUsedTick, "lastUsedTick", 0);
        }

        /// <summary>
        /// 加入工作
        /// </summary>
        /// <param name="driller"></param>
        public virtual void DrillJoinWork(Pawn driller)
        {
            foreach (Pawn p in drillers)
            {
                if (driller == p)
                    return;
            }

            float statValue = driller.GetStatValue(StatDefOf.DeepDrillingSpeed);
            DrillPower += statValue;
            drillers.Add(driller);
        }

        /// <summary>
        /// 离开工作
        /// </summary>
        /// <param name="driller"></param>
        public virtual void DrillLeaveWork(Pawn driller)
        {
            float statValue = driller.GetStatValue(StatDefOf.DeepDrillingSpeed);
            DrillPower -= statValue;
            drillers.Remove(driller);
        }

        public virtual void DrillWork()
        {
            portionProgress += DrillPower;
            foreach (var p in drillers)
            {
                float statValue = p.GetStatValue(StatDefOf.DeepDrillingSpeed);
                PortionYieldPct += drillers.Count * statValue * p.GetStatValue(StatDefOf.MiningYield) / 10000f;
            }
            lastUsedTick = Find.TickManager.TicksGame;

            if (portionProgress > 10000f)
            {
                TryProducePortion(PortionYieldPct);
                portionProgress = 0f;
                PortionYieldPct = 0f;
            }
        }

        public override void PostDeSpawn(Map map)
        {
            portionProgress = 0f;
            PortionYieldPct = 0f;
            lastUsedTick = -99999;
        }

        protected virtual void TryProducePortion(float yieldPct, Pawn driller = null)
        { }

        /// <summary>
        /// 更新可挖掘状态
        /// </summary>
        protected virtual void UpdateCanDrillState(){ }

        public virtual bool UsedLastTick()
        {
            return lastUsedTick >= Find.TickManager.TicksGame - 1;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }

            if (DebugSettings.ShowDevGizmos)
            {
                Command_Action command_Action = new Command_Action();
                command_Action.defaultLabel = "DEV: Produce portion (100% yield)";
                command_Action.action = delegate
                {
                    TryProducePortion(1f);
                };
                yield return command_Action;
            }
        }
    }
}