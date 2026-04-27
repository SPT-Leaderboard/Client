using Newtonsoft.Json;

namespace SPTLeaderboard.Data
{
    public class RaidSettingsData
    {
        [JsonProperty("BotAmount")]
        public string BotAmount { get; set; } = "AsOnline";
        [JsonProperty("BotDifficulty")]
        public string BotDifficulty { get; set; } = "AsOnline";
        [JsonProperty("BossesEnabled")]
        public bool BossesEnabled { get; set; } = true;
        [JsonProperty("BotsEnabled")]
        public bool BotsEnabled { get; set; } = true;
        [JsonProperty("MetabolismDisabled")]
        public bool MetabolismDisabled { get; set; } = false;

        /// <summary>
        /// Present when Fika Core is loaded and custom raid settings were read successfully
        /// </summary>
        [JsonProperty("fikaCustomRaidSettings", NullValueHandling = NullValueHandling.Ignore)]
        public FikaCustomRaidSettingsPayload FikaCustomRaidSettings { get; set; }

        public RaidSettingsData Clone()
        {
            FikaCustomRaidSettingsPayload fika = null;
            if (FikaCustomRaidSettings != null)
            {
                fika = new FikaCustomRaidSettingsPayload
                {
                    UseCustomWeather = FikaCustomRaidSettings.UseCustomWeather,
                    DisableOverload = FikaCustomRaidSettings.DisableOverload,
                    DisableLegStamina = FikaCustomRaidSettings.DisableLegStamina,
                    DisableArmStamina = FikaCustomRaidSettings.DisableArmStamina,
                    InstantLoad = FikaCustomRaidSettings.InstantLoad,
                    FastLoad = FikaCustomRaidSettings.FastLoad
                };
            }

            return new RaidSettingsData
            {
                BotAmount = BotAmount,
                BotDifficulty = BotDifficulty,
                BossesEnabled = BossesEnabled,
                BotsEnabled = BotsEnabled,
                MetabolismDisabled = MetabolismDisabled,
                FikaCustomRaidSettings = fika
            };
        }
    }

    /// <summary>
    /// Mirrors FikaCustomRaidSettingsJSON shape for payload
    /// </summary>
    public class FikaCustomRaidSettingsPayload
    {
        [JsonProperty("useCustomWeather")]
        public bool UseCustomWeather { get; set; }

        [JsonProperty("disableOverload")]
        public bool DisableOverload { get; set; }

        [JsonProperty("disableLegStamina")]
        public bool DisableLegStamina { get; set; }

        [JsonProperty("disableArmStamina")]
        public bool DisableArmStamina { get; set; }

        [JsonProperty("instantLoad")]
        public bool InstantLoad { get; set; }

        [JsonProperty("fastLoad")]
        public bool FastLoad { get; set; }
    }
}