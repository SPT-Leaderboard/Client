using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    public class OnCoopApplyShotFourPatch: ModulePatch
    {
        protected override MethodBase GetTargetMethod() => DataUtils.GetPluginType(DataUtils.FikaCore, "Fika.Core.Main.ObservedClasses.PlayerBridge.ObservedClientBridge")
            .GetMethod("ApplyShot", BindingFlags.Instance | BindingFlags.Public);
    
        [PatchPostfix]
        static void PostFix(DamageInfoStruct damageInfo, EBodyPart bodyPart, EBodyPartColliderType bodyPartCollider, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
        {
            if (!Settings.Instance.EnableSendData.Value)
                return;
        
            Utils.Logger.LogDebugWarning("[ProcessShot ObservedClientBridge] Hit");

            IPlayerOwner player = damageInfo.Player;

            Utils.Logger.LogDebugWarning($"[ProcessShot ObservedClientBridge] Nick -> {player?.Nickname}");

            if ((Player)player?.iPlayer != PlayerHelper.Instance.Player)
            {
                return;
            }
        
            var zoneTracker = LeaderboardPlugin.Instance?.ZoneTrackerService;
            if (zoneTracker != null && zoneTracker.CurrentZone != null)
            {
                zoneTracker.OnEnemyDamage(damageInfo);
            }
        
            HitsTracker.Instance.IncreaseHit(bodyPart);
        
#if DEBUG || BETA
            OverlayDebug.Instance.UpdateOverlay();
#endif
            Utils.Logger.LogDebugWarning($"[ProcessShot ObservedClientBridge] Hit BodyType {bodyPart.ToString()}");
            Utils.Logger.LogDebugWarning($"[ProcessShot ObservedClientBridge] Hit EBodyPartColliderType {bodyPartCollider.ToString()}");
        }
    }
}