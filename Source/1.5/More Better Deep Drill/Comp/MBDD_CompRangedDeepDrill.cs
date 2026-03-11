using MoreBetterDeepDrill.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace MoreBetterDeepDrill.Comp
{
    public class MBDD_CompRangedDeepDrill : MBDD_CompDeepDrill
    {
        private const int ResourceScanCacheTicks = 60;

        private int cachedResourceScanTick = -99999;
        private bool cachedResourceFound;
        private ThingDef cachedResourceDef;
        private int cachedResourceCount;
        private IntVec3 cachedResourceCell = IntVec3.Invalid;

        public override void PostDeSpawn()
        {
            base.PostDeSpawn();
            InvalidateResourceCache();
        }

        protected override void TryProducePortion(float yieldPct, Pawn driller = null)
        {
            ThingDef resDef;
            int countPresent;
            IntVec3 cell;
            bool nextResource = GetNextResource(out resDef, out countPresent, out cell);

            if (resDef == null)
                return;

            int num = Mathf.Min(countPresent, resDef.deepCountPerPortion);

            if (nextResource)
            {
                parent.Map.deepResourceGrid.SetAt(cell, resDef, countPresent - num);
                InvalidateResourceCache();
            }

            int stackCount = Mathf.Max(1, GenMath.RoundRandom(num * yieldPct));
            Thing thing = ThingMaker.MakeThing(resDef);
            thing.stackCount = stackCount;
            GenPlace.TryPlaceThing(thing, parent.InteractionCell, parent.Map, ThingPlaceMode.Near, null, (IntVec3 p) => p != parent.Position && p != parent.InteractionCell);

            if (driller != null)
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.Mined, driller.Named(HistoryEventArgsNames.Doer)));

            if (!nextResource || ValuableResourcesPresent())
                return;

            ThingDef baseResource = DeepDrillUtility.GetBaseResource(parent.Map, parent.Position);
            if (baseResource == null)
            {
                Messages.Message("DeepDrillExhaustedNoFallback".Translate(), parent, MessageTypeDefOf.TaskCompletion);
                return;
            }

            Messages.Message("DeepDrillExhausted".Translate(Find.ActiveLanguageWorker.Pluralize(baseResource.label)), parent, MessageTypeDefOf.TaskCompletion);

            for (int i = 0; i < 10000; i++)
            {
                IntVec3 c = cell + GenRadial.RadialPattern[i];
                if (c.InBounds(parent.Map))
                {
                    ThingWithComps firstThingWithComp = c.GetFirstThingWithComp<MBDD_CompRangedDeepDrill>(parent.Map);
                    if (firstThingWithComp != null && !firstThingWithComp.GetComp<MBDD_CompRangedDeepDrill>().ValuableResourcesPresent())
                        firstThingWithComp.SetForbidden(true);
                }
            }
        }

        protected override void UpdateCanDrillState()
        {
            if (powerComp != null && powerComp.PowerOn)
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
            return GetNextResource(out _, out _, out _);
        }

        public override string CompInspectStringExtra()
        {
            if (!parent.Spawned)
                return null;

            GetNextResource(out ThingDef resDef, out _, out _);
            if (resDef == null)
                return "DeepDrillNoResources".Translate();

            if (DebugSettings.ShowDevGizmos)
                return "ResourceBelow".Translate() + ": " + resDef.LabelCap + "\n" + "ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0") + $"\nPortionYieldPct: {PortionYieldPct}\nDrillPower: {DrillPower}";

            return "ResourceBelow".Translate() + ": " + resDef.LabelCap + "\n" + "ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0");
        }

        private bool GetNextResource(out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            int currentTick = Find.TickManager.TicksGame;
            if (cachedResourceScanTick >= 0 && currentTick - cachedResourceScanTick < ResourceScanCacheTicks)
            {
                resDef = cachedResourceDef;
                countPresent = cachedResourceCount;
                cell = cachedResourceCell;
                return cachedResourceFound;
            }

            bool found = Utils.DeepDrillUtil.GetNextResource(parent.Position, parent.Map, out resDef, out countPresent, out cell);
            cachedResourceScanTick = currentTick;
            cachedResourceFound = found;
            cachedResourceDef = resDef;
            cachedResourceCount = countPresent;
            cachedResourceCell = cell;
            return found;
        }

        private void InvalidateResourceCache()
        {
            cachedResourceScanTick = -99999;
            cachedResourceFound = false;
            cachedResourceDef = null;
            cachedResourceCount = 0;
            cachedResourceCell = IntVec3.Invalid;
        }
    }
}
