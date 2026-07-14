using Verse;

namespace MoreBetterDeepDrill.Utils
{
    /// <summary>
    /// 日志工具。提供 GodMode 过滤的日志输出。
    /// 默认 requireGodMode=true，仅在开发者模式下输出，避免刷屏。
    /// </summary>
    public static class LogUtil
    {
        /// <summary>判断是否应输出日志。false = 始终输出，true = 仅 GodMode</summary>
        private static bool ShouldLog(bool requireGodMode)
        {
            return !requireGodMode || DebugSettings.godMode;
        }

        /// <summary>普通日志</summary>
        public static void LogNormal(string msg, bool requireGodMode = true)
        {
            if (ShouldLog(requireGodMode))
                Log.Message(msg);
        }

        /// <summary>警告日志</summary>
        public static void LogWarning(string msg, bool requireGodMode = true)
        {
            if (ShouldLog(requireGodMode))
                Log.Warning(msg);
        }

        /// <summary>错误日志</summary>
        public static void LogError(string msg, bool requireGodMode = true)
        {
            if (ShouldLog(requireGodMode))
                Log.Error(msg);
        }
    }
}
