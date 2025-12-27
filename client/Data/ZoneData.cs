using System;
using Newtonsoft.Json;
using UnityEngine;

namespace SPTLeaderboard.Data;

public class ZoneData
{
    [JsonProperty("GUID", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string GUID { get; set; }
    
    [JsonProperty("Name")]
    public string Name { get; set; }
    
    [JsonProperty("Center")]
    public Vector3 Center { get; set; }
    
    [JsonProperty("Size")]
    public Vector3 Size { get; set; }

    [JsonProperty("RotationZ")]
    public float RotationZ { get; set; }
    
    public ZoneData()
    {
        GUID = Guid.Empty.ToString();
        Name = "";
        Center = Vector3.zero;
        Size = Vector3.zero;
        RotationZ = 0.0f;
    }

    public Bounds GetBounds()
    {
        return new Bounds(Center, Size);
    }
}