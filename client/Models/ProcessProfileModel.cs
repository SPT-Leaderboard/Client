using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Quests;
using EFT.UI;
using Newtonsoft.Json;
using SPTLeaderboard.Data;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Models;

public class ProcessProfileModel
{
    public static ProcessProfileModel Instance { get; private set; }

    /// <summary>
    /// Processes and sends profile data after raid
    /// </summary>
    /// <param name="localRaidSettings">Local raid settings</param>
    /// <param name="resultRaid">Raid result</param>
    public void ProcessAndSendProfile(LocalRaidSettings localRaidSettings, RaidEndDescriptorClass resultRaid)
    {
        if (!ShouldProcessProfile())
            return;

        if (!Singleton<PreloaderUI>.Instantiated)
            return;

        var session = PlayerHelper.GetSession();
        if (session.Profile == null)
            return;

        var profileData = DeserializeProfileData(resultRaid);
        if (profileData == null)
            return;

        var isScavRaid = DetermineRaidType(session.Profile, profileData);

        var sessionData = GetSessionData(session);

        var raidInfo = GetRaidInfo(localRaidSettings, resultRaid, session.Profile);


        ProcessAndSendProfileData(sessionData, raidInfo, isScavRaid, resultRaid);
    }

    /// <summary>
    /// Checks if profile should be processed
    /// </summary>
    private bool ShouldProcessProfile()
    {
        return SettingsModel.Instance.EnableSendData.Value ||
               !PlayerHelper.GetLimitViolationsSilent(PlayerHelper.GetEquipmentData());
    }

    /// <summary>
    /// Deserializes profile data from raid result
    /// </summary>
    private ProfileData DeserializeProfileData(RaidEndDescriptorClass resultRaid)
    {
        try
        {
            return JsonConvert.DeserializeObject<ProfileData>(resultRaid.profile.JObject.ToString());
        }
        catch (Exception e)
        {
            LeaderboardPlugin.logger.LogError($"[DeserializeProfileData] Cant parse data profile {e}");
            return null;
        }
    }

    /// <summary>
    /// Determines raid type (PMC or SCAV)
    /// </summary>
    private bool DetermineRaidType(Profile profile, ProfileData profileData)
    {
        bool isScavRaid = profile.Side == EPlayerSide.Savage;
        if (profileData != null)
        {
            isScavRaid = profileData.Info.Side == "Savage";
        }

        return isScavRaid;
    }

    /// <summary>
    /// Gets session data
    /// </summary>
    private (string profileId, Profile pmcData, Profile scavData) GetSessionData(ISession session)
    {
        var profileId = session.Profile.Id;
        var pmcData = session.GetProfileBySide(ESideType.Pmc);
        var scavData = session.GetProfileBySide(ESideType.Savage);

        return (profileId, pmcData, scavData);
    }

    /// <summary>
    /// Gets raid information
    /// </summary>
    private (string gameVersion, string lastRaidLocationRaw, string lastRaidLocation) GetRaidInfo(
        LocalRaidSettings localRaidSettings, RaidEndDescriptorClass resultRaid, Profile profile)
    {
        var gameVersion = profile.Info.GameVersion;
        var lastRaidLocationRaw = localRaidSettings.location;
        var lastRaidLocation = DataUtils.GetPrettyMapName(lastRaidLocationRaw.ToLower());

        return (gameVersion, lastRaidLocationRaw, lastRaidLocation);
    }

