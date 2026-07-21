using MoreBetterDeepDrill.Utils;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace MoreBetterDeepDrill.Comp
{
    /// <summary>
    /// 深钻井核心 Comp。管理多 pawn 协同钻井的逻辑：
    /// 挖掘进度累积、产量计算、队伍管理、状态检测。
    /// CompTick 每 tick 驱动进度推进，子类重写 TryProducePortion 实现产出。
    /// </summary>
    public class MBDD_CompDeepDrill : ThingComp
    {
        /// <summary>电力 Comp，用于判断钻机是否通电</summary>
        protected CompPowerTrader powerComp;

        /// <summary>当前产出周期进度 (tick 累积值)</summary>
        protected float portionProgress = 0;

        /// <summary>最大挖掘力量上限，从 Mod 设置读取，默认 3</summary>
        protected float maxDrillPower => StaticValues.ModSetting?.MaxDrillPower ?? 3f;

        /// <summary>当前产出周期的累计产出倍率 (受所有 pawn 采矿速度×产出率影响)</summary>
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

        /// <summary>当前总挖掘力 (所有在岗 pawn 深钻速度之和，受 maxDrillPower 上限约束)</summary>
        public float DrillPower
        {
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

        /// <summary>上次被使用的 tick。用于判断钻机是否活跃</summary>
        protected int lastUsedTick = -99999;

        /// <summary>状态检测计数器（每 300 tick 触发 UpdateCanDrillState）</summary>
        private int stateCheckCounter;
        /// <summary>速度缓存计数器（每 120 tick 触发 UpdateCachedPawnDrillSpeed）</summary>
        private int speedCheckCounter;

        /// <summary>每次产出所需的基础工作量 (tick)</summary>
        protected const float WorkPerPortionBase = 10000f;

        /// <summary>当前产出进度百分比 (0~1)</summary>
        public float ProgressToNextPortionPercent => portionProgress / WorkPerPortionBase;

        /// <summary>当前在钻井机上工作的 pawn 列表</summary>
        public List<Pawn> Drillers => drillers;

        protected List<Pawn> drillers = new List<Pawn>();

        /// <summary>pawn 深度钻探速度缓存。用于计算产出倍率，每 120 tick 刷新</summary>
        protected Dictionary<Pawn, float> cachedPawnDeepdrillSpeedDict = new Dictionary<Pawn, float>();
        /// <summary>pawn 采矿产出率缓存。用于计算产出倍率，每 120 tick 刷新</summary>
        protected Dictionary<Pawn, float> cachedPawnMiningYieldDict = new Dictionary<Pawn, float>();

        /// <summary>钻机当前是否可以工作（由 UpdateCanDrillState 更新）</summary>
        public bool CanDrillNow;

        /// <summary>是否有 pawn 正在钻机上工作</summary>
        public bool IsDrillingNow => drillers.Count != 0;

        /// <summary>
        /// 每 tick 执行。计数器以 thingIDNumber 为初始偏移，真正错开多个钻机的检测时刻：
        /// - 每 300 tick：检查钻机是否可工作
        /// - 每 120 tick：刷新 pawn 速度缓存
        /// - 若可工作且有工人在岗，推进挖掘进度
        /// </summary>
        public override void CompTick()
        {
            base.CompTick();

            if (++stateCheckCounter >= 300)
            {
                stateCheckCounter = 0;
                UpdateCanDrillState();
            }

            if (++speedCheckCounter >= 120)
            {
                speedCheckCounter = 0;
                UpdateCachedPawnDrillSpeed();
            }

            if (CanDrillNow && drillers.Count > 0)
                DrillWork();
        }

        /// <summary>生成后初始化：缓存电力 Comp 引用，并用 thingIDNumber 错开检测计数器初始相位</summary>
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.TryGetComp<CompPowerTrader>();
            stateCheckCounter = parent.thingIDNumber % 300;
            speedCheckCounter = parent.thingIDNumber % 120;
            UpdateCanDrillState();
        }

        /// <summary>存档</summary>
        public override void PostExposeData()
        {
            Scribe_Values.Look(ref portionProgress, "portionProgress", 0f);
            Scribe_Values.Look(ref portionYieldPct, "portionYieldPct", 0f);
            Scribe_Values.Look(ref lastUsedTick, "lastUsedTick", 0);
        }

        /// <summary>
        /// pawn 加入钻井工作。更新总挖掘力并立即填充缓存字典，
        /// 避免等待 periodic 刷新导致首个 tick 数据缺失。
        /// </summary>
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
        /// pawn 离开钻井工作。更新总挖掘力并清理缓存字典条目，
        /// 防止离开后字典内残留数据造成内存泄漏。
        /// </summary>
        public virtual void DrillLeaveWork(Pawn driller)
        {
            if (!drillers.Contains(driller))
                return;

            if (cachedPawnDeepdrillSpeedDict.TryGetValue(driller, out float cachedSpeed))
                drillPower -= cachedSpeed;
            if (drillPower < 0f)
                drillPower = 0f;

            drillers.Remove(driller);
            cachedPawnDeepdrillSpeedDict.Remove(driller);
            cachedPawnMiningYieldDict.Remove(driller);
        }

        /// <summary>
        /// 每 tick 推进挖掘进度。累计 DrillPower 到 portionProgress，
        /// 同时根据各 pawn 的深钻速度×采矿产出率累计产量倍率。
        /// 当总钻速超过 maxDrillPower 上限时，产量累积按同一比例缩放，
        /// 保证每个产出周期的产量始终约为各 pawn 产出率的加权平均，不随人数膨胀。
        /// 进度达 WorkPerPortionBase 时触发 TryProducePortion 产出。
        /// </summary>
        public virtual void DrillWork()
        {
            portionProgress += DrillPower;

            float yieldScale = drillPower > maxDrillPower ? DrillPower / drillPower : 1f;
            foreach (var pawn in Drillers)
            {
                if (cachedPawnDeepdrillSpeedDict.TryGetValue(pawn, out float statValueDeepdrillSpeed)
                    && cachedPawnMiningYieldDict.TryGetValue(pawn, out float statValueMingYield))
                {
                    PortionYieldPct += statValueDeepdrillSpeed * statValueMingYield * 0.0001f * yieldScale;
                }
            }

            lastUsedTick = Find.TickManager.TicksGame;

            if (portionProgress > WorkPerPortionBase)
            {
                TryProducePortion(PortionYieldPct, drillers.Count > 0 ? drillers[drillers.Count - 1] : null);
                portionProgress = 0f;
                PortionYieldPct = 0f;
            }
        }

        /// <summary>移除/销毁时重置所有状态</summary>
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

        /// <summary>
        /// 产出矿物。由子类重写实现具体产出逻辑。
        /// </summary>
        /// <param name="yieldPct">产出倍率 (0~1+，受 pawn 技能和产出率影响)</param>
        /// <param name="driller">触发产出的 pawn（可能为 null）</param>
        protected virtual void TryProducePortion(float yieldPct, Pawn driller = null)
        { }

        /// <summary>检查钻机是否可以工作，由子类重写</summary>
        protected virtual void UpdateCanDrillState()
        { }

        /// <summary>
        /// 刷新在岗 pawn 的速度/产出率缓存。
        /// 使用索引器赋值（而非 Clear+Add），保留字典内部数组容量避免重分配。
        /// 配合 DrillJoinWork 即时填充，保证首 tick 数据可用。
        /// </summary>
        protected virtual void UpdateCachedPawnDrillSpeed()
        {
            drillPower = 0f;
            foreach (var p in Drillers)
            {
                float speed = p.GetStatValue(StatDefOf.DeepDrillingSpeed);
                cachedPawnDeepdrillSpeedDict[p] = speed;
                cachedPawnMiningYieldDict[p] = p.GetStatValue(StatDefOf.MiningYield);
                drillPower += speed;
            }
        }

        /// <summary>判断钻机在上一个 tick 是否被使用</summary>
        public virtual bool UsedLastTick()
        {
            return lastUsedTick >= Find.TickManager.TicksGame - 1;
        }

        /// <summary>
        /// PRF 自动化 Mod 专用接口。由外部直接推送进度和产量数据。
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

        /// <summary>
        /// 扩展 Gizmo。Dev 模式下添加即时产出按钮方便调试。
        /// </summary>
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
