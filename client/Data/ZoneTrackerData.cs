using System.Collections.Generic;
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
        
        [JsonProperty("CausedDamageInZones")]
        public Dictionary<string, float> CausedDamageInZones = new();
        
        [JsonProperty("TakenDamageInZones")]
        public Dictionary<string, float> TakenDamageInZones = new();
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