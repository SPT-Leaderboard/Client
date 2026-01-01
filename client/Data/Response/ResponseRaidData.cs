using Newtonsoft.Json;

namespace SPTLeaderboard.Data.Response;

public class ResponseRaidData
{
    [JsonProperty("response")]
    public string Response { get; set; } = "success";

    [JsonProperty("addedToBalance")]
    public int AddedToBalance { get; set; }

    [JsonProperty("battlePassEXP")]
    public int BattlePassExp { get; set; }
}