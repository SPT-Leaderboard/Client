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
    }
}