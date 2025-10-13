using System.Collections.Generic;

namespace SPTLeaderboard.Data;

public class HitsDistanceData
{
    public List<float> Head { get; set; } = new();
    
    public List<float> Chest { get; set; } = new();
    
    public List<float> Stomach { get; set; } = new();
    
    public List<float> LeftArm { get; set; } = new();
    
    public List<float> RightArm { get; set; } = new();
    
    public List<float> LeftLeg { get; set; } = new();
    
    public List<float> RightLeg { get; set; } = new();

    public void Add(float value, EBodyPart bodyPart)
    {
        switch (bodyPart)
        {
            case EBodyPart.Head:
                Head.Add(value);
                break;
            case EBodyPart.Chest:
                Chest.Add(value);
                break;
            case EBodyPart.Stomach:
                Stomach.Add(value);
                break;
            case EBodyPart.LeftArm:
                LeftArm.Add(value);
                break;
            case EBodyPart.RightArm:
                RightArm.Add(value);
                break;
            case EBodyPart.LeftLeg:
                LeftLeg.Add(value);
                break;
            case EBodyPart.RightLeg:
                RightLeg.Add(value);
                break;
            default:
                break;
        }
    }
}