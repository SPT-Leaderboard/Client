using System.Collections.Generic;
using Newtonsoft.Json;

namespace SPTLeaderboard.Data
{
    public class AdditiveProfileData(BaseData baseData)
    {
        #region BaseData
        
        [JsonProperty("accountType")]
        public string AccountType { get; set; } = baseData.AccountType;

        [JsonProperty("health")]
        public float Health { get; set; } = baseData.Health;

        [JsonProperty("id")]
        public string Id { get; set; } = baseData.Id;

        [JsonProperty("isScav")]
        public bool IsScav { get; set; } = baseData.IsScav;

        [JsonProperty("lastPlayed")]
        public long LastPlayed { get; set; } = baseData.LastPlayed;

        [JsonProperty("modINT")]
        public string ModInt { get; set; } = baseData.ModInt;

        [JsonProperty("mods")]
        public List<string> Mods { get; set; } = baseData.Mods;

        [JsonProperty("name")]
        public string Name { get; set; } = baseData.Name;

        [JsonProperty("pmcHealth")]
        public float PmcHealth { get; set; } = baseData.PmcHealth;

        [JsonProperty("pmcLevel")]
        public int PmcLevel { get; set; } = baseData.PmcLevel;

        [JsonProperty("raidKills")]
        public int RaidKills { get; set; } = baseData.RaidKills;

        [JsonProperty("raidResult")]
        public string RaidResult { get; set; } = baseData.RaidResult;

        [JsonProperty("raidTime")]
        public float RaidTime { get; set; } = baseData.RaidTime;

        [JsonProperty("sptVer")]
        public string SptVersion { get; set; } = baseData.SptVersion;
        
        [JsonProperty("token")]
        public string Token { get; set; } = baseData.Token;

        [JsonProperty("DBinINV")]
        public bool DBinInv { get; set; } = baseData.DBinInv;

        [JsonProperty("isCasual")]
        public bool IsCasual { get; set; } = baseData.IsCasual;
        
        [JsonProperty("RaidSettings")]
        public RaidSettingsData RaidSettingsData = baseData.RaidSettingsData;
        
        #endregion
        
        [JsonProperty("discFromRaid")]
        public bool DiscFromRaid { get; set; }
        
        [JsonProperty("agressorName")]
        public string AgressorName { get; set; }
        
        [JsonProperty("isTransition")]
        public bool IsTransition { get; set; }

        [JsonProperty("isUsingStattrack")]
        public bool IsUsingStattrack { get; set; } = false;
        
        [JsonProperty("lastRaidEXP")]
        public int LastRaidEXP { get; set; }

        [JsonProperty("hideout")]
        public HideoutData HideoutData { get; set; } = null;
        
        [JsonProperty("lastRaidHits")]
        public int LastRaidHits { get; set; }
        
        [JsonProperty("lastRaidMap")]
        public string LastRaidMap { get; set; }
        
        [JsonProperty("lastRaidMapRaw")]
        public string LastRaidMapRaw { get; set; }
        
        [JsonProperty("lastRaidTransitionTo")]
        public string LastRaidTransitionTo { get; set; }
        
        [JsonProperty("raidHits")]
        public HitsData RaidHits { get; set; }
        
        [JsonProperty("allAchievements")]
        public Dictionary<string, int> AllAchievements { get; set; }
        
        [JsonProperty("longestShot")]
        public int LongestShot { get; set; }
        
        [JsonProperty("longestHeadshot")]
        public int LongestHeadshot { get; set; }    
        
        [JsonProperty("lastRaidAverageShot")]
        public float AverageShot { get; set; }
        
        [JsonProperty("DiedAtX")]
        public float DiedAtX { get; set; }
        
        [JsonProperty("DiedAtY")]
        public float DiedAtY { get; set; }
        
        [JsonProperty("savageKills")]
        public int SavageKills { get; set; }
        
        [JsonProperty("bossKills")]
        public int BossKills { get; set; }

