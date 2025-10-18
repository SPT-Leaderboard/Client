using System.Collections.Generic;
using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using SPTLeaderboard.Enums;
using SPTLeaderboard.Models;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    internal class OnEndRaidPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(Class308).GetMethod(
                "LocalRaidEnded",
                BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        static bool Prefix(LocalRaidSettings settings, RaidEndDescriptorClass results, FlatItemsDataClass[] lostInsuredItems, Dictionary<string, FlatItemsDataClass[]> transferItems, object __instance)
        {
            if (!SettingsModel.Instance.EnableSendData.Value)
                return true;

            LeaderboardPlugin.Instance.StopInRaidHeartbeat();
            ProcessProfileModel.Create().ProcessAndSendProfile(settings, results);
            
            HeartbeatSender.Send(results.result == ExitStatus.Transit ? PlayerState.IN_TRANSIT : PlayerState.RAID_END);
            
            LeaderboardPlugin.logger.LogWarning("[State] Player ended raid");
            return true;
        }
    }
}