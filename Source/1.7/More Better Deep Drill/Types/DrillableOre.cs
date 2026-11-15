using Verse;

namespace MoreBetterDeepDrill.Types
{
    /// <summary>
    /// 可深钻矿石数据模型。存储矿石类型和每次产出数量。
    /// 持久化于 Mod 设置中，支持运行时修改。
    /// </summary>
    public class DrillableOre : IExposable
    {
        /// <summary>矿石 ThingDef</summary>
        public ThingDef OreDef => thingDef;
        private ThingDef thingDef;

        /// <summary>每次产出的数量</summary>
        public int amountPerPortion;

        public DrillableOre()
        { }

        public DrillableOre(ThingDef thingDef, int amountPerPortion)
        {
            this.thingDef = thingDef;
            this.amountPerPortion = amountPerPortion;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look<ThingDef>(ref this.thingDef, "thingDef");
            Scribe_Values.Look<int>(ref this.amountPerPortion, "amountPerPortion");
        }
    }
}
