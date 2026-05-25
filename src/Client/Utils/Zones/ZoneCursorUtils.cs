using System;
using System.Reflection;
using UnityEngine;

namespace SPTLeaderboard.Utils.Zones
{
    // Code copied from https://github.com/Drexira/DragonDen-DevTool
    internal static class ZoneCursorUtils
    {
        private static PropertyInfo s_lockProperty;
        private static PropertyInfo s_visibleProperty;
        private static bool s_useLegacyApi;

        private static bool s_isUiOpen;
        private static object s_previousLockValue;
        private static bool s_previousVisible;

        public static bool IsUiOpen
        {
            get => s_isUiOpen;
            set
            {
                if (s_isUiOpen == value) return;
                s_isUiOpen = value;

                if (s_isUiOpen)
                {
                    if (s_lockProperty == null || s_visibleProperty == null) return;
                    s_previousLockValue = s_lockProperty.GetValue(null, null);
                    s_previousVisible = (bool)s_visibleProperty.GetValue(null, null);
                }
                else
                {
                    if (s_lockProperty == null || s_visibleProperty == null) return;
                    ApplyState(s_previousLockValue, s_previousVisible);
                }
            }
        }

        public static void Initialize()
        {
            var cursorType = typeof(Cursor);
            s_lockProperty = cursorType.GetProperty("lockState", BindingFlags.Static | BindingFlags.Public);
            s_visibleProperty = cursorType.GetProperty("visible", BindingFlags.Static | BindingFlags.Public);

            if (s_lockProperty != null && s_visibleProperty != null)
            {
                s_useLegacyApi = false;
                return;
            }

            s_useLegacyApi = true;
            s_lockProperty = typeof(Screen).GetProperty("lockCursor", BindingFlags.Static | BindingFlags.Public);
            s_visibleProperty = typeof(Screen).GetProperty("showCursor", BindingFlags.Static | BindingFlags.Public);
        }

        public static void ApplyState(object lockValue, bool isVisible)
        {
            if (s_lockProperty == null || s_visibleProperty == null) return;

            if (s_useLegacyApi)
            {
                var legacy = Convert.ToBoolean(lockValue);
                s_lockProperty.SetValue(null, legacy, null);
            }
            else
            {
                s_lockProperty.SetValue(null, lockValue, null);
            }

            s_visibleProperty.SetValue(null, isVisible, null);
        }
    }
}