    /// <summary>
    /// Processes and sends profile data
    /// </summary>
    private void ProcessAndSendProfileData(
        (string profileId, Profile pmcData, Profile scavData) sessionData,
        (string gameVersion, string lastRaidLocationRaw, string lastRaidLocation) raidInfo,
        bool isScavRaid, RaidEndDescriptorClass resultRaid)
    {
        var (profileId, pmcData, scavData) = sessionData;
        var (gameVersion, lastRaidLocationRaw, lastRaidLocation) = raidInfo;

        var completedQuests = ProcessQuests(pmcData, isScavRaid);
        var nameKiller = GetKillerName(resultRaid, pmcData, scavData);
        var discFromRaid = resultRaid.result == ExitStatus.Left;
        var revenueRaid = CalculateRaidRevenue(resultRaid, isScavRaid);
        var (isTransition, lastRaidTransitionTo) = GetTransitionData(resultRaid);
        var allAchievementsDict = GetAllAchievements(pmcData);
        var (haveDevItems, hasKappa) = ValidatePlayerItems(pmcData);
        var (averageShot, longestShot, longestHeadshot) = GetShotStatistics();
        var (maxHealth, currentHealth, currentEnergy, currentHydration, maxEnergy, maxHydration) =
            GetHealthStats(pmcData);
        var (killedPmc, killedSavage, killedBoss, expLooting, hitCount, totalDamage, damageTaken) =
            GetSessionStats(pmcData, isScavRaid, scavData);
        var hideoutData = GetHideoutData(pmcData, isScavRaid);
        var listModsPlayer = GetModsList();
        var (statTrackIsUsed, processedStatTrackData) = ProcessStatTrackData(profileId);
        LeaderboardPlugin.logger.LogWarning("ProcessAndSendProfileData 1");
        CreateAndSendProfileData(profileId, gameVersion, lastRaidLocationRaw, lastRaidLocation,
            isScavRaid, completedQuests, nameKiller, discFromRaid, revenueRaid, isTransition,
            lastRaidTransitionTo, allAchievementsDict, haveDevItems, hasKappa, averageShot,
            longestShot, longestHeadshot, maxHealth, currentHealth, currentEnergy, currentHydration,
            maxEnergy, maxHydration, killedPmc, killedSavage, killedBoss, expLooting, hitCount,
            totalDamage, damageTaken, hideoutData, listModsPlayer, statTrackIsUsed,
            processedStatTrackData, pmcData, scavData, resultRaid);
    }

    /// <summary>
    /// Processes player quests
    /// </summary>
    private Dictionary<string, QuestInfoData> ProcessQuests(Profile pmcData, bool isScavRaid)
    {
        var completedQuests = new Dictionary<string, QuestInfoData>();
        if (isScavRaid)
            return completedQuests;

        foreach (var quest in pmcData.QuestsData)
        {
            var successTime = quest.StatusStartTimestamps
                .Where(timestamp => timestamp.Key == EQuestStatus.Success)
                .Select(timestamp => (int)timestamp.Value)
                .FirstOrDefault();

            var questInfo = new QuestInfoData
            {
                AcceptTime = quest.StartTime,
                FinishTime = successTime
            };

            if (!completedQuests.ContainsKey(quest.Id))
                completedQuests.Add(quest.Id, questInfo);
        }

        return completedQuests;
    }

    /// <summary>
    /// Gets killer name
    /// </summary>
    private string GetKillerName(RaidEndDescriptorClass resultRaid, Profile pmcData, Profile scavData)
    {
        if (resultRaid.result != ExitStatus.Killed)
            return "";

        var nameKiller = PlayerHelper.TryGetAgressorName(pmcData);
        if (string.IsNullOrEmpty(nameKiller))
        {
            nameKiller = PlayerHelper.TryGetAgressorName(scavData);
        }

        return nameKiller;
    }

    /// <summary>
    /// Calculates raid revenue
    /// </summary>
    private int CalculateRaidRevenue(RaidEndDescriptorClass resultRaid, bool isScavRaid)
    {
        var trackingLoot = LeaderboardPlugin.Instance.TrackingLoot;
        var trackedLootRevenue = DataUtils.GetPriceItems(trackingLoot.TrackedIds.ToList());

#if DEBUG
        LeaderboardPlugin.logger.LogWarning(
            $"List tracked {trackingLoot.TrackedIds.Count} items: {JsonConvert.SerializeObject(trackingLoot.TrackedIds.ToList())}");
#endif

        if (resultRaid.result is ExitStatus.Runner or ExitStatus.Transit or ExitStatus.Survived)
        {
#if DEBUG
            LeaderboardPlugin.logger.LogWarning($"trackedLootRevenue = {trackedLootRevenue}");
            if (!isScavRaid)
                LeaderboardPlugin.logger.LogWarning($"PreRaidLootValue = {trackingLoot.PreRaidLootValue}");
#endif
            return trackedLootRevenue;
        }
        else
        {
            if (!isScavRaid)
            {
                return -(trackingLoot.PreRaidLootValue + trackedLootRevenue);
            }
            else
            {
                return -trackedLootRevenue;
            }
        }
    }

