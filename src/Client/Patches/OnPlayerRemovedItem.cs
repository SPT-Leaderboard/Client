using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    public class OnPlayerRemovedItem: ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(Player).GetInterfaceMap(typeof(IRemoveHandler)).TargetMethods[0];

        [PatchPostfix]
        static void Postfix(object __instance, RemoveItemEventArgs eventArgs)
        {
            if (ReferenceEquals(__instance, PlayerHelper.Instance.Player))
            {
                foreach (var item in eventArgs.Item.GetAllItems())
                {
                    LeaderboardPlugin.Instance.TrackingLoot.Remove(item);
                }
            }
        }
    }
}
