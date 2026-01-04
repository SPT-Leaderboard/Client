using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;

namespace SPTLeaderboard.Patches
{
    internal class OpenStashPanelShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(SimpleStashPanel).GetMethod(
                "Show",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                [
                    typeof(CompoundItem),
                    typeof(InventoryController),
                    typeof(ItemContextAbstractClass),
                    typeof(bool),
                    typeof(SortingTableItemClass),
                    typeof(SimpleStashPanel.EStashSearchAvailability),
                    typeof(InventoryController),
                    typeof(ItemsPanel.EItemsTab)
                ],
                null
            );

        [PatchPrefix]
        static bool Prefix(CompoundItem item)
        {
            if (item == null) return true;
            
            LeaderboardPlugin.Instance.ZoneTrackerService.OnStashOpened(item);
            
            return true;
        }
    }
}

