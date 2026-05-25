using System.Reflection;
using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Enums;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    public class OnShotWeaponPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var targetType = typeof(LocationStatisticsCollectorAbstractClass);
            return targetType?.GetMethod(
                "OnShot",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
        }

        [PatchPrefix]
        static bool Prefix(Weapon weapon, AmmoItemClass ammo)
        {
            if (!Settings.Instance.EnableSendData.Value)
                return true;
            PlayerHelper.LastActionState = ActionState.FIRED;
            return true;
        }
    }
}

