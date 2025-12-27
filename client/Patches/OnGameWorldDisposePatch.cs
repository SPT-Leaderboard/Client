using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using SPTLeaderboard.Utils;
using SPTLeaderboard.Utils.Zones;
using UnityEngine;

namespace SPTLeaderboard.Patches
{
    public class OnGameWorldDisposePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(GameWorld).GetMethod(
                "Dispose",
                BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        static void Prefix()
        {
#if DEBUG
            OverlayDebug.Instance.Disable();
            
            if (LeaderboardPlugin.Instance.ZoneTracker)
            {
                Object.Destroy(LeaderboardPlugin.Instance.ZoneTracker.gameObject);
                LeaderboardPlugin.Instance.ZoneTracker = null;
            }
            
            if (LeaderboardPlugin.Instance.ZoneInterface)
            {
                Object.Destroy(LeaderboardPlugin.Instance.ZoneInterface.gameObject);
                LeaderboardPlugin.Instance.ZoneInterface = null;
            }
#endif
            Utils.Logger.LogDebugWarning("Player dispose world");
        }
    }
}