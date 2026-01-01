using System.Reflection;
using EFT;
using EFT.UI;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Enums;
using SPTLeaderboard.Services;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    internal class OpenMainMenuScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(MenuScreen).GetMethod(
                "Show",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[]
                {
                    typeof(Profile), 
                    typeof(MatchmakerPlayerControllerClass),
                    typeof(ESessionMode)
                },
                null
            );

        [PatchPrefix]
        static bool Prefix()
        {
            if (!LeaderboardPlugin.Instance.cachedPlayerModelPreview)
            {
                LeaderboardPlugin.Instance.CacheFullBodyPlayerModelView();
            }
            
            if (!LeaderboardPlugin.Instance.engLocaleLoaded)
            { 
                bool hasEnLocale = LocaleManagerClass.LocaleManagerClass.Dictionary_4.TryGetValue("en", out _);
                if (!hasEnLocale)
                {
                    _ = LocalizationService.Instance.LoadEnglishLocaleAsync();
                }
                else
                {
                    if (LocalizationService.GetLocaleName("5ea03f7400685063ec28bfa8 ShortName") != "Unknown")
                    {
                        LeaderboardPlugin.Instance.engLocaleLoaded = true;
                    }
                    else
                    {
                        _ = LocalizationService.Instance.LoadEnglishLocaleAsync();
                    }
                }
            }

            if (!LeaderboardPlugin.Instance.configUpdated)
            {
                ConfigUpdater.UpdateEquipmentLimits();
            }
            
            if (!Settings.Instance.EnableSendData.Value)
                return true;
            
            if (!PlayerHelper.HasRaidStarted())
            {
                HeartbeatSender.Send(PlayerState.IN_MENU);
                Utils.Logger.LogDebugWarning("[State] Player opened MainMenu screen");
            }
            
            return true;
        }
    }
}