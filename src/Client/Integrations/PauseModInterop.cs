using System;
using BepInEx.Bootstrap;

namespace SPTLeaderboard.Integrations
{
    public static class PauseModInterop
    {
        public static DateTime? lastPauseTime;
        public static TimeSpan totalPausedTime;
        public static bool Loaded()
        {
            bool present = Chainloader.PluginInfos.TryGetValue("com.netVnum.pause", out _);
            return present;
        }
    
        public static void OnPausePostfix()
        {
            lastPauseTime = DateTime.UtcNow;
            Utils.Logger.LogInfo("[PauseModInterop] PAUSE");
        }

        public static void OnUnpausePostfix()
        {
            if (lastPauseTime.HasValue)
            {
                totalPausedTime += DateTime.UtcNow - lastPauseTime.Value;
                Utils.Logger.LogInfo("[PauseModInterop] UNPAUSE");
                lastPauseTime = null;
            }
        }
    
        public static void OnRaidStart()
        {
            totalPausedTime = TimeSpan.Zero;
            lastPauseTime = null;
            Utils.Logger.LogInfo("[PauseModInterop] totalPausedTime reset");
        }
    }
}