using System.Reflection;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Enums;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    internal class HideoutAwakePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutScreenOverlay), nameof(HideoutScreenOverlay.Show));
        }

        [PatchPostfix]
        private static void Postfix()
        {
            if (!Settings.Instance.EnableSendData.Value)
                return;
            
            HeartbeatSender.Send(PlayerState.IN_HIDEOUT);
            Utils.Logger.LogDebugWarning("[State] Player entered in hideout");
        }
    }
}