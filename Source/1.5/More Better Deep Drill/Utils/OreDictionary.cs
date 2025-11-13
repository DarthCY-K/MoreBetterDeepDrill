using MoreBetterDeepDrill.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MoreBetterDeepDrill.Utils
{
    /// <summary>
    /// 矿物辞典静态类
    /// </summary>
    [StaticConstructorOnStartup]
    public static class OreDictionary
    {
        private static Predicate<ThingDef> validOre;

        public static Dictionary<ThingDef, DrillableOre> DrillableOreDict;

        static OreDictionary()
        {
            validOre = ((ThingDef def) => def.deepCommonality > 0);
        }
        
        /// <summary>
        /// 建立矿物辞典
        /// </summary>
        /// <param name="rebuild">重建</param>
        public static void Build(bool rebuild = false)
        {
            List<DrillableOre> list = (rebuild || GenList.NullOrEmpty<DrillableOre>(StaticValues.ModSetting.oreDictionary)) ? new List<DrillableOre>() : StaticValues.ModSetting.oreDictionary;
            IEnumerable<ThingDef> validOres = from def in DefDatabase<ThingDef>.AllDefs
                                              where OreDictionary.validOre(def)
                                              select def;

            foreach (ThingDef ore in validOres)
            {
                bool shouldAdd = rebuild || GenList.NullOrEmpty<DrillableOre>(list) || !GenCollection.Any<DrillableOre>(list, (DrillableOre x) => ore == x.OreDef);
                if (shouldAdd)
                    list.Add(new DrillableOre(ore, ore.deepCountPerPortion));
            }

            StaticValues.ModSetting.oreDictionary = list;
        }

        /// <summary>
        /// 刷新并清理list内错误对象
        /// </summary>
        public static void Refresh()
        {
            var oreDict = StaticValues.ModSetting.oreDictionary;
            oreDict?.RemoveAll(ore => ore == null);
        }

        /// <summary>
        /// 追加可挖掘对象
        /// </summary>
        /// <param name="defs"></param>
        public static void AddExtraDrillable(List<ThingDef> defs)
        {
            var dict = StaticValues.ModSetting.oreDictionary;
            if (dict == null)
                return;

            // 使用HashSet快速查找已存在的矿物
            HashSet<ThingDef> existingOreDefs = new HashSet<ThingDef>();
            foreach (var exist in dict)
            {
                if (exist.OreDef != null)
                    existingOreDefs.Add(exist.OreDef);
            }

            // 添加新的可挖掘对象
            foreach (var target in defs)
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

                // 不重复就添加
                if (!existingOreDefs.Contains(tempOreDef))
                {
                    dict.Add(new DrillableOre(tempOreDef, amount));
                    existingOreDefs.Add(tempOreDef);
                }
            }
        }
    }
}