using HarmonyLib;
using System.Reflection;
using Verse;

namespace MBDD_PRF_Support.Patch
{
    /// <summary>
    /// 补丁主类
    /// </summary>
    [StaticConstructorOnStartup]
    public class PatchMain
    {
        /// <summary>
        /// 补丁实例
        /// </summary>
        public static Harmony instance;

        static PatchMain()
        {
            instance = new Harmony("MBDD_PRF_Support.Patch");
            //对所有特性标签的方法进行patch
            instance.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("MBDD_PRF_Support Patched");
        }
    }
}
