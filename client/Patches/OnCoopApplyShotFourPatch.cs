using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPTLeaderboard.Models;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches;

public class OnCoopApplyShotFourPatch: ModulePatch
{
    protected override MethodBase GetTargetMethod() => DataUtils.GetPluginType(DataUtils.FikaCore, "Fika.Core.Main.ObservedClasses.PlayerBridge.ObservedClientBridge")
        .GetMethod("ApplyShot", BindingFlags.Instance | BindingFlags.Public);
    
    [PatchPostfix]
    static void PostFix(DamageInfoStruct damageInfo, EBodyPart bodyPart, EBodyPartColliderType bodyPartCollider, EArmorPlateCollider armorPlateCollider, ShotIdStruct shotId)
    {
        if (!SettingsModel.Instance.EnableSendData.Value)
            return;
            
#if DEBUG || BETA
        LeaderboardPlugin.logger.LogWarning("[ProcessShot ObservedClientBridge] Hit");
#endif
        IPlayerOwner player = damageInfo.Player;
#if DEBUG || BETA
        LeaderboardPlugin.logger.LogWarning($"[ProcessShot ObservedClientBridge] Nick -> {player?.Nickname}");
#endif
        if ((Player)((player != null) ? player.iPlayer : null) != PlayerHelper.Instance.Player)
        {
            return;
        }
        
        HitsTracker.Instance.IncreaseHit(bodyPart);
        
#if DEBUG || BETA
        OverlayDebug.Instance.UpdateOverlay();
        LeaderboardPlugin.logger.LogWarning($"[ProcessShot ObservedClientBridge] Hit BodyType {bodyPart.ToString()}");
        LeaderboardPlugin.logger.LogWarning($"[ProcessShot ObservedClientBridge] Hit EBodyPartColliderType {bodyPartCollider.ToString()}");
#endif
    }
}