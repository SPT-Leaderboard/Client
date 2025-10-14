using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class QuestInfoData
{
    [JsonProperty("accept_time")]
    public int AcceptTime { get; set; }

    [JsonProperty("finish_time")]
    public int FinishTime { get; set; }

    [JsonProperty("image_url")]
    public string ImageUrl { get; set; }
}