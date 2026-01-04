using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Services;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    public class OnEnemyKillPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var targetType = typeof(LocationStatisticsCollectorAbstractClass);
            return targetType?.GetMethod(
                "OnEnemyKill",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
        }

        [PatchPrefix]
        static bool Prefix(
            DamageInfoStruct damage,
            EDamageType lethalDamageType,
            EBodyPart bodyPart,
            EPlayerSide playerSide,
            WildSpawnType role,
            string playerAccountId,
            string playerProfileId,
            string playerName,
            string groupId,
            int level,
            int killExp,
            float distance,
            int hour,
            List<string> targetEquipment,
            HealthEffects enemyEffects,
            List<string> zoneIds,
            bool isFriendly,
            bool isAI)
        {
            if (!Settings.Instance.EnableSendData.Value)
                return true;
            
            IPlayerOwner player = damage.Player;
            if ((Player)player?.iPlayer != PlayerHelper.Instance.Player)
            {
                return true;
            }
            
            var zoneTracker = LeaderboardPlugin.Instance?.ZoneTrackerService;
            if (zoneTracker != null && zoneTracker.CurrentZone != null)
            {
                zoneTracker.OnEnemyKilledInZone(damage, role.ToStringNoBox(), distance, bodyPart);
            }

            LocalizationService.Notification($"role={role.ToStringNoBox()}");
            Utils.Logger.LogDebugWarning($"[OnEnemyKill] " +
                $"distance={distance:F1}, " +
                $"role={role.ToStringNoBox()}, " +
                $"bodyPart={bodyPart}, " +
                $"zone={zoneTracker?.CurrentZone?.Name ?? "None"}");
            
            return true;
        }
    }
}

