using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class HitsData
{
    [JsonProperty("head")]
    public int Head { get; set; }
    
    [JsonProperty("chest")]
    public int Chest { get; set; }
    
    [JsonProperty("stomach")]
    public int Stomach { get; set; }
    
    [JsonProperty("leftArm")]
    public int LeftArm { get; set; }
    
    [JsonProperty("rightArm")]
    public int RightArm { get; set; }
    
    [JsonProperty("leftLeg")]
    public int LeftLeg { get; set; }
    
    [JsonProperty("rightLeg")]
    public int RightLeg { get; set; }

    public HitsData Clone() =>
        new HitsData
        {
            Head = Head,
            Chest = Chest,
            Stomach = Stomach,
            LeftArm = LeftArm,
            RightArm = RightArm,
            LeftLeg = LeftLeg,
            RightLeg = RightLeg
        };
}