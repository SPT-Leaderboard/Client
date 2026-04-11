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
                plugin = info.Instance as BaseUnityPlugin;

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
                DisableArmStamina = ReadBool("DisableArmStamina")
            };

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
