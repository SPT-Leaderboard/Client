using System.Reflection;
using EFT.UI;
using SPT.Reflection.Patching;
using SPTLeaderboard.Models;

namespace SPTLeaderboard.Patches
{
    internal class ConsoleScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ConsoleScreen).GetMethod(
                "TryCommand",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(string) },
                null
            );
        }

        [PatchPrefix]
        static bool Prefix(string input)
        {
            if (!SettingsModel.Instance.EnableSendData.Value)
                return true;

            if (!input.Contains("debug t") || !input.Contains("fps"))
            {
                Utils.Logger.LogDebugWarning($"[Console] Player executed suspicious command: {input.Trim()}");
                LeaderboardPlugin.Instance.IsExecutedSuspiciousCommand = true;
            }

            return true;
        }
    }
}