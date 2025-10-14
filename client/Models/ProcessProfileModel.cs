using System;
using System.Collections.Generic;
using System.IO;
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

    public void ProcessAndSendProfile(LocalRaidSettings localRaidSettings, RaidEndDescriptorClass resultRaid)
    {
        if (!SettingsModel.Instance.EnableSendData.Value || PlayerHelper.GetLimitViolationsSilent(PlayerHelper.GetEquipmentData()))
            return;
        
        if (Singleton<PreloaderUI>.Instantiated)
        {
            var session = PlayerHelper.GetSession();
            if (session.Profile != null)
            {
                var profileID = session.Profile.Id;
                
                var pmcData = session.GetProfileBySide(ESideType.Pmc);
                var scavData = session.GetProfileBySide(ESideType.Savage);
                
                #region AgressorProcess
                
                string nameKiller = "";
                if (resultRaid.result == ExitStatus.Killed)
                {
                    nameKiller = PlayerHelper.TryGetAgressorName(session.Profile);
                    if (string.IsNullOrEmpty(nameKiller))
                    {
                        nameKiller = PlayerHelper.TryGetAgressorName(scavData);
                    }
                }
                
                #endregion
                
                var gameVersion = session.Profile.Info.GameVersion;
                var lastRaidLocationRaw = localRaidSettings.location;
                var lastRaidLocation = DataUtils.GetPrettyMapName(lastRaidLocationRaw.ToLower());
                
                ProfileData profileData = null;
                try
                {
                    profileData = JsonConvert.DeserializeObject<ProfileData>(resultRaid.profile.JObject.ToString());
                }
                catch (Exception e)
                {
                    LeaderboardPlugin.logger.LogError($"Cant parse data profile {e.Message}");
                    return;
                }
                
                bool isScavRaid = session.Profile.Side == EPlayerSide.Savage;
                if (profileData != null)
                {
                    isScavRaid = profileData.Info.Side == "Savage";
                }

                #region Quests process
                
                var completedQuests = new Dictionary<string, QuestInfoData>();
                if (!isScavRaid)
                {
                    foreach (var quest in pmcData.QuestsData)
                    {
                        var successTime = 0;
                        foreach (var timestamp in quest.StatusStartTimestamps.Where(timestamp =>
                                     timestamp.Key == EQuestStatus.Success))
                        {
                            successTime = (int)timestamp.Value;
                        }

                        var imageUrl = Path.GetFileName(quest.Template?.Image);
                        var questInfo = new QuestInfoData
                        {
                            AcceptTime = quest.StartTime,
                            FinishTime = successTime,
                            ImageUrl = imageUrl
                        };
                        if (!completedQuests.ContainsKey(quest.Id))
                            completedQuests.Add(quest.Id, questInfo);
                    }
                }
                
                #endregion
                
                bool discFromRaid = resultRaid.result == ExitStatus.Left;

                #region ItemsProcess

                var revenueRaid = 0;
                var trackingLoot = LeaderboardPlugin.Instance.TrackingLoot;
                var trackedLootRevenue = DataUtils.GetPriceItems(trackingLoot.TrackedIds.ToList());
                if (resultRaid.result is ExitStatus.Runner or ExitStatus.Transit or ExitStatus.Survived)
                {
                    var listItems = PlayerHelper.GetEquipmentItemsTemplateId();
                    DataUtils.GetPriceItems(listItems);
                
                    LeaderboardPlugin.logger.LogWarning($"trackedLootRevenue = {trackedLootRevenue}");
                    
                    if(!isScavRaid)
                        LeaderboardPlugin.logger.LogWarning($"PreRaidLootValue = {trackingLoot.PreRaidLootValue}");
                    
                    revenueRaid = trackedLootRevenue;
                }
                else
                {
                    if (!isScavRaid)
                    {
                        revenueRaid = -(trackingLoot.PreRaidLootValue + trackedLootRevenue);
                    }
                    else
                    {
                        revenueRaid = -trackedLootRevenue;
                    }
                        
                }
                
                #endregion
                
                #region Transition Process
                
                var isTransition = false;
                var lastRaidTransitionTo = "None";

                DataUtils.TryGetTransitionData(resultRaid, (s, b) =>
                {
                    lastRaidTransitionTo = s;
                    isTransition = b;
                });
                
                #endregion

                var allAchievementsDict = pmcData.AchievementsData.ToDictionary(
                    pair => pair.Key.ToString(),
                    pair => pair.Value
                );
                
                #region CheckGodBalaclava

                var allItemsRaw = pmcData.Inventory.GetPlayerItems();
                var allItems = allItemsRaw.ToList();
                
                bool haveDevItems = DataUtils.CheckDevItems(allItems);
                
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
                        return;
                    }
#else
                    return;
#endif
                }
                
                #endregion
                
                #region CheckHasKappa

                bool hasKappa = DataUtils.CheckHasKappa(allItems);
                
                #endregion

                #region Stats
                
                var AverageShot = 0.0f;
                var LongestShot = 0;
                var LongestHeadshot = 0;

                AverageShot = (float)Math.Round(HitsTracker.Instance.GetAverageShot(), 1);
                LongestShot = (int)HitsTracker.Instance.GetLongestShot();
                LongestHeadshot = (int)HitsTracker.Instance.GetLongestHeadshot();
#if DEBUG || BETA
                LeaderboardPlugin.logger.LogWarning($"[Session Counter] AverageShot {AverageShot}");
                LeaderboardPlugin.logger.LogWarning($"[Session Counter] LongestShot {LongestShot}");
                LeaderboardPlugin.logger.LogWarning($"[Session Counter] LongestHeadshot {LongestHeadshot}");
#endif
                
                
                #region PMCStats
                
                var MaxHealth = pmcData.Health.BodyParts.Where(
                    bodyPart => bodyPart.Value?.Health != null).
                    Sum(bodyPart => bodyPart.Value.Health.Maximum);
                                
                var CurrentHealth = pmcData.Health.BodyParts.Where(
                    bodyPart => bodyPart.Value?.Health != null).
                    Sum(bodyPart => bodyPart.Value.Health.Current);

                var CurrentEnergy = pmcData.Health.Energy.Current;
                var CurrentHydration = pmcData.Health.Hydration.Current;
                
                var KilledPmc = session.Profile.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledPmc);
                var KilledSavage = session.Profile.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledSavage);
                var KilledBoss = session.Profile.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledBoss);
                // var LongestShot = (int)session.Profile.Stats.Eft.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.LongestShot); // Don`t work in 4.1.0 
                var ExpLooting = session.Profile.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.ExpLooting);
                var HitCount = session.Profile.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.HitCount);
                var TotalDamage = (int)session.Profile.Stats.Eft.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.CauseBodyDamage);
                var DamageTaken = (int)session.Profile.Stats.Eft.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.BloodLoss);
                
                
