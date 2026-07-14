using HarmonyLib;
using System.Reflection;
using Verse;

namespace MoreBetterDeepDrill.Patch
{
    /// <summary>
    /// Harmony Patch 入口。自动扫描并应用当前程序集中所有 [HarmonyPatch] 标注。
    /// </summary>
    [StaticConstructorOnStartup]
    public static class PatchMain
    {
        public static Harmony instance;

        static PatchMain()
        {
            instance = new Harmony("MoreBetterDeepDrill.Patch");
            instance.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("MoreBetterDeepDrill Patched");
        }
    }
}
