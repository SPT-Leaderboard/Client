using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPT.Common.Utils;
using SPT.Reflection.Utils;
using SPTLeaderboard.Data;
using SPTLeaderboard.Enums;
using SPTLeaderboard.Models;
using UnityEngine;
using TraderData = SPTLeaderboard.Data.TraderData;

namespace SPTLeaderboard.Utils;

public static class DataUtils
{
    public static long CurrentTimestamp => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    /// <summary>
    /// Get string from enum PlayerState type
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public static string GetPlayerState(PlayerState state)
    {
        return Enum.GetName(typeof(PlayerState), state)?.ToLower();
    }
    
    /// <summary>
    /// Parsing version SPT from PlayerPrefs
    /// </summary>
    /// <returns></returns>
    public static string GetSptVersion()
    {
        var rawString = PlayerPrefs.GetString("SPT_Version");
        var match = Regex.Match(rawString, @"SPT\s+([0-9\.]+)\s+-");
        if (match.Success)
        {
            string version = match.Groups[1].Value;
            return version;
        }

        return GlobalData.BaseSptVersion;
    }
    
    /// <summary>
    /// Gets mods list
    /// </summary>
    public static List<string> GetModsList()
    {
        Logger.LogDebugWarning("[Logs mods source]");
        Logger.LogDebugWarning($"[ServerMods] {JsonConvert.SerializeObject(GetServerMods())}");
        Logger.LogDebugWarning($"[UserMods] {JsonConvert.SerializeObject(GetUserMods())}");
        Logger.LogDebugWarning($"[BepinexMods] {JsonConvert.SerializeObject(GetBepinexMods())}");
        Logger.LogDebugWarning($"[BepinexDll] {JsonConvert.SerializeObject(GetBepinexDll())}");
        Logger.LogDebugWarning($"[ClientMods] {JsonConvert.SerializeObject(GetClientMods())}");
        
        return GetServerMods()
            .Concat(GetUserMods())
            .Concat(GetBepinexMods())
            .Concat(GetBepinexDll())
            .Concat(GetClientMods())
            .ToList();
    }
    
    /// <summary>
    /// Get list loaded mods from server for user
    /// </summary>
    /// <returns></returns>
    private static List<string> GetServerMods()
    {
        List<string> listServerMods = new List<string>();

        try
        {
            string json = RequestHandler.GetJson("/launcher/server/serverModsUsedByProfile");

            if (string.IsNullOrWhiteSpace(json))
                return listServerMods;
            
            List<ModItem> serverMods = Json.Deserialize<List<ModItem>>(json);

            if (serverMods != null)
            {
                var listMods = serverMods.Select(mod => mod.Name).ToList();
                listServerMods.AddRange(listMods);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"GetServerMods failed: {ex}");
        }

        return listServerMods;
    }

    /// <summary>
    /// Get list loaded mods in client
    /// </summary>
    /// <returns></returns>
    private static List<string> GetClientMods()
    {
        return Chainloader.PluginInfos.Select(pluginInfo => pluginInfo.Value.Metadata.GUID).ToList();
    }

    public static List<string> GetUserMods()
    {
        return GetDirectories(GlobalData.UserModsPath);
    }
    
    public static List<string> GetBepinexMods()
    {
        return GetDirectories(BepInEx.Paths.PluginPath);
    }
    
    public static List<string> GetBepinexDll()
    {
        return GetDllFiles(BepInEx.Paths.PluginPath);
    }
    
    public static List<string> GetDirectories(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            return new List<string>();

        return Directory.GetDirectories(dirPath)
            .Select(path => Path.GetFileName(path))
            .ToList();
    }
    
    public static List<string> GetDllFiles(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            return new List<string>();

        return Directory.GetFiles(dirPath, "*.dll", SearchOption.TopDirectoryOnly)
            .Select(file => Path.GetFileName(file))
            .ToList();
    }
    
    public static bool TryGetPlugin(string pluginGUID, out BaseUnityPlugin plugin)
    {
        PluginInfo pluginInfo;
        bool flag = Chainloader.PluginInfos.TryGetValue(pluginGUID, out pluginInfo);
        plugin = (flag ? pluginInfo.Instance : null);
        return flag;
    }

    public static void CheckFikaCore(Action<bool> callback)
    {
        TryGetPlugin("com.fika.core", out var FikaCoreTemp);
        FikaCore = FikaCoreTemp;
        IsCheckedFikaCore = true;
        callback.Invoke(FikaCore != null);
    }
    
    public static void CheckFikaHeadless(Action<bool> callback)
    {
        TryGetPlugin("com.fika.headless", out var FikaHeadlessTemp);
        FikaHeadless = FikaHeadlessTemp;
        IsCheckedFikaHeadless = true;
        callback.Invoke(FikaHeadless != null);
    }
    
    public static bool IsCheckedFikaCore;
    public static bool IsCheckedFikaHeadless;
    
    public static BaseUnityPlugin FikaCore;
    public static BaseUnityPlugin FikaHeadless;
    
    public static Type GetPluginType(BaseUnityPlugin plugin, string typePath)
    {
        if (plugin == null)
        {
            throw new ArgumentNullException("plugin");
        }
        return plugin.GetType().Assembly.GetType(typePath, true);
    }

    public static string GetRaidGameTime()
    {
        try
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld != null)
            {
                string time = gameWorld.GameDateTime.Calculate().ToString("HH:mm:ss");
                return time;
            }

