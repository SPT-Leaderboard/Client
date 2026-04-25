using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using SPTLeaderboard.Data;

namespace SPTLeaderboard.Integrations
{
    public static class FikaInterop
    {
        public const string FikaCoreGuid = "com.fika.core";
        public const string FikaHeadlessGuid = "com.fika.headless";

        private const string FikaBackendUtilsTypeName = "Fika.Core.Main.Utils.FikaBackendUtils";

        public static bool IsCheckedFikaCore { get; private set; }
        public static bool IsCheckedFikaHeadless { get; private set; }

        public static BaseUnityPlugin FikaCore { get; private set; }
        public static BaseUnityPlugin FikaHeadless { get; private set; }

        public static void CheckFikaCore(Action<bool> callback)
        {
            bool found = Chainloader.PluginInfos.TryGetValue(FikaCoreGuid, out var info);
            FikaCore = found ? info.Instance : null;
            IsCheckedFikaCore = true;
            callback.Invoke(FikaCore != null);
        }

        public static void CheckFikaHeadless(Action<bool> callback)
        {
            bool found = Chainloader.PluginInfos.TryGetValue(FikaHeadlessGuid, out var info);
            FikaHeadless = found ? info.Instance : null;
            IsCheckedFikaHeadless = true;
            callback.Invoke(FikaHeadless != null);
        }
        
        public static bool TryGetCustomRaidSettings(out FikaCustomRaidSettingsPayload settings)
        {
            settings = null;

            var plugin = FikaCore;
            if (plugin == null && Chainloader.PluginInfos.TryGetValue(FikaCoreGuid, out var info))
                plugin = info.Instance;

            if (plugin == null)
                return false;

            var utilsType = plugin.GetType().Assembly.GetType(FikaBackendUtilsTypeName, throwOnError: false);
            if (utilsType == null)
                return false;

            var customRaidProp = utilsType.GetProperty("CustomRaidSettings", BindingFlags.Public | BindingFlags.Static);
            if (customRaidProp == null)
                return false;

            var customRaid = customRaidProp.GetValue(null);
            if (customRaid == null)
                return false;

            var instanceType = customRaid.GetType();

            bool ReadBool(string propertyName)
            {
                var p = instanceType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                return p?.GetValue(customRaid) is bool b && b;
            }

            settings = new FikaCustomRaidSettingsPayload
            {
                UseCustomWeather = ReadBool("UseCustomWeather"),
                DisableOverload = ReadBool("DisableOverload"),
                DisableLegStamina = ReadBool("DisableLegStamina"),
                DisableArmStamina = ReadBool("DisableArmStamina"),
                InstantLoad = TryGetInstantLoad(out bool instantLoad) ? instantLoad : false,
                FastLoad = TryGetFastLoad(out bool fastLoad) ? fastLoad : false
            };

            return true;
        }
        
        public static bool TryGetInstantLoad(out bool instantLoad)
        {
            instantLoad = false;

            var plugin = FikaCore;
            if (plugin == null && Chainloader.PluginInfos.TryGetValue(FikaCoreGuid, out var info))
                plugin = info.Instance;

            if (plugin == null)
                return false;

            var pluginType = plugin.GetType();
            var settingsProp = pluginType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (settingsProp == null)
                return false;

            var settingsTarget = settingsProp.GetMethod?.IsStatic == true ? null : plugin;
            var settings = settingsProp.GetValue(settingsTarget);
            if (settings == null)
                return false;

            var instantLoadProp = settings.GetType().GetProperty("InstantLoad", BindingFlags.Public | BindingFlags.Instance);
            if (instantLoadProp == null)
                return false;

            if (instantLoadProp.GetValue(settings) is not bool value)
                return false;

            instantLoad = value;
            return true;
        }

        public static bool TryGetFastLoad(out bool FastLoad)
        {
            FastLoad = false;

            var plugin = FikaCore;
            if (plugin == null && Chainloader.PluginInfos.TryGetValue(FikaCoreGuid, out var info))
                plugin = info.Instance;

            if (plugin == null)
                return false;

            var pluginType = plugin.GetType();
            var settingsProp = pluginType.GetProperty("Settings", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            if (settingsProp == null)
                return false;

            var settingsTarget = settingsProp.GetMethod?.IsStatic == true ? null : plugin;
            var settings = settingsProp.GetValue(settingsTarget);
            if (settings == null)
                return false;

            var FastLoadProp = settings.GetType().GetProperty("FastLoad", BindingFlags.Public | BindingFlags.Instance);
            if (FastLoadProp == null)
                return false;

            if (FastLoadProp.GetValue(settings) is not bool value)
                return false;

            FastLoad = value;
            return true;
        }

        public static Type GetObservedClientBridgeType()
        {
            if (FikaCore == null)
                throw new InvalidOperationException("Fika Core plugin is not loaded.");
            return FikaCore.GetType().Assembly.GetType("Fika.Core.Main.ObservedClasses.PlayerBridge.ObservedClientBridge", true);
        }
    }
}
