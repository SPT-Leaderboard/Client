using System.Reflection;
using EFT.InventoryLogic;
using EFT.UI.WeaponModding;
using SPT.Reflection.Patching;
using SPTLeaderboard.Enums;
using SPTLeaderboard.Models;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    internal class WeaponModdingScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(WeaponModdingScreen).GetMethod(
                "Show",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[]
                {
                    typeof(Item),
                    typeof(InventoryController),
                    typeof(CompoundItem[])
                },
                null
            );
        }

        [PatchPrefix]
        static bool Prefix()
        {
            if (!SettingsModel.Instance.EnableSendData.Value)
                return true;

            if (!PlayerHelper.HasRaidStarted())
            {
                HeartbeatSender.Send(PlayerState.IN_MODDING);
                Utils.Logger.LogDebugWarning("[State] Player opened WeaponModding screen");
            }

            return true;
        }
    }
}