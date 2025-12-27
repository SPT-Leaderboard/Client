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
    
        public void Initialize(ZoneData zone, Camera camera)
        {
            zoneData = zone;
            mainCamera = camera;
            isVisible = true;

            SetOverlayContent();
        
            if (!CameraClass.Instance.SSAA.isActiveAndEnabled) return;
        
            _screenScale = CameraClass.Instance.SSAA.GetOutputWidth() / (float)CameraClass.Instance.SSAA.GetInputWidth();
            Logger.LogDebugInfo($"[ZoneOverlay] The overlay for the zone has been initialized: {zone.Name}");
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
        }
    
        public void SetOverlayContent()
        {
            string guidShort = string.IsNullOrEmpty(zoneData.GUID) ? "N/A" : zoneData.GUID.Substring(0, Mathf.Min(8, zoneData.GUID.Length));
            string text = $"{zoneData.Name}\n" +
                          $"GUID: {guidShort}...\n" +
                          $"Center: ({zoneData.Center.x:F1}, {zoneData.Center.y:F1}, {zoneData.Center.z:F1})\n" +
                          $"Size: ({zoneData.Size.x:F1}, {zoneData.Size.y:F1}, {zoneData.Size.z:F1})\n" +
                          $"Rotation Z: {zoneData.RotationZ:F1}°";

            _content.text = text;
        }
    }
}
#endif
