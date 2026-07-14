using MoreBetterDeepDrill.Types;
using MoreBetterDeepDrill.Utils;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace MoreBetterDeepDrill.Settings
{
    /// <summary>Mod 初始化与设置界面。</summary>
    public class MBDD_Mod : Mod
    {
        private const float SectionGap = 14f;
        private const float RowHeight = 38f;

        public static MBDD_Settings ModSetting;

        private readonly Dictionary<string, string> oreAmountBuffers = new Dictionary<string, string>();
        private Vector2 scrollPosition = Vector2.zero;
        private string oreSearch = string.Empty;

        public MBDD_Mod(ModContentPack content) : base(content)
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                ModSetting = GetSettings<MBDD_Settings>();
                if (OreDictionary.Synchronize())
                    WriteSettings();
            });
        }

        public override string SettingsCategory()
        {
            return StaticValues.MoreBetterDeepDrill;
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect content = inRect.ContractedBy(10f);
            float y = content.y;

            DrawSectionHeader(new Rect(content.x, y, content.width, 30f), "MBDD_Settings_Gameplay".Translate());
            y += 38f;

            Rect gameplayRect = new Rect(content.x, y, content.width, 86f);
            Widgets.DrawBoxSolid(gameplayRect, new Color(0.12f, 0.13f, 0.14f, 0.32f));
            Listing_Standard gameplay = new Listing_Standard();
            gameplay.Begin(gameplayRect.ContractedBy(12f));
            gameplay.CheckboxLabeled("MBDD_Label_EnableInsectoids".Translate(), ref ModSetting.EnableInsectoids,
                "MBDD_Desc_EnableInsectoids".Translate());
            gameplay.CheckboxLabeled("MBDD_Label_EnableMechdroids".Translate(), ref ModSetting.EnableMechdroids,
                "MBDD_Desc_EnableMechdroids".Translate());
            gameplay.End();
            y = gameplayRect.yMax + SectionGap;

            DrawSectionHeader(new Rect(content.x, y, content.width, 30f), "MBDD_Label_OreListedDisplay".Translate());
            y += 38f;

            List<DrillableOre> allOres = ModSetting.oreDictionary ?? new List<DrillableOre>();
            Rect toolbar = new Rect(content.x, y, content.width, 32f);
            Rect searchRect = new Rect(toolbar.x, toolbar.y, Mathf.Max(180f, toolbar.width - 250f), toolbar.height);
            Rect resetRect = new Rect(toolbar.xMax - 190f, toolbar.y, 190f, toolbar.height);
            oreSearch = Widgets.TextField(searchRect, oreSearch);
            if (oreSearch.NullOrEmpty())
                Widgets.Label(searchRect.ContractedBy(7f, 4f), "MBDD_Settings_Search".Translate().Colorize(ColoredText.SubtleGrayColor));
            if (Widgets.ButtonText(resetRect, "MBDD_ButtonText_ResetOreDefaults".Translate()))
            {
                OreDictionary.ResetToDefaults();
                oreAmountBuffers.Clear();
                WriteSettings();
            }
            y = toolbar.yMax + 8f;

            List<DrillableOre> filtered = GetFilteredOres(allOres);
            Rect summaryRect = new Rect(content.x, y, content.width, 24f);
            Widgets.Label(summaryRect, "MBDD_Settings_OreCount".Translate(filtered.Count, allOres.Count));
            y = summaryRect.yMax + 4f;

            Rect tableRect = new Rect(content.x, y, content.width, Mathf.Max(180f, content.yMax - y));
            Widgets.DrawBoxSolid(tableRect, new Color(0.08f, 0.09f, 0.1f, 0.28f));
            DrawOreTable(tableRect.ContractedBy(6f), filtered);
        }

        private void DrawOreTable(Rect outRect, List<DrillableOre> ores)
        {
            const float HeaderHeight = 30f;
            Rect header = new Rect(outRect.x, outRect.y, outRect.width - 16f, HeaderHeight);
            Widgets.DrawBoxSolid(header, new Color(0.16f, 0.18f, 0.2f, 0.75f));
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(header.x + 42f, header.y + 7f, header.width - 180f, 22f), "MBDD_Settings_OreColumn".Translate());
            Widgets.Label(new Rect(header.xMax - 116f, header.y + 7f, 106f, 22f), "MBDD_Settings_YieldColumn".Translate());
            Text.Font = GameFont.Small;

            Rect scrollOut = new Rect(outRect.x, header.yMax + 2f, outRect.width, outRect.height - HeaderHeight - 2f);
            Rect viewRect = new Rect(0f, 0f, scrollOut.width - 16f, Mathf.Max(scrollOut.height, ores.Count * RowHeight));
            Widgets.BeginScrollView(scrollOut, ref scrollPosition, viewRect);
            for (int i = 0; i < ores.Count; i++)
            {
                DrillableOre ore = ores[i];
                Rect row = new Rect(0f, i * RowHeight, viewRect.width, RowHeight);
                if (i % 2 == 1)
                    Widgets.DrawAltRect(row);
                Widgets.DrawHighlightIfMouseover(row);

                Rect iconRect = new Rect(row.x + 5f, row.y + 4f, 30f, 30f);
                Widgets.ThingIcon(iconRect, ore.OreDef);
                Rect labelRect = new Rect(iconRect.xMax + 8f, row.y + 8f, row.width - 175f, 24f);
                Widgets.Label(labelRect, ore.OreDef.LabelCap);

                Rect amountRect = new Rect(row.xMax - 110f, row.y + 5f, 92f, 28f);
                string buffer = GetOreAmountBuffer(ore);
                Widgets.TextFieldNumeric(amountRect, ref ore.amountPerPortion, ref buffer, 1, 100000);
                oreAmountBuffers[ore.OreDef.defName] = buffer;
                TooltipHandler.TipRegion(row, ore.OreDef.description);
            }
            Widgets.EndScrollView();
        }

        private List<DrillableOre> GetFilteredOres(List<DrillableOre> ores)
        {
            if (oreSearch.NullOrEmpty())
                return ores;

            string query = oreSearch.Trim().ToLowerInvariant();
            List<DrillableOre> result = new List<DrillableOre>();
            for (int i = 0; i < ores.Count; i++)
            {
                DrillableOre ore = ores[i];
                if (ore?.OreDef != null && (ore.OreDef.defName.ToLowerInvariant().Contains(query)
                    || ore.OreDef.LabelCap.ToString().ToLowerInvariant().Contains(query)))
                    result.Add(ore);
            }
            return result;
        }

        private static void DrawSectionHeader(Rect rect, string label)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, label);
            Text.Font = GameFont.Small;
            Widgets.DrawLineHorizontal(rect.x, rect.yMax - 1f, rect.width, new Color(0.42f, 0.48f, 0.52f, 0.65f));
        }

        private string GetOreAmountBuffer(DrillableOre ore)
        {
            if (!oreAmountBuffers.TryGetValue(ore.OreDef.defName, out string buffer) || buffer.NullOrEmpty())
            {
                buffer = ore.amountPerPortion.ToString();
                oreAmountBuffers[ore.OreDef.defName] = buffer;
            }
            return buffer;
        }
    }
}
