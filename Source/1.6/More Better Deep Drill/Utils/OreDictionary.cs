using MoreBetterDeepDrill.Types;
using System;
using System.Collections.Generic;
using Verse;

namespace MoreBetterDeepDrill.Utils
{
    /// <summary>
    /// Static ore dictionary.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class OreDictionary
    {
        private static Predicate<ThingDef> validOre;

        public static Dictionary<ThingDef, DrillableOre> DrillableOreDict;

        static OreDictionary()
        {
            validOre = (ThingDef def) => def.deepCommonality > 0;
        }

        /// <summary>
        /// Build the ore dictionary.
        /// </summary>
        /// <param name="rebuild">Whether to rebuild from scratch.</param>
        public static void Build(bool rebuild = false)
        {
            List<DrillableOre> list = (rebuild || GenList.NullOrEmpty<DrillableOre>(StaticValues.ModSetting.oreDictionary))
                ? new List<DrillableOre>()
                : StaticValues.ModSetting.oreDictionary;

            HashSet<ThingDef> existingOreDefs = new HashSet<ThingDef>();
            for (int i = 0; i < list.Count; i++)
            {
                DrillableOre existingOre = list[i];
                if (existingOre?.OreDef != null)
                    existingOreDefs.Add(existingOre.OreDef);
            }

            foreach (ThingDef ore in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (!validOre(ore) || !existingOreDefs.Add(ore))
                    continue;

                LogUtil.LogNormal($"[MoreBetterDeepDrill]: DefName:[{ore.defName}] was added to the OreDict.");
                list.Add(new DrillableOre(ore, ore.deepCountPerPortion));
            }

            StaticValues.ModSetting.oreDictionary = list;
        }

        /// <summary>
        /// Remove invalid entries from the saved list.
        /// </summary>
        public static void Refresh()
        {
            var oreDict = StaticValues.ModSetting.oreDictionary;
            oreDict?.RemoveAll(ore => ore == null || ore.OreDef == null);
        }

        /// <summary>
        /// Add extra drillable defs.
        /// </summary>
        /// <param name="defs"></param>
        public static void AddExtraDrillable(List<ThingDef> defs)
        {
            var dict = StaticValues.ModSetting.oreDictionary;
            if (dict == null)
                return;

            HashSet<ThingDef> existingOreDefs = new HashSet<ThingDef>();
            foreach (DrillableOre exist in dict)
            {
                if (exist?.OreDef != null)
                    existingOreDefs.Add(exist.OreDef);
            }

            foreach (ThingDef target in defs)
            {
                ThingDef tempOreDef;
                int amount = 1;

                if (target.building != null)
                {
                    tempOreDef = target.building.mineableThing;
                    amount = target.building.mineableYield;
                }
                else
                {
                    tempOreDef = target;
                }

                // SolidIce has no yield and should not be added as a drill target.
                if (tempOreDef == null || !existingOreDefs.Add(tempOreDef))
                    continue;

                LogUtil.LogNormal($"[MoreBetterDeepDrill]: DefName:[{tempOreDef.defName}] was added to the OreDict.");
                dict.Add(new DrillableOre(tempOreDef, amount));
            }
        }
    }
}
