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

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look<bool>(ref targetingOreEnable, "targetingOreEnable");
            Scribe_Deep.Look(ref selectedOre, "selectedOre");
        }

        protected override void TryProducePortion(float yieldPct, Pawn driller = null)
        {
            ThingDef resDef;
            int countPresent;
            IntVec3 cell;
            bool nextResource = false;

            //定向模式下找不到矿就直接不挖了
            if (targetingOreEnable)
            {
                if (selectedOre == null)
                    return;

                nextResource = GetNextResource(out resDef, out countPresent, out cell, selectedOre);
                if(nextResource == false)
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

        private bool GetNextResource(out ThingDef resDef, out int countPresent, out IntVec3 cell, DrillableOre targetOre)
        {
            return Utils.DeepDrillUtil.GetNextResource(parent.Position, parent.Map, out resDef, out countPresent, out cell, targetOre.OreDef);
        }

        protected override void UpdateCanDrillState()
        {
            if (powerComp != null && powerComp.PowerOn)
            {
                //定向挖掘情况下找不到指定矿就不能允许工作
                if (targetingOreEnable)
                {
                    CanDrillNow = ValuableResourcesPresent();
                }
                else
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

            if (targetingOreEnable)
            {
                if (selectedOre != null)
                    return GetNextResource(out resDef, out countPresent, out cell, selectedOre);
                else
                    return false;
            }
            else
                return GetNextResource(out resDef, out countPresent, out cell);
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
                isActive = (() => targetingOreEnable),
                toggleAction = delegate ()
                {
                    targetingOreEnable = !targetingOreEnable;
                }
            };

            if (targetingOreEnable)
            {
                Command_Action action_selectOre = new Command_Action();
                action_selectOre.defaultLabel = "MBDD_RangedDeepDrill_Gizmo_SelectOre".Translate();
                if (selectedOre != null)
                    action_selectOre.icon = selectedOre.OreDef.uiIcon;
                else
                    action_selectOre.icon = ThingDefOf.DeepDrill.uiIcon;
                action_selectOre.Disabled = StaticValues.ModSetting.oreDictionary == null || StaticValues.ModSetting.oreDictionary.Count <= 0;
                action_selectOre.disabledReason = "MBDD_RangedDeepDrill_Gizmo_NoOre".Translate();
                action_selectOre.action = delegate
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    using (IEnumerator<DrillableOre> enumerator = MBDD_Mod.ModSetting.oreDictionary.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            var ore = enumerator.Current;
                            FloatMenuOption floatMenu_selectOre = new FloatMenuOption("MBDD_RangedDeepDrill_FloatMenu_SelectOre".Translate() + ore.OreDef.LabelCap, delegate ()
                            {
                                selectedOre = ore;
                            }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                            floatMenu_selectOre.Disabled = selectedOre == ore;
                            action_selectOre.disabledReason = "MBDD_RangedDeepDrill_FloatMenu_SameOre".Translate();

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
            if (parent.Spawned)
            {
                ThingDef resDef;

                if (targetingOreEnable)
                {
                    if (selectedOre == null)
                        return "DeepDrillNoResource_SelectedOre_Null".Translate();
                    else
                        GetNextResource(out resDef, out var _, out var _, selectedOre);
                }
                else
                {
                    GetNextResource(out resDef, out var _, out var _);
                }

                if (resDef == null)
                {
                    return targetingOreEnable ? "DeepDrillNoResource_SelectedOre_Null".Translate() : "DeepDrillNoResources".Translate();
                }
                else
                {
                    if (DebugSettings.ShowDevGizmos)
                        return "ResourceBelow".Translate() + ": " + resDef.LabelCap + "\n" + "ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0") + $"\nPortionYieldPct: {PortionYieldPct}\nDrillPower: {drillPower} (Max is: {maxDrillPower})";
                    else
                        return "ResourceBelow".Translate() + ": " + resDef.LabelCap + "\n" + "ProgressToNextPortion".Translate() + ": " + ProgressToNextPortionPercent.ToStringPercent("F0");
                }
            }

            return null;
        }
    }
}