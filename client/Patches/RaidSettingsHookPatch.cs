using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPTLeaderboard.Data;
using SPTLeaderboard.Integrations;

namespace SPTLeaderboard.Patches
{
    internal class RaidSettingsHookPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Class308), "SendRaidSettings");
        }

        [PatchPostfix]
        public static void Postfix(RaidSettings settings)
        {
            if (settings == null)
                return;

            var saved = new RaidSettingsData
            {
                BotAmount = settings.WavesSettings.BotAmount.ToString(),
                BotDifficulty = settings.WavesSettings.BotDifficulty.ToString(),
                BossesEnabled = settings.WavesSettings.IsBosses,
                BotsEnabled = settings.BotSettings.IsEnabled,
                MetabolismDisabled = settings.MetabolismDisabled
            };

            if (FikaInterop.TryGetCustomRaidSettings(out var fikaCustom))
                saved.FikaCustomRaidSettings = fikaCustom;

            LeaderboardPlugin.Instance.SavedRaidSettingsData = saved;
        }
    }
}