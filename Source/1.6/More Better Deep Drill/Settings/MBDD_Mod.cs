using MoreBetterDeepDrill.Types;
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

        private readonly Dictionary<string, string> oreAmountBuffers = new Dictionary<string, string>();
        private Vector2 scrollPosition = Vector2.zero;

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
        /// Cache all defs for the current load.
        /// </summary>
        private void PushToDatabase()
        {
            ModSetting.database = DefDatabase<ThingDef>.AllDefsListForReading;
        }

        /// <summary>
        /// Build the ore dictionary on first load.
        /// </summary>
        private void BuildDictionary()
        {
            if (ModSetting.oreDictionary == null || ModSetting.oreDictionary.Count <= 0)
            {
                OreDictionary.Build(false);
                AddExteraDrillable();
                oreAmountBuffers.Clear();
            }
        }

        private void AddExteraDrillable()
        {
            List<ThingDef> extraThingList = new List<ThingDef>();
            foreach (ThingDef def in ModSetting.database)
            {
                if (def.building != null && (def.building.isResourceRock || def.building.isNaturalRock) && def.mineable)
                    extraThingList.Add(def);
            }

            if (extraThingList.Count > 0)
                OreDictionary.AddExtraDrillable(extraThingList);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            float y = 0f;

            y += 40f;
            Widgets.Checkbox(0f, y, ref ModSetting.EnableInsectoids, 25f, false, false, null, null);
            Widgets.Label(new Rect(35f, y + 1f, inRect.width - 50f, 25f), "MBDD_Label_EnableInsectoids".Translate());

            y += 40f;
            Widgets.Checkbox(0f, y, ref ModSetting.EnableMechdroids, 25f, false, false, null, null);
            Widgets.Label(new Rect(35f, y + 1f, inRect.width - 50f, 25f), "MBDD_Label_EnableMechdroids".Translate());

            y += 80f;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, y, inRect.width - 50f, 30f), "MBDD_Label_OreListedDisplay".Translate());

            y += 40f;
            Text.Font = GameFont.Small;
            bool clicked = Widgets.ButtonText(new Rect(0f, y, 290f, 25f), "MBDD_ButtonText_ReBuildOreDictionary".Translate());
            if (clicked)
            {
                OreDictionary.Build(true);
                AddExteraDrillable();
                oreAmountBuffers.Clear();
            }

            List<DrillableOre> oreDictionary = ModSetting.oreDictionary;
            if (oreDictionary != null && oreDictionary.Count > 0)
            {
                y += 40f;
                Rect outRect = new Rect(0f, y, 310f, 300f);
                Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, oreDictionary.Count * 32f);

                Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect, true);
                float rowY = 0f;

                for (int i = 0; i < oreDictionary.Count; i++)
                {
                    DrillableOre ore = oreDictionary[i];
                    if (ore?.OreDef == null)
                        continue;

                    Rect rectRow = new Rect(0f, rowY, viewRect.width, 32f);
                    Rect rectOreIcon = GenUI.LeftPartPixels(rectRow, 32f);
                    Rect rectOreLabel = new Rect(rectRow.x + 35f, rectRow.y + 5f, rectRow.width - 32f, rectRow.height);
                    Rect rectDeepCountPerPortion = new Rect(rectOreLabel.x + 185f, rectRow.y, 65f, rectRow.height);

                    Widgets.ThingIcon(rectOreIcon, ore.OreDef, null, null, 1f, null, null);
                    Widgets.Label(rectOreLabel, ore.OreDef.LabelCap);

                    string buffer = GetOreAmountBuffer(ore);
                    Widgets.TextFieldNumeric(rectDeepCountPerPortion, ref ore.amountPerPortion, ref buffer);
                    oreAmountBuffers[ore.OreDef.defName] = buffer;

                    if (Mouse.IsOver(rectRow))
                        Widgets.DrawHighlight(rectRow);
                    TooltipHandler.TipRegion(rectRow, ore.OreDef.description);

                    rowY += 32f;
                }

                Widgets.EndScrollView();
            }
        }

        private string GetOreAmountBuffer(DrillableOre ore)
        {
            if (ore?.OreDef == null)
                return "0";

            if (!oreAmountBuffers.TryGetValue(ore.OreDef.defName, out string buffer) || string.IsNullOrEmpty(buffer))
            {
                buffer = ore.amountPerPortion.ToString();
                oreAmountBuffers[ore.OreDef.defName] = buffer;
            }

            return buffer;
        }
    }
}
