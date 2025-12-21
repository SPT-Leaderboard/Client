using System;
using System.Collections.Generic;
using UnityEngine;

namespace SPTLeaderboard.Data;

[System.Serializable]
public class ZoneData
{
    public string GUID { get; set; } = Guid.Empty.ToString();
    public string Name { get; set; } = "";
    public Vector3 Center { get; set; } = Vector3.zero;
    public Vector3 Size { get; set; } = Vector3.zero;
    public float RotationVertical { get; set; } = 0.0f;
    public List<ZoneData> SubZones { get; set; } = new();

    public Bounds GetBounds()
    {
        return new Bounds(Center, Size);
    }
}