using RimWorld;

namespace MoreBetterDeepDrill.Comp
{
    public class MBDD_StorytellerCompProperties_Infestation : StorytellerCompProperties
    {
        public float baseMtbDaysPerDrill;

        public MBDD_StorytellerCompProperties_Infestation()
        {
            this.compClass = typeof(MBDD_StorytellerComp_Infestation);
        }
    }
}