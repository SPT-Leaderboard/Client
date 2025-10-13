using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class HideoutData
{
    [JsonProperty("Vents")]
    public int Vents { get; set; } = 0;
    [JsonProperty("Security")]
    public int Security { get; set; } = 0;
    [JsonProperty("WaterCloset")]
    public int WaterCloset { get; set; } = 0;
    [JsonProperty("Stash")]
    public int Stash { get; set; } = 0;
    [JsonProperty("Generator")]
    public int Generator { get; set; } = 0;
    [JsonProperty("Heating")]
    public int Heating { get; set; } = 0;
    [JsonProperty("WaterCollector")]
    public int WaterCollector { get; set; } = 0;
    [JsonProperty("MedStation")]
    public int MedStation { get; set; } = 0;
    [JsonProperty("Kitchen")]
    public int Kitchen { get; set; } = 0;
    [JsonProperty("RestSpace")]
    public int RestSpace { get; set; } = 0;
    [JsonProperty("Workbench")]
    public int Workbench { get; set; } = 0;
    [JsonProperty("IntelligenceCenter")]
    public int IntelligenceCenter { get; set; } = 0;
    [JsonProperty("ShootingRange")]
    public int ShootingRange { get; set; } = 0;
    [JsonProperty("Library")]
    public int Library { get; set; } = 0;
    [JsonProperty("ScavCase")]
    public int ScavCase { get; set; } = 0;
    [JsonProperty("Illumination")]
    public int Illumination { get; set; } = 0;
    [JsonProperty("PlaceOfFame")]
    public int PlaceOfFame { get; set; } = 0;
    [JsonProperty("AirFilteringUnit")]
    public int AirFilteringUnit { get; set; } = 0;
    [JsonProperty("SolarPower")]
    public int SolarPower { get; set; } = 0;
    [JsonProperty("BoozeGenerator")]
    public int BoozeGenerator { get; set; } = 0;
    [JsonProperty("BitcoinFarm")]
    public int BitcoinFarm { get; set; } = 0;
    [JsonProperty("ChristmasIllumination")]
    public int ChristmasIllumination { get; set; } = 0;
    [JsonProperty("EmergencyWall")]
    public int EmergencyWall { get; set; } = 0;
    [JsonProperty("Gym")]
    public int Gym { get; set; } = 0;
    [JsonProperty("WeaponStand")]
    public int WeaponStand { get; set; } = 0;
    [JsonProperty("WeaponStandSecondary")]
    public int WeaponStandSecondary { get; set; } = 0;
    [JsonProperty("EquipmentPresetsStand")]
    public int EquipmentPresetsStand { get; set; } = 0;
    [JsonProperty("CircleOfCultists")]
    public int CircleOfCultists { get; set; } = 0;
}