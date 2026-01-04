using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    internal class OnApplyDamageInfoPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() =>
            typeof(Player).GetMethod(
                "ApplyDamageInfo",
                BindingFlags.Instance | BindingFlags.Public);

        [PatchPrefix]
        static bool Prefix(DamageInfoStruct damageInfo, EBodyPart bodyPartType, EBodyPartColliderType colliderType)
        {
            if (!Settings.Instance.EnableSendData.Value)
                return true;
            
            Utils.Logger.LogDebugWarning("[ProcessShot Local] Hit");

            IPlayerOwner player = damageInfo.Player;

            Utils.Logger.LogDebugWarning($"[ProcessShot Local] Nick -> {player?.Nickname}");

            if ((Player)player?.iPlayer != PlayerHelper.Instance.Player)
            {
                return true;
            }

            var zoneTracker = LeaderboardPlugin.Instance?.ZoneTracker;
            if (zoneTracker != null && zoneTracker.CurrentZone != null)
            {
                zoneTracker.OnEnemyDamage(damageInfo);
            }
            
            HitsTracker.Instance.IncreaseHit(bodyPartType);
#if DEBUG
            OverlayDebug.Instance.UpdateOverlay();
#endif
            Utils.Logger.LogDebugWarning($"[ProcessShot Local] Hit BodyType {bodyPartType.ToString()}");
            Utils.Logger.LogDebugWarning($"[ProcessShot Local] Hit EBodyPartColliderType {colliderType.ToString()}");
            
            return true;
        }
    }
}