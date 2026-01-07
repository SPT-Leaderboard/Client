using System.Reflection;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using SPT.Reflection.Patching;

namespace SPTLeaderboard.Patches;

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
                typeof(ItemContextAbstractClass),
                typeof(CompoundItem),
                typeof(ISession),
                typeof(InventoryController),
                typeof(IHealthController),
                typeof(Profile),
                typeof(InsuranceCompanyClass),
                typeof(EquipmentBuildsStorageClass),
                typeof(ItemsPanel.EItemsTab),
                typeof(bool),
                typeof(SortingTableItemClass),
                typeof(SimpleStashPanel.EStashSearchAvailability),
                typeof(bool),
                typeof(InventoryEquipment)
            ],
            null
        );
    }

    [PatchPostfix]
    private static void Postfix(
        ItemsPanel __instance,
        ItemContextAbstractClass sourceContext,
        CompoundItem lootItem,
        ISession session,
        InventoryController inventoryController,
        bool inRaid)
    {
        if (lootItem != null) LeaderboardPlugin.Instance.ZoneTrackerService.OnStashOpened(lootItem);
    }
}