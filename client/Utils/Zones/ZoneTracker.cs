using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SPTLeaderboard.Data;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace SPTLeaderboard.Utils.Zones
{
    public class ZoneTracker : MonoBehaviour
    {
        public ZoneData CurrentZone { get; private set; }

        private Dictionary<string, List<ZoneData>> allZones;

        public List<ZoneData> Zones = new();
        public List<string> ZonesEntered = new();
        public Action<Dictionary<string, List<ZoneData>>> OnZonesLoaded;
        public bool ShowOverlays { get; set; }

        private float zoneEntryTime;
        public Dictionary<string, float> ZoneTimes { get; private set; } = new();
        public Action<string, float> OnZoneTimeUpdated;


#if DEBUG || BETA
        private List<LineRenderer> debugViews = new();
        private List<ZoneOverlay> zoneOverlays = new();
#endif
        public void Enable()
        {
            LeaderboardPlugin.Instance.FixedTick += CheckPlayerPosition;
            LoadZones();
            LoadZonesMap(DataUtils.GetPrettyMapName(DataUtils.GetRaidRawMap()));
        }

        public void LoadZonesMap(string mapName)
        { 
            if (allZones != null && allZones.ContainsKey(mapName))
            {
                Zones = allZones[mapName];
                Logger.LogWarning($"[ZoneManager] Loaded {Zones.Count} zones for map {mapName}");
            }
            else
            {
                Zones.Clear();
                Logger.LogWarning($"[ZoneManager] For map {mapName} zones not found.");
            }
        }

        public void Disable()
        {
            LeaderboardPlugin.Instance.FixedTick -= CheckPlayerPosition;
#if DEBUG || BETA
            foreach (var debugView in debugViews)
            {
                Destroy(debugView.gameObject);
            }

            debugViews = null;
#endif
            Zones = null;
            zoneEntryTime = 0f;
            ZoneTimes = new();
            ZonesEntered = null;
            allZones = null;
            CurrentZone = null;
        }

        public void LoadZones()
        {
            if (!File.Exists(GlobalData.ZonesConfig))
            {
                Logger.LogDebugInfo(
                    $"[ZoneTracker] zones.json not found: {GlobalData.ZonesConfig}"); //TODO: Load from DLL file
                return;
            }

            string json = File.ReadAllText(GlobalData.ZonesConfig);
            Logger.LogDebugInfo(json);
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters = new List<JsonConverter> { new ZoneVector3Converter() },
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Include,
                    DefaultValueHandling = DefaultValueHandling.Populate,
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = null
                    },
                    Error = (sender, args) =>
                    {
                        Logger.LogDebugInfo($"[ZoneTracker] Error deserialization: {args.ErrorContext.Error.Message}");
                        Logger.LogDebugInfo($"[ZoneTracker] Path: {args.ErrorContext.Path}");
                        args.ErrorContext.Handled = true;
                    }
                };

                allZones = JsonConvert.DeserializeObject<Dictionary<string, List<ZoneData>>>(json, settings);

                if (allZones != null && allZones.Count > 0)
                {
                    Logger.LogDebugInfo($"[ZoneTracker] Deserialize maps: {allZones.Count}");
                    foreach (var kvp in allZones)
                    {
                        Logger.LogDebugInfo($"[ZoneTracker] Map: {kvp.Key}, Zones: {kvp.Value?.Count ?? 0}");
                    }
                }
                else
                {
                    Logger.LogDebugInfo("[ZoneTracker] allZones is null or empty after deserialization!");
                }

                if (allZones != null)
                {
                    int totalZones = 0;
                    foreach (var category in allZones.Values)
                    {
                        totalZones += category.Count;
                    }

                    Logger.LogDebugInfo($"[ZoneTracker] Loaded maps: {allZones.Count}, Zones: {totalZones}");

                    foreach (var category in allZones.Values)
                    {
                        foreach (var zone in category)
                        {
                            if (string.IsNullOrEmpty(zone.GUID) || zone.GUID == Guid.Empty.ToString())
                            {
                                zone.GUID = Guid.NewGuid().ToString();
                            }
                        }
                    }

                    if (OnZonesLoaded != null)
                    {
                        Logger.LogDebugInfo(
                            $"[ZoneTracker] Calling the OnZonesLoaded callback, subscribers: {OnZonesLoaded.GetInvocationList().Length}");
                        OnZonesLoaded.Invoke(allZones);
                    }
                    else
                    {
                        Logger.LogDebugInfo("[ZoneTracker] The OnZonesLoaded callback is not sunscribed!");
                    }
                }
                else
                {
                    Zones.Clear();
                    Logger.LogDebugInfo($"[ZoneTracker] no zones found.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebugInfo($"[ZoneTracker] Error read JSON: {ex.Message}");
            }
        }

        public void CheckPlayerPosition()
        {
            if (PlayerHelper.Instance.Player)
            {
                CheckPlayerPosition(PlayerHelper.Instance.Player.PlayerBones.transform.position);
            }
        }

        public void CheckPlayerPosition(Vector3 pos)
        {
            foreach (var zone in Zones)
            {
                if (zone.GetBounds().Contains(pos))
                {
                    if (CurrentZone != zone)
                    {
                        // Exit from previous zone - save time
                        if (CurrentZone != null && zoneEntryTime > 0)
                        {
                            float timeSpent = Time.fixedTime - zoneEntryTime;
                            if (!ZoneTimes.ContainsKey(CurrentZone.GUID)) ZoneTimes[CurrentZone.GUID] = 0f;
                            ZoneTimes[CurrentZone.GUID] += timeSpent;
                            // OnZoneTimeUpdated?.Invoke(CurrentZone.GUID, ZoneTimes[CurrentZone.GUID]);
                            Logger.LogDebugInfo(
                                $"ZoneTracker: Exit {CurrentZone.Name}, total time: {ZoneTimes[CurrentZone.GUID]:F1}s");
                        }

                        // Enter in zone
                        CurrentZone = zone;
                        zoneEntryTime = Time.fixedTime;
                        
                        if (!ZonesEntered.Contains(zone.GUID))
                        {
                            ZonesEntered.Add(zone.GUID);
                        }

                        Logger.LogDebugWarning($"ZoneTracker: Enter {CurrentZone.Name}");
                    }

                    // Player already in zone
                    return;
                }
            }

            // Exit from any zone
            if (CurrentZone != null && zoneEntryTime > 0)
            {
                float timeSpent = Time.fixedTime - zoneEntryTime;
                if (!ZoneTimes.ContainsKey(CurrentZone.GUID)) ZoneTimes[CurrentZone.GUID] = 0f;
                ZoneTimes[CurrentZone.GUID] += timeSpent;
                // OnZoneTimeUpdated?.Invoke(CurrentZone.GUID, ZoneTimes[CurrentZone.GUID]);
                Logger.LogDebugWarning($"ZoneTracker: Exit {CurrentZone.Name}, total time: {ZoneTimes[CurrentZone.GUID]:F1}s");
                CurrentZone = null;
                zoneEntryTime = 0f;
            }
        }


#if DEBUG || BETA
        public void DrawZone(Vector3 Size, Vector3 Center)
        {
            Vector3 half = Size / 2;

            Vector3[] corners = new Vector3[8];
            corners[0] = Center + new Vector3(-half.x, -half.y, -half.z);
            corners[1] = Center + new Vector3(-half.x, -half.y, half.z);
            corners[2] = Center + new Vector3(half.x, -half.y, half.z);
            corners[3] = Center + new Vector3(half.x, -half.y, -half.z);

            corners[4] = Center + new Vector3(-half.x, half.y, -half.z);
            corners[5] = Center + new Vector3(-half.x, half.y, half.z);
            corners[6] = Center + new Vector3(half.x, half.y, half.z);
            corners[7] = Center + new Vector3(half.x, half.y, -half.z);

            int[,] edges = new int[,]
            {
                { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, // низ
                { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, // верх
                { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } // вертикали
            };

            for (int i = 0; i < edges.GetLength(0); i++)
            {
                var go = new GameObject($"Edge{i}");
                var lr = go.AddComponent<LineRenderer>();
                debugViews.Add(lr);
                lr.widthMultiplier = 0.05f;
                lr.positionCount = 2;

                lr.SetPosition(0, corners[edges[i, 0]]);
                lr.SetPosition(1, corners[edges[i, 1]]);
            }
        }


        public void SaveZones(Dictionary<string, List<ZoneData>> zonesToSave)
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
            catch (System.Exception ex)
            {
                Logger.LogDebugInfo($"[ZoneTracker] Error saving JSON: {ex.Message}");
            }
        }

        public void ClearDebugViews()
        {
            foreach (var lr in debugViews)
            {
                if (lr != null && lr.gameObject != null)
                {
                    Destroy(lr.gameObject);
                }
            }

            debugViews.Clear();

            // Очищаем оверлеи
            ClearOverlays();

            Logger.LogDebugInfo("[ZoneTracker] All rendered zones have been cleared.");
        }

        public void ClearOverlays()
        {
            foreach (var overlay in zoneOverlays)
            {
                if (overlay != null && overlay.gameObject != null)
                {
                    Destroy(overlay.gameObject);
                }
            }

            zoneOverlays.Clear();
        }

        public void DrawZone(Vector3 Size, Vector3 Center, float rotationZ)
        {
            Vector3 half = Size / 2;

            Vector3[] corners = new Vector3[8];
            corners[0] = Center + new Vector3(-half.x, -half.y, -half.z);
            corners[1] = Center + new Vector3(-half.x, -half.y, half.z);
            corners[2] = Center + new Vector3(half.x, -half.y, half.z);
            corners[3] = Center + new Vector3(half.x, -half.y, -half.z);
            corners[4] = Center + new Vector3(-half.x, half.y, -half.z);
            corners[5] = Center + new Vector3(-half.x, half.y, half.z);
            corners[6] = Center + new Vector3(half.x, half.y, half.z);
            corners[7] = Center + new Vector3(half.x, half.y, -half.z);

            if (Mathf.Abs(rotationZ) > 0.001f)
            {
                Quaternion rotation = Quaternion.Euler(0, rotationZ, 0);
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 relativePos = corners[i] - Center;
                    relativePos = rotation * relativePos;
                    corners[i] = Center + relativePos;
                }
            }

            int[,] edges = new int[,]
            {
                { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, // down
                { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, // up
                { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } // verticals
            };

            for (int i = 0; i < edges.GetLength(0); i++)
            {
                var go = new GameObject($"Edge{i}");
                var lr = go.AddComponent<LineRenderer>();
                debugViews.Add(lr);
                lr.widthMultiplier = 0.05f;
                lr.positionCount = 2;
                lr.SetPosition(0, corners[edges[i, 0]]);
                lr.SetPosition(1, corners[edges[i, 1]]);
            }
        }

        public void DrawZonesForMap(string mapName)
        {
            if (allZones == null || !allZones.ContainsKey(mapName))
            {
                Logger.LogDebugInfo($"[ZoneTracker] Map {mapName} not found");
                return;
            }

            ClearDebugViews();

            var zones = allZones[mapName];
            if (zones == null)
            {
                Logger.LogDebugInfo($"[ZoneTracker] List zones for map {mapName} is empty");
                return;
            }

            foreach (var zone in zones)
            {
                if (zone != null)
                {
                    DrawZone(zone.Size, zone.Center, zone.RotationZ);

                    if (ShowOverlays)
                    {
                        CreateZoneOverlay(zone);
                    }
                }
            }

            Logger.LogDebugInfo($"[ZoneTracker] Rendered {zones.Count} zones for map {mapName}");
        }

        void CreateZoneOverlay(ZoneData zone)
        {
            if (zone == null)
            {
                Logger.LogDebugInfo("[ZoneTracker] Attempt to create an overlay for a null zone");
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = FindObjectOfType<Camera>();
                if (cam == null)
                {
                    Logger.LogDebugInfo("[ZoneTracker] The camera was not found, and the overlay will not be created.");
                    return;
                }
            }

            GameObject overlayObj = new GameObject($"ZoneOverlay_{zone.Name}_{zone.GUID}");
            ZoneOverlay zoneOverlay = overlayObj.AddComponent<ZoneOverlay>();
            zoneOverlay.transform.position = zone.Center;
            zoneOverlay.Initialize(zone, cam);
            zoneOverlays.Add(zoneOverlay);

            Logger.LogDebugInfo($"[ZoneTracker] An overlay has been created for the zone: {zone.Name}");
        }


        public void CreateOverlaysForCurrentZones(string categoryName = null)
        {
            if (!ShowOverlays)
            {
                Logger.LogDebugInfo("[ZoneTracker] ShowOverlays is disabled, overlays will not be created");
                return;
            }

            if (Camera.main == null)
            {
                Logger.LogDebugInfo(
                    "[ZoneTracker] Camera.main was not found! Overlays require a camera with the Maincam tag.");
                return;
            }

            ClearOverlays();

            int createdCount = 0;

            if (allZones != null)
            {
                if (categoryName != null && allZones.ContainsKey(categoryName))
                {
                    var category = allZones[categoryName];
                    if (category != null)
                    {
                        foreach (var zone in category)
                        {
                            if (zone != null)
                            {
                                CreateZoneOverlay(zone);
                                createdCount++;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var category in allZones.Values)
                    {
                        if (category != null)
                        {
                            foreach (var zone in category)
                            {
                                if (zone != null)
                                {
                                    CreateZoneOverlay(zone);
                                    createdCount++;
                                }
                            }
                        }
                    }
                }
            }

            Logger.LogDebugInfo(
                $"[ZoneTracker] {createdCount} overlays created (total in the list: {zoneOverlays.Count})");
        }
#endif
    }
}