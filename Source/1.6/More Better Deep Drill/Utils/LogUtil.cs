using Verse;

namespace MoreBetterDeepDrill.Utils
{
    public static class LogUtil
    {
        private static bool ShouldLog(bool requireGodMode)
        {
            return !requireGodMode || DebugSettings.godMode;
        }

        public static void LogNormal(string msg, bool requireGodMode = true)
        {
            if (ShouldLog(requireGodMode))
                Log.Message(msg);
        }

        public static void LogWarning(string msg, bool requireGodMode = true)
        {
            if (ShouldLog(requireGodMode))
                Log.Warning(msg);
        }

        public static void LogError(string msg, bool requireGodMode = true)
        {
            if (ShouldLog(requireGodMode))
                Log.Error(msg);
        }
    }
}
