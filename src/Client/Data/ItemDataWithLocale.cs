using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class ItemDataWithLocale
{
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonProperty("template_id")]
    public string TemplateId { get; set; }
    [JsonProperty("amount")]
    public int Amount { get; set; }
    [JsonProperty("shortName")]
    public string ShortName { get; set; }
    [JsonProperty("name")]
    public string Name { get; set; }
    
    public ItemDataWithLocale()
    {
    }
    
    public ItemDataWithLocale(string id, string templateId, int amount, string shortName, string name)
    {
        Id = id;
        TemplateId = templateId;
        Amount = amount;
        Name = name;
        ShortName = shortName;
    }
}