    /// <summary>
    /// Gets transition data
    /// </summary>
    private (bool isTransition, string lastRaidTransitionTo) GetTransitionData(RaidEndDescriptorClass resultRaid)
    {
        var isTransition = false;
        var lastRaidTransitionTo = "None";

        DataUtils.TryGetTransitionData(resultRaid, (s, b) =>
        {
            lastRaidTransitionTo = s;
            isTransition = b;
        });

        return (isTransition, lastRaidTransitionTo);
    }

    /// <summary>
    /// Gets all player achievements
    /// </summary>
    private Dictionary<string, int> GetAllAchievements(Profile pmcData)
    {
        return pmcData.AchievementsData.ToDictionary(
            pair => pair.Key.ToString(),
            pair => pair.Value
        );
    }

    /// <summary>
    /// Validates player items for prohibited elements
    /// </summary>
    private (bool haveDevItems, bool hasKappa) ValidatePlayerItems(Profile pmcData)
    {
        var allItemsRaw = pmcData.Inventory.GetPlayerItems();
        var allItems = allItemsRaw.ToList();

        var haveDevItems = DataUtils.CheckDevItems(allItems);
        var hasKappa = DataUtils.CheckHasKappa(allItems);

        if (haveDevItems)
        {
            LocalizationModel.NotificationWarning(LocalizationModel.Instance.GetLocaleErrorText(ErrorType.DEVITEMS),
                ServerErrorHandler.GetDurationType(ErrorType.DEVITEMS));
#if DEBUG
            if (SettingsModel.Instance.Debug.Value)
            {
                haveDevItems = false;
            }
            else
            {
                return (true, hasKappa);
            }
#else
            return (true, hasKappa);
#endif
        }

        return (false, hasKappa);
    }

    /// <summary>
    /// Gets shot statistics
    /// </summary>
    private (float averageShot, int longestShot, int longestHeadshot) GetShotStatistics()
    {
        var averageShot = (float)Math.Round(HitsTracker.Instance.GetAverageShot(), 1);
        var longestShot = (int)HitsTracker.Instance.GetLongestShot();
        var longestHeadshot = (int)HitsTracker.Instance.GetLongestHeadshot();

#if DEBUG || BETA
        LeaderboardPlugin.logger.LogWarning($"[Session Counter] AverageShot {averageShot}");
        LeaderboardPlugin.logger.LogWarning($"[Session Counter] LongestShot {longestShot}");
        LeaderboardPlugin.logger.LogWarning($"[Session Counter] LongestHeadshot {longestHeadshot}");
#endif

        return (averageShot, longestShot, longestHeadshot);
    }

    /// <summary>
    /// Gets health statistics
    /// </summary>
    private (float maxHealth, float currentHealth, float currentEnergy, float currentHydration, float maxEnergy, float
        maxHydration) GetHealthStats(Profile pmcData)
    {
        var maxHealth = pmcData.Health.BodyParts
            .Where(bodyPart => bodyPart.Value?.Health != null)
            .Sum(bodyPart => bodyPart.Value.Health.Maximum);

        var currentHealth = pmcData.Health.BodyParts
            .Where(bodyPart => bodyPart.Value?.Health != null)
            .Sum(bodyPart => bodyPart.Value.Health.Current);

        var currentEnergy = pmcData.Health.Energy.Current;
        var currentHydration = pmcData.Health.Hydration.Current;
        var maxEnergy = pmcData.Health.Energy.Maximum;
        var maxHydration = pmcData.Health.Hydration.Maximum;

        return (maxHealth, currentHealth, currentEnergy, currentHydration, maxEnergy, maxHydration);
    }

    /// <summary>
    /// Gets session statistics
    /// </summary>
    private (int killedPmc, int killedSavage, int killedBoss, int expLooting, int hitCount, int totalDamage, int
        damageTaken) GetSessionStats(
            Profile pmcData, bool isScavRaid, Profile scavData)
    {
        int killedPmc, killedSavage, killedBoss, expLooting, hitCount, totalDamage, damageTaken;

        if (isScavRaid)
        {
            killedPmc = scavData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledPmc);
            killedSavage = scavData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledSavage);
            killedBoss = scavData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledBoss);
            hitCount = scavData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.HitCount);
            totalDamage =
                (int)scavData.Stats.Eft.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.CauseBodyDamage);
            expLooting = 0;
            damageTaken = 0;

