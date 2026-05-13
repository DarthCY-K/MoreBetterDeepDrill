using MoreBetterDeepDrill.Settings;
using MoreBetterDeepDrill.Types;
using MoreBetterDeepDrill.Utils;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MoreBetterDeepDrill.Comp
{
    public class MBDD_CompRangedDeepDrill : MBDD_CompDeepDrill
    {
        protected DrillableOre selectedOre;
        protected bool targetingOreEnable;

        private DrillableOre cachedSelectedOreEntry;
        private ThingDef cachedSelectedOreDef;

        private DrillableOre SelectedOreEntry
        {
            get
            {
                var currentDef = selectedOre?.OreDef;
                if (currentDef == cachedSelectedOreDef)
                    return cachedSelectedOreEntry;

                cachedSelectedOreDef = currentDef;
                if (currentDef == null)
                    return cachedSelectedOreEntry = null;

                var oreDictionary = StaticValues.ModSetting?.oreDictionary;
                if (oreDictionary != null)
                {
                    for (int i = 0; i < oreDictionary.Count; i++)
                    {
                        if (oreDictionary[i]?.OreDef == currentDef)
                            return cachedSelectedOreEntry = oreDictionary[i];
                    }
                }

                return cachedSelectedOreEntry = selectedOre;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref targetingOreEnable, "targetingOreEnable");
            Scribe_Deep.Look(ref selectedOre, "selectedOre");
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
        }

        protected override void TryProducePortion(float yieldPct, Pawn driller = null)
        {
            ThingDef resDef;
            int countPresent;
            IntVec3 cell;
            bool nextResource = false;

            if (targetingOreEnable)
            {
                if (SelectedOreEntry == null)
                    return;

                nextResource = GetNextResource(out resDef, out countPresent, out cell);
                if (!nextResource)
                    return;
            }
            else
            {
                nextResource = GetNextResource(out resDef, out countPresent, out cell);
            }

            if (resDef == null)
                return;

            int num = Mathf.Min(countPresent, resDef.deepCountPerPortion);

            if (nextResource)
            {
                parent.Map.deepResourceGrid.SetAt(cell, resDef, countPresent - num);
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

            var allBuildings = parent.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildings.Count; i++)
            {
                if (allBuildings[i].def == Defs.ThingDefOf.MBDD_RangedDeepDrill)
                {
                    var rangedComp = allBuildings[i].GetComp<MBDD_CompRangedDeepDrill>();
                    if (rangedComp != null && !rangedComp.ValuableResourcesPresent())
                        allBuildings[i].SetForbidden(true);
                }
            }
        }

        protected override void UpdateCanDrillState()
        {
            if (powerComp != null && powerComp.PowerOn)
            {
                if (targetingOreEnable)
                {
                    CanDrillNow = ValuableResourcesPresent();
                }
                else if (Utils.DeepDrillUtil.GetBaseResource(parent.Map, parent.Position) != null)
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

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }

            yield return new Command_Toggle
            {
                defaultLabel = "MBDD_RangedDeepDrill_CommandToggle_EnableOreTargeting_Label".Translate(),
                defaultDesc = "MBDD_RangedDeepDrill_CommandToggle_EnableOreTargeting_Desc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/EnableOreTargetingToggle"),
                isActive = () => targetingOreEnable,
                toggleAction = delegate
                {
                    targetingOreEnable = !targetingOreEnable;
                    cachedSelectedOreDef = null;
                }
            };

            if (targetingOreEnable)
            {
                DrillableOre currentSelectedOre = SelectedOreEntry;
                Command_Action action_selectOre = new Command_Action();
                action_selectOre.defaultLabel = "MBDD_RangedDeepDrill_Gizmo_SelectOre".Translate();
                action_selectOre.icon = currentSelectedOre?.OreDef?.uiIcon ?? ThingDefOf.DeepDrill.uiIcon;
                action_selectOre.Disabled = StaticValues.ModSetting.oreDictionary == null || StaticValues.ModSetting.oreDictionary.Count <= 0;
                action_selectOre.disabledReason = "MBDD_RangedDeepDrill_Gizmo_NoOre".Translate();
                action_selectOre.action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    using (IEnumerator<DrillableOre> enumerator = MBDD_Mod.ModSetting.oreDictionary.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            DrillableOre ore = enumerator.Current;
                            FloatMenuOption floatMenu_selectOre = new FloatMenuOption("MBDD_RangedDeepDrill_FloatMenu_SelectOre".Translate() + ore.OreDef.LabelCap, delegate
                            {
                                selectedOre = ore;
                                cachedSelectedOreDef = null;
                            }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                            floatMenu_selectOre.Disabled = currentSelectedOre?.OreDef == ore.OreDef;
                            floatMenu_selectOre.disabledReason = "MBDD_RangedDeepDrill_FloatMenu_SameOre".Translate();
                            list.Add(floatMenu_selectOre);
                        }
                    }

                    if (list.Count != 0)
                    {
                        FloatMenu window = new FloatMenu(list);
                        Find.WindowStack.Add(window);
                    }
                };

                yield return action_selectOre;
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!parent.Spawned)
                return null;

            DrillableOre currentSelectedOre = SelectedOreEntry;
            if (targetingOreEnable && currentSelectedOre == null)
                return "DeepDrillNoResource_SelectedOre_Null".Translate();

            GetNextResource(out ThingDef resDef, out _, out _);
            if (resDef == null)
                return "DeepDrillNoResources".Translate();

            if (DebugSettings.ShowDevGizmos)
            {
                return "ResourceBelow".Translate() + ": " + resDef.LabelCap + "\n" + "ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0") + $"\nPortionYieldPct: {PortionYieldPct}\nDrillPower: {DrillPower} (Raw: {drillPower}, Max: {maxDrillPower})";
            }

            return "ResourceBelow".Translate() + ": " + resDef.LabelCap + "\n" + "ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0");
        }

        private bool GetNextResource(out ThingDef resDef, out int countPresent, out IntVec3 cell)
        {
            ThingDef targetOreDef = targetingOreEnable ? SelectedOreEntry?.OreDef : null;
            if (targetingOreEnable && targetOreDef == null)
            {
                resDef = null;
                countPresent = int.MaxValue;
                cell = parent.Position;
                return false;
            }

            return targetOreDef != null
                ? Utils.DeepDrillUtil.GetNextResource(parent.Position, parent.Map, out resDef, out countPresent, out cell, targetOreDef)
                : Utils.DeepDrillUtil.GetNextResource(parent.Position, parent.Map, out resDef, out countPresent, out cell);
        }
    }
}
