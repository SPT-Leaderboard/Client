#if DEBUG || BETA
using System.Collections.Generic;
using SPTLeaderboard.Data;
using UnityEngine;

namespace SPTLeaderboard.Utils.Zones;

public class ZoneDebugRenderer: MonoBehaviour
{
    private readonly List<LineRenderer> _debugViews = new();
    private readonly List<ZoneOverlay> _zoneOverlays = new();

    public bool ShowOverlays { get; set; }
    public ZoneTracker ZoneTracker { get; set; }
    
    public void Clear()
    {
        ClearDebugViews();
        ClearOverlays();
    }
    
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
            _debugViews.Add(lr);
            lr.widthMultiplier = 0.05f;
            lr.positionCount = 2;

            lr.SetPosition(0, corners[edges[i, 0]]);
            lr.SetPosition(1, corners[edges[i, 1]]);
        }
    }

    public void ClearDebugViews()
    {
        foreach (var lr in _debugViews)
        {
            if (lr != null && lr.gameObject != null)
            {
                Destroy(lr.gameObject);
            }
        }

        _debugViews.Clear();
        
        ClearOverlays();

        Logger.LogDebugInfo("[ZoneTracker] All rendered zones have been cleared.");
    }

    public void ClearOverlays()
    {
        foreach (var overlay in _zoneOverlays)
        {
            if (overlay != null && overlay.gameObject != null)
            {
                Destroy(overlay.gameObject);
            }
        }

        _zoneOverlays.Clear();
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
            _debugViews.Add(lr);
            lr.widthMultiplier = 0.05f;
            lr.positionCount = 2;
            lr.SetPosition(0, corners[edges[i, 0]]);
            lr.SetPosition(1, corners[edges[i, 1]]);
        }
    }

    public void DrawZonesForMap(string mapName)
    {
        if (ZoneTracker.AllZones == null || !ZoneTracker.AllZones.ContainsKey(mapName))
        {
            Logger.LogDebugInfo($"[ZoneTracker] Map {mapName} not found");
            return;
        }

        ClearDebugViews();

        var zones = ZoneTracker.AllZones[mapName];
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
        _zoneOverlays.Add(zoneOverlay);

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

        if (ZoneTracker.AllZones != null)
        {
            if (categoryName != null && ZoneTracker.AllZones.ContainsKey(categoryName))
            {
                var category = ZoneTracker.AllZones[categoryName];
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
                foreach (var category in ZoneTracker.AllZones.Values)
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
            $"[ZoneTracker] {createdCount} overlays created (total in the list: {_zoneOverlays.Count})");
    }
}
#endif