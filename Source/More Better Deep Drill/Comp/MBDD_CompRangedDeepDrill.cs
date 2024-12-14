using MoreBetterDeepDrill.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MoreBetterDeepDrill.Comp
{
    public class MBDD_CompRangedDeepDrill : MBDD_CompDeepDrill
    {
        protected override void TryProducePortion(float yieldPct, Pawn driller = null)
        {
            ThingDef resDef;
            int countPresent;
            IntVec3 cell;
            bool nextResource = GetNextResource(out resDef, out countPresent, out cell);

            if (resDef == null)
                return;
            int num = Mathf.Min(countPresent, OreDictionary.DrillableOreDict[resDef].amountPerPortion);

            if (nextResource)
                parent.Map.deepResourceGrid.SetAt(cell, resDef, countPresent - num);

            int stackCount = Mathf.Max(1, GenMath.RoundRandom((float)num * yieldPct));
            Thing thing = ThingMaker.MakeThing(resDef);
            thing.stackCount = stackCount;
            GenPlace.TryPlaceThing(thing, parent.InteractionCell, parent.Map, ThingPlaceMode.Near, null, (IntVec3 p) => p != parent.Position && p != parent.InteractionCell);
            
            if (driller != null)
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.Mined, driller.Named(HistoryEventArgsNames.Doer)));

            if (!nextResource || ValuableResourcesPresent())
                return;

            if (DeepDrillUtility.GetBaseResource(parent.Map, parent.Position) == null)
            {
                Messages.Message("DeepDrillExhaustedNoFallback".Translate(), parent, MessageTypeDefOf.TaskCompletion);
                return;
            }

            Messages.Message("DeepDrillExhausted".Translate(Find.ActiveLanguageWorker.Pluralize(Utils.DeepDrillUtil.GetBaseResource(parent.Map, parent.Position).label)), parent, MessageTypeDefOf.TaskCompletion);

            for (int i = 0; i < 10000; i++)
            {
                IntVec3 c = cell + GenRadial.RadialPattern[i];
                if (c.InBounds(parent.Map))
                {
                    ThingWithComps firstThingWithComp = c.GetFirstThingWithComp<MBDD_CompRangedDeepDrill>(parent.Map);
                    if (firstThingWithComp != null && !firstThingWithComp.GetComp<MBDD_CompRangedDeepDrill>().ValuableResourcesPresent())
                        firstThingWithComp.SetForbidden(value: true);
                }
            }
        }

        private bool GetNextResource(out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            return Utils.DeepDrillUtil.GetNextResource(parent.Position, parent.Map, out resDef, out countPresent, out cell);
        }

        protected override void UpdateCanDrillState()
        {
            if(powerComp != null && powerComp.PowerOn)
            {
                if (Utils.DeepDrillUtil.GetBaseResource(parent.Map, parent.Position) != null)
                {
                    CanDrillNow = true;
                }
                else
                {
                    CanDrillNow = ValuableResourcesPresent();
                }
            }
            else
            {
                CanDrillNow = false;
            }
        }

        public bool ValuableResourcesPresent()
        {
            ThingDef resDef;
            int countPresent;
            IntVec3 cell;
            return GetNextResource(out resDef, out countPresent, out cell);
        }

        public override string CompInspectStringExtra()
        {
            if (parent.Spawned)
            {
                GetNextResource(out var resDef, out var _, out var _);
                if (resDef == null)
                {
                    return "DeepDrillNoResources".Translate();
                }

                if (DebugSettings.ShowDevGizmos)
                    return "ResourceBelow".Translate() + ": " + resDef.LabelCap + "\n" + "ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0") + $"\nPortionYieldPct: {PortionYieldPct}\nDrillPower: {DrillPower}";
                else
                    return "ResourceBelow".Translate() + ": " + resDef.LabelCap + "\n" + "ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0");
            }

            return null;
        }
    }
}