using System.Collections.Generic;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using SPT.Reflection.Patching;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Patches
{
    public class OnEnemyKillPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            var targetType = typeof(IStatisticsManager);
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
            
            var zoneTracker = LeaderboardPlugin.Instance?.ZoneTracker;
            if (zoneTracker != null && zoneTracker.CurrentZone != null)
            {
                zoneTracker.OnEnemyKilledInZone(damage, playerAccountId, distance, bodyPart);
            }

            Utils.Logger.LogDebugWarning($"[OnEnemyKill Postfix] " +
                $"playerAccountId={playerAccountId}, " +
                $"damage={damage.Damage}, " +
                $"distance={distance:F1}, " +
                $"bodyPart={bodyPart}, " +
                $"zone={zoneTracker?.CurrentZone?.Name ?? "None"}");
            
            return true;
        }
    }
}

