using System.Collections.Generic;
using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class PreRaidData
{
    [JsonProperty("profile_id")]
    public string ProfileId { get; set; }
    
    [JsonProperty("ver")]
    public string VersionMod { get; set; }
    
    [JsonProperty("mods")]
    public List<string> Mods { get; set; }
    
    [JsonProperty("isCasual")]
    public bool IsCasual { get; set; }
    
    [JsonProperty("hash")]
    public string Hash { get; set; }
}