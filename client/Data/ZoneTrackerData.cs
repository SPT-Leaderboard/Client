using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SPTLeaderboard.Data
{
    public class ZoneTrackerData
    {
        [JsonProperty("ZonesEnteredInRaid")]
        public List<string> ZonesEntered = new();
    
        [JsonProperty("TimeSpendInZones")]
        public Dictionary<string, float> TimeSpendInZones = new();
        
        [JsonProperty("KilometerWalkedInZones")]
        public Dictionary<string, float> KilometerWalkedInZones = new();
        
        [JsonProperty("MedicinesUsedInZones")]
        public Dictionary<string, int> MedicinesUsedInZones = new();
        
        [JsonProperty("HealthHealedUsedInZones")]
        public Dictionary<string, float> HealthHealedUsedInZones = new();
        
        [JsonProperty("KillsInZones")]
        public Dictionary<string, int> KillsInZones = new();
        
        [JsonProperty("KillDetailsInZones")]
        public Dictionary<string, List<KillInfo>> KillDetailsInZones = new();
        
        [JsonProperty("DamageToEnemyInZones")]
        public Dictionary<string, float> DamageToEnemyInZones = new();
        
        [JsonProperty("DamageToPlayerInZones")]
        public Dictionary<string, float> DamageToPlayerInZones = new();

        [JsonProperty("LootedItemsInZones")]
        public Dictionary<string, List<ItemData>> LootedItemsInZones = new();
        
        [JsonProperty("AmountComputersOpenedInZones")]
        public Dictionary<string, int> AmountComputersOpenedInZones = new();
        
        [JsonProperty("AmountSafeOpenedInZones")]
        public Dictionary<string, int> AmountSafeOpenedInZones = new();
        
        [JsonProperty("AmountContainersOpenedInZones")]
        public Dictionary<string, int> AmountContainersOpenedInZones = new();

        /// <summary>
        /// Deep copy for safe serialization off the main thread (avoids concurrent mutation of live raid data).
        /// </summary>
        public ZoneTrackerData Clone()
        {
            var c = new ZoneTrackerData
            {
                ZonesEntered = new List<string>(ZonesEntered),
                TimeSpendInZones = new Dictionary<string, float>(TimeSpendInZones),
                KilometerWalkedInZones = new Dictionary<string, float>(KilometerWalkedInZones),
                MedicinesUsedInZones = new Dictionary<string, int>(MedicinesUsedInZones),
                HealthHealedUsedInZones = new Dictionary<string, float>(HealthHealedUsedInZones),
                KillsInZones = new Dictionary<string, int>(KillsInZones),
                DamageToEnemyInZones = new Dictionary<string, float>(DamageToEnemyInZones),
                DamageToPlayerInZones = new Dictionary<string, float>(DamageToPlayerInZones),
                AmountComputersOpenedInZones = new Dictionary<string, int>(AmountComputersOpenedInZones),
                AmountSafeOpenedInZones = new Dictionary<string, int>(AmountSafeOpenedInZones),
                AmountContainersOpenedInZones = new Dictionary<string, int>(AmountContainersOpenedInZones)
            };

            c.KillDetailsInZones = KillDetailsInZones.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Select(k => new KillInfo
                {
                    Weapon = k.Weapon,
                    Distance = k.Distance,
                    Role = k.Role,
                    BodyPart = k.BodyPart
                }).ToList());

            c.LootedItemsInZones = LootedItemsInZones.ToDictionary(
                kv => kv.Key,
                kv => new List<ItemData>(kv.Value));

            return c;
        }
    }
    
    public class KillInfo
    {
        [JsonProperty("weapon")]
        public string Weapon { get; set; }
        
        [JsonProperty("distance")]
        public float Distance { get; set; }
        
        [JsonProperty("role")]
        public string Role { get; set; }
        
        [JsonProperty("bodyPart")]
        public string BodyPart { get; set; }
    }
}