using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using SPTLeaderboard.Services;
using SPTLeaderboard.Utils;
using SPTLeaderboard.Utils.Zones;
using UnityEngine;

namespace SPTLeaderboard.Patches
{
    public class OnGameWorldStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(GameWorld).GetMethod(
                "OnGameStarted",
                BindingFlags.Instance | BindingFlags.Public);

        [PatchPostfix]
        static void PostFix()
        {
#if DEBUG
            OverlayDebug.Instance.Enable();

            if (!LeaderboardPlugin.Instance.ZoneTrackerService)
            {
                var zonesTrackerObj = new GameObject("[SPTLeaderboard] ZonesTracker");
                Object.DontDestroyOnLoad(zonesTrackerObj);
                LeaderboardPlugin.Instance.ZoneTrackerService = zonesTrackerObj.AddComponent<ZoneTrackerService>();
                LeaderboardPlugin.Instance.ZoneTrackerService.Enable();
            }
            else
            {
                LeaderboardPlugin.Instance.ZoneTrackerService.Enable();
            }
            
            var zonesInterfaceObj = new GameObject("[SPTLeaderboard] ZonesInterface");
            Object.DontDestroyOnLoad(zonesInterfaceObj);
            LeaderboardPlugin.Instance.ZoneInterface = zonesInterfaceObj.AddComponent<ZoneInterface>();
#endif
            Utils.Logger.LogDebugWarning("Player started world");
        }
    }
}