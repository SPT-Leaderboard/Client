using Newtonsoft.Json;

namespace SPTLeaderboard.Data.Response;

public class ErrorRequestData
{
    [JsonProperty("error")]
    public string Error { get; set; }
}