using Newtonsoft.Json;

namespace SPTLeaderboard.Data;

public class HideoutData
{
    [JsonProperty("Vents")]
    public int Vents { get; set; }
    [JsonProperty("Security")]
    public int Security { get; set; }
    [JsonProperty("WaterCloset")]
    public int WaterCloset { get; set; }
    [JsonProperty("Stash")]
    public int Stash { get; set; }
    [JsonProperty("Generator")]
    public int Generator { get; set; }
    [JsonProperty("Heating")]
    public int Heating { get; set; }
    [JsonProperty("WaterCollector")]
    public int WaterCollector { get; set; }
    [JsonProperty("MedStation")]
    public int MedStation { get; set; }
    [JsonProperty("Kitchen")]
    public int Kitchen { get; set; }
    [JsonProperty("RestSpace")]
    public int RestSpace { get; set; }
    [JsonProperty("Workbench")]
    public int Workbench { get; set; }
    [JsonProperty("IntelligenceCenter")]
    public int IntelligenceCenter { get; set; }
    [JsonProperty("ShootingRange")]
    public int ShootingRange { get; set; }
    [JsonProperty("Library")]
    public int Library { get; set; }
    [JsonProperty("ScavCase")]
    public int ScavCase { get; set; }
    [JsonProperty("Illumination")]
    public int Illumination { get; set; }
    [JsonProperty("PlaceOfFame")]
    public int PlaceOfFame { get; set; }
    [JsonProperty("AirFilteringUnit")]
    public int AirFilteringUnit { get; set; }
    [JsonProperty("SolarPower")]
    public int SolarPower { get; set; }
    [JsonProperty("BoozeGenerator")]
    public int BoozeGenerator { get; set; }
    [JsonProperty("BitcoinFarm")]
    public int BitcoinFarm { get; set; }
    [JsonProperty("ChristmasIllumination")]
    public int ChristmasIllumination { get; set; }
    [JsonProperty("EmergencyWall")]
    public int EmergencyWall { get; set; }
    [JsonProperty("Gym")]
    public int Gym { get; set; }
    [JsonProperty("WeaponStand")]
    public int WeaponStand { get; set; }
    [JsonProperty("WeaponStandSecondary")]
    public int WeaponStandSecondary { get; set; }
    [JsonProperty("EquipmentPresetsStand")]
    public int EquipmentPresetsStand { get; set; }
    [JsonProperty("CircleOfCultists")]
    public int CircleOfCultists { get; set; }
}