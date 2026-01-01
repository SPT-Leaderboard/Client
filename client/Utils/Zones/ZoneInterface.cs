#if DEBUG || BETA
using System;
using System.Collections.Generic;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Data;
using SPTLeaderboard.Services;
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

        // Sub-zone navigation
        private ZoneData currentParentZone;
        private List<ZoneData> currentZoneHierarchy = new();

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

        private bool _liveUpdateEnabled = true;
        private string _lastCenterX, _lastCenterY, _lastCenterZ;
        private string _lastSizeX, _lastSizeY, _lastSizeZ;
        private string _lastRotationZ;
        private bool _wasUIOpen;
        private PlayerRotateBlocker _rotateBlocker;

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
                LocalizationService.NotificationWarning("Tracker not found!");
            }
        }

        void OnDestroy()
        {
            _rotateBlocker?.Unlock();
            _rotateBlocker = null;
            if (_zoneZoneTracker != null)
            {
                _zoneZoneTracker.OnZonesLoaded -= OnZonesLoaded;
            }
        }

        void LateUpdate()
        {
            if (IsUIOpen) ZoneCursorUtils.ApplyState(0, true);
        }

        private void Update()
        {
            if (Settings.Instance.ToggleZonesInterfaceKey.Value.IsDown())
            {
                IsUIOpen = !IsUIOpen;
                Cursor.lockState = IsUIOpen ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = IsUIOpen;
                LocalizationService.NotificationWarning($"ZonesInterface: {IsUIOpen}");
            }
            
            if (_wasUIOpen != IsUIOpen)
            {
                if (IsUIOpen)
                {
                    if (!PlayerHelper.Instance.Player)
                    {
                        return;
                    }

                    if (_rotateBlocker == null && PlayerHelper.Instance.Player)
                    {
                        _rotateBlocker = new PlayerRotateBlocker(PlayerHelper.Instance.Player);
                    }

                    _rotateBlocker?.Lock();
                }
                else
                {
                    _rotateBlocker?.Unlock();
                }

                _wasUIOpen = IsUIOpen;
            }
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
                    LocalizationService.NotificationWarning("Tracker not found!");
                }
            }

            if (selectedMap != null)
            {
                int zoneCount = currentMapZones != null ? currentMapZones.Count : 0;
                int totalZones = GetTotalZoneCount(selectedMap);
                string zoneText = currentParentZone == null ?
                    $"Map: {selectedMap} | Zones: {zoneCount} (Total: {totalZones})" :
                    $"Sub-zones: {zoneCount}";
                GUILayout.Label(zoneText, GUILayout.Height(30));
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
                string zonePath = BuildZonePathString();
                GUILayout.Label($"=== ZONES: {selectedMap}{zonePath} ===", GUI.skin.box);

                if (currentParentZone != null)
                {
                    if (GUILayout.Button("← Back to parent zone", GUILayout.Height(30)))
                    {
                        NavigateToParentZone();
                    }
                }
                else
                {
                    if (GUILayout.Button("← Back to select map", GUILayout.Height(30)))
                    {
                        selectedMap = null;
                        currentMapZones = null;
                        selectedZoneIndex = -1;
                        selectedZone = null;
                        currentParentZone = null;
                        currentZoneHierarchy.Clear();
                    }
                }

                GUILayout.Space(5);

                if (currentParentZone == null)
                {
                    if (GUILayout.Button("+ Add new zone", GUILayout.Height(30)))
                    {
                        AddNewZone();
                    }
                }
                else
                {
                    if (GUILayout.Button("+ Add new sub-zone", GUILayout.Height(30)))
                    {
                        AddNewSubZone();
                    }
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
                        LocalizationService.NotificationWarning("Tracker not found!");
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
                        
                        bool hasSubZones = zone.SubZones != null && zone.SubZones.Count > 0;
                        if (hasSubZones)
                        {
                            buttonText += $"\n[Sub-zones: {zone.SubZones.Count}]";
                        }

                        if (GUILayout.Button(buttonText, GUILayout.Height(hasSubZones ? 60 : 50)))
                        {
                            SelectZone(i);
                        }

                        GUI.backgroundColor = Color.white;
                        
                        if (hasSubZones && currentParentZone == null)
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(20);
                            if (GUILayout.Button($"→ Enter {zone.Name}", GUILayout.Height(25)))
                            {
                                NavigateToSubZones(zone);
                            }
                            GUILayout.EndHorizontal();
                            GUILayout.Space(5);
                        }
                    }
                }

                GUILayout.EndScrollView();
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("=== DETAILS ZONE ===", GUI.skin.box);

            // Live update toggle
            _liveUpdateEnabled = GUILayout.Toggle(_liveUpdateEnabled, "Live Visual Updates", GUILayout.Height(25));

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
                if (GUILayout.Button("📍", GUILayout.Width(30), GUILayout.Height(20)))
                {
                    SetCenterToPlayerPosition();
                    GUI.FocusControl(null); // Remove focus to ensure text fields update
                }
                GUILayout.EndHorizontal();

                // Live update center if values changed
                if (_liveUpdateEnabled && selectedZone != null &&
                    (_lastCenterX != editedCenterX || _lastCenterY != editedCenterY || _lastCenterZ != editedCenterZ ||
                     _lastSizeX != editedSizeX || _lastSizeY != editedSizeY || _lastSizeZ != editedSizeZ ||
                     _lastRotationZ != editedRotationZ))
                {
                    UpdateZoneVisualWithEditedValues();

                    // Update last values
                    _lastCenterX = editedCenterX;
                    _lastCenterY = editedCenterY;
                    _lastCenterZ = editedCenterZ;
                    _lastSizeX = editedSizeX;
                    _lastSizeY = editedSizeY;
                    _lastSizeZ = editedSizeZ;
                    _lastRotationZ = editedRotationZ;
                }

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

                // Sub-zone management buttons
                if (selectedZone != null)
                {
                    if (currentParentZone == null) // Only show for main zones
                    {
                        if (GUILayout.Button("Add Sub-Zone", GUILayout.Height(30)))
                        {
                            AddSubZoneToSelectedZone();
                        }

                        // Only allow deleting main zones if they have no sub-zones
                        bool canDelete = selectedZone.SubZones == null || selectedZone.SubZones.Count == 0;
                        if (canDelete)
                        {
                            if (GUILayout.Button("Delete Zone", GUILayout.Height(30)))
                            {
                                DeleteSelectedZone();
                            }
                        }
                        else
                        {
                            GUI.enabled = false;
                            GUILayout.Button("Delete Zone (has sub-zones)", GUILayout.Height(30));
                            GUI.enabled = true;
                        }
                    }

                    if (currentParentZone != null) // Show for sub-zones
                    {
                        if (GUILayout.Button("Delete Sub-Zone", GUILayout.Height(30)))
                        {
                            DeleteSelectedSubZone();
                        }
                    }
                }

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
                LocalizationService.NotificationWarning("Received null instead of zones!");
                return;
            }

            allZones = zones;
            selectedMap = null;
            currentMapZones = null;
            selectedZoneIndex = -1;
            selectedZone = null;
            currentParentZone = null;
            currentZoneHierarchy.Clear();

            int totalZones = 0;
            foreach (var map in allZones.Values)
            {
                if (map != null)
                    totalZones += map.Count;
            }

            Logger.LogDebugInfo($"[ZonesInterface] Uploaded {allZones.Count} maps, total {totalZones} zones");
            LocalizationService.Notification($"Uploaded {allZones.Count} maps, total {totalZones} zones");
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

                // Initialize last values for change detection
                _lastCenterX = editedCenterX;
                _lastCenterY = editedCenterY;
                _lastCenterZ = editedCenterZ;
                _lastSizeX = editedSizeX;
                _lastSizeY = editedSizeY;
                _lastSizeZ = editedSizeZ;
                _lastRotationZ = editedRotationZ;
            }
        }

        void AddNewZone()
        {
            if (selectedMap == null)
            {
                Logger.LogDebugInfo("[ZonesInterface] Not selected map for adding zone");
                LocalizationService.NotificationWarning("Not selected map for adding zone");
                return;
            }

            if (allZones == null || !allZones.ContainsKey(selectedMap))
            {
                Logger.LogDebugInfo($"[ZonesInterface] Map {selectedMap} not found in allZones");
                LocalizationService.NotificationWarning($"Map {selectedMap} not found in allZones");
                return;
            }

            ZoneData newZone = new ZoneData
            {
                GUID = Guid.NewGuid().ToString(),
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
            LocalizationService.Notification($"Added new zone in map {selectedMap}");
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
                LocalizationService.NotificationWarning("Not selected zone for applying changes");
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
                    LocalizationService.NotificationWarning("Error parsing coords center");
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
                    LocalizationService.NotificationWarning("Error parse scale");
                }


                if (float.TryParse(editedRotationZ, out float rotationZ))
                {
                    selectedZone.RotationZ = rotationZ;
                }
                else
                {
                    Logger.LogDebugInfo("[ZonesInterface] Error parse rotation by axis Z");
                    LocalizationService.NotificationWarning("Error parse rotation by axis Z");
                }

                Logger.LogDebugInfo($"[ZonesInterface] Changes applied to zone: {selectedZone.Name}");
                LocalizationService.Notification($"Changes applied to zone: {selectedZone.Name}");
                Logger.LogDebugInfo(
                    $"[ZonesInterface] New values - Name: '{selectedZone.Name}', Center: {selectedZone.Center}, Size: {selectedZone.Size}, RotationZ: {selectedZone.RotationZ}");

                // Auto-render zones after applying changes
                if (_zoneDebugRenderer != null && selectedMap != null)
                {
                    _zoneDebugRenderer.DrawZonesForMap(selectedMap);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebugInfo($"[ZonesInterface] Error apply changes: {ex.Message}");
                LocalizationService.NotificationWarning($"Error apply changes: {ex.Message}");
            }
        }

        void SaveZones()
        {
            if (_zoneZoneTracker != null && allZones != null)
            {
                _zoneZoneTracker.ZoneRepository.SaveAllZones(allZones);
                Logger.LogDebugInfo("[ZonesInterface] Zones saved");
                LocalizationService.Notification("Zones saved");
            }
            else
            {
                LocalizationService.NotificationWarning("Cant save: Tracker not found or zones not loaded");
                Logger.LogDebugInfo("[ZonesInterface] Cant save: Tracker not found or zones not loaded");
            }
        }

        string BuildZonePathString()
        {
            if (currentZoneHierarchy.Count == 0)
                return "";

            string path = " > ";
            for (int i = 0; i < currentZoneHierarchy.Count; i++)
            {
                path += currentZoneHierarchy[i].Name;
                if (i < currentZoneHierarchy.Count - 1)
                    path += " > ";
            }
            return path;
        }

        void NavigateToParentZone()
        {
            if (currentZoneHierarchy.Count > 0)
            {
                currentZoneHierarchy.RemoveAt(currentZoneHierarchy.Count - 1);
            }

            if (currentZoneHierarchy.Count == 0)
            {
                currentParentZone = null;
                currentMapZones = allZones[selectedMap] ?? new List<ZoneData>();
            }
            else
            {
                currentParentZone = currentZoneHierarchy[currentZoneHierarchy.Count - 1];
                currentMapZones = currentParentZone.SubZones ?? new List<ZoneData>();
            }

            selectedZoneIndex = -1;
            selectedZone = null;
        }

        void NavigateToSubZones(ZoneData zone)
        {
            currentZoneHierarchy.Add(zone);
            currentParentZone = zone;
            currentMapZones = zone.SubZones ?? new List<ZoneData>();
            selectedZoneIndex = -1;
            selectedZone = null;
        }

        void AddNewSubZone()
        {
            if (currentParentZone == null)
            {
                Logger.LogDebugInfo("[ZonesInterface] Not selected parent zone for adding sub-zone");
                LocalizationService.NotificationWarning("Not selected parent zone for adding sub-zone");
                return;
            }

            ZoneData newSubZone = new ZoneData
            {
                GUID = Guid.NewGuid().ToString(),
                Name = "Новая подзона",
                Center = Vector3.zero,
                Size = Vector3.one * 5f,
                RotationZ = 0f
            };

            if (currentParentZone.SubZones == null)
            {
                currentParentZone.SubZones = new List<ZoneData>();
            }

            currentParentZone.SubZones.Add(newSubZone);
            currentMapZones = currentParentZone.SubZones;

            selectedZoneIndex = currentMapZones.Count - 1;
            selectedZone = newSubZone;
            SyncFieldsWithSelectedZone();

            Logger.LogDebugInfo($"[ZonesInterface] Added new sub-zone in zone {currentParentZone.Name}");
            LocalizationService.Notification($"Added new sub-zone in zone {currentParentZone.Name}");
        }

        int GetTotalZoneCount(string mapName)
        {
            if (!allZones.ContainsKey(mapName) || allZones[mapName] == null)
                return 0;

            return CountZonesRecursive(allZones[mapName]);
        }

        int CountZonesRecursive(List<ZoneData> zones)
        {
            int count = zones.Count;
            foreach (var zone in zones)
            {
                if (zone != null && zone.SubZones != null)
                {
                    count += CountZonesRecursive(zone.SubZones);
                }
            }
            return count;
        }

        void AddSubZoneToSelectedZone()
        {
            if (selectedZone == null)
            {
                Logger.LogDebugInfo("[ZonesInterface] No zone selected for adding sub-zone");
                LocalizationService.NotificationWarning("No zone selected for adding sub-zone");
                return;
            }

            ZoneData newSubZone = new ZoneData
            {
                GUID = Guid.NewGuid().ToString(),
                Name = $"{selectedZone.Name} Sub-Zone",
                Center = selectedZone.Center,
                Size = selectedZone.Size * 0.5f, // Make sub-zones smaller by default
                RotationZ = selectedZone.RotationZ
            };

            if (selectedZone.SubZones == null)
            {
                selectedZone.SubZones = new List<ZoneData>();
            }

            selectedZone.SubZones.Add(newSubZone);

            Logger.LogDebugInfo($"[ZonesInterface] Added sub-zone '{newSubZone.Name}' to zone '{selectedZone.Name}'");
            LocalizationService.Notification($"Added sub-zone to {selectedZone.Name}");

            // Refresh the display if we're currently viewing this zone's sub-zones
            if (currentParentZone == selectedZone)
            {
                currentMapZones = selectedZone.SubZones;
            }
        }

        void DeleteSelectedSubZone()
        {
            if (selectedZone == null || currentParentZone == null)
            {
                Logger.LogDebugInfo("[ZonesInterface] Cannot delete: no sub-zone selected or not in sub-zone view");
                LocalizationService.NotificationWarning("Cannot delete: no sub-zone selected");
                return;
            }

            if (currentParentZone.SubZones == null || !currentParentZone.SubZones.Contains(selectedZone))
            {
                Logger.LogDebugInfo("[ZonesInterface] Selected zone not found in parent sub-zones");
                LocalizationService.NotificationWarning("Selected zone not found in parent");
                return;
            }

            string zoneName = selectedZone.Name;
            currentParentZone.SubZones.Remove(selectedZone);

            // Update the current view
            currentMapZones = currentParentZone.SubZones ?? new List<ZoneData>();
            selectedZoneIndex = -1;
            selectedZone = null;

            Logger.LogDebugInfo($"[ZonesInterface] Deleted sub-zone '{zoneName}' from zone '{currentParentZone.Name}'");
            LocalizationService.Notification($"Deleted sub-zone {zoneName}");
        }

        void DeleteSelectedZone()
        {
            if (selectedZone == null || currentParentZone != null || selectedMap == null)
            {
                Logger.LogDebugInfo("[ZonesInterface] Cannot delete: invalid state for zone deletion");
                LocalizationService.NotificationWarning("Cannot delete zone in current state");
                return;
            }

            if (!allZones.ContainsKey(selectedMap) || allZones[selectedMap] == null)
            {
                Logger.LogDebugInfo("[ZonesInterface] Map not found for zone deletion");
                LocalizationService.NotificationWarning("Map not found");
                return;
            }

            var zoneList = allZones[selectedMap];
            if (!zoneList.Contains(selectedZone))
            {
                Logger.LogDebugInfo("[ZonesInterface] Selected zone not found in map");
                LocalizationService.NotificationWarning("Zone not found in map");
                return;
            }

            string zoneName = selectedZone.Name;
            zoneList.Remove(selectedZone);

            // Update the current view
            currentMapZones = zoneList;
            selectedZoneIndex = -1;
            selectedZone = null;

            Logger.LogDebugInfo($"[ZonesInterface] Deleted zone '{zoneName}' from map '{selectedMap}'");
            LocalizationService.Notification($"Deleted zone {zoneName}");
        }

        void SetCenterToPlayerPosition()
        {
            try
            {
                var player = PlayerHelper.Instance.Player;
                if (player == null)
                {
                    Logger.LogDebugInfo("[ZonesInterface] Player not found for position setting");
                    LocalizationService.NotificationWarning("Player not found");
                    return;
                }

                Vector3 playerPos = player.PlayerBones.transform.position;
                editedCenterX = playerPos.x.ToString("F2");
                editedCenterY = playerPos.y.ToString("F2");
                editedCenterZ = playerPos.z.ToString("F2");

                Logger.LogDebugInfo($"[ZonesInterface] Set zone center to player position: {playerPos}");
                LocalizationService.Notification("Center set to player position");

                // Update last values to prevent immediate re-trigger
                _lastCenterX = editedCenterX;
                _lastCenterY = editedCenterY;
                _lastCenterZ = editedCenterZ;

                // Update visual immediately
                UpdateZoneVisualWithEditedValues();
            }
            catch (Exception ex)
            {
                Logger.LogDebugInfo($"[ZonesInterface] Error getting player position: {ex.Message}");
                LocalizationService.NotificationWarning($"Error getting player position: {ex.Message}");
            }
        }

        void UpdateZoneVisualWithEditedValues()
        {
            if (selectedZone == null || _zoneDebugRenderer == null || selectedMap == null)
                return;

            // Simply re-render all zones for the current map
            _zoneDebugRenderer.DrawZonesForMap(selectedMap);
        }
    }
}
#endif
