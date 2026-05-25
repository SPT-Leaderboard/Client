using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class EquipmentData
{
    [JsonProperty("rig")]
    public int TacticalVest { get; set; }
    
    [JsonProperty("pockets")]
    public int Pockets { get; set; }
    
    [JsonProperty("backpack")]
    public int Backpack { get; set; }
    
    [JsonProperty("securedContainer")]
    public int SecuredContainer { get; set; }
    
    [JsonProperty("stash")]
    public int Stash { get; set; }
}