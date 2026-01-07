#if DEBUG || BETA
using System;
using SPTLeaderboard.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EFT;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Utils;

public class OverlayDebug: MonoBehaviour
{
    private static OverlayDebug _instance;
    public static OverlayDebug Instance => _instance ??= new OverlayDebug();
    
    private TextMeshProUGUI _overlayText;
    private GameObject _overlay;
    private float _lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.5f; // Update every 500ms to avoid FPS drops
    
    public void Enable()
    {
        _instance = this;
        
        _overlay = new GameObject("[SPTLeaderboard] Overlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = _overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = _overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        var textObj = new GameObject("[SPTLeaderboard] OverlayText", typeof(RectTransform));
        textObj.transform.SetParent(_overlay.transform, false);

        _overlayText = textObj.AddComponent<TextMeshProUGUI>();
        _overlayText.text = "Overlay initialized";
        _overlayText.fontSize = Settings.Instance.FontSizeDebug.Value;
        _overlayText.color = Color.white;
        _overlayText.alignment = TextAlignmentOptions.TopLeft;
        _overlayText.enableWordWrapping = false;

        var rectTransform = _overlayText.rectTransform;
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.sizeDelta = new Vector2(800, 200);
        
        SetOverlayPosition(new Vector2(Settings.Instance.PositionXDebug.Value, Settings.Instance.PositionYDebug.Value));

        LeaderboardPlugin.Instance.Tick += UpdateOverlay;
    }

    private void Update()
    {
        if (Time.time - _lastUpdateTime >= UPDATE_INTERVAL)
        {
            UpdateOverlay();
            _lastUpdateTime = Time.time;
        }
    }

    public void UpdateOverlay()
    {
        if (_overlayText == null) return;

        var debugValues = new System.Text.StringBuilder();

        // Raid hits section
        var currentHitsData = HitsTracker.Instance.GetHitsData();
        int usedMedicines = LeaderboardPlugin.Instance.ZoneTrackerService?.GetUsedMedicines() ?? 0;
        float healthHealed = LeaderboardPlugin.Instance.ZoneTrackerService?.GetHealthHealed() ?? 0f;
        float combatDamage = LeaderboardPlugin.Instance.ZoneTrackerService?.GetDamageToPlayer() ?? 0f;
        float damageToEnemy = LeaderboardPlugin.Instance.ZoneTrackerService?.GetDamageToEnemy() ?? 0f;
        
        
        debugValues.AppendLine("        ─═ RAID HITS ═─");
        debugValues.AppendFormat("Head: {0} | Chest: {1} | Stomach: {2}\n", currentHitsData.Head, currentHitsData.Chest, currentHitsData.Stomach);
        debugValues.AppendFormat("   Left Arm: {0} | Right Arm: {1}\n", currentHitsData.LeftArm, currentHitsData.RightArm);
        debugValues.AppendFormat("   Left Leg: {0} | Right Leg: {1}\n", currentHitsData.LeftLeg, currentHitsData.RightLeg);
        debugValues.AppendLine();

        // Player position
        if (PlayerHelper.Instance.Player != null)
        {
            var pos = PlayerHelper.Instance.Player.PlayerBones.transform.position;
            debugValues.AppendLine("         ─═ PLAYER POSITION ═─");
            debugValues.AppendFormat("X: {0:F1} | Y: {1:F1} | Z: {2:F1}\n", pos.x, pos.y, pos.z);
            debugValues.AppendLine();
        }

        // Zone information
        if (LeaderboardPlugin.Instance.ZoneTrackerService != null)
        {
            var zoneTracker = LeaderboardPlugin.Instance.ZoneTrackerService;
            float kilometerStat = LeaderboardPlugin.Instance.ZoneTrackerService?.GetKilometer() ?? 0f;

            debugValues.AppendLine("        ─═ CURRENT ZONE ═─");
            if (zoneTracker.CurrentZone != null)
            {
                debugValues.AppendLine($"ZONE: <color=yellow>{zoneTracker.CurrentZone.Name}</color>");
                
                // Build time and distance line properly
                bool hasTime = zoneTracker.ZoneEntryTime > 0;
                bool hasDistance = zoneTracker.ZoneEntryPedometer > 0;

                if (hasTime || hasDistance)
                {
                    if (hasTime)
                    {
                        float timeInZone = Time.fixedTime - zoneTracker.ZoneEntryTime;
                        debugValues.AppendFormat("Time: <color=#00FFFF>{0:F1}s</color>", timeInZone);
                    }

                    if (hasTime && hasDistance)
                    {
                        debugValues.Append(" | ");
                    }

                    if (hasDistance)
                    {
                        float distanceInZone = kilometerStat - zoneTracker.ZoneEntryPedometer;
                        debugValues.AppendFormat("Distance: <color=green>{0:F2}km</color>", distanceInZone);
                    }

                    debugValues.AppendLine();
                }
                
                float currentCombatDamage = PlayerHelper.GetProfile()?.EftStats.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.CombatDamage) ?? 0f;
                int currentMeds = PlayerHelper.GetProfile()?.EftStats.SessionCounters.GetInt(SessionCounterTypesAbstractClass.Medicines) ?? 0;
                float currentHeal = PlayerHelper.GetProfile()?.EftStats.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.Heal) ?? 0f;

                float damageInZone = currentCombatDamage - zoneTracker.ZoneDamageToPlayer;
                int medsInZone = currentMeds - zoneTracker.ZoneMedicinesUsed;
                float healInZone = currentHeal - zoneTracker.ZoneHealthHealed;

                if (damageInZone > 0 || medsInZone > 0 || healInZone > 0)
                {
                    debugValues.AppendLine("CURRENT SESSION:");
                    if (damageInZone > 0)
                        debugValues.AppendFormat("Damage: <color=red>{0:F1}</color> | ", damageInZone);
                    if (medsInZone > 0)
                        debugValues.AppendFormat("Meds: <color=orange>{0}</color> | ", medsInZone);
                    if (healInZone > 0)
                        debugValues.AppendFormat("Heal: <color=green>{0:F1}</color>", healInZone);
                    debugValues.AppendLine();
                }
                
                var raidData = zoneTracker.CurrentRaidData;
                if (raidData != null && zoneTracker.CurrentZone != null)
                {
                    string zoneGuid = zoneTracker.CurrentZone.GUID;

                    debugValues.AppendLine("TOTAL RAID STATS:");

                    // Build total time and distance line properly
                    bool hasTotalTime = raidData.TimeSpendInZones.TryGetValue(zoneGuid, out float totalTime);
                    bool hasTotalKm = raidData.KilometerWalkedInZones.TryGetValue(zoneGuid, out float totalKm);

                    if (hasTotalTime || hasTotalKm)
                    {
                        if (hasTotalTime)
                        {
                            debugValues.AppendFormat("Total Time: <color=#00FFFF>{0:F1}s</color>", totalTime);
                        }

                        if (hasTotalTime && hasTotalKm)
                        {
                            debugValues.Append(" | ");
                        }

                        if (hasTotalKm)
                        {
                            debugValues.AppendFormat("Total Distance: <color=green>{0:F2}km</color>", totalKm);
                        }

                        debugValues.AppendLine();
                    }
                    
                    bool hasCombatStats = false;
                    if (raidData.KillsInZones.TryGetValue(zoneGuid, out int kills))
                    {
                        debugValues.AppendFormat("Kills: <color=red>{0}</color> | ", kills);
                        hasCombatStats = true;
                    }

                    if (raidData.DamageToEnemyInZones.TryGetValue(zoneGuid, out float damageToEnemyZone))
                    {
                        debugValues.AppendFormat("Damage to Enemy: <color=red>{0:F1}</color> | ", damageToEnemyZone);
                        hasCombatStats = true;
                    }

                    if (raidData.DamageToPlayerInZones.TryGetValue(zoneGuid, out float damageToPlayer))
                    {
                        debugValues.AppendFormat("Damage To Me: <color=orange>{0:F1}</color>\n", damageToPlayer);
                        hasCombatStats = true;
                    }
                    else if (hasCombatStats)
                        debugValues.AppendLine();
                    
                    bool hasHealingStats = false;
                    if (raidData.MedicinesUsedInZones.TryGetValue(zoneGuid, out int totalMeds))
                    {
                        debugValues.AppendFormat("Total Meds: <color=orange>{0}</color> | ", totalMeds);
                        hasHealingStats = true;
                    }

                    if (raidData.HealthHealedUsedInZones.TryGetValue(zoneGuid, out float totalHeal))
                    {
                        debugValues.AppendFormat("Total Heal: <color=green>{0:F1}</color>\n", totalHeal);
                        hasHealingStats = true;
                    }
                    else if (hasHealingStats)
                        debugValues.AppendLine();

                    // Container stats
                    bool hasContainerStats = false;
                    if (raidData.AmountContainersOpenedInZones.TryGetValue(zoneGuid, out int containers))
                    {
                        debugValues.AppendFormat("Containers: <color=blue>{0}</color> | ", containers);
                        hasContainerStats = true;
                    }

                    if (raidData.AmountComputersOpenedInZones.TryGetValue(zoneGuid, out int computers))
                    {
                        debugValues.AppendFormat("Computers: <color=blue>{0}</color> | ", computers);
                        hasContainerStats = true;
                    }

                    if (raidData.AmountSafeOpenedInZones.TryGetValue(zoneGuid, out int safes))
                    {
                        debugValues.AppendFormat("Safes: <color=blue>{0}</color>\n", safes);
                        hasContainerStats = true;
                    }
                    else if (hasContainerStats)
                        debugValues.AppendLine();

                    // Loot stats
                    if (raidData.LootedItemsInZones.TryGetValue(zoneGuid, out var lootedItems) && lootedItems.Count > 0)
                    {
                        debugValues.AppendFormat("Looted Items: <color=yellow>{0}</color>\n", lootedItems.Count);
                    }
                }
            }
            else
            {
                debugValues.AppendLine("ZONE: <color=#808080>No zone</color>");
            }

            // Sub-zone information
            if (zoneTracker.CurrentSubZone != null)
            {
                debugValues.AppendLine("\n");
                debugValues.AppendLine("      ─═ SUB-ZONE ═─");
                debugValues.AppendLine($"SUB-ZONE: <color=yellow>{zoneTracker.CurrentSubZone.Name}</color>");

                // Build sub-zone time and distance line properly
                bool hasSubTime = zoneTracker.SubZoneEntryTime > 0;
                bool hasSubDistance = zoneTracker.SubZoneEntryPedometer > 0;

                if (hasSubTime || hasSubDistance)
                {
                    if (hasSubTime)
                    {
                        float timeInSubZone = Time.fixedTime - zoneTracker.SubZoneEntryTime;
                        debugValues.AppendFormat("Time: <color=#00FFFF>{0:F1}s</color>", timeInSubZone);
                    }

                    if (hasSubTime && hasSubDistance)
                    {
                        debugValues.Append(" | ");
                    }

                    if (hasSubDistance)
                    {
                        float distanceInSubZone = kilometerStat - zoneTracker.SubZoneEntryPedometer;
                        debugValues.AppendFormat("Distance: <color=green>{0:F2}km</color>", distanceInSubZone);
                    }

                    debugValues.AppendLine();
                }
                
                float currentCombatDamage = PlayerHelper.GetProfile()?.EftStats.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.CombatDamage) ?? 0f;
                int currentMeds = PlayerHelper.GetProfile()?.EftStats.SessionCounters.GetInt(SessionCounterTypesAbstractClass.Medicines) ?? 0;
                float currentHeal = PlayerHelper.GetProfile()?.EftStats.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.Heal) ?? 0f;

