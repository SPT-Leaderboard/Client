using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Utils;
using UnityEngine;

namespace SPTLeaderboard.Patches
{
    internal class OnStartRaidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(Class308).GetMethod(
                "LocalRaidStarted",
                BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        static bool Prefix(LocalRaidSettings settings)
        {
            if (!Settings.Instance.EnableSendData.Value)
                return true;
            
            HitsTracker.Instance.Clear();

            LeaderboardPlugin.Instance.TrackingLoot.OnStartRaid(settings.playerSide);
            
            LeaderboardPlugin.Instance.CreateIconPlayer();
            LeaderboardPlugin.Instance.StartInRaidHeartbeat();
            Utils.Logger.LogDebugWarning("[State] Player started raid");
            PlayerHelper.Instance.LastDeathPosition = Vector3.zero;
            return true;
        }
    }
}