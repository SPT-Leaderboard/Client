using System.Reflection;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;

namespace SPTLeaderboard.Patches
{
    internal class OpenStashPanelShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ItemsPanel).GetMethod(
                "Show",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                CallingConventions.Any,
                [
                    typeof(ItemContext),                 // sourceContext
                    typeof(CompoundItem),                // lootItem
                    typeof(IEftSession),                 // session
                    typeof(InventoryController),         // inventoryController
                    typeof(IHealthController),           // health
                    typeof(Profile),                     // profile
                    typeof(EFT.UI.Insurance.InsuranceCompany), // insurance
                    typeof(EFT.UI.Builds.EquipmentBuildsStorage), // buildsStorage
                    typeof(ItemsPanel.EItemsTab),        // currentTab
                    typeof(bool),                        // inRaid
                    typeof(SortingTable),                // sortingTable
                    typeof(SimpleStashPanel.EStashSearchAvailability), // searchAvailability
                    typeof(bool),                        // isInventoryBlocked
                    typeof(InventoryEquipment)           // equipment (nullable, но тип обязателен)
                ],
                null
            );
        }

        [PatchPostfix]
        static void Postfix(
            ItemsPanel __instance,
            ItemContext sourceContext,
            CompoundItem lootItem,
            IEftSession session,
            InventoryController inventoryController,
            bool inRaid)
        {
            if (lootItem != null)
            {
                LeaderboardPlugin.Instance.ZoneTrackerService?.OnStashOpened(lootItem);
            }
        }
    }
}
