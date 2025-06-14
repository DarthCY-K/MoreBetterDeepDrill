using MoreBetterDeepDrill.Types;
using System;
using System.Collections.Generic;
using Verse;

namespace MoreBetterDeepDrill.Settings
{
    public class MBDD_Settings : ModSettings
    {
        public List<DrillableOre> oreDictionary = null;
        public List<ThingDef> database;

        public bool EnableInsectoids = true;
        public bool EnableMechdroids = false;

        public override void ExposeData()
        {
            Scribe_Values.Look<bool>(ref this.EnableInsectoids, "MBDD_EnableInsectoids", true, false);
            Scribe_Values.Look<bool>(ref this.EnableMechdroids, "MBDD_EnableMechdroids", false, false);
            Scribe_Collections.Look<DrillableOre>(ref oreDictionary, "MBDD_OreDictionary", LookMode.Deep, Array.Empty<object>());

            CheckVaild(ref oreDictionary);
        }

        /// <summary>
        /// 检查并清理无效数据
        /// </summary>
        /// <param name="oreDict"></param>
        private void CheckVaild(ref List<DrillableOre> oreDict)
        {
            for (int i = oreDict.Count - 1; i >= 0; i--)
            {
                if (oreDict[i].OreDef == null)
                    oreDict.Remove(oreDict[i]);
            }
        }
    }
}