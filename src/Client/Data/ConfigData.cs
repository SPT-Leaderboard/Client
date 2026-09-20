using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class ConfigRequestData
{
    [JsonProperty("playerid")]
    public string PlayerId { get; set; }

    [JsonProperty("token")]
    public string Token { get; set; }
    
    [JsonProperty("password")]
    public string Password { get; set; }
}

public class ConfigResponseData
{
    [JsonProperty("authorized")]
    public bool Authorized { get; set; }

    [JsonProperty("config")]
    public EquipmentData Config { get; set; }

    [JsonProperty("isBusyHands")]
    public bool IsBusyHands { get; set; }
}
