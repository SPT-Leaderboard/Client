using System.Collections.Generic;
using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class ItemsDataGlobal
{
    [JsonProperty("items")]
    public List<string> Items { get; set; } = new List<string>();
    [JsonProperty("profileId")]
    public string ProfileID { get; set; } = "";
    [JsonProperty("ver")]
    public string Version { get; set; } = "";
    [JsonProperty("token")]
    public string Token { get; set; } = "";
    [JsonProperty("method")]
    public string Method { get; set; } = "";
    [JsonProperty("prices")]
    public string PricesType { get; set; } = "";
}