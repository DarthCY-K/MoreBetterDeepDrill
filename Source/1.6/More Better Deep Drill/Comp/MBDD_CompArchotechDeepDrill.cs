using MoreBetterDeepDrill.Settings;
using MoreBetterDeepDrill.Types;
using MoreBetterDeepDrill.Utils;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MoreBetterDeepDrill.Comp
{
    /// <summary>
    /// 超凡深钻井 Comp。无需地图资源格子，直接从 oreDictionary 中选择矿石产出。
    /// 只要选中了有效矿石且通电即可无限产出。
    /// </summary>
    public class MBDD_CompArchotechDeepDrill : MBDD_CompDeepDrill
    {
        /// <summary>用户选择的产出矿石类型</summary>
        protected DrillableOre selectedOre;

        /// <summary>SelectedOreEntry 返回值缓存</summary>
        private DrillableOre cachedSelectedOreEntry;
        private ThingDef cachedSelectedOreDef;
        private List<DrillableOre> cachedOreDictionary;

        /// <summary>
        /// 获取 selectedOre 在 oreDictionary 中的当前条目。
        /// 使用 OreDef 引用缓存，selectedOre 变更时触发一次 O(n) 搜索。
        /// </summary>
        private DrillableOre SelectedOreEntry
        {
            get
            {
                var currentDef = selectedOre?.OreDef;
                var oreDictionary = StaticValues.ModSetting?.oreDictionary;
                if (currentDef == cachedSelectedOreDef && ReferenceEquals(oreDictionary, cachedOreDictionary))
                    return cachedSelectedOreEntry;

                cachedSelectedOreDef = currentDef;
                cachedOreDictionary = oreDictionary;
                if (currentDef == null)
                    return cachedSelectedOreEntry = null;

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

        /// <summary>存档：持久化选择的矿石</summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref selectedOre, "selectedOre");
        }

        /// <summary>
        /// 产出矿物。直接从 SelectedOreEntry 获取矿石种类和数量，
        /// 不消耗地图深钻井资源格子。
        /// </summary>
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

        /// <summary>钻机可工作条件：通电 + 选中了有效矿石</summary>
        protected override void UpdateCanDrillState()
        {
            if (powerComp != null && !powerComp.PowerOn)
                CanDrillNow = false;
            else if (SelectedOreEntry == null)
                CanDrillNow = false;
            else
                CanDrillNow = true;
        }

        /// <summary>Gizmo：矿石选择 FloatMenu</summary>
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
                        bool isCurrent = currentSelectedOre?.OreDef == ore.OreDef;
                        string label = "MBDD_ArchotechDeepDrill_FloatMenu_SelectOre".Translate() + ore.OreDef.LabelCap;
                        if (isCurrent)
                            label += " (" + "MBDD_ArchotechDeepDrill_FloatMenu_SameOre".Translate() + ")";
                        FloatMenuOption floatMenu_selectOre = new FloatMenuOption(label, isCurrent ? null : (System.Action)(() => selectedOre = ore));
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

        /// <summary>检视面板：显示当前产出矿石和进度</summary>
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
