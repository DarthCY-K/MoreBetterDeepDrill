using Verse;

namespace MoreBetterDeepDrill.Utils
{
    public static class LogUtil
    {
        public static void LogNormal(string msg, bool requireGodMode = true)
        {
            if (requireGodMode && DebugSettings.godMode)
                Log.Message(msg);
            else
                Log.Message(msg);
        }

        public static void LogWarning(string msg, bool requireGodMode = true)
        {
            if (requireGodMode && DebugSettings.godMode)
                Log.Warning(msg);
            else
                Log.Warning(msg);
        }

        public static void LogError(string msg, bool requireGodMode = true)
        {
            if (requireGodMode && DebugSettings.godMode)
                Log.Error(msg);
            else
                Log.Error(msg);
        }
    }
}
