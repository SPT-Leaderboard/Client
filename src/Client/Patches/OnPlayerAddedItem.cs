using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    public class OnPlayerAddedItem: ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(Player).GetInterfaceMap(typeof(IAddHandler)).TargetMethods[0];
    
        [PatchPostfix]
        static void Postfix(object __instance, AddItemEventArgs eventArgs)
        {
            if (ReferenceEquals(__instance, PlayerHelper.Instance.Player))
            {
                foreach (var item in eventArgs.Item.GetAllItems())
                {
                    LeaderboardPlugin.Instance.TrackingLoot.Add(item);
                }
            }
        }

    }
}