            return "";
        }
        catch
        {
            return "";
        }
    }
    
    public static string GetRaidPlayerSide()
    {
        try
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld != null)
            {
                string side = gameWorld.MainPlayer.Side.ToString();
                return side;
            }

            return "";
        }
        catch
        {
            return "";
        }
    }
    
    public static string GetRaidRawMap()
    {
        try
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld != null)
            {
                string rawMap = gameWorld.LocationId;
                return rawMap;
            }

            return "";
        }
        catch
        {
            return "";
        }
    }
    
    public static Dictionary<string, TraderData> GetTraderInfo(Profile pmcData)
    {
        var traderInfoPmc = pmcData.TradersInfo;
        
        Dictionary<string, TraderData> tradersData = new Dictionary<string, TraderData>();
        foreach (var trader in GlobalData.TraderMap)
        {
            if (traderInfoPmc.ContainsKey(trader.Key))
            {
                tradersData[trader.Value] = new TraderData
                {
                    ID = trader.Key,
                    SalesSum = traderInfoPmc[trader.Key].SalesSum,
                    Unlocked = traderInfoPmc[trader.Key].Unlocked,
                    Standing = traderInfoPmc[trader.Key].Standing,
                    LoyaltyLevel = traderInfoPmc[trader.Key].LoyaltyLevel,
                    Disabled = traderInfoPmc[trader.Key].Disabled
                };
            }
            else
            {
                tradersData[trader.Value] = new TraderData
                {
                    ID = trader.Key,
                    SalesSum = 0,
                    Unlocked = false,
                    Standing = 0,
                    LoyaltyLevel = 0,
                    Disabled = true,
                    NotFound = true
                };
            }
        }

        return tradersData;
    }
    
    public static string GetPrettyMapName(string entry)
    {
        return entry switch
        {
            "bigmap" => "Customs",
            "factory4_day" => "Factory",
            "factory4_night" => "Night Factory",
            "interchange" => "Interchange",
            "laboratory" => "Labs",
            "rezervbase" => "Reserve",
            "shoreline" => "Shoreline",
            "woods" => "Woods",
            "lighthouse" => "Lighthouse",
            "tarkovstreets" => "Streets of Tarkov",
            "sandbox" => "Ground Zero - Low",
            "sandbox_high" => "Ground Zero - High",
            "labyrinth" => "The Labyrinth",
            _ => "UNKNOWN"
        };
    }

    public static void TryGetTransitionData(RaidEndDescriptorClass resultRaid, Action<string, bool> callback)
    {
        var lastRaidTransitionTo = "None";
        
        if (resultRaid.result != ExitStatus.Transit)
        {
            callback.Invoke(lastRaidTransitionTo, false);
            return;
        }
        
        TarkovApplication tarkovApplication;
        if (!TarkovApplication.Exist(out tarkovApplication))
        {
            Logger.LogWarning($"[TryGetTransitionData] TarkovApplication does not exist, cannot retrieve transition status");
            callback.Invoke(lastRaidTransitionTo, false);
            return;
        }

        try
        {
            var location = tarkovApplication.transitionStatus.Location;
            var inTransition = tarkovApplication.transitionStatus.InTransition;
            
            if (!inTransition)
            {
                Logger.LogInfo($"[TryGetTransitionData] Not in transition, skipping");
                callback.Invoke(lastRaidTransitionTo, false);
                return;
            }
            
            if (string.IsNullOrEmpty(location))
            {
                Logger.LogWarning($"[TryGetTransitionData] Transition location is empty");
                callback.Invoke(lastRaidTransitionTo, false);
                return;
            }
            
            lastRaidTransitionTo = GetPrettyMapName(location.ToLower());
            Logger.LogInfo($"[TryGetTransitionData] Player transit to map: {lastRaidTransitionTo} (raw: {location})");
            callback.Invoke(lastRaidTransitionTo, true);
        }
        catch (Exception ex)
        {
            Logger.LogError($"[TryGetTransitionData] Error accessing transitionStatus: {ex.Message}");
            callback.Invoke(lastRaidTransitionTo, false);
        }
    }
    
    /// <summary>
    /// Checking exists kappa secured container in items 
    /// </summary>
    /// <param name="allItems"></param>
    /// <returns></returns>
    public static bool CheckHasKappa(IEnumerable<Item> allItems)
    {
        foreach (var item in allItems)
        {
            if (item.TemplateId == "676008db84e242067d0dc4c9" || item.TemplateId == "5c093ca986f7740a1867ab12")
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Checking exists dev items in items 
    /// </summary>
    /// <param name="allItems"></param>
    /// <returns></returns>
    public static bool CheckDevItems(IEnumerable<Item> allItems)
    {
        foreach (var item in allItems)
        {
            if (item.TemplateId == "58ac60eb86f77401897560ff" || item.TemplateId == "5c0a5a5986f77476aa30ae64")
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Computes a hash of the data to detect duplicates (async version - runs in background thread)
    /// </summary>
    public static async UniTask<string> ComputeHashAsync(string input, CancellationToken cancellationToken = default)
    {
        // Run hash computation in background thread to avoid blocking main thread
        return await UniTask.RunOnThreadPool(() =>
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hashBytes);
            }
        }, cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// Computes a hash of the data to detect duplicates (synchronous version - use async version when possible)
    /// </summary>
    public static string ComputeHash(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashBytes);
        }
    }
}