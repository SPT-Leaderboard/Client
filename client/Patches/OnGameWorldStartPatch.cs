using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
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

            if (!LeaderboardPlugin.Instance.ZoneTracker)
            {
                var zonesTrackerObj = new GameObject("[SPTLeaderboard] ZonesTracker");
                Object.DontDestroyOnLoad(zonesTrackerObj);
                LeaderboardPlugin.Instance.ZoneTracker = zonesTrackerObj.AddComponent<ZoneTracker>();
                LeaderboardPlugin.Instance.ZoneTracker.Enable();
            }
            else
            {
                LeaderboardPlugin.Instance.ZoneTracker.Enable();
            }
            
            var zonesInterfaceObj = new GameObject("[SPTLeaderboard] ZonesInterface");
            Object.DontDestroyOnLoad(zonesInterfaceObj);
            LeaderboardPlugin.Instance.ZoneInterface = zonesInterfaceObj.AddComponent<ZoneInterface>();
#endif
            Utils.Logger.LogDebugWarning("Player started world");
        }
    }
}