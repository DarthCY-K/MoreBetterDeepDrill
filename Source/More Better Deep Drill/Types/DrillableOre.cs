using Verse;

namespace MoreBetterDeepDrill.Types
{
    public class DrillableOre : IExposable
    {
        public ThingDef OreDef => thingDef;
        private ThingDef thingDef;

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