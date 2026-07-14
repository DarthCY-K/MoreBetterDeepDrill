using MoreBetterDeepDrill.Types;
using System.Collections.Generic;
using Verse;

namespace MoreBetterDeepDrill.Utils
{
    /// <summary>自动同步所有可钻矿物，同时保留玩家已经配置的产量。</summary>
    public static class OreDictionary
    {
        public static bool Synchronize()
        {
            var settings = StaticValues.ModSetting;
            if (settings == null)
                return false;

            List<DrillableOre> current = settings.oreDictionary ?? new List<DrillableOre>();
            Dictionary<ThingDef, DrillableOre> existing = new Dictionary<ThingDef, DrillableOre>();
            for (int i = 0; i < current.Count; i++)
            {
                DrillableOre ore = current[i];
                if (ore?.OreDef != null && !existing.ContainsKey(ore.OreDef))
                    existing.Add(ore.OreDef, ore);
            }

            List<DrillableOre> synchronized = new List<DrillableOre>();
            HashSet<ThingDef> added = new HashSet<ThingDef>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.deepCommonality > 0f)
                    AddOre(synchronized, added, existing, def, def.deepCountPerPortion);
            }
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.building != null && def.mineable
                    && (def.building.isResourceRock || def.building.isNaturalRock)
                    && def.building.mineableThing != null && def.building.mineableYield > 0)
                    AddOre(synchronized, added, existing, def.building.mineableThing, def.building.mineableYield);
            }

            synchronized.Sort((left, right) => string.Compare(left.OreDef.label, right.OreDef.label, System.StringComparison.CurrentCultureIgnoreCase));
            bool changed = current.Count != synchronized.Count;
            if (!changed)
            {
                for (int i = 0; i < current.Count; i++)
                {
                    if (current[i]?.OreDef != synchronized[i].OreDef)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
                settings.oreDictionary = synchronized;
            return changed;
        }

        public static void ResetToDefaults()
        {
            var settings = StaticValues.ModSetting;
            if (settings == null)
                return;

            List<DrillableOre> defaults = new List<DrillableOre>();
            HashSet<ThingDef> added = new HashSet<ThingDef>();
            Dictionary<ThingDef, DrillableOre> none = new Dictionary<ThingDef, DrillableOre>();
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.deepCommonality > 0f)
                    AddOre(defaults, added, none, def, def.deepCountPerPortion);
            }
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.building != null && def.mineable
                    && (def.building.isResourceRock || def.building.isNaturalRock)
                    && def.building.mineableThing != null && def.building.mineableYield > 0)
                    AddOre(defaults, added, none, def.building.mineableThing, def.building.mineableYield);
            }
            defaults.Sort((left, right) => string.Compare(left.OreDef.label, right.OreDef.label, System.StringComparison.CurrentCultureIgnoreCase));
            settings.oreDictionary = defaults;
        }

        private static void AddOre(List<DrillableOre> target, HashSet<ThingDef> added,
            Dictionary<ThingDef, DrillableOre> existing, ThingDef oreDef, int defaultAmount)
        {
            if (!added.Add(oreDef))
                return;

            if (existing.TryGetValue(oreDef, out DrillableOre saved))
                target.Add(saved);
            else
                target.Add(new DrillableOre(oreDef, defaultAmount));
        }
    }
}
