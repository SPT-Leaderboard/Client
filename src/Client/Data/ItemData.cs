using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class ItemData
{
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("template_id")]
    public string TemplateId { get; set; }
    [JsonProperty("amount")]
    public int Amount { get; set; }
    [JsonProperty("color")]
    public string Color { get; set; }
    
    public ItemData()
    {
    }
    
    public ItemData(string id, string templateId, int amount, string color = "default")
    {
        Id = id;
        TemplateId = templateId;
        Amount = amount;
        Color = color;
    }
}