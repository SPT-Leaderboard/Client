using System.Reflection;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI.Ragfair;
using SPT.Reflection.Patching;
using SPTLeaderboard.Enums;
using SPTLeaderboard.Models;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    internal class RagfairScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(RagfairScreen).GetMethod(
                "Show",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[]
                {
                    typeof(Profile),
                    typeof(InventoryController),
                    typeof(CompoundItem[]),
                    typeof(IHealthController),
                    typeof(ISession),
                    typeof(GClass3943)
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
                HeartbeatSender.Send(PlayerState.IN_FLEA); 
                Utils.Logger.LogDebugWarning("[State] Player opened Flea screen");
            }

            return true;
        }
    }
}