#if DEBUG || BETA
                LeaderboardPlugin.logger.LogWarning($"Death coordinates {PlayerHelper.Instance.LastDeathPosition}");
#endif
                var hideoutData = new HideoutData();
                if (!isScavRaid)
                {
#if DEBUG || BETA
                    LeaderboardPlugin.logger.LogWarning("\n");
                    LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledPmc {KilledPmc}");
                    LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledSavage {KilledSavage}");
                    LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledBoss {KilledBoss}");
                    // LeaderboardPlugin.logger.LogWarning($"[Session Counter] LongestShot {LongestShot}"); // Don`t work in 4.1.0 
                    LeaderboardPlugin.logger.LogWarning($"[Session Counter] CauseBodyDamage {TotalDamage}");
                    LeaderboardPlugin.logger.LogWarning($"[Session Counter] ExpLooting {ExpLooting}");
                    LeaderboardPlugin.logger.LogWarning($"[Session Counter] HitCount {HitCount}");
#endif
                    
                    #region Hideout

                    var areasPmc = (pmcData.Hideout.Areas).ToList();
                    hideoutData = new HideoutData();
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

                    #endregion
                }

                #endregion
                
                #region ScavStats
                
                if (isScavRaid)
                {
                    KilledPmc = scavData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledPmc);
                    KilledSavage = scavData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledSavage);
                    KilledBoss = scavData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.KilledBoss);
                    // LongestShot = (int)scavData.Stats.Eft.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.LongestShot); // Don`t work in 4.1.0 
                    HitCount = scavData.Stats.Eft.SessionCounters.GetInt(SessionCounterTypesAbstractClass.HitCount);
                    TotalDamage = (int)scavData.Stats.Eft.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.CauseBodyDamage);

