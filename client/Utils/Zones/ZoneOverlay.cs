#if DEBUG || BETA
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Data;
using UnityEngine;

namespace SPTLeaderboard.Utils.Zones
{
    public class ZoneOverlay : MonoBehaviour
    {
        public ZoneData zoneData;
        private Camera mainCamera;
        private GUIStyle guiStyle;
        private float _screenScale = 1.0f;

        private GUIContent _content = new();
        private Rect _rect;

        private bool isVisible;
        private int zoneLevel; // 0 = main zone, 1 = sub-zone, 2 = sub-sub-zone, etc.

        // Corner coordinate labels

        public void Initialize(ZoneData zone, Camera camera, int level = 0)
        {
            if (zone == null || camera == null)
            {
                Logger.LogDebugInfo("[ZoneOverlay] Cannot initialize overlay with null zone or camera");
                return;
            }

            zoneData = zone;
            mainCamera = camera;
            zoneLevel = level;
            isVisible = true;

            SetOverlayContent();

            try
            {
                if (CameraClass.Instance?.SSAA?.isActiveAndEnabled == true)
                {
                    _screenScale = CameraClass.Instance.SSAA.GetOutputWidth() / (float)CameraClass.Instance.SSAA.GetInputWidth();
                }
            }
            catch (System.Exception ex)
            {
                Logger.LogDebugInfo($"[ZoneOverlay] Error getting screen scale: {ex.Message}");
                _screenScale = 1.0f;
            }

            Logger.LogDebugInfo($"[ZoneOverlay] The overlay for the zone has been initialized: {zone.Name} (Level: {zoneLevel})");
        }
    
        private void OnGUI()
        {
            if (!isVisible || zoneData == null || mainCamera == null) return;

            if (guiStyle == null)
            {
                CreateGuiStyle();
            }

            try
            {
                var pos = transform.position;
                var dist = Mathf.RoundToInt((transform.position - mainCamera.transform.position).magnitude);

                if (_content.text.Length <= 0 || Settings.Instance == null || !(dist < Settings.Instance.OverlayMaxDist.Value)) return;

                var screenPos = mainCamera.WorldToScreenPoint(pos + (Vector3.up * Settings.Instance.OverlayUpDist.Value));

                if (screenPos.z <= 0) return;

                SetRectSize(screenPos);

                GUI.Box(_rect, _content, guiStyle);
            }
            catch (System.Exception ex)
            {
                Logger.LogDebugInfo($"[ZoneOverlay] Error in OnGUI: {ex.Message}");
                isVisible = false; // Disable overlay on error to prevent spam
            }
        }

    
        private void SetRectSize(Vector3 screenPos)
        {
            var guiSize = guiStyle.CalcSize(_content);
            _rect.x = (screenPos.x * _screenScale) - (guiSize.x / 2);
            _rect.y = Screen.height - ((screenPos.y * _screenScale) + guiSize.y);
            _rect.size = guiSize;
        }
	
        private void CreateGuiStyle()
        {
            if (guiStyle != null) return; // Already created

            try
            {
                guiStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleLeft,
                    margin = new RectOffset(3, 3, 3, 3),
                    richText = true
                };

                if (Settings.Instance != null)
                {
                    guiStyle.fontSize = Settings.Instance.OverlayFontSize.Value;
                }

                // Set background color based on zone level
                Texture2D backgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                Color backgroundColor;

                switch (zoneLevel)
                {
                    case 0: // Main zone - original color
                        backgroundColor = new Color(0, 0, 0, 0.8f); // Semi-transparent black
                        guiStyle.normal.textColor = Color.white;
                        break;
                    case 1: // Sub-zone - different color
                        backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Semi-transparent gray
                        guiStyle.normal.textColor = Color.black;
                        break;
                    case 2: // Sub-sub-zone - another different color
                        backgroundColor = new Color(0, 0.5f, 0, 0.8f); // Semi-transparent green
                        guiStyle.normal.textColor = Color.black;
                        break;
                    default: // Deeper nesting - blue tint
                        backgroundColor = new Color(0, 0.5f, 0.5f, 0.8f); // Semi-transparent cyan
                        guiStyle.normal.textColor = Color.white;
                        break;
                }

                backgroundTexture.SetPixel(0, 0, backgroundColor);
                backgroundTexture.Apply();
                guiStyle.normal.background = backgroundTexture;
            }
            catch (System.Exception ex)
            {
                Logger.LogDebugInfo($"[ZoneOverlay] Error creating GUI style: {ex.Message}");
                guiStyle = GUI.skin.box; // Fallback to default
            }
        }
    
        public void SetOverlayContent()
        {
            if (zoneData == null)
            {
                _content.text = "Zone data is null";
                return;
            }

            try
            {
                string levelIndicator = zoneLevel > 0 ? "[Sub-Zone] " : "[Main Zone] ";
                string guidShort = string.IsNullOrEmpty(zoneData.GUID) ? "N/A" : zoneData.GUID.Substring(0, Mathf.Min(8, zoneData.GUID.Length));
                string zoneName = string.IsNullOrEmpty(zoneData.Name) ? "Unnamed Zone" : zoneData.Name;

                string text = $"{levelIndicator}{zoneName}\n" +
                              $"GUID: {guidShort}...\n" +
                              $"Center: ({zoneData.Center.x:F1}, {zoneData.Center.y:F1}, {zoneData.Center.z:F1})\n" +
                              $"Size: ({zoneData.Size.x:F1}, {zoneData.Size.y:F1}, {zoneData.Size.z:F1})\n" +
                              $"Rotation Z: {zoneData.RotationZ:F1}°";

                _content.text = text;
            }
            catch (System.Exception ex)
            {
                Logger.LogDebugInfo($"[ZoneOverlay] Error setting overlay content: {ex.Message}");
                _content.text = "Error loading zone data";
            }
        }

    }
}
#endif
