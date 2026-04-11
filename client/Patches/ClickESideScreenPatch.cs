using System.Reflection;
using EFT;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;

namespace SPTLeaderboard.Patches
{
    internal class ClickESideScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MatchMakerSideSelectionScreen).GetMethod(
                "method_12",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(ESideType) },
                null
            );
        }

        [PatchPrefix]
        static bool Prefix(ESideType side)
        {
            if (!Settings.Instance.EnableSendData.Value)
                return true;
            if (side == ESideType.Pmc)
            {
                LeaderboardPlugin.Instance.IsPMCSelected = true;
            }
            else
            {
                LeaderboardPlugin.Instance.IsPMCSelected = false;
            }
            Utils.Logger.LogDebugWarning($"[State] Player selected side: {side.ToString()}");

            return true;
        }
    }
}
