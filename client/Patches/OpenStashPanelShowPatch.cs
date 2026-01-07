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
                    typeof(ItemContextAbstractClass),    // sourceContext
                    typeof(CompoundItem),                // lootItem
                    typeof(ISession),                    // session
                    typeof(InventoryController),         // inventoryController
                    typeof(IHealthController),           // health
                    typeof(Profile),                     // profile
                    typeof(InsuranceCompanyClass),       // insurance
                    typeof(EquipmentBuildsStorageClass), // buildsStorage
                    typeof(ItemsPanel.EItemsTab),        // currentTab
                    typeof(bool),                        // inRaid
                    typeof(SortingTableItemClass),       // sortingTable
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
            ItemContextAbstractClass sourceContext,
            CompoundItem lootItem,
            ISession session,
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
