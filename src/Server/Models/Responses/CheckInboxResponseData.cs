using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace SPTLeaderboard.Server.Models.Responses;

public record CheckInboxResponseData
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("sessionId")]
    public MongoId SessionId { get; set; }
    
    [JsonPropertyName("messageText")]
    public string? Message { get; set; }
    
    [JsonPropertyName("rewardTpls")]
    public List<MongoId>? Items { get; set; }
}
