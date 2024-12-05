using MoreBetterDeepDrill.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MoreBetterDeepDrill.Utils
{
    [StaticConstructorOnStartup]
    public static class OreDictionary
    {
        private static Predicate<ThingDef> validOre;

        static OreDictionary()
        {
            validOre = ((ThingDef def) => def.deepCommonality > 0);
        }

        public static void Build(bool rebuild = false)
        {
            List<DrillableOre> list = (rebuild || GenList.NullOrEmpty<DrillableOre>(StaticValues.ModSetting.oreDictionary)) ? new List<DrillableOre>() : StaticValues.ModSetting.oreDictionary;
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where OreDictionary.validOre(def)
                                               select def;
            using (IEnumerator<ThingDef> enumerator = enumerable.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ThingDef ore = enumerator.Current;
                    bool flag = rebuild || GenList.NullOrEmpty<DrillableOre>(list) || !GenCollection.Any<DrillableOre>(list, (DrillableOre x) => ore == x.OreDef);
                    if (flag)
                        list.Add(new DrillableOre(ore, ore.deepCountPerPortion));
                }
            }
            StaticValues.ModSetting.oreDictionary = list;
        }

        public static void Refresh()
        {
            var oreDict = StaticValues.ModSetting.oreDictionary;
            for (int i = oreDict.Count - 1; i >= 0; i--)
            {
                if (oreDict[i] == null)
                {
                    oreDict.Remove(oreDict[i]);
                }
            }
        }
    }
}