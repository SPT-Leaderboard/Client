using System.Linq;
using System.Reflection;
using EFT;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Data;
using SPTLeaderboard.Integrations;
using SPTLeaderboard.Services;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    internal class OpenAcceptMatchScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(MatchMakerAcceptScreen).GetMethod(
                "Show",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                [typeof(MatchMakerAcceptScreen.GClass3914)],
                null
            );

        [PatchPrefix]
        static bool Prefix()
        {
            Utils.Logger.LogDebugWarning("Player opened accept match screen");
            if (!Settings.Instance.EnableSendData.Value)
                return true;

            if (!Settings.Instance.ModCasualMode.Value)
            {
                PlayerHelper.GetLimitViolations(PlayerHelper.GetEquipmentData());
            }

            var modsPlayer = DataUtils.GetModsList();
            
            var session = PlayerHelper.GetSession();
            
            var pmcData = session.GetProfileBySide(ESideType.Pmc);
            
            var currentEnergy = pmcData.Health.Energy.Current;
            var currentHydration = pmcData.Health.Hydration.Current;
            var maxEnergy = pmcData.Health.Energy.Maximum;
            var maxHydration = pmcData.Health.Hydration.Maximum;
            var currentEquipment = PlayerHelper.GetAllEquipmentItems(ESideType.Pmc);
            var allItemsRaw = pmcData.Inventory.GetPlayerItems();
            var allItems = allItemsRaw.ToList();
            var haveDevItems = DataUtils.CheckDevItems(allItems);


            if (FikaInterop.FikaCore != null)
            {
                var saved = LeaderboardPlugin.Instance.SavedRaidSettingsData?.Clone() ?? new RaidSettingsData();
                if (FikaInterop.TryGetCustomRaidSettings(out var fikaCustom))
                    saved.FikaCustomRaidSettings = fikaCustom;
                
                LeaderboardPlugin.Instance.SavedRaidSettingsData = saved;
            }
            
            var preRaidData = new PreRaidData
            {
                ProfileId = PlayerHelper.GetProfile().ProfileId,
                VersionMod = GlobalData.Version,
                IsCasual = Settings.Instance.ModCasualMode.Value,
#if DEBUG
                Mods = Settings.Instance.Debug.Value ? ["DEBUGSPTLB"] : modsPlayer,
#else
                Mods = modsPlayer,
#endif
                Hash = EncryptionService.Instance.GetHashMod(),
                Token = EncryptionService.Instance.Token,
                Password = EncryptionService.Instance.Password,
                MaxHydration = maxHydration,
                MaxEnergy = maxEnergy,
                Hydration = currentHydration,
                Energy = currentEnergy,
                EquipmentItems = currentEquipment,
                IsExecutedSuspiciousCommand = LeaderboardPlugin.Instance.IsExecutedSuspiciousCommand,
                DBinInv = haveDevItems,
                RaidSettingsData = LeaderboardPlugin.Instance.SavedRaidSettingsData?.Clone() ?? new RaidSettingsData()
            };
            
            LeaderboardPlugin.SendPreRaidData(preRaidData);
            return true;
        }
    }
}
