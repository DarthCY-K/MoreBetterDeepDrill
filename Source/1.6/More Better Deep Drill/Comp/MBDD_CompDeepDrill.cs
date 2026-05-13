using MoreBetterDeepDrill.Utils;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MoreBetterDeepDrill.Comp
{
    public class MBDD_CompDeepDrill : ThingComp
    {
        protected CompPowerTrader powerComp;

        protected float portionProgress = 0;

        //TODO 测试用最大挖掘力量，后续需要移动到setting里
        protected float maxDrillPower = 3f;

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
            //挖掘力不能超过最大限制
            get => drillPower > maxDrillPower ? maxDrillPower : drillPower;
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

        public float ProgressToNextPortionPercent => portionProgress / WorkPerPortionBase;

        public List<Pawn> Drillers
        {
            get => drillers;
            set
            {
                if (value != drillers)
                {
                    drillers = value;
                }
            }
        }

        protected List<Pawn> drillers = new List<Pawn>();

        protected Dictionary<Pawn, float> cachedPawnDeepdrillSpeedDict = new Dictionary<Pawn, float>();
        protected Dictionary<Pawn, float> cachedPawnMiningYieldDict = new Dictionary<Pawn, float>();

        public bool CanDrillNow;

        public bool IsDrillingNow => drillers.Count != 0;

        public override void CompTick()
        {
            //每300tick（5s）更新一次状态
            if (Current.Game.tickManager.TicksGame % 300 == 0)
            {
                UpdateCanDrillState();
            }

            //每120tick（2s）更新一次状态
            if (Current.Game.tickManager.TicksGame % 120 == 0)
            {
                UpdateCachedPawnDrillSpeed();
            }

            if (CanDrillNow)
            {
                base.CompTick();

                if (drillers.Count > 0)
                    DrillWork();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
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
            if (drillers.Contains(driller))
                return;

            float statValue = driller.GetStatValue(StatDefOf.DeepDrillingSpeed);
            drillPower += statValue;
            drillers.Add(driller);
            cachedPawnDeepdrillSpeedDict[driller] = statValue;
            cachedPawnMiningYieldDict[driller] = driller.GetStatValue(StatDefOf.MiningYield);
        }

        /// <summary>
        /// 离开工作
        /// </summary>
        /// <param name="driller"></param>
        public virtual void DrillLeaveWork(Pawn driller)
        {
            if (!drillers.Contains(driller))
                return;

            float statValue = driller.GetStatValue(StatDefOf.DeepDrillingSpeed);
            drillPower -= statValue;
            if (drillPower < 0f)
                drillPower = 0f;

            drillers.Remove(driller);

            //LogUtil.LogNormal($"MBDD: Worker named [{driller.Name.ToStringSafe()}] leaved the drillwork.");
        }

        public virtual void DrillWork()
        {
            portionProgress += DrillPower;

            foreach (var pawn in Drillers)
            {
                if (cachedPawnDeepdrillSpeedDict.TryGetValue(pawn, out float statValueDeepdrillSpeed)
                    && cachedPawnMiningYieldDict.TryGetValue(pawn, out float statValueMingYield))
                {
                    PortionYieldPct += statValueDeepdrillSpeed * statValueMingYield * 0.0001f;
                }
            }

            lastUsedTick = Find.TickManager.TicksGame;

            if (portionProgress > WorkPerPortionBase)
            {
                TryProducePortion(PortionYieldPct);
                portionProgress = 0f;
                PortionYieldPct = 0f;
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            portionProgress = 0f;
            PortionYieldPct = 0f;
            drillPower = 0f;
            lastUsedTick = -99999;
            CanDrillNow = false;
            drillers.Clear();
            cachedPawnDeepdrillSpeedDict.Clear();
            cachedPawnMiningYieldDict.Clear();
        }

        protected virtual void TryProducePortion(float yieldPct, Pawn driller = null)
        { }

        /// <summary>
        /// 更新可挖掘状态
        /// </summary>
        protected virtual void UpdateCanDrillState()
        { }

        protected virtual void UpdateCachedPawnDrillSpeed()
        {
            foreach (var p in Drillers)
            {
                cachedPawnDeepdrillSpeedDict[p] = p.GetStatValue(StatDefOf.DeepDrillingSpeed);
                cachedPawnMiningYieldDict[p] = p.GetStatValue(StatDefOf.MiningYield);
            }
        }

        public virtual bool UsedLastTick()
        {
            return lastUsedTick >= Find.TickManager.TicksGame - 1;
        }

        /// <summary>
        /// PRF Mod专用生产方法
        /// </summary>
        public virtual void DrillWorkForPRF(float progress, float yieldPct, int lastUsedTick)
        {
            this.portionProgress += progress;
            this.PortionYieldPct += yieldPct;
            this.lastUsedTick = lastUsedTick;

            if (portionProgress > WorkPerPortionBase)
            {
                TryProducePortion(PortionYieldPct);
                portionProgress = 0f;
                PortionYieldPct = 0f;
            }
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
