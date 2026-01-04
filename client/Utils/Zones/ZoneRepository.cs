using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SPTLeaderboard.Data;
using SPTLeaderboard.Services;

namespace SPTLeaderboard.Utils.Zones;

public class ZoneRepository()
{
    public Dictionary<string, List<ZoneData>> ZonesConfigData = null;
    public Dictionary<string, List<ZoneData>> LoadAllZones()
    {
        if (ZonesConfigData != null) return ZonesConfigData;
        
        if (File.Exists(GlobalData.ZonesConfig))
        {
            var zonesFromFile = LoadFromFile(GlobalData.ZonesConfig);
            if (zonesFromFile != null)
            {
                Logger.LogDebugInfo("[ZoneTracker] Loaded zones from CUSTOM file");
                LocalizationService.NotificationWarning("Zones config loaded from custom file!!!");
                ZonesConfigData = zonesFromFile;
                return zonesFromFile;
            }
        }
        
        var zonesFromDll = DeserializeZones(DataUtils.LoadFromEmbeddedResource("BuildScripts/zonesConfig.json"));
        if (zonesFromDll != null)
        {
            Logger.LogDebugInfo("[ZoneTracker] Loaded default zones from DLL");
            ZonesConfigData = zonesFromDll;
            return zonesFromDll;
        }
        
        Logger.LogDebugInfo("[ZoneTracker] No zones found in DLL or file");
        return null;
    }

    private Dictionary<string, List<ZoneData>> LoadFromFile(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            return DeserializeZones(json);
        }
        catch (Exception ex)
        {
            Logger.LogDebugInfo($"[ZoneTracker] Error loading file zones: {ex.Message}");
            return null;
        }
    }

    private Dictionary<string, List<ZoneData>> DeserializeZones(string json)
    {
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = new List<JsonConverter> { new ZoneVector3Converter() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Include,
            DefaultValueHandling = DefaultValueHandling.Populate,
            ContractResolver = new DefaultContractResolver { NamingStrategy = null },
            Error = (sender, args) =>
            {
                Logger.LogDebugInfo($"[ZoneTracker] Deserialization error: {args.ErrorContext.Error.Message}");
                args.ErrorContext.Handled = true;
            }
        };

        var mapsZones = JsonConvert.DeserializeObject<Dictionary<string, List<ZoneData>>>(json, settings);
        
        if (mapsZones != null && mapsZones.Count > 0)
        {
            Logger.LogDebugInfo($"[ZoneTracker] Deserialize maps: {mapsZones.Count}");
            foreach (var kvp in mapsZones)
            {
                Logger.LogDebugInfo($"[ZoneTracker] Map: {kvp.Key}, Zones: {kvp.Value?.Count ?? 0}");
            }
        }
        else
        {
            Logger.LogDebugInfo("[ZoneTracker] allZones is null or empty after deserialization!");
        }
        
        if (mapsZones != null)
        {
            foreach (var category in mapsZones.Values)
            {
                foreach (var zone in category ?? new List<ZoneData>())
                {
                    if (string.IsNullOrEmpty(zone?.GUID) || zone.GUID == Guid.Empty.ToString())
                        if (zone != null)
                            zone.GUID = Guid.NewGuid().ToString();
                }
            }
        }
        
        return mapsZones;
    }
    
    public void SaveAllZones(Dictionary<string, List<ZoneData>> zonesToSave)
    {
        if (zonesToSave == null)
        {
            Logger.LogDebugInfo("[ZoneTracker] Null zones cannot be saved");
            return;
        }

        try
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Converters = new List<JsonConverter> { new ZoneVector3Converter() },
                Formatting = Formatting.Indented
            };

            string json = JsonConvert.SerializeObject(zonesToSave, settings);
            File.WriteAllText(GlobalData.ZonesConfig, json);

            Logger.LogDebugInfo($"[ZoneTracker] The zones are saved in {GlobalData.ZonesConfig}");
        }
        catch (Exception ex)
        {
            Logger.LogDebugInfo($"[ZoneTracker] Error saving JSON: {ex.Message}");
        }
    }
}
