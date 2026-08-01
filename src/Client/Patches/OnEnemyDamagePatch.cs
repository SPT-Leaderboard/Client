using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    public class OnEnemyDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var targetType = typeof(EFT.BaseStatisticsManager);
            return targetType?.GetMethod(
                "OnEnemyDamage",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
        }

        [PatchPostfix]
        static void PostFix(
            object __instance,
            EFT.Ballistics.DamageInfo damage,
            EBodyPart bodyPart,
            string playerProfileId,
            EPlayerSide playerSide,
            WildSpawnType role,
            string groupId,
            float fullHealth,
            bool isHeavyDamage,
            float distance,
            int hour,
            List<string> targetEquipment,
            HealthEffects enemyEffects,
            List<string> zoneIds)
        {
            if (!Settings.Instance.EnableSendData.Value)
                return;
            
            if (damage.Weapon is not EFT.InventoryLogic.ThrowWeap)
            {
                HitsTracker.Instance.AddHit(distance, bodyPart);
            }
            
            Utils.Logger.LogDebugWarning($"[OnEnemyDamage Postfix] side={playerSide} role={role} body={bodyPart} dist={distance:0.0} heavy={isHeavyDamage}");
        }
    }
}
