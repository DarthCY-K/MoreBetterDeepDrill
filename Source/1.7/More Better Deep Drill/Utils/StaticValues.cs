using MoreBetterDeepDrill.Settings;
using Verse;

namespace MoreBetterDeepDrill.Utils
{
    /// <summary>
    /// 全局静态值。提供 Mod 名称翻译和设置引用。
    /// </summary>
    public static class StaticValues
    {
        /// <summary>Mod 名称（已本地化）</summary>
        public static string MoreBetterDeepDrill => "MoreBetterDeepDrill".Translate();

        /// <summary>Mod 设置实例快捷引用</summary>
        public static MBDD_Settings ModSetting => MBDD_Mod.ModSetting;
    }
}
