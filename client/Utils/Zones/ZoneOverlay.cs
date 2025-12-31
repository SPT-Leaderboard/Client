#if DEBUG || BETA
using System;
using SPTLeaderboard.Data;
using SPTLeaderboard.Models;
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
        private int zoneLevel = 0; // 0 = main zone, 1 = sub-zone, 2 = sub-sub-zone, etc.

        public void Initialize(ZoneData zone, Camera camera, int level = 0)
        {
            zoneData = zone;
            mainCamera = camera;
            zoneLevel = level;
            isVisible = true;

            SetOverlayContent();

            if (!CameraClass.Instance.SSAA.isActiveAndEnabled) return;

            _screenScale = CameraClass.Instance.SSAA.GetOutputWidth() / (float)CameraClass.Instance.SSAA.GetInputWidth();
            Logger.LogDebugInfo($"[ZoneOverlay] The overlay for the zone has been initialized: {zone.Name} (Level: {zoneLevel})");
        }
    
        private void OnGUI()
        {
            if (guiStyle is null)
            {
                CreateGuiStyle();
            }

            if (!isVisible) return;
		
            var pos = transform.position;
            var dist = Mathf.RoundToInt((transform.position - mainCamera.transform.position).magnitude);
		
            if (_content.text.Length <= 0 || !(dist < SettingsModel.Instance.OverlayMaxDist.Value)) return;
		
            var screenPos = mainCamera.WorldToScreenPoint(pos + (Vector3.up * SettingsModel.Instance.OverlayUpDist.Value));
        
            if (screenPos.z <= 0) return;
		
            SetRectSize(screenPos);
		
            GUI.Box(_rect, _content, guiStyle);
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
            guiStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = SettingsModel.Instance.OverlayFontSize.Value,
                margin = new RectOffset(3, 3, 3, 3),
                richText = true
            };

            // Set background color based on zone level
            Texture2D backgroundTexture = new Texture2D(1, 1);
            Color backgroundColor;

            switch (zoneLevel)
            {
                case 0: // Main zone - original color
                    backgroundColor = Color.black;
                    guiStyle.normal.textColor = Color.white;
                    break;
                case 1: // Sub-zone - different color
                    backgroundColor = Color.gray;
                    guiStyle.normal.textColor = Color.black;
                    break;
                case 2: // Sub-sub-zone - another different color
                    backgroundColor = Color.green;
                    guiStyle.normal.textColor = Color.black;
                    break;
                default: // Deeper nesting - blue tint
                    backgroundColor = Color.cyan;
                    guiStyle.normal.textColor = Color.white;
                    break;
            }

            backgroundTexture.SetPixel(0, 0, backgroundColor);
            backgroundTexture.Apply();
            guiStyle.normal.background = backgroundTexture;
        }
    
        public void SetOverlayContent()
        {
            string levelIndicator = zoneLevel > 0 ? "[Sub-Zone] " : "[Main Zone] ";
            string guidShort = string.IsNullOrEmpty(zoneData.GUID) ? "N/A" : zoneData.GUID.Substring(0, Mathf.Min(8, zoneData.GUID.Length));
            string text = $"{levelIndicator}{zoneData.Name}\n" +
                          $"GUID: {guidShort}...\n" +
                          $"Center: ({zoneData.Center.x:F1}, {zoneData.Center.y:F1}, {zoneData.Center.z:F1})\n" +
                          $"Size: ({zoneData.Size.x:F1}, {zoneData.Size.y:F1}, {zoneData.Size.z:F1})\n" +
                          $"Rotation Z: {zoneData.RotationZ:F1}°";
                          
            _content.text = text;
        }
    }
}
#endif
