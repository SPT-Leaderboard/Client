#if DEBUG || BETA
using System.Collections.Generic;
using SPTLeaderboard.Data;
using SPTLeaderboard.Models;
using UnityEngine;

namespace SPTLeaderboard.Utils.Zones
{
    public class ZoneInterface : MonoBehaviour
    {
        private Rect windowRect = new(100, 100, 600, 700);

        private Dictionary<string, List<ZoneData>> allZones = new();
        private List<ZoneData> currentMapZones = new();
        private int selectedZoneIndex = -1;
        private ZoneData selectedZone;
        private string selectedMap;

        private Vector2 mapsListScrollPosition = Vector2.zero;
        private Vector2 zonesListScrollPosition = Vector2.zero;
        private Vector2 detailsScrollPosition = Vector2.zero;

        private string editedGuid = "";
        private string editedName = "";
        private string editedCenterX = "0";
        private string editedCenterY = "0";
        private string editedCenterZ = "0";
        private string editedSizeX = "0";
        private string editedSizeY = "0";
        private string editedSizeZ = "0";
        private string editedRotationZ = "0";

        private ZoneTracker _zoneZoneTracker;
        private ZoneDebugRenderer _zoneDebugRenderer;

        static bool IsUIOpen
        {
            get => ZoneCursorUtils.IsUiOpen;
            set => ZoneCursorUtils.IsUiOpen = value;
        }

        void Start()
        {
            _zoneZoneTracker = LeaderboardPlugin.Instance.ZoneTracker;
            
            
            ZoneCursorUtils.Initialize();
            if (_zoneZoneTracker != null)
            {
                _zoneDebugRenderer = gameObject.AddComponent<ZoneDebugRenderer>();
                _zoneDebugRenderer.ZoneTracker = _zoneZoneTracker;
                _zoneZoneTracker.OnZonesLoaded += OnZonesLoaded;
            }
            else
            {
                Logger.LogDebugInfo("[ZonesInterface] Tracker not found!");
                LocalizationModel.NotificationWarning("Tracker not found!");
            }
        }

        void OnDestroy()
        {
            if (_zoneZoneTracker != null)
            {
                _zoneZoneTracker.OnZonesLoaded -= OnZonesLoaded;
            }
        }

        void LateUpdate()
        {
            if (IsUIOpen) ZoneCursorUtils.ApplyState(0, true);
        }

        void OnGUI()
        {
            if (!IsUIOpen)
                return;

            ZoneCursorUtils.ApplyState(0, true);

            windowRect = GUILayout.Window(0, windowRect, DrawWindow, "Editor zones");
        }

        void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load zones", GUILayout.Height(30)))
            {
                if (_zoneZoneTracker != null)
                {
                    _zoneZoneTracker.LoadZones();
                }
                else
                {
                    Logger.LogDebugInfo("[ZonesInterface] Tracker not found!");
                    LocalizationModel.NotificationWarning("Tracker not found!");
                }
            }

