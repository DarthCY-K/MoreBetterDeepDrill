using MoreBetterDeepDrill.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static UnityEngine.GraphicsBuffer;

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

        /// <summary>
        /// 追加可挖掘对象
        /// </summary>
        /// <param name="defs"></param>
        public static void AddExtraDrillable(List<ThingDef> defs)
        {
            var dumplicate = false;
            var dict = StaticValues.ModSetting.oreDictionary;
            ThingDef tempOreDef;
            List<DrillableOre> newDrillableList = new List<DrillableOre>();

            if (dict != null)
            {
                foreach(var target in defs)
                {
                    var amount = 1;
                    dumplicate = false;

                    if (target.building != null)
                    {
                        tempOreDef = target.building.mineableThing;
                        amount = target.building.mineableYield;
                    }
                    else
                    {
                        tempOreDef = target;
                    }

                    //检查现在的列表
                    foreach (var exist in dict)
                    {
                        if (exist.OreDef != null && exist.OreDef == tempOreDef)
                        {
                            dumplicate = true;
                            break;
                        }
                    }

                    //不重复就添加，重复的话就中止添加
                    if (!dumplicate)
                        newDrillableList.Add(new DrillableOre(tempOreDef, amount));
                    else
                        continue;
                }

                //添加新开采物
                foreach(var newOre in newDrillableList)
                {
                    dict.Add(newOre);
                }
            }
        }
    }
}