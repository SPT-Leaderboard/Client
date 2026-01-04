using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SPTLeaderboard.Data;
using SPTLeaderboard.Services;
using SPTLeaderboard.Utils.Zones;

namespace SPTLeaderboard.Utils
{
    public static class ConfigUpdater
    {
        public static void UpdateEquipmentLimits()
        {
            try
            {
                EquipmentData newConfig;
                var request = NetworkApiRequest.CreateGet(GlobalData.ConfigUrl);
                request.OnSuccess = (response, code) =>
                {
                    newConfig = JsonConvert.DeserializeObject<EquipmentData>(response);
                
                    if (newConfig != null)
                    {
                        Logger.LogInfo($"Request GET OnSuccess {response}");
                    
                        if(newConfig.TacticalVest <= 0 || newConfig.Pockets <= 0 || newConfig.Backpack <= 0 || newConfig.SecuredContainer <= 0 || newConfig.Stash <= 0 )
                            return;
                    
                        GlobalData.EquipmentLimits = new EquipmentData
                        {
                            TacticalVest = newConfig.TacticalVest,
                            Pockets = newConfig.Pockets,
                            Backpack = newConfig.Backpack,
                            SecuredContainer = newConfig.SecuredContainer,
                            Stash = newConfig.Stash
                        };
                    
                        LeaderboardPlugin.Instance.configLimitsUpdated = true;
                    }
                };
                request.Send();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error update limits config: {ex.Message}");
            }
        }
        
        public static void UpdateZones()
        {
            try
            {
                string currentVersion = GetCurrentZonesVersion();
                
                var request = NetworkApiRequest.CreateGet(GlobalData.ZonesConfigUrl);
                request.OnSuccess = (response, code) =>
                {
                    try
                    {
                        var jsonObject = JObject.Parse(response);
                        string serverVersion = jsonObject["ver"]?.ToString();
                        
                        Logger.LogInfo($"[UpdateZones] Current version: {currentVersion}, Server version: {serverVersion}");
                        
                        if (CompareVersions(serverVersion, currentVersion) > 0)
                        {
                            Logger.LogInfo($"[UpdateZones] Server version is newer, updating zones config");
                            
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

                            var zonesDict = new Dictionary<string, List<ZoneData>>();
                            
                            foreach (var prop in jsonObject.Properties())
                            {
                                if (prop.Name == "ver")
                                    continue;
                                
                                var zonesList = prop.Value.ToObject<List<ZoneData>>(JsonSerializer.Create(settings));
                                if (zonesList != null)
                                {
                                    zonesDict[prop.Name] = zonesList;
                                }
                            }
                            
                            if (zonesDict.Count > 0)
                            {
                                SetZonesConfigData(zonesDict);
                                Logger.LogInfo($"[UpdateZones] Zones config updated successfully. Loaded {zonesDict.Count} maps");
                            }
                            else
                            {
                                SetZonesConfigDataFallback("[UpdateZones] No zones found in server response");
                            }
                        }
                        else
                        {
                            SetZonesConfigDataFallback("[UpdateZones] Current version is up to date or newer");
                        }
                    }
                    catch (Exception ex)
                    {
                        SetZonesConfigDataFallback($"[UpdateZones] Error processing zones config: {ex.Message}");
                    }
                };
                
                request.OnFail = (error, code) =>
                {
                    SetZonesConfigDataFallback($"[UpdateZones] Failed to fetch zones config: {error}");
                };
                
                request.Send();
            }
            catch (Exception ex)
            {
                SetZonesConfigDataFallback($"Error update zones config: {ex.Message}");
            }
        }
        
        private static void SetZonesConfigData(Dictionary<string, List<ZoneData>> zonesDict)
        {
            var repository = LeaderboardPlugin.Instance.ZoneRepository ?? new ZoneRepository();
            repository.ZonesConfigData = zonesDict;
            LeaderboardPlugin.Instance.configZonesUpdated = true;
        }
        
        private static void SetZonesConfigDataFallback(string logMessage)
        {
            var repository = LeaderboardPlugin.Instance.ZoneRepository ?? new ZoneRepository();
            repository.ZonesConfigData = repository.LoadAllZones();
            LeaderboardPlugin.Instance.configZonesUpdated = true;
            
            if (logMessage.Contains("Error") || logMessage.Contains("Failed"))
                Logger.LogError(logMessage);
            else if (logMessage.Contains("Warning") || logMessage.Contains("No zones"))
                Logger.LogWarning(logMessage);
            else
                Logger.LogInfo(logMessage);
        }
        
        private static string GetCurrentZonesVersion()
        {
            try
            {
                var json = DataUtils.LoadFromEmbeddedResource(GlobalData.ZonesEmbeddedConfig);
                var jsonObject = JObject.Parse(json);
                return jsonObject["ver"]?.ToString() ?? "0.0";
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[UpdateZones] Error getting current zones version: {ex.Message}");
                return "0.0";
            }
        }
        
        private static int CompareVersions(string version1, string version2)
        {
            if (string.IsNullOrEmpty(version1)) version1 = "0.0";
            if (string.IsNullOrEmpty(version2)) version2 = "0.0";
            
            try
            {
                var v1Parts = version1.Split('.');
                var v2Parts = version2.Split('.');
                
                int maxLength = Math.Max(v1Parts.Length, v2Parts.Length);
                
                for (int i = 0; i < maxLength; i++)
                {
                    int v1Part = i < v1Parts.Length ? int.Parse(v1Parts[i]) : 0;
                    int v2Part = i < v2Parts.Length ? int.Parse(v2Parts[i]) : 0;
                    
                    if (v1Part > v2Part) return 1;
                    if (v1Part < v2Part) return -1;
                }
                
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}