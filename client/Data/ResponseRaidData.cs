using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class ResponseRaidData
{
    [JsonProperty("response")]
    public string Response { get; set; } = "success";

    [JsonProperty("addedToBalance")]
    public int AddedToBalance { get; set; } = 0;

    [JsonProperty("battlePassEXP")]
    public int BattlePassEXP { get; set; } = 0;
}