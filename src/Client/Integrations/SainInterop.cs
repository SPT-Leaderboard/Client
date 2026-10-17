using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using SPTLeaderboard.Services;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Integrations
{
    public static class SainInterop
    {
        public const string PluginGuid = "me.sol.sain";

        private static PropertyInfo _loadedPresetProperty;
        private static PropertyInfo _editorWindowProperty;

        public static void Initialize()
        {
            if (!Chainloader.PluginInfos.TryGetValue(PluginGuid, out var plugin))
                return;

            try
            {
                var pluginType = plugin.Instance.GetType();
                _loadedPresetProperty = AccessTools.Property(pluginType, "LoadedPreset")
                    ?? throw new MissingMemberException(pluginType.FullName, "LoadedPreset");

                var editorType = pluginType.Assembly.GetType("SAIN.Editor.SAINEditor", throwOnError: true);
                _editorWindowProperty = AccessTools.Property(editorType, "DisplayingWindow");
                var setEditorWindowMethod = _editorWindowProperty?.GetSetMethod()
                    ?? throw new MissingMemberException(editorType.FullName, "DisplayingWindow");

                var harmony = new Harmony("harmonyzt.SPTLeaderboard.SAIN");
                harmony.Patch(setEditorWindowMethod, prefix: new HarmonyMethod(
                    typeof(SainInterop), nameof(OnEditorWindowOpening)));
                Logger.LogInfo("[SAIN] Integration enabled");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[SAIN] Could not initialize integration: {ex}");
            }
        }

        public static string CaptureRaidPreset()
        {
            if (_loadedPresetProperty == null)
                return string.Empty;

            try
            {
                // The player may have left the editor open while loading into the raid.
                _editorWindowProperty?.SetValue(null, false);

                var preset = _loadedPresetProperty.GetValue(null);
                if (preset == null)
                    throw new InvalidOperationException("SAIN has no loaded preset");

                var presetInfo = AccessTools.Property(preset.GetType(), "Info")?.GetValue(preset)
                    ?? throw new MissingMemberException(preset.GetType().FullName, "Info");
                var presetName = AccessTools.Field(presetInfo.GetType(), "Name")?.GetValue(presetInfo) as string
                    ?? throw new MissingMemberException(presetInfo.GetType().FullName, "Name");

                Logger.LogDebugInfo($"[SAIN] Raid preset: {presetName}");
                return presetName;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"[SAIN] Could not read raid preset: {ex}");
                return string.Empty;
            }
        }

        private static bool OnEditorWindowOpening([HarmonyArgument(0)] bool isOpening)
        {
            // The shared setter covers the shortcut, config button and console command.
            if (!isOpening || !PlayerHelper.HasRaidStarted())
                return true;

            LocalizationService.NotificationWarning(LocalizationService.Instance.GetSainEditorBlockedText());
            return false;
        }
    }
}
