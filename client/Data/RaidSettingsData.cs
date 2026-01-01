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
        public bool MetabolismDisabled { get; set; }
        
    }
}