#if DEBUG || BETA
            LeaderboardPlugin.logger.LogWarning($"\n");
            LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledPmc Scav {killedPmc}");
            LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledSavage Scav {killedSavage}");
            LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledBoss Scav {killedBoss}");
            LeaderboardPlugin.logger.LogWarning($"[Session Counter] HitCount Scav {hitCount}");
            LeaderboardPlugin.logger.LogWarning($"[Session Counter] CauseBodyDamage Scav {totalDamage}");
#endif
        }
        else
        {
            killedPmc = pmcData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledPmc);
            killedSavage = pmcData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledSavage);
            killedBoss = pmcData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledBoss);
            expLooting = pmcData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.ExpLooting);
            hitCount = pmcData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.HitCount);
            totalDamage =
                (int)pmcData.Stats.Eft.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.CauseBodyDamage);
            damageTaken = (int)pmcData.Stats.Eft.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.BloodLoss);

#if DEBUG || BETA
            LeaderboardPlugin.logger.LogWarning($"Death coordinates {PlayerHelper.Instance.LastDeathPosition}");
            LeaderboardPlugin.logger.LogWarning("\n");
            LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledPmc {killedPmc}");
            LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledSavage {killedSavage}");
            LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledBoss {killedBoss}");
            LeaderboardPlugin.logger.LogWarning($"[Session Counter] CauseBodyDamage {totalDamage}");
            LeaderboardPlugin.logger.LogWarning($"[Session Counter] ExpLooting {expLooting}");
            LeaderboardPlugin.logger.LogWarning($"[Session Counter] HitCount {hitCount}");
