using System.Reflection;
using EFT.Communications;
using EFT.UI;
using SPT.Reflection.Patching;
using SPTLeaderboard.Models;
using SPTLeaderboard.Utils;

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
                string notificationMessage = LocalizationModel.Instance.GetLocaleErrorText(ErrorType.CONSOLE_CHEAT_DETECTED);
                LocalizationModel.NotificationWarning(notificationMessage);
                LeaderboardPlugin.Instance.IsExecutedSuspiciousCommand = true;
            }

            return true;
        }
    }
}