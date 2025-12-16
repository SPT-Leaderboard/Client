using System;
using System.Collections.Generic;
using System.Linq;
using SPTLeaderboard.Data;

namespace SPTLeaderboard.Utils;

public class HitsTracker
{
    private static HitsTracker _instance;
    public static HitsTracker Instance => _instance ??= new HitsTracker();

    private HitsData data = new ();
    private HitsDistanceData hitDistances = new ();

    private HitsTracker() { }

    public void IncreaseHit(EBodyPart part)
    {
        switch (part)
        {
            case EBodyPart.Head:
                data.Head++;
                break;
            case EBodyPart.Chest:
                data.Chest++;
                break;
            case EBodyPart.Stomach:
                data.Stomach++;
                break;
            case EBodyPart.LeftArm:
                data.LeftArm++;
                break;
            case EBodyPart.RightArm:
                data.RightArm++;
                break;
            case EBodyPart.LeftLeg:
                data.LeftLeg++;
                break;
            case EBodyPart.RightLeg:
                data.RightLeg++;
                break;
            case EBodyPart.Common:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(part), part, null);
        }
    }
    
    public void Clear()
    {
        data = new HitsData();
        hitDistances = new HitsDistanceData();
    }

    public HitsData GetHitsData()
    {
        return data;
    }
    
    public float GetLongestShot()
    {
        var hitLists = new List<List<float>> {
            hitDistances.Head, hitDistances.Chest, hitDistances.Stomach,
            hitDistances.LeftArm, hitDistances.RightArm, hitDistances.LeftLeg, hitDistances.RightLeg
        };

        var maxDistances = hitLists
            .Where(list => list.Count > 0)
            .Select(list => list.Max());

        return maxDistances.Any() ? maxDistances.Max() : 0f;
    }
    
    public float GetLongestHeadshot()
    {
        return hitDistances.Head.Any() ? hitDistances.Head.Max() : 0f;
    }
    
    public float GetAverageShot()
    {
        var hitLists = new List<List<float>> {
            hitDistances.Head, hitDistances.Chest, hitDistances.Stomach,
            hitDistances.LeftArm, hitDistances.RightArm, hitDistances.LeftLeg, hitDistances.RightLeg
        };

        var averageDistances = hitLists
            .Where(list => list.Count > 0)
            .Select(list => list.Average());

        return averageDistances.Any() ? averageDistances.Average() : 0f;
    }
    
    public void AddHit(float distance, EBodyPart bodyPart)
    {
        var roundedDistance = (float)Math.Round(distance, 1);

        Logger.LogDebugInfo($"[HitsTracker] Add hit with distance {roundedDistance} for {bodyPart.ToString()}");

        hitDistances.Add(roundedDistance, bodyPart);
    }

    
}