#if DEBUG || BETA
                        LeaderboardPlugin.logger.LogWarning($"\n");
                        LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledPmc Scav {KilledPmc}");
                        LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledSavage Scav {KilledSavage}");
                        LeaderboardPlugin.logger.LogWarning($"[Session Counter] KilledBoss Scav {KilledBoss}");
                        // LeaderboardPlugin.logger.LogWarning($"[Session Counter] LongestShot Scav {LongestShot}"); // Don`t work in 4.1.0 
                        LeaderboardPlugin.logger.LogWarning($"[Session Counter] HitCount Scav {HitCount}");
                        LeaderboardPlugin.logger.LogWarning($"[Session Counter] CauseBodyDamage Scav {TotalDamage}");
#endif
                }
                
                #endregion
                
                if (HitCount <= 0) {
                    HitCount = 0;
                }
                
                #endregion
                
                var listModsPlayer = DataUtils.GetServerMods()
                    .Concat(DataUtils.GetUserMods())
                    .Concat(DataUtils.GetBepinexMods())
                    .Concat(DataUtils.GetBepinexDll())
                    .ToList();
                
                #region StatTrack

                // var statTrackIsUsed = StatTrackInterop.Loaded();
                var statTrackIsUsed = false;
                Dictionary<string, Dictionary<string, WeaponInfo>> processedStatTrackData = new Dictionary<string, Dictionary<string, WeaponInfo>>();
                