#endif
        }

        if (hitCount <= 0)
        {
            hitCount = 0;
        }

        return (killedPmc, killedSavage, killedBoss, expLooting, hitCount, totalDamage, damageTaken);
    }

    /// <summary>
    /// Gets hideout data
    /// </summary>
    private HideoutData GetHideoutData(Profile pmcData, bool isScavRaid)
    {
        if (isScavRaid)
            return new HideoutData();

        var areasPmc = pmcData.Hideout.Areas.ToList();
        var hideoutData = new HideoutData();

        foreach (var areaPmc in areasPmc)
        {
            if (areaPmc.AreaType != EAreaType.NotSet)
            {
                var propertyName = areaPmc.AreaType.ToString();
                var property = typeof(HideoutData).GetProperty(propertyName);
                if (property != null && property.PropertyType == typeof(int))
                {
                    property.SetValue(hideoutData, areaPmc.Level);
                }
            }
        }

        return hideoutData;
    }

    /// <summary>
    /// Gets mods list
    /// </summary>
    private List<string> GetModsList()
    {
        return DataUtils.GetServerMods()
            .Concat(DataUtils.GetUserMods())
            .Concat(DataUtils.GetBepinexMods())
            .Concat(DataUtils.GetBepinexDll())
            .ToList();
    }

    /// <summary>
    /// Processes StatTrack data
    /// </summary>
    private (bool statTrackIsUsed, Dictionary<string, Dictionary<string, WeaponInfo>> processedStatTrackData)
        ProcessStatTrackData(string profileId)
    {
        var statTrackIsUsed = false;
        Dictionary<string, Dictionary<string, WeaponInfo>> processedStatTrackData =
            new Dictionary<string, Dictionary<string, WeaponInfo>>();

        // Commented because StatTrack not exists on 4.0
        // var statTrackIsUsed = StatTrackInterop.Loaded();

        return (statTrackIsUsed, processedStatTrackData);
    }

    /// <summary>
    /// Creates and sends profile data
    /// </summary>
    private void CreateAndSendProfileData(
        string profileId, string gameVersion, string lastRaidLocationRaw, string lastRaidLocation,
        bool isScavRaid, Dictionary<string, QuestInfoData> completedQuests, string nameKiller,
        bool discFromRaid, int revenueRaid, bool isTransition, string lastRaidTransitionTo,
        Dictionary<string, int> allAchievementsDict, bool haveDevItems, bool hasKappa,
        float averageShot, int longestShot, int longestHeadshot, float maxHealth, float currentHealth,
        float currentEnergy, float currentHydration, float maxEnergy, float maxHydration,
        int killedPmc, int killedSavage, int killedBoss, int expLooting, int hitCount,
        int totalDamage, int damageTaken, HideoutData hideoutData, List<string> listModsPlayer,
        bool statTrackIsUsed, Dictionary<string, Dictionary<string, WeaponInfo>> processedStatTrackData,
        Profile pmcData, Profile scavData, RaidEndDescriptorClass resultRaid)
    {
        if (haveDevItems)
            return;

        var baseData = CreateBaseData(profileId, gameVersion, isScavRaid, maxHealth, currentHealth,
            pmcData, killedPmc, resultRaid, listModsPlayer, haveDevItems);

        if (isScavRaid)
        {
            var scavProfileData = CreateScavProfileData(baseData, nameKiller, discFromRaid, isTransition,
                statTrackIsUsed, hitCount, lastRaidLocation, lastRaidLocationRaw, lastRaidTransitionTo,
                allAchievementsDict, longestShot, longestHeadshot, averageShot, killedBoss, killedSavage,
                processedStatTrackData, pmcData, scavData, totalDamage, damageTaken, completedQuests,
                revenueRaid, maxEnergy, maxHydration);

            SendProfileData(scavProfileData, "SCAV");
        }
        else
        {
            var pmcProfileData = CreatePmcProfileData(baseData, nameKiller, discFromRaid, isTransition,
                statTrackIsUsed, expLooting, hideoutData, hitCount, lastRaidLocation, lastRaidLocationRaw,
                lastRaidTransitionTo, allAchievementsDict, longestShot, longestHeadshot, averageShot,
                killedBoss, killedSavage, processedStatTrackData, pmcData, scavData, hasKappa,
                totalDamage, damageTaken, completedQuests, revenueRaid, currentEnergy, currentHydration,
                maxEnergy, maxHydration);

            SendProfileData(pmcProfileData, "PMC");
        }
    }

    /// <summary>
    /// Creates base profile data
    /// </summary>
    private BaseData CreateBaseData(string profileId, string gameVersion, bool isScavRaid, float maxHealth,
        float currentHealth, Profile pmcData, int killedPmc, RaidEndDescriptorClass resultRaid,
        List<string> listModsPlayer, bool haveDevItems)
    {
        return new BaseData
        {
            AccountType = gameVersion,
            Health = currentHealth,
            Id = profileId,
            IsScav = isScavRaid,
            LastPlayed = DataUtils.CurrentTimestamp,
#if DEBUG
            Mods = SettingsModel.Instance.Debug.Value ? ["IhanaMies-LootValueBackend", "SpecialSlots"] : listModsPlayer,
#else
            Mods = listModsPlayer,
#endif
            ModInt = EncryptionModel.Instance.GetHashMod(),
            Name = pmcData.Info.Nickname,
            PmcHealth = maxHealth,
            PmcLevel = pmcData.Info.Level,
            RaidKills = killedPmc,
            RaidResult = resultRaid.result.ToString(),
            RaidTime = resultRaid.playTime,
            SptVersion = DataUtils.GetSptVersion(),
            Token = EncryptionModel.Instance.Token,
            DBinInv = haveDevItems,
            IsCasual = SettingsModel.Instance.ModCasualMode.Value
        };
    }

    /// <summary>
    /// Creates PMC profile data
    /// </summary>
    private AdditiveProfileData CreatePmcProfileData(BaseData baseData, string nameKiller, bool discFromRaid,
        bool isTransition, bool statTrackIsUsed, int expLooting, HideoutData hideoutData, int hitCount,
        string lastRaidLocation, string lastRaidLocationRaw, string lastRaidTransitionTo,
        Dictionary<string, int> allAchievementsDict, int longestShot, int longestHeadshot, float averageShot,
        int killedBoss, int killedSavage, Dictionary<string, Dictionary<string, WeaponInfo>> processedStatTrackData,
        Profile pmcData, Profile scavData, bool hasKappa, int totalDamage, int damageTaken,
        Dictionary<string, QuestInfoData> completedQuests, int revenueRaid, float currentEnergy,
        float currentHydration, float maxEnergy, float maxHydration)
    {
        var traderInfoData = DataUtils.GetTraderInfo(pmcData);

        return new AdditiveProfileData(baseData)
        {
            DiscFromRaid = discFromRaid,
            AgressorName = nameKiller,
            IsTransition = isTransition,
            IsUsingStattrack = statTrackIsUsed,
            LastRaidEXP = expLooting,
            HideoutData = hideoutData,
            LastRaidHits = hitCount,
            LastRaidMap = lastRaidLocation,
            LastRaidMapRaw = lastRaidLocationRaw,
            LastRaidTransitionTo = lastRaidTransitionTo,
            RaidHits = HitsTracker.Instance.GetHitsData(),
            AllAchievements = allAchievementsDict,
            LongestShot = longestShot,
            LongestHeadshot = longestHeadshot,
            AverageShot = averageShot,
            DiedAtX = PlayerHelper.Instance.LastDeathPosition.x,
            DiedAtY = PlayerHelper.Instance.LastDeathPosition.y,
            BossKills = killedBoss,
            SavageKills = killedSavage,
            ModWeaponStats = processedStatTrackData,
            PlayedAs = "PMC",
            PmcSide = pmcData.Side.ToString(),
            Prestige = pmcData.Info.PrestigeLevel,
            HasKappa = hasKappa,
            ScavLevel = scavData.Info.Level,
            RaidDamage = totalDamage,
            DamageTaken = damageTaken,
            RegistrationDate = pmcData.Info.RegistrationDate,
            TraderInfo = traderInfoData,
            Quests = completedQuests,
            RevenueRaid = revenueRaid,
            Energy = currentEnergy,
            Hydration = currentHydration,
            MaxEnergy = maxEnergy,
            MaxHydration = maxHydration
        };
    }

    /// <summary>
    /// Creates SCAV profile data
    /// </summary>
    private AdditiveProfileData CreateScavProfileData(BaseData baseData, string nameKiller, bool discFromRaid,
        bool isTransition, bool statTrackIsUsed, int hitCount, string lastRaidLocation,
        string lastRaidLocationRaw, string lastRaidTransitionTo, Dictionary<string, int> allAchievementsDict,
        int longestShot, int longestHeadshot, float averageShot, int killedBoss, int killedSavage,
        Dictionary<string, Dictionary<string, WeaponInfo>> processedStatTrackData, Profile pmcData,
        Profile scavData, int totalDamage, int damageTaken, Dictionary<string, QuestInfoData> completedQuests,
        int revenueRaid, float maxEnergy, float maxHydration)
    {
        var traderInfoData = DataUtils.GetTraderInfo(pmcData);

        return new AdditiveProfileData(baseData)
        {
            DiscFromRaid = discFromRaid,
            AgressorName = nameKiller,
            IsTransition = isTransition,
            IsUsingStattrack = statTrackIsUsed,
            LastRaidEXP = 0,
            LastRaidHits = hitCount,
            LastRaidMap = lastRaidLocation,
            LastRaidMapRaw = lastRaidLocationRaw,
            LastRaidTransitionTo = lastRaidTransitionTo,
            RaidHits = HitsTracker.Instance.GetHitsData(),
            AllAchievements = allAchievementsDict,
            LongestShot = longestShot,
            LongestHeadshot = longestHeadshot,
            AverageShot = averageShot,
            DiedAtX = PlayerHelper.Instance.LastDeathPosition.x,
            DiedAtY = PlayerHelper.Instance.LastDeathPosition.y,
            BossKills = killedBoss,
            SavageKills = killedSavage,
            ModWeaponStats = processedStatTrackData,
            PlayedAs = "SCAV",
            PmcSide = pmcData.Side.ToString(),
            Prestige = pmcData.Info.PrestigeLevel,
            ScavLevel = scavData.Info.Level,
            RaidDamage = totalDamage,
            DamageTaken = damageTaken,
            RegistrationDate = pmcData.Info.RegistrationDate,
            TraderInfo = traderInfoData,
            Quests = completedQuests,
            RevenueRaid = revenueRaid,
            MaxEnergy = maxEnergy,
            MaxHydration = maxHydration
        };
    }

    /// <summary>
    /// Sends profile data
    /// </summary>
    private void SendProfileData(AdditiveProfileData profileData, string profileType)
    {
#if DEBUG
        LeaderboardPlugin.logger.LogWarning($"DATA {profileType} {JsonConvert.SerializeObject(profileData)}");
#endif

#if BETA
        var betaDataProfile = AdditiveProfileData.MakeBetaCopy(profileData);
        betaDataProfile.ModInt = "BETA";
        betaDataProfile.Mods = ["BETA"];
        betaDataProfile.Token = "BETA";
        
        LeaderboardPlugin.logger.LogWarning($"DATA {profileType} {JsonConvert.SerializeObject(betaDataProfile)}");
#endif

        LeaderboardPlugin.SendRaidData(profileData);
    }

    public static ProcessProfileModel Create()
    {
        if (Instance != null)
        {
            return Instance;
        }

        return Instance = new ProcessProfileModel();
    }
}