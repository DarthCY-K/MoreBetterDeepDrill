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
    /// 远距深钻井 Comp。继承基础钻井逻辑，增加全图资源扫描和定向采矿功能。
    /// 通过全局 MapResourceCache 避免重复全图扫描。
    /// </summary>
    public class MBDD_CompRangedDeepDrill : MBDD_CompDeepDrill
    {
        /// <summary>用户选择的定向采矿目标</summary>
        protected DrillableOre selectedOre;
        /// <summary>是否启用定向采矿模式</summary>
        protected bool targetingOreEnable;

        /// <summary>SelectedOreEntry 返回值缓存。以 OreDef 引用为键，selectedOre 变更时失效</summary>
        private DrillableOre cachedSelectedOreEntry;
        private ThingDef cachedSelectedOreDef;
        private List<DrillableOre> cachedOreDictionary;

        /// <summary>
        /// 获取 selectedOre 在 oreDictionary 中的当前条目。
        /// 使用 OreDef 引用缓存，避免每帧遍历 oreDictionary 列表。
        /// 仅在 selectedOre 变更或首次访问时 O(n) 搜索一次。
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

        /// <summary>存档：额外持久化定向采矿状态</summary>
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref targetingOreEnable, "targetingOreEnable");
            Scribe_Deep.Look(ref selectedOre, "selectedOre");
        }

        /// <summary>
        /// 产出矿物。定向模式下先查找指定资源，非定向模式扫描任意资源。
        /// 资源枯竭时禁用全图同类 Ranged 钻机。
        /// </summary>
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

            // 资源枯竭时，遍历殖民者建筑列表查找同类钻机关闭
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

        /// <summary>
        /// 每 300 tick 更新钻机可工作状态。
        /// 有电 + (有基岩资源 或 定向模式找到目标资源) = 可工作
        /// </summary>
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

        /// <summary>检查全图是否还有可开采资源</summary>
        public bool ValuableResourcesPresent()
        {
            return GetNextResource(out _, out _, out _);
        }

        /// <summary>Gizmo：定向采矿开关 + 矿石选择菜单</summary>
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
                            bool isCurrent = currentSelectedOre?.OreDef == ore.OreDef;
                            string label = "MBDD_RangedDeepDrill_FloatMenu_SelectOre".Translate() + ore.OreDef.LabelCap;
                            if (isCurrent)
                                label += " (" + "MBDD_RangedDeepDrill_FloatMenu_SameOre".Translate() + ")";
                            FloatMenuOption floatMenu_selectOre = new FloatMenuOption(label, isCurrent ? null : (System.Action)(() =>
                            {
                                selectedOre = ore;
                                cachedSelectedOreDef = null;
                            }));
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

        /// <summary>检视面板信息：显示当前资源类型、进度百分比等</summary>
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

        /// <summary>
        /// 查找下一个可开采资源。定向模式传入 targetOreDef 精确匹配。
        /// 使用全局 MapResourceCache 查询。
        /// </summary>
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