//                 if (!SettingsModel.Instance.EnableModSupport.Value && !statTrackIsUsed)
//                 {
//                     LeaderboardPlugin.logger.LogWarning(
//                         $"StatTrack process data skip. StatTrack Find? : {statTrackIsUsed} | Enabled Mod Support? : {SettingsModel.Instance.EnableModSupport.Value}");
//                     processedStatTrackData = null;
//                 }
//                 else
//                 {
//                     LeaderboardPlugin.logger.LogWarning($"Loaded StatTrack plugin {statTrackIsUsed}");
//                     
//                     var dataStatTrack = StatTrackInterop.LoadFromServer();
//                     if (dataStatTrack != null)
//                     {
// #if DEBUG || BETA
//                         LeaderboardPlugin.logger.LogWarning(
//                             $"Data raw StatTrack {JsonConvert.SerializeObject(dataStatTrack).ToJson()}");
// #endif
//                         processedStatTrackData = StatTrackInterop.GetAllValidWeapons(profileID ,dataStatTrack);
// #if DEBUG || BETA
//                         if (processedStatTrackData != null)
//                         {
//                             LeaderboardPlugin.logger.LogWarning("processedStatTrackData != null: Data -> "+JsonConvert.SerializeObject(processedStatTrackData).ToJson());
//                         }
// #endif
//                     }
//                 }

                #endregion
                
                var baseData = new BaseData {
                    AccountType = gameVersion,
                    Health = CurrentHealth,
                    Id = profileID,
                    IsScav = isScavRaid,
                    LastPlayed = DataUtils.CurrentTimestamp,
#if DEBUG
                    Mods = SettingsModel.Instance.Debug.Value ? ["IhanaMies-LootValueBackend", "SpecialSlots"] : listModsPlayer,
#else
                    Mods = listModsPlayer,
#endif
                    ModInt = EncryptionModel.Instance.GetHashMod(),
                    Name = session.Profile.Nickname,
                    PmcHealth = MaxHealth,
                    PmcLevel = pmcData.Info.Level,
                    RaidKills = KilledPmc,
                    RaidResult = resultRaid.result.ToString(),
                    RaidTime = resultRaid.playTime,
                    SptVersion = DataUtils.GetSptVersion(),
                    Token = EncryptionModel.Instance.Token,
                    DBinInv = haveDevItems,
                    IsCasual = SettingsModel.Instance.ModCasualMode.Value
                };
                
                if (!isScavRaid)
                {
                    var traderInfoData = DataUtils.GetTraderInfo(pmcData);
                    
                    var pmcProfileData = new AdditiveProfileData(baseData)
                    {
                        DiscFromRaid = discFromRaid,
                        AgressorName = nameKiller,
                        IsTransition = isTransition,
                        IsUsingStattrack = statTrackIsUsed,
                        LastRaidEXP = ExpLooting,
                        HideoutData = hideoutData,
                        LastRaidHits = HitCount,
                        LastRaidMap = lastRaidLocation,
                        LastRaidMapRaw = lastRaidLocationRaw,
                        LastRaidTransitionTo = lastRaidTransitionTo,
                        RaidHits = HitsTracker.Instance.GetHitsData(),
                        AllAchievements = allAchievementsDict,
                        LongestShot = LongestShot,
                        LongestHeadshot = LongestHeadshot,
                        AverageShot = AverageShot,
                        DiedAtX = PlayerHelper.Instance.LastDeathPosition.x,
                        DiedAtY = PlayerHelper.Instance.LastDeathPosition.y,
                        BossKills = KilledBoss,
                        SavageKills = KilledSavage,
                        ModWeaponStats = processedStatTrackData,
                        PlayedAs = "PMC",
                        PmcSide = pmcData.Side.ToString(),
                        Prestige = pmcData.Info.PrestigeLevel,
                        HasKappa = hasKappa,
                        ScavLevel = scavData.Info.Level,
                        RaidDamage = TotalDamage,
                        DamageTaken = DamageTaken,
                        RegistrationDate = session.Profile.Info.RegistrationDate,
                        TraderInfo = traderInfoData,
                        Quests = completedQuests,
                        RevenueRaid = revenueRaid,
                        Energy = CurrentEnergy,
                        Hydration = CurrentHydration
                    };
                    
#if DEBUG
                    LeaderboardPlugin.logger.LogWarning($"DATA PMC {JsonConvert.SerializeObject(pmcProfileData)}");
#endif
                    
#if BETA
                    var betaDataPmcProfile = AdditiveProfileData.MakeBetaCopy(pmcProfileData);
                    betaDataPmcProfile.ModInt = "BETA";
                    betaDataPmcProfile.Mods = ["BETA"];
                    betaDataPmcProfile.Token = "BETA";
                    
                    LeaderboardPlugin.logger.LogWarning($"DATA PMC {JsonConvert.SerializeObject(betaDataPmcProfile)}");
#endif

                    LeaderboardPlugin.SendRaidData(pmcProfileData);
                }
                else
                {
                    var traderInfoData = DataUtils.GetTraderInfo(pmcData);
                    
                    var scavProfileData = new AdditiveProfileData(baseData)
                    {
                        DiscFromRaid = discFromRaid,
                        AgressorName = nameKiller,
                        IsTransition = isTransition,
                        IsUsingStattrack = statTrackIsUsed,
                        LastRaidEXP = 0,
                        LastRaidHits = HitCount,
                        LastRaidMap = lastRaidLocation,
                        LastRaidMapRaw = lastRaidLocationRaw,
                        LastRaidTransitionTo = lastRaidTransitionTo,
                        RaidHits = HitsTracker.Instance.GetHitsData(),
                        AllAchievements = allAchievementsDict,
                        LongestShot = LongestShot,
                        LongestHeadshot = LongestHeadshot,
                        AverageShot = AverageShot,
                        DiedAtX = PlayerHelper.Instance.LastDeathPosition.x,
                        DiedAtY = PlayerHelper.Instance.LastDeathPosition.y,
                        BossKills = KilledBoss,
                        SavageKills = KilledSavage,
                        ModWeaponStats = processedStatTrackData,
                        PlayedAs = "SCAV",
                        PmcSide = pmcData.Side.ToString(),
                        Prestige = pmcData.Info.PrestigeLevel,
                        ScavLevel = scavData.Info.Level, 
                        RaidDamage = TotalDamage,
                        DamageTaken = DamageTaken,
                        RegistrationDate = session.Profile.Info.RegistrationDate,
                        TraderInfo = traderInfoData,
                        Quests = completedQuests,
                        RevenueRaid = revenueRaid
                    };
                    
#if DEBUG
                    LeaderboardPlugin.logger.LogWarning(
                        $"DATA SCAV {JsonConvert.SerializeObject(scavProfileData)}");
#endif
                    
#if BETA
                    var betaDataScavProfile = AdditiveProfileData.MakeBetaCopy(scavProfileData);
                    betaDataScavProfile.ModInt = "BETA";
                    betaDataScavProfile.Mods = ["BETA"];
                    betaDataScavProfile.Token = "BETA";
                    
                    LeaderboardPlugin.logger.LogWarning($"DATA SCAV {JsonConvert.SerializeObject(betaDataScavProfile)}");
#endif

                    LeaderboardPlugin.SendRaidData(scavProfileData);
                }
            }
        }
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