using MoreBetterDeepDrill.Utils;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MoreBetterDeepDrill.Settings
{
    public class MBDD_Mod : Mod
    {
        public static MBDD_Settings ModSetting;

        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight = 0f;

        public MBDD_Mod(ModContentPack content) : base(content)
        {
            LongEventHandler.ExecuteWhenFinished(() => { ModSetting = GetSettings<MBDD_Settings>(); });
            LongEventHandler.ExecuteWhenFinished(PushToDatabase);
            LongEventHandler.ExecuteWhenFinished(BuildDictionary);

            LongEventHandler.ExecuteWhenFinished(OreDictionary.Refresh);
        }

        public override string SettingsCategory()
        {
            return Utils.StaticValues.MoreBetterDeepDrill;
        }

        /// <summary>
        /// 临时获取所有的def
        /// </summary>
        private void PushToDatabase()
        {
            ModSetting.database = DefDatabase<ThingDef>.AllDefsListForReading;
        }

        /// <summary>
        /// 根据筛选条件建立矿辞
        /// </summary>
        private void BuildDictionary()
        {
            bool flag = ModSetting.oreDictionary == null || ModSetting.oreDictionary.Count <= 0;
            if (flag)
            {
                OreDictionary.Build(false);
                AddExteraDrillable();
            }
        }

        private void AddExteraDrillable()
        {
            List<ThingDef> extraThingList = new List<ThingDef>();
            foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.building != null && (def.building.isResourceRock || def.building.isNaturalRock) && def.mineable)
                    extraThingList.Add(def);
            }

            if (extraThingList.Count > 0)
                OreDictionary.AddExtraDrillable(extraThingList);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float y = 0;

            //是否关闭虫巢
            y += 40;
            Widgets.Checkbox(0f, y, ref ModSetting.EnableInsectoids, 25f, false, false, null, null);
            Widgets.Label(new Rect(35f, y + 1f, inRect.width - 50f, 25f), "MBDD_Label_EnableInsectoids".Translate());

            //是否启用机械族挖矿
            y += 40;
            Widgets.Checkbox(0f, y, ref ModSetting.EnableMechdroids, 25f, false, false, null, null);
            Widgets.Label(new Rect(35f, y + 1f, inRect.width - 50f, 25f), "MBDD_Label_EnableMechdroids".Translate());

            //分区文本
            y += 80;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0, y, inRect.width - 50f, 30f), "MBDD_Label_OreListedDisplay".Translate());

            //主动刷新矿物列表
            y += 40;
            Text.Font = GameFont.Small;
            bool clicked = Widgets.ButtonText(new Rect(0f, y, 290f, 25f), "MBDD_ButtonText_ReBuildOreDictionary".Translate());
            if (clicked)
            {
                OreDictionary.Build(true);
                AddExteraDrillable();
            }

            if (ModSetting.oreDictionary != null && ModSetting.oreDictionary.Count > 0)
            {
                //滚动条
                y += 40;
                Rect outRect = new Rect(0, y, 310f, 300f);
                Rect rectView = new Rect(0, y, outRect.width - 16f, ModSetting.oreDictionary.Count * 32f);

                Widgets.BeginScrollView(outRect, ref this.scrollPosition, rectView, true);
                float num = y;

                for (int i = 0; i < ModSetting.oreDictionary.Count; i++)
                {
                    var ore = ModSetting.oreDictionary;

                    Rect rectRow = new Rect(0f, num, rectView.width, 32f);
                    Rect rectOreIcon = GenUI.LeftPartPixels(rectRow, 32f);
                    Rect rectOreLabel = new Rect(rectRow.x + 35f, rectRow.y + 5f, rectRow.width - 32f, rectRow.height);
                    Rect rectDeepCountPerPortion = new Rect(rectOreLabel.x + 185f, rectRow.y, 65f, rectRow.height);

                    //矿石图标和名称
                    Widgets.ThingIcon(rectOreIcon, ore[i].OreDef, null, null, 1f, null, null);
                    Widgets.Label(rectOreLabel, ore[i].OreDef.LabelCap);

                    //每堆数量
                    string buffer = ore[i].amountPerPortion.ToString();
                    Widgets.TextFieldNumeric(rectDeepCountPerPortion, ref ore[i].amountPerPortion, ref buffer);

                    //鼠标移入 显示矿石介绍
                    if (Mouse.IsOver(rectRow))
                        Widgets.DrawHighlight(rectRow);
                    TooltipHandler.TipRegion(rectRow, ore[i].OreDef.description);

                    //每行垂直偏移
                    num += 32f;
                    this.scrollViewHeight = num;
                }

                Widgets.EndScrollView();
            }
        }
    }
}