using System.Reflection;
using EFT.UI;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Services;
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
            if (!Settings.Instance.EnableSendData.Value)
                return true;

            if (!input.Contains("debug t") || !input.Contains("fps"))
            {
                Utils.Logger.LogDebugWarning($"[Console] Player executed suspicious command: {input.Trim()}");
                string notificationMessage = LocalizationService.Instance.GetLocaleErrorText(ErrorType.CONSOLE_CHEAT_DETECTED);
                LocalizationService.NotificationWarning(notificationMessage);
                LeaderboardPlugin.Instance.IsExecutedSuspiciousCommand = true;
            }

            return true;
        }
    }
}