using System.Reflection;
using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPTLeaderboard.Data;

namespace SPTLeaderboard.Patches
{
    internal class RaidSettingsHookPatch : ModulePatch
    {
        private static FieldInfo _raidSettingsField;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RaidSettingsWindow), nameof(RaidSettingsWindow.UpdateRaidSettings));
        }

        [PatchPostfix]
        public static void PatchPostfix(RaidSettingsWindow __instance)
        {
            _raidSettingsField ??= AccessTools.Field(typeof(RaidSettingsWindow), "_raidSettings");
            
            var raidSettings = (RaidSettings)_raidSettingsField.GetValue(__instance);

            LeaderboardPlugin.Instance.SavedRaidSettingsData = new RaidSettingsData
            {
                BotAmount = raidSettings.BotSettings.BotAmount.ToString(),
                BotDifficulty = raidSettings.WavesSettings.BotDifficulty.ToString(),
                BossesEnabled = raidSettings.WavesSettings.IsBosses,
                BotsEnabled = raidSettings.BotSettings.IsEnabled,
                MetabolismDisabled = raidSettings.MetabolismDisabled,
                FikaCustomRaidSettings = new FikaCustomRaidSettingsPayload()
            };
        }
    }
}
