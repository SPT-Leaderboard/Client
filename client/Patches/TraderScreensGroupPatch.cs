using System.Reflection;
using EFT.UI;
using SPT.Reflection.Patching;
using SPTLeaderboard.Enums;
using SPTLeaderboard.Models;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    internal class TraderScreensGroupPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TraderScreensGroup).GetMethod(
                "Show",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[]
                {
                    typeof(TraderScreensGroup.GClass3888)
                },
                null
            );
        }

        [PatchPrefix]
        static bool Prefix()
        {
            if (!SettingsModel.Instance.EnableSendData.Value)
                return true;

            if (!PlayerHelper.HasRaidStarted())
            {
                HeartbeatSender.Send(PlayerState.IN_TRADING); 
                Utils.Logger.LogDebugWarning("[State] Player opened Trader screen");
            }

            return true;
        }
    }
}