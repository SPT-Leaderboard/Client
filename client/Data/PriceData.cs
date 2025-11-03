using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class PriceData
{
    [JsonProperty("success")] public bool Success { get; set; } = false;
    [JsonProperty("total_price")] public int TotalPrice { get; set; } = 0;
}