                float subDamageInZone = currentCombatDamage - zoneTracker.SubZoneDamageToPlayer;
                int subMedsInZone = currentMeds - zoneTracker.SubZoneMedicinesUsed;
                float subHealInZone = currentHeal - zoneTracker.SubZoneHealthHealed;

                if (subDamageInZone > 0 || subMedsInZone > 0 || subHealInZone > 0)
                {
                    debugValues.AppendLine("      CURRENT SESSION:");
                    if (subDamageInZone > 0)
                        debugValues.AppendFormat("Damage: <color=red>{0:F1}</color> | ", subDamageInZone);
                    if (subMedsInZone > 0)
                        debugValues.AppendFormat("Meds: <color=orange>{0}</color> | ", subMedsInZone);
                    if (subHealInZone > 0)
                        debugValues.AppendFormat("Heal: <color=green>{0:F1}</color>", subHealInZone);
                    debugValues.AppendLine();
                }
                
                var raidData = zoneTracker.CurrentRaidData;
                if (raidData != null && zoneTracker.CurrentSubZone != null)
                {
                    string subZoneGuid = zoneTracker.CurrentSubZone.GUID;

                    debugValues.AppendLine("      TOTAL RAID STATS:");
                    
                    bool hasSubTotalTime = raidData.TimeSpendInZones.TryGetValue(subZoneGuid, out float totalTime);
                    bool hasSubTotalKm = raidData.KilometerWalkedInZones.TryGetValue(subZoneGuid, out float totalKm);

                    if (hasSubTotalTime || hasSubTotalKm)
                    {
                        if (hasSubTotalTime)
                        {
                            debugValues.AppendFormat("Total Time: <color=#00FFFF>{0:F1}s</color>", totalTime);
                        }

                        if (hasSubTotalTime && hasSubTotalKm)
                        {
                            debugValues.Append(" | ");
                        }

                        if (hasSubTotalKm)
                        {
                            debugValues.AppendFormat("Total Distance: <color=green>{0:F2}km</color>", totalKm);
                        }

                        debugValues.AppendLine();
                    }

                    // Combat stats
                    bool hasCombatStats = false;
                    if (raidData.KillsInZones.TryGetValue(subZoneGuid, out int kills))
                    {
                        debugValues.AppendFormat("Kills: <color=red>{0}</color> | ", kills);
                        hasCombatStats = true;
                    }

                    if (raidData.DamageToEnemyInZones.TryGetValue(subZoneGuid, out float damageToEnemyInSubZone))
                    {
                        debugValues.AppendFormat("Damage Dealt: <color=red>{0:F1}</color> | ", damageToEnemyInSubZone);
                        hasCombatStats = true;
                    }

                    if (raidData.DamageToPlayerInZones.TryGetValue(subZoneGuid, out float takenDamage))
                    {
                        debugValues.AppendFormat("Damage Taken: <color=orange>{0:F1}</color>\n", takenDamage);
                    }
                    else if (hasCombatStats)
                        debugValues.AppendLine();

                    // Healing stats
                    bool hasHealingStats = false;
                    if (raidData.MedicinesUsedInZones.TryGetValue(subZoneGuid, out int totalMeds))
                    {
                        debugValues.AppendFormat("Total Meds: <color=orange>{0}</color> | ", totalMeds);
                        hasHealingStats = true;
                    }

                    if (raidData.HealthHealedUsedInZones.TryGetValue(subZoneGuid, out float totalHeal))
                    {
                        debugValues.AppendFormat("Total Heal: <color=green>{0:F1}</color>\n", totalHeal);
                    }
                    else if (hasHealingStats)
                        debugValues.AppendLine();

                    // Container stats
                    bool hasContainerStats = false;
                    if (raidData.AmountContainersOpenedInZones.TryGetValue(subZoneGuid, out int containers))
                    {
                        debugValues.AppendFormat("Containers: <color=blue>{0}</color> | ", containers);
                        hasContainerStats = true;
                    }

                    if (raidData.AmountComputersOpenedInZones.TryGetValue(subZoneGuid, out int computers))
                    {
                        debugValues.AppendFormat("Computers: <color=blue>{0}</color> | ", computers);
                        hasContainerStats = true;
                    }

                    if (raidData.AmountSafeOpenedInZones.TryGetValue(subZoneGuid, out int safes))
                    {
                        debugValues.AppendFormat("Safes: <color=blue>{0}</color>\n", safes);
                    }
                    else if (hasContainerStats)
                        debugValues.AppendLine();
                    
                    if (raidData.LootedItemsInZones.TryGetValue(subZoneGuid, out var lootedItems) && lootedItems.Count > 0)
                    {
                        debugValues.AppendFormat("Looted Items: <color=yellow>{0}</color>\n", lootedItems.Count);
                    }
                }
            }
            
            // Session Counters Debug Block
            debugValues.AppendLine();
            debugValues.AppendLine("    ─═ SESSION COUNTERS ═─");
            
            debugValues.AppendFormat("Used Medicines: <color=orange>{0}</color>\n", usedMedicines);
            debugValues.AppendFormat("Health Healed: <color=green>{0:F1}</color>\n", healthHealed);
            debugValues.AppendFormat("Kilometer: <color=#00ffff>{0:F2}</color>\n", kilometerStat);
            debugValues.AppendFormat("Damage to Me: <color=red>{0:F1}</color>\n", combatDamage);
            debugValues.AppendFormat("Damage to Enemy: <color=#a45054>{0:F2}</color>\n", damageToEnemy);
        }

        _overlayText.text = debugValues.ToString();
    }

    public void SetOverlayPosition(Vector2 anchoredPosition)
    {
        if (_overlayText != null)
            _overlayText.rectTransform.anchoredPosition = anchoredPosition;
    }
    
    public void SetFontSize(int size)
    {
        if (_overlayText != null)
            _overlayText.fontSize = size;
    }

    public void Disable()
    {
        LeaderboardPlugin.Instance.Tick -= UpdateOverlay;
        Destroy(_overlay);
        Destroy(this);
    }
    
    public static void DebugGetProperties(object item)
    {
        try
        {
            var itemType = item.GetType();
            var properties = itemType.GetProperties();
            Logger.LogInfo($"[Debug] parameter properties count: {properties.Length}");
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(item);
                    Logger.LogInfo($"[Debug] parameter.{prop.Name} = {value}");
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"[Debug] Error reading property {prop.Name}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"[Debug] Error investigating parameter properties: {ex.Message}");
        }
    }
}
#endif