            if (selectedMap != null)
            {
                int zoneCount = currentMapZones != null ? currentMapZones.Count : 0;
                GUILayout.Label($"Map: {selectedMap} | Zones: {zoneCount}", GUILayout.Height(30));
            }
            else
            {
                GUILayout.Label($"Maps: {allZones.Count}", GUILayout.Height(30));
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool showOverlays = _zoneZoneTracker != null && _zoneDebugRenderer.ShowOverlays;
            bool newShowOverlays = GUILayout.Toggle(showOverlays, "Show overlay zones", GUILayout.Height(25));
            if (_zoneDebugRenderer != null && newShowOverlays != showOverlays)
            {
                _zoneDebugRenderer.ShowOverlays = newShowOverlays;
                if (newShowOverlays)
                {
                    if (selectedMap != null)
                    {
                        _zoneDebugRenderer.CreateOverlaysForCurrentZones(selectedMap);
                    }
                    else
                    {
                        _zoneDebugRenderer.CreateOverlaysForCurrentZones();
                    }
                }
                else
                {
                    _zoneDebugRenderer.ClearOverlays();
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(250));

            if (selectedMap == null)
            {
                GUILayout.Label("=== SELECT MAP ===", GUI.skin.box);

                mapsListScrollPosition = GUILayout.BeginScrollView(mapsListScrollPosition, GUILayout.Height(400));

                foreach (var map in allZones.Keys)
                {
                    int zoneCount = allZones[map]?.Count ?? 0;
                    if (GUILayout.Button($"{map}\nZones: {zoneCount}", GUILayout.Height(50)))
                    {
                        SelectMap(map);
                    }
                }

                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label($"=== ZONES: {selectedMap} ===", GUI.skin.box);

                if (GUILayout.Button("← Back to select map", GUILayout.Height(30)))
                {
                    selectedMap = null;
                    currentMapZones = null;
                    selectedZoneIndex = -1;
                    selectedZone = null;
                }

                GUILayout.Space(5);

                if (GUILayout.Button("+ Add new zone", GUILayout.Height(30)))
                {
                    AddNewZone();
                }

                GUILayout.Space(5);

                if (GUILayout.Button("RENDER", GUILayout.Height(30)))
                {
                    if (_zoneZoneTracker != null)
                    {
                        _zoneDebugRenderer.DrawZonesForMap(selectedMap);
                    }
                    else
                    {
                        Logger.LogDebugInfo("[ZonesInterface] Tracker not found!");
                        LocalizationModel.NotificationWarning("Tracker not found!");
                    }
                }

                GUILayout.Space(5);

                zonesListScrollPosition = GUILayout.BeginScrollView(zonesListScrollPosition, GUILayout.Height(300));

                if (currentMapZones == null)
                {
                    GUILayout.Label("List zones is empty!", GUI.skin.box);
                }
                else
                {
                    for (int i = 0; i < currentMapZones.Count; i++)
                    {
                        ZoneData zone = currentMapZones[i];
                        if (zone == null)
                        {
                            continue;
                        }

                        string displayName = string.IsNullOrEmpty(zone.Name) ? $"Zone {i + 1}" : zone.Name;
                        string guidShort;
                        if (string.IsNullOrEmpty(zone.GUID))
                        {
                            guidShort = "-";
                        }
                        else
                        {
                            int len = Mathf.Min(8, zone.GUID.Length);
                            guidShort = zone.GUID.Substring(0, len);
                        }

                        if (i == selectedZoneIndex)
                        {
                            GUI.backgroundColor = Color.cyan;
                        }

                        string buttonText = $"{displayName}";
                        if (!string.IsNullOrEmpty(zone.Name))
                        {
                            buttonText += $"\nGUID: {guidShort}...";
                        }

                        if (GUILayout.Button(buttonText, GUILayout.Height(50)))
                        {
                            SelectZone(i);
                        }

                        GUI.backgroundColor = Color.white;
                    }
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("=== DETAILS ZONE ===", GUI.skin.box);

            if (selectedZone != null)
            {
                detailsScrollPosition = GUILayout.BeginScrollView(detailsScrollPosition, GUILayout.Height(400));

                GUILayout.Label($"Selected zone: {selectedZone.Name}", GUI.skin.box);
                GUILayout.Space(5);

                GUILayout.Label("GUID (read-only):");
                GUILayout.Label(selectedZone.GUID ?? "Empty", GUI.skin.box);

                GUILayout.Space(5);

                GUILayout.Label("Name:");
                editedName = GUILayout.TextField(editedName);

                GUILayout.Space(5);

                GUILayout.Label("Center (Vector3):");
                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                editedCenterX = GUILayout.TextField(editedCenterX);
                GUILayout.Label("Y:", GUILayout.Width(20));
                editedCenterY = GUILayout.TextField(editedCenterY);
                GUILayout.Label("Z:", GUILayout.Width(20));
                editedCenterZ = GUILayout.TextField(editedCenterZ);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                GUILayout.Label("Scale (Vector3):");
                GUILayout.BeginHorizontal();
                GUILayout.Label("X:", GUILayout.Width(20));
                editedSizeX = GUILayout.TextField(editedSizeX);
                GUILayout.Label("Y:", GUILayout.Width(20));
                editedSizeY = GUILayout.TextField(editedSizeY);
                GUILayout.Label("Z:", GUILayout.Width(20));
                editedSizeZ = GUILayout.TextField(editedSizeZ);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);

                GUILayout.Label("Rotation by Vertical Axis:");
                editedRotationZ = GUILayout.TextField(editedRotationZ);

                GUILayout.EndScrollView();

                GUILayout.Space(10);

                if (GUILayout.Button("Apply changes", GUILayout.Height(35)))
                {
                    ApplyChanges();
                }

                if (GUILayout.Button("Cancel changes", GUILayout.Height(30)))
                {
                    SyncFieldsWithSelectedZone();
                }
            }
            else
            {
                GUILayout.Label("Select a zone from the list", GUI.skin.box);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("Save all zones in JSON", GUILayout.Height(40)))
            {
                SaveZones();
            }

            GUILayout.EndVertical();

            if (GUI.Button(new Rect(windowRect.width - 25, 5, 20, 20), "X"))
            {
                IsUIOpen = false;
            }

            GUI.DragWindow();
        }

        void OnZonesLoaded(Dictionary<string, List<ZoneData>> zones)
        {
            Logger.LogDebugInfo(
                $"[ZonesInterface] The OnZonesLoaded callback has been called! Received maps: {zones?.Count ?? 0}");

            if (zones == null)
            {
                Logger.LogDebugInfo("[ZonesInterface] Received null instead of zones!");
                LocalizationModel.NotificationWarning("Received null instead of zones!");
                return;
            }

            allZones = zones;
            selectedMap = null;
            currentMapZones = null;
            selectedZoneIndex = -1;
            selectedZone = null;

            int totalZones = 0;
            foreach (var map in allZones.Values)
            {
                if (map != null)
                    totalZones += map.Count;
            }

            Logger.LogDebugInfo($"[ZonesInterface] Uploaded {allZones.Count} maps, total {totalZones} zones");
            LocalizationModel.Notification($"Uploaded {allZones.Count} maps, total {totalZones} zones");
        }

        void SelectMap(string mapName)
        {
            if (allZones != null && allZones.ContainsKey(mapName))
            {
                selectedMap = mapName;
                currentMapZones = allZones[mapName] ?? new List<ZoneData>();
                selectedZoneIndex = -1;
                selectedZone = null;
                Logger.LogDebugInfo($"[ZonesInterface] Selected map: {mapName}, zones: {currentMapZones.Count}");
            }
        }

        void SelectZone(int index)
        {
            if (selectedMap == null || currentMapZones == null)
                return;

            if (index >= 0 && index < currentMapZones.Count)
            {
                selectedZoneIndex = index;
                selectedZone = currentMapZones[index];

                Logger.LogDebugInfo($"[ZonesInterface] Selected zone: {selectedZone.Name}, GUID: {selectedZone.GUID}");

                SyncFieldsWithSelectedZone();
            }
        }

        void AddNewZone()
        {
            if (selectedMap == null)
            {
                Logger.LogDebugInfo("[ZonesInterface] Not selected map for adding zone");
                LocalizationModel.NotificationWarning("Not selected map for adding zone");
                return;
            }

            if (allZones == null || !allZones.ContainsKey(selectedMap))
            {
                Logger.LogDebugInfo($"[ZonesInterface] Map {selectedMap} not found in allZones");
                LocalizationModel.NotificationWarning($"Map {selectedMap} not found in allZones");
                return;
            }

            ZoneData newZone = new ZoneData
            {
                GUID = System.Guid.NewGuid().ToString(),
                Name = "Новая зона",
                Center = Vector3.zero,
                Size = Vector3.one * 10f,
                RotationZ = 0f
            };

            if (allZones[selectedMap] == null)
            {
                allZones[selectedMap] = new List<ZoneData>();
            }

            allZones[selectedMap].Add(newZone);
            if (currentMapZones == null)
            {
                currentMapZones = allZones[selectedMap];
            }

            selectedZoneIndex = currentMapZones.Count - 1;
            selectedZone = newZone;
            SyncFieldsWithSelectedZone();

            Logger.LogDebugInfo($"[ZonesInterface] Added new zone in map {selectedMap}");
            LocalizationModel.Notification($"Added new zone in map {selectedMap}");
        }

        void SyncFieldsWithSelectedZone()
        {
            if (selectedZone == null)
                return;

            editedGuid = selectedZone.GUID ?? "";
            editedName = selectedZone.Name ?? "";
            editedCenterX = selectedZone.Center.x.ToString("F2");
            editedCenterY = selectedZone.Center.y.ToString("F2");
            editedCenterZ = selectedZone.Center.z.ToString("F2");
            editedSizeX = selectedZone.Size.x.ToString("F2");
            editedSizeY = selectedZone.Size.y.ToString("F2");
            editedSizeZ = selectedZone.Size.z.ToString("F2");
            editedRotationZ = selectedZone.RotationZ.ToString("F2");
        }

        void ApplyChanges()
        {
            if (selectedZone == null)
            {
                Logger.LogDebugInfo("[ZonesInterface] Not selected zone for applying changes");
                LocalizationModel.NotificationWarning("Not selected zone for applying changes");
                return;
            }

            try
            {
                selectedZone.Name = editedName ?? "";

                if (float.TryParse(editedCenterX, out float cx) &&
                    float.TryParse(editedCenterY, out float cy) &&
                    float.TryParse(editedCenterZ, out float cz))
                {
                    selectedZone.Center = new Vector3(cx, cy, cz);
                }
                else
                {
                    Logger.LogDebugInfo("[ZonesInterface] Error parsing coords center");
                    LocalizationModel.NotificationWarning("Error parsing coords center");
                }

                if (float.TryParse(editedSizeX, out float sx) &&
                    float.TryParse(editedSizeY, out float sy) &&
                    float.TryParse(editedSizeZ, out float sz))
                {
                    selectedZone.Size = new Vector3(sx, sy, sz);
                }
                else
                {
                    Logger.LogDebugInfo("[ZonesInterface] Error parse scale");
                    LocalizationModel.NotificationWarning("Error parse scale");
                }


                if (float.TryParse(editedRotationZ, out float rotationZ))
                {
                    selectedZone.RotationZ = rotationZ;
                }
                else
                {
                    Logger.LogDebugInfo("[ZonesInterface] Error parse rotation by axis Z");
                    LocalizationModel.NotificationWarning("Error parse rotation by axis Z");
                }

                Logger.LogDebugInfo($"[ZonesInterface] Changes applied to zone: {selectedZone.Name}");
                LocalizationModel.Notification($"Changes applied to zone: {selectedZone.Name}");
                Logger.LogDebugInfo(
                    $"[ZonesInterface] New values - Name: '{selectedZone.Name}', Center: {selectedZone.Center}, Size: {selectedZone.Size}, RotationZ: {selectedZone.RotationZ}");
            }
            catch (System.Exception ex)
            {
                Logger.LogDebugInfo($"[ZonesInterface] Error apply changes: {ex.Message}");
                LocalizationModel.NotificationWarning($"Error apply changes: {ex.Message}");
            }
        }

        void SaveZones()
        {
            if (_zoneZoneTracker != null && allZones != null)
            {
                _zoneZoneTracker.ZoneRepository.SaveAllZones(allZones);
                Logger.LogDebugInfo("[ZonesInterface] Zones saved");
                LocalizationModel.Notification("Zones saved");
            }
            else
            {
                LocalizationModel.NotificationWarning("Cant save: Tracker not found or zones not loaded");
                Logger.LogDebugInfo("[ZonesInterface] Cant save: Tracker not found or zones not loaded");
            }
        }
    }
}
#endif
