using Newtonsoft.Json;

namespace SPTLeaderboard.Data.Response;

public class ErrorBannedModsData
{
    [JsonProperty("error")]
    public string Error { get; set; }
    [JsonProperty("blocked_mods")]
    public string[] BlockedMods { get; set; }
}
