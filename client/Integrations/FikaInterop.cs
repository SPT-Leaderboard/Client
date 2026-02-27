using System;
using BepInEx;
using BepInEx.Bootstrap;

namespace SPTLeaderboard.Integrations
{
    public static class FikaInterop
    {
        public const string FikaCoreGuid = "com.fika.core";
        public const string FikaHeadlessGuid = "com.fika.headless";

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
        public static Type GetObservedClientBridgeType()
        {
            if (FikaCore == null)
                throw new InvalidOperationException("Fika Core plugin is not loaded.");
            return FikaCore.GetType().Assembly.GetType("Fika.Core.Main.ObservedClasses.PlayerBridge.ObservedClientBridge", true);
        }
    }
}
