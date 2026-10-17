using System;
using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Integrations;
using SPTLeaderboard.Utils;
using UnityEngine;

namespace SPTLeaderboard.Patches
{
    internal class OnStartRaidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(EftClientBackendSession).GetMethod(
                "LocalRaidStarted",
                BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        static bool Prefix(LocalRaidSettings settings)
        {
            LeaderboardPlugin.Instance.SainPresetName = string.Empty;

            if (!Settings.Instance.EnableSendData.Value)
                return true;
            
            HitsTracker.Instance.Clear();

            LeaderboardPlugin.Instance.HasOtherFikaPlayers = false;

            LeaderboardPlugin.Instance.TrackingLoot.OnStartRaid(settings.playerSide);
            
            LeaderboardPlugin.Instance.CreateIconPlayer();
            LeaderboardPlugin.Instance.StartInRaidHeartbeat();
            Utils.Logger.LogDebugWarning("[State] Player started raid");
            PlayerHelper.Instance.LastDeathPosition = Vector3.zero;
            return true;
        }
    }

    internal class OnRaidGameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Instance | BindingFlags.Public);

        [PatchPostfix]
        static void Postfix()
        {
            LeaderboardPlugin.Instance.SainPresetName = SainInterop.CaptureRaidPreset();

            if (!Settings.Instance.EnableSendData.Value)
                return;

            try
            {
                // Check once when the raid starts, not while players are still loading.
                LeaderboardPlugin.Instance.HasOtherFikaPlayers =
                    FikaInterop.TryGetHasOtherHumanPlayers(out var hasOtherHumanPlayers) && hasOtherHumanPlayers;
            }
            catch (Exception ex)
            {
                Utils.Logger.LogWarning($"[Fika] Failed to read human players: {ex}");
            }
        }
    }
}
