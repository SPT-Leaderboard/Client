using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class ErrorBannedModsData
{
    [JsonProperty("error")]
    public string Error { get; set; }
    [JsonProperty("blocked_mods")]
    public string[] BlockedMods { get; set; }
}