        [JsonProperty("modWeaponStats")]
        public Dictionary<string, WeaponInfo> ModWeaponStats { get; set; } = null;
        
        [JsonProperty("playedAs")]
        public string PlayedAs { get; set; }
        
        [JsonProperty("pmcSide")]
        public string PmcSide { get; set; }
        
        [JsonProperty("prestige")]
        public int Prestige { get; set; }

        [JsonProperty("publicProfile")]
        public bool PublicProfile { get; set; } = true;
        
        [JsonProperty("hasKappa")]
        public bool HasKappa { get; set; } = false;
        
        [JsonProperty("raidDamage")]
        public int RaidDamage { get; set; }
        
        [JsonProperty("damageTaken")]
        public int DamageTaken { get; set; }
        
        [JsonProperty("registrationDate")]
        public long RegistrationDate { get; set; }
        
        [JsonProperty("scavLevel")]
        public int ScavLevel { get; set; }

        [JsonProperty("traderInfo")]
        public Dictionary<string, TraderData> TraderInfo { get; set; } = null;

        [JsonProperty("completed_quests")]
        public Dictionary<string, QuestInfoData> Quests { get; set; } = new();
        
        [JsonProperty("energy")]
        public float Energy { get; set; } = 0;
        
        [JsonProperty("hydration")]
        public float Hydration { get; set; } = 0;
        
        [JsonProperty("max_energy")]
        public float MaxEnergy { get; set; } = 0;
        
        [JsonProperty("max_hydration")]
        public float MaxHydration { get; set; } = 0;
        
        [JsonProperty("revenue_items")]
        public List<ItemData> RevenueItems { get; set; } = new();

        public static AdditiveProfileData MakeCopy(AdditiveProfileData original)
        {
            return new AdditiveProfileData(new BaseData
            {
                AccountType = original.AccountType,
                Health = original.Health,
                Id = original.Id,
                IsScav = original.IsScav,
                LastPlayed = original.LastPlayed,
                ModInt = "BETA",
                Mods = ["BETA"],
                Name = original.Name,
                PmcHealth = original.PmcHealth,
                PmcLevel = original.PmcLevel,
                RaidKills = original.RaidKills,
                RaidResult = original.RaidResult,
                RaidTime = original.RaidTime,
                SptVersion = original.SptVersion,
                Token = "BETA",
                DBinInv = original.DBinInv,
                IsCasual = original.IsCasual
            })
            {
                DiscFromRaid = original.DiscFromRaid,
                AgressorName = original.AgressorName,
                IsTransition = original.IsTransition,
                IsUsingStattrack = original.IsUsingStattrack,
                LastRaidEXP = original.LastRaidEXP,
                HideoutData = original.HideoutData,
                LastRaidHits = original.LastRaidHits,
                LastRaidMap = original.LastRaidMap,
                LastRaidMapRaw = original.LastRaidMapRaw,
                LastRaidTransitionTo = original.LastRaidTransitionTo,
                RaidHits = original.RaidHits,
                AllAchievements = original.AllAchievements,
                LongestShot = original.LongestShot,
                LongestHeadshot = 0,
                AverageShot = original.AverageShot,
                DiedAtX = original.DiedAtX,
                DiedAtY = original.DiedAtY,
                SavageKills = original.SavageKills,
                BossKills = original.BossKills,
                ModWeaponStats = original.ModWeaponStats,
                PlayedAs = original.PlayedAs,
                PmcSide = original.PmcSide,
                Prestige = original.Prestige,
                PublicProfile = original.PublicProfile,
                HasKappa = original.HasKappa,
                RaidDamage = original.RaidDamage,
                DamageTaken = 0,
                RegistrationDate = original.RegistrationDate,
                ScavLevel = original.ScavLevel,
                TraderInfo = original.TraderInfo,
                Quests = original.Quests,
                Energy = original.Energy,
                Hydration = original.Hydration,
                MaxEnergy = original.MaxEnergy,
                MaxHydration = original.MaxHydration,
                RevenueItems = original.RevenueItems
            };
        }
    }
}