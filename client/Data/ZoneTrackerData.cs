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
        
        [JsonProperty("KillsInZones")]
        public Dictionary<string, int> KillsInZones = new();
        
        [JsonProperty("KillDetailsInZones")]
        public Dictionary<string, List<KillInfo>> KillDetailsInZones = new();
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