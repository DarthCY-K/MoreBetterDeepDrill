using MoreBetterDeepDrill.Utils;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MoreBetterDeepDrill.Comp
{
    internal class MBDD_StorytellerComp_Infestation : StorytellerComp
    {
        private static List<Thing> tmpDrills = new List<Thing>();

        protected MBDD_StorytellerCompProperties_Infestation Props => (MBDD_StorytellerCompProperties_Infestation)props;

        private float DeepDrillInfestationMTBDaysPerDrill
        {
            get
            {
                Difficulty difficulty = Find.Storyteller.difficulty;
                if (difficulty.deepDrillInfestationChanceFactor <= 0f)
                {
                    return -1f;
                }

                return Props.baseMtbDaysPerDrill / difficulty.deepDrillInfestationChanceFactor;
            }
        }

        public override IEnumerable<FiringIncident> MakeIntervalIncidents(IIncidentTarget target)
        {
            if (!StaticValues.ModSetting.EnableInsectoids)
                yield break;

            Map map = (Map)target;
            tmpDrills.Clear();
            Utils.InfestationIncidentUtil.GetUsableDeepDrills(map, tmpDrills);
            if (!tmpDrills.Any())
            {
                yield break;
            }

            float mtb = DeepDrillInfestationMTBDaysPerDrill;
            if (mtb < 0f)
            {
                yield break;
            }

            for (int i = 0; i < tmpDrills.Count; i++)
            {
                if (Rand.MTBEventOccurs(mtb, 60000f, 1000f))
                {
                    IncidentParms parms = GenerateParms(IncidentCategoryDefOf.DeepDrillInfestation, target);
                    yield return new FiringIncident(Defs.IncidentDefOf.MBDD_Infestation, this, parms);
                }
            }
        }
    }
}