using MoreBetterDeepDrill.Settings;
using MoreBetterDeepDrill.Types;
using MoreBetterDeepDrill.Utils;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MoreBetterDeepDrill.Comp
{
    public class MBDD_CompArchotechDeepDrill : MBDD_CompDeepDrill
    {
        protected DrillableOre selectedOre;

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
            Scribe_Deep.Look(ref selectedOre, "selectedOre");
        }

        protected override void TryProducePortion(float yieldPct, Pawn driller = null)
        {
            DrillableOre oreToProduce = SelectedOreEntry;
            if (oreToProduce == null)
            {
                Messages.Message("DeepDrillExhaustedNoFallback".Translate(), parent, MessageTypeDefOf.TaskCompletion);
                return;
            }

            Thing thing = ThingMaker.MakeThing(oreToProduce.OreDef);
            thing.stackCount = Mathf.Max(1, GenMath.RoundRandom(oreToProduce.amountPerPortion * yieldPct));
            GenPlace.TryPlaceThing(thing, parent.InteractionCell, parent.Map, ThingPlaceMode.Near, null, (IntVec3 p) => p != parent.Position && p != parent.InteractionCell);
            if (driller != null)
            {
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.Mined, driller.Named(HistoryEventArgsNames.Doer)));
            }
        }

        protected override void UpdateCanDrillState()
        {
            if (powerComp != null && !powerComp.PowerOn)
                CanDrillNow = false;
            else if (SelectedOreEntry == null)
                CanDrillNow = false;
            else
                CanDrillNow = true;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }

            DrillableOre currentSelectedOre = SelectedOreEntry;
            Command_Action action_selectOre = new Command_Action();
            action_selectOre.defaultLabel = "MBDD_ArchotechDeepDrill_Gizmo_SelectOre".Translate();
            action_selectOre.icon = currentSelectedOre?.OreDef?.uiIcon ?? ThingDefOf.DeepDrill.uiIcon;
            action_selectOre.Disabled = StaticValues.ModSetting.oreDictionary == null || StaticValues.ModSetting.oreDictionary.Count <= 0;
            action_selectOre.disabledReason = "MBDD_ArchotechDeepDrill_Gizmo_NoOre".Translate();
            action_selectOre.action = delegate
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                using (IEnumerator<DrillableOre> enumerator = MBDD_Mod.ModSetting.oreDictionary.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        DrillableOre ore = enumerator.Current;
                        FloatMenuOption floatMenu_selectOre = new FloatMenuOption("MBDD_ArchotechDeepDrill_FloatMenu_SelectOre".Translate() + ore.OreDef.LabelCap, delegate
                        {
                            selectedOre = ore;
                        }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                        floatMenu_selectOre.Disabled = currentSelectedOre?.OreDef == ore.OreDef;
                        floatMenu_selectOre.disabledReason = "MBDD_ArchotechDeepDrill_FloatMenu_SameOre".Translate();
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

        public override string CompInspectStringExtra()
        {
            DrillableOre currentSelectedOre = SelectedOreEntry;
            if (parent.Spawned && currentSelectedOre != null)
            {
                if (DebugSettings.ShowDevGizmos)
                    return "ResourceBelow".Translate() + ": " + currentSelectedOre.OreDef.LabelCap + "\n" + "ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0") + $"\nPortionYieldPct: {PortionYieldPct}\nDrillPower: {DrillPower}";

                return "ResourceBelow".Translate() + ": " + currentSelectedOre.OreDef.LabelCap + "\n" + "ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0");
            }

            return null;
        }
    }
}
