#if DEBUG || BETA
using System.Collections.Generic;
using SPTLeaderboard.Data;
using SPTLeaderboard.Services;
using UnityEngine;

namespace SPTLeaderboard.Utils.Zones;

public class ZoneDebugRenderer: MonoBehaviour
{
    private readonly List<Object> _debugViews = new();
    private readonly List<ZoneOverlay> _zoneOverlays = new();
    private readonly Dictionary<string, Color> _zoneColors = new();

    // Cached materials to prevent memory leaks
    private Material _normalMaterial;
    private Material _seeThroughMaterial;

    public bool ShowOverlays { get; set; }
    public ZoneTrackerService ZoneTrackerService { get; set; }

    private Material GetNormalMaterial()
    {
        if (_normalMaterial == null)
        {
            _normalMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        return _normalMaterial;
    }

    private Material GetSeeThroughMaterial()
    {
        if (_seeThroughMaterial == null)
        {
            _seeThroughMaterial = new Material(Shader.Find("GUI/Text Shader"));
        }
        return _seeThroughMaterial;
    }

    private Color GetZoneColor(string zoneGuid)
    {
        if (string.IsNullOrEmpty(zoneGuid))
        {
            return Color.white; // Default color for null/empty GUID
        }

        if (_zoneColors.TryGetValue(zoneGuid, out Color color))
        {
            return color;
        }

        try
        {
            // Generate a unique random color for this zone
            // Use zone GUID hash to ensure consistent color for the same zone
            int hash = zoneGuid.GetHashCode();
            System.Random random = new System.Random(hash);

            Color newColor = new Color(
                (float)random.NextDouble(),
                (float)random.NextDouble(),
                (float)random.NextDouble()
            );

            // Ensure minimum brightness for visibility
            float brightness = newColor.r * 0.299f + newColor.g * 0.587f + newColor.b * 0.114f;
            if (brightness < 0.3f)
            {
                // Brighten dark colors
                newColor = Color.Lerp(newColor, Color.white, 0.4f);
            }

            _zoneColors[zoneGuid] = newColor;
            return newColor;
        }
        catch (System.Exception ex)
        {
            Logger.LogDebugInfo($"[ZoneDebugRenderer] Error generating color for zone {zoneGuid}: {ex.Message}");
            return Color.white; // Fallback color
        }
    }

    public void Clear()
    {
        ClearDebugViews();
        ClearOverlays();
        _zoneColors.Clear();

        // Destroy cached materials to prevent memory leaks
        if (_normalMaterial != null)
        {
            Destroy(_normalMaterial);
            _normalMaterial = null;
        }
        if (_seeThroughMaterial != null)
        {
            Destroy(_seeThroughMaterial);
            _seeThroughMaterial = null;
        }

        Logger.LogDebugInfo($"[ZoneDebugRenderer] Cleared {_zoneColors.Count} cached zone colors and materials");
    }
    
    public void DrawZone(Vector3 Size, Vector3 Center, string zoneGuid = null)
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

        int[,] edges = new[,]
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, // низ
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, // верх
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } // вертикали
        };

        Color zoneColor = zoneGuid != null ? GetZoneColor(zoneGuid) : Color.white;

        Material zoneMaterial = SPTLeaderboard.Configuration.Settings.Instance.ZonesSeeThroughWalls.Value
            ? GetSeeThroughMaterial()
            : GetNormalMaterial();

        for (int i = 0; i < edges.GetLength(0); i++)
        {
            var go = new GameObject($"Edge{i}_{zoneGuid ?? "default"}");
            var lr = go.AddComponent<LineRenderer>();
            _debugViews.Add(lr);
            lr.widthMultiplier = 0.05f;
            lr.positionCount = 2;
            lr.startColor = zoneColor;
            lr.endColor = zoneColor;
            lr.material = zoneMaterial;

            lr.SetPosition(0, corners[edges[i, 0]]);
            lr.SetPosition(1, corners[edges[i, 1]]);
        }
    }

    public void ClearDebugViews()
    {
        foreach (var view in _debugViews)
        {
            if (view != null)
            {
                if (view is LineRenderer lr && lr.gameObject != null)
                {
                    Destroy(lr.gameObject);
                }
                else if (view is GameObject go)
                {
                    Destroy(go);
                }
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

    public void DrawZone(Vector3 Size, Vector3 Center, float rotationZ, string zoneGuid = null)
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

        int[,] edges = new[,]
        {
            { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, // down
            { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, // up
            { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } // verticals
        };

        Color zoneColor = zoneGuid != null ? GetZoneColor(zoneGuid) : Color.white;

        Material zoneMaterial = Configuration.Settings.Instance.ZonesSeeThroughWalls.Value
            ? new Material(Shader.Find("GUI/Text Shader")) // See through walls
            : new Material(Shader.Find("Sprites/Default")); // Normal rendering

        for (int i = 0; i < edges.GetLength(0); i++)
        {
            var go = new GameObject($"Edge{i}_{zoneGuid ?? "default"}");
            var lr = go.AddComponent<LineRenderer>();
            _debugViews.Add(lr);
            lr.widthMultiplier = 0.05f;
            lr.positionCount = 2;
            lr.startColor = zoneColor;
            lr.endColor = zoneColor;
            lr.material = zoneMaterial;
            lr.SetPosition(0, corners[edges[i, 0]]);
            lr.SetPosition(1, corners[edges[i, 1]]);
        }
    }

    public void DrawCoordinateAxes(Vector3 center, float axisLength = 2.0f, string zoneGuid = null)
    {
        Color axisColor = zoneGuid != null ? GetZoneColor(zoneGuid) : Color.white;

        // X axis (Red)
        DrawAxisLine(center, center + Vector3.right * axisLength, Color.red, $"XAxis_{zoneGuid ?? "default"}");

        // Y axis (Green)
        DrawAxisLine(center, center + Vector3.up * axisLength, Color.green, $"YAxis_{zoneGuid ?? "default"}");

        // Z axis (Blue)
        DrawAxisLine(center, center + Vector3.forward * axisLength, Color.blue, $"ZAxis_{zoneGuid ?? "default"}");
    }

    private void DrawAxisLine(Vector3 start, Vector3 end, Color color, string name)
    {
        var go = new GameObject(name);
        var lr = go.AddComponent<LineRenderer>();
        _debugViews.Add(lr);
        lr.widthMultiplier = 0.08f; // Thicker lines for axes
        lr.positionCount = 2;
        lr.startColor = color;
        lr.endColor = color;
        lr.material = SPTLeaderboard.Configuration.Settings.Instance.ZonesSeeThroughWalls.Value
            ? new Material(Shader.Find("GUI/Text Shader"))
            : new Material(Shader.Find("Sprites/Default"));
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }

    public void DrawZonePlanes(Vector3 Size, Vector3 Center, float rotationZ, string zoneGuid = null)
    {
        if (!SPTLeaderboard.Configuration.Settings.Instance.ShowZonePlanes.Value)
            return;

        Vector3 half = Size / 2;
        Color zoneColor = zoneGuid != null ? GetZoneColor(zoneGuid) : Color.white;
        zoneColor.a = 0.1f; // Very transparent

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

        // Create planes for each face
        // Bottom face
        CreateQuad(corners[0], corners[1], corners[2], corners[3], zoneColor, $"BottomPlane_{zoneGuid ?? "default"}");
        // Top face
        CreateQuad(corners[4], corners[7], corners[6], corners[5], zoneColor, $"TopPlane_{zoneGuid ?? "default"}");
        // Front face
        CreateQuad(corners[0], corners[3], corners[7], corners[4], zoneColor, $"FrontPlane_{zoneGuid ?? "default"}");
        // Back face
        CreateQuad(corners[1], corners[5], corners[6], corners[2], zoneColor, $"BackPlane_{zoneGuid ?? "default"}");
        // Left face
        CreateQuad(corners[0], corners[4], corners[5], corners[1], zoneColor, $"LeftPlane_{zoneGuid ?? "default"}");
        // Right face
        CreateQuad(corners[3], corners[2], corners[6], corners[7], zoneColor, $"RightPlane_{zoneGuid ?? "default"}");
    }

    private void CreateQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color color, string name)
    {
        try
        {
            var go = new GameObject(name);
            var meshFilter = go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();

            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[] { v1, v2, v3, v4 };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            // Create semi-transparent material
            var material = new Material(Shader.Find("Standard"));
            if (material == null)
            {
                Logger.LogDebugInfo("[ZoneTracker] Could not create material for zone plane");
                Destroy(go);
                return;
            }

            material.color = color;
            material.SetFloat("_Mode", 3); // Transparent mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;

            meshRenderer.material = material;
            _debugViews.Add(go);
        }
        catch (System.Exception ex)
        {
            Logger.LogDebugInfo($"[ZoneTracker] Error creating quad {name}: {ex.Message}");
        }
    }

    public void DrawZonesForMap(string mapName)
    {
        if (ZoneTrackerService == null)
        {
            Logger.LogDebugInfo("[ZoneTracker] ZoneTrackerService is null");
            return;
        }

        if (ZoneTrackerService.AllZones == null || !ZoneTrackerService.AllZones.ContainsKey(mapName))
        {
            Logger.LogDebugInfo($"[ZoneTracker] Map {mapName} not found or AllZones is null");
            return;
        }

        try
        {
            ClearDebugViews();

            var zones = ZoneTrackerService.AllZones[mapName];
            if (zones == null)
            {
                Logger.LogDebugInfo($"[ZoneTracker] List zones for map {mapName} is empty");
                return;
            }

            int renderedCount = 0;
            foreach (var zone in zones)
            {
                if (zone != null)
                {
                    DrawZoneRecursive(zone);
                    renderedCount++;
                }
            }

            Logger.LogDebugInfo($"[ZoneTracker] Rendered {renderedCount} zones for map {mapName}");
        }
        catch (System.Exception ex)
        {
            Logger.LogDebugInfo($"[ZoneTracker] Error drawing zones for map {mapName}: {ex.Message}");
        }
    }

    void DrawZoneRecursive(ZoneData zone, int level = 0)
    {
        if (zone == null) return;

        try
        {
            // Draw the main zone
            DrawZone(zone.Size, zone.Center, zone.RotationZ, zone.GUID);

            // Draw zone planes if enabled
            DrawZonePlanes(zone.Size, zone.Center, zone.RotationZ, zone.GUID);

            // Draw coordinate axes for the zone if enabled
            if (SPTLeaderboard.Configuration.Settings.Instance?.ShowCoordinateAxes?.Value == true)
            {
                DrawCoordinateAxes(zone.Center, zone.Size.magnitude * 0.3f, zone.GUID);
            }

            if (ShowOverlays)
            {
                CreateZoneOverlay(zone, level);
            }

            if (zone.SubZones != null && zone.SubZones.Count > 0)
            {
                foreach (var subZone in zone.SubZones)
                {
                    if (subZone != null)
                    {
                        DrawZoneRecursive(subZone, level + 1);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogDebugInfo($"[ZoneTracker] Error drawing zone {zone?.Name ?? "null"}: {ex.Message}");
        }
    }

    void CreateZoneOverlay(ZoneData zone, int level = 0)
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

        GameObject overlayObj = new GameObject($"ZoneOverlay_{zone.Name}_{zone.GUID}_Level{level}");
        ZoneOverlay zoneOverlay = overlayObj.AddComponent<ZoneOverlay>();
        zoneOverlay.transform.position = zone.Center;
        zoneOverlay.Initialize(zone, cam, level);
        _zoneOverlays.Add(zoneOverlay);

        Logger.LogDebugInfo($"[ZoneTracker] An overlay has been created for the zone: {zone.Name} (Level: {level})");
    }

    public void CreateOverlaysForCurrentZones(string mapName = null)
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

        if (ZoneTrackerService.AllZones != null)
        {
            if (mapName != null && ZoneTrackerService.AllZones.ContainsKey(mapName))
            {
                var category = ZoneTrackerService.AllZones[mapName];
                if (category != null)
                {
                    foreach (var zone in category)
                    {
                        if (zone != null)
                        {
                            createdCount += CreateZoneOverlaysRecursive(zone);
                        }
                    }
                }
            }
        }

        int CreateZoneOverlaysRecursive(ZoneData zone, int level = 0)
        {
            int count = 0;

            CreateZoneOverlay(zone, level);
            count++;

            // Recursively create overlays for sub-zones
            if (zone.SubZones != null && zone.SubZones.Count > 0)
            {
                foreach (var subZone in zone.SubZones)
                {
                    if (subZone != null)
                    {
                        count += CreateZoneOverlaysRecursive(subZone, level + 1);
                    }
                }
            }

            return count;
        }

        Logger.LogDebugInfo(
            $"[ZoneTracker] {createdCount} overlays created (total in the list: {_zoneOverlays.Count})");
    }
}
#endif