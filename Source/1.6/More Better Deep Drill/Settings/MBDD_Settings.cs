using MoreBetterDeepDrill.Types;
using System;
using System.Collections.Generic;
using Verse;

namespace MoreBetterDeepDrill.Settings
{
    /// <summary>
    /// Mod 设置数据。持久化矿石字典、虫灾/机械族开关。
    /// </summary>
    public class MBDD_Settings : ModSettings
    {
        /// <summary>可深钻矿石列表（构建后缓存）</summary>
        public List<DrillableOre> oreDictionary = null;
        /// <summary>是否启用虫灾事件</summary>
        public bool EnableInsectoids = true;
        /// <summary>是否允许机械体操作钻井</summary>
        public bool EnableMechdroids = false;
        /// <summary>钻机总挖掘力上限（所有在岗 pawn 深钻速度之和的封顶值）</summary>
        public float MaxDrillPower = 3f;

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.EnableInsectoids, "MBDD_EnableInsectoids", true, false);
            Scribe_Values.Look<bool>(ref this.EnableMechdroids, "MBDD_EnableMechdroids", false, false);
            Scribe_Values.Look<float>(ref this.MaxDrillPower, "MBDD_MaxDrillPower", 3f, false);
            Scribe_Collections.Look<DrillableOre>(ref oreDictionary, "MBDD_OreDictionary", LookMode.Deep, Array.Empty<object>());

            if (oreDictionary == null)
                oreDictionary = new List<DrillableOre>();

            CheckValid(oreDictionary);
        }

        /// <summary>
        /// 清除矿石字典中的无效条目（null 或 def 丢失）。
        /// 从尾部向前遍历以避免 RemoveAt 索引偏移。
        /// </summary>
        private static void CheckValid(List<DrillableOre> oreDict)
        {
            if (oreDict == null)
                return;

            for (int i = oreDict.Count - 1; i >= 0; i--)
            {
                if (oreDict[i] == null || oreDict[i].OreDef == null)
                    oreDict.RemoveAt(i);
            }
        }
    }
}
