using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Integrations;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    public class OnCoopApplyShotFourPatch: ModulePatch
    {
        protected override MethodBase GetTargetMethod() => FikaInterop.GetObservedClientBridgeType()
            .GetMethod("ApplyShot", BindingFlags.Instance | BindingFlags.Public);
    
        [PatchPostfix]
        static void PostFix(EFT.Ballistics.DamageInfo damageInfo, EBodyPart bodyPart, EBodyPartColliderType bodyPartCollider, EArmorPlateCollider armorPlateCollider, EFT.Ballistics.ShotId shotId)
        {
            if (!Settings.Instance.EnableSendData.Value)
                return;
        
            Utils.Logger.LogDebugWarning("[ProcessShot ObservedClientBridge] Hit");
            
            IObserverToPlayerBridge player = damageInfo.Player;

            Utils.Logger.LogDebugWarning($"[ProcessShot ObservedClientBridge] Nick -> {player?.Nickname}");

            if ((Player)player?.iPlayer != PlayerHelper.Instance.Player)
            {
                return;
            }
            
            HitsTracker.Instance.IncreaseHit(bodyPart);

            Utils.Logger.LogDebugWarning($"[ProcessShot ObservedClientBridge] Hit BodyType {bodyPart.ToString()}");
            Utils.Logger.LogDebugWarning($"[ProcessShot ObservedClientBridge] Hit EBodyPartColliderType {bodyPartCollider.ToString()}");
        }
    }
}
