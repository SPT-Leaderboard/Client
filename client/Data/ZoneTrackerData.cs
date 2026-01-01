using System.Collections.Generic;
using Newtonsoft.Json;

namespace SPTLeaderboard.Data
{
    public class ZoneTrackerData
    {
        [JsonProperty("ZonesEnteredInRaid")]
        public List<string> ZonesEntered = new();
    
        [JsonProperty("ZonesTimesSpendInRaid")]
        public Dictionary<string, float> ZonesTimesSpend = new();
    }
}