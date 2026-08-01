using System.Reflection;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Enums;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    internal class OpenInventoryScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(InventoryScreen).GetMethod(
                "Show",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                [
                    typeof(IHealthController),
                    typeof(InventoryController),
                    typeof(EFT.Quests.QuestController),
                    typeof(EFT.Achievements.AchievementsController),
                    typeof(EFT.Prestige.PrestigeController),
                    typeof(CompoundItem),
                    typeof(EInventoryTab),
                    typeof(EFT.IEftSession),
                    typeof(ItemContext),
                    typeof(bool)
                ],
                null
            );

        [PatchPrefix]
        static bool Prefix()
        {
            if (!Settings.Instance.EnableSendData.Value)
                return true;
            
            if (!PlayerHelper.HasRaidStarted())
            {
                HeartbeatSender.Send(PlayerState.IN_STASH);
                Utils.Logger.LogDebugWarning("[State] Player opened Inventory screen");
            }

            return true;
        }
    }
}
