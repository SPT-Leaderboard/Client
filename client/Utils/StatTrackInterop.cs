using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using Comfort.Logs;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPTLeaderboard.Data;
using SPTLeaderboard.Models;

namespace SPTLeaderboard.Utils;

public static class StatTrackInterop
{
    public static readonly Version RequiredVersion = new Version(2, 0, 0);

    public static Dictionary<string, Dictionary<string, CustomizedObject>> WeaponInfoOutOfRaid { get; set; } = new Dictionary<string, Dictionary<string, CustomizedObject>>();
    
    public static bool? StatTrackLoaded;
    
    public static bool Loaded()
    {
        if (!StatTrackLoaded.HasValue)
        {
            bool present = Chainloader.PluginInfos.TryGetValue("com.acidphantasm.stattrack", out PluginInfo pluginInfo);
            StatTrackLoaded = present && pluginInfo.Metadata.Version >= RequiredVersion;
        }

        return StatTrackLoaded.Value;
    }
    
    public static Dictionary<string, Dictionary<string, CustomizedObject>> LoadFromServer()
    {
        if (Loaded())
        {
            try
            {
                string json = RequestHandler.GetJson("/stattrack/load");
                WeaponInfoOutOfRaid = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, CustomizedObject>>>(json);
                return WeaponInfoOutOfRaid;
            }
            catch (Exception ex)
            {
                LeaderboardPlugin.logger.LogError("[StatTrackInterop] Failed to load: " + ex.ToString());
                return null;
            }
        }

        return null;
    }
    
    public static Dictionary<string, Dictionary<string, WeaponInfo>> GetAllValidWeapons(string playerId, Dictionary<string, Dictionary<string, CustomizedObject>> info)
    {
        if (!info.ContainsKey(playerId))
        {
            Logger.LogWarning($"[StatTrack] Not exists data for current player: {playerId}");
            return null;
        }

        var result = new Dictionary<string, Dictionary<string, WeaponInfo>>
        {
            [playerId] = new Dictionary<string, WeaponInfo>()
        };

        foreach (var weaponInfo in info[playerId])
        {
            string weaponId = weaponInfo.Key;
            CustomizedObject weaponStats = weaponInfo.Value;
            string weaponName = LocalizationModel.GetLocaleName(weaponId + " ShortName");

            // Skip weapons with unknown names
            if (weaponName == "Unknown")
            {
                Logger.LogDebugWarning($"[StatTrack] Not exists locale {weaponId + " ShortName"}");
                continue;
            }

            Logger.LogDebugWarning($"[StatTrack] Add {weaponId + " ShortName"}");

            result[playerId][weaponName] = new WeaponInfo
            {
                stats = weaponStats,
                originalId = weaponId
            };
        }

        if (result[playerId].Count == 0)
        {
            Logger.LogWarning($"[StatTrack] ListWeapons is empty");

            return null;
        }

        return result;
    }
}