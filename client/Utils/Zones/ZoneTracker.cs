using System;
using System.Collections.Generic;
using EFT;
using EFT.HealthSystem;
using SPTLeaderboard.Data;
using SPTLeaderboard.Services;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace SPTLeaderboard.Utils.Zones
{
    public class ZoneTracker : MonoBehaviour
    {
        public ZoneData CurrentZone { get; private set; }
        public ZoneData CurrentSubZone { get; private set; }
        
        public float ZoneEntryTime;
        public float ZoneEntryPedometer;
        public int ZoneMedicinesUsed;
        
        public float SubZoneEntryTime;
        public float SubZoneEntryPedometer;
        public int SubZoneMedicinesUsed;
        
        public Action OnZoneUpdated;
        public Action OnSubZoneUpdated;
        
        public Dictionary<string, List<ZoneData>> AllZones => _allZones;
        public ZoneRepository ZoneRepository => _zoneRepository;
        public ZoneTrackerData CurrentRaidData { get; set; } = new();
        
        public Action<Dictionary<string, List<ZoneData>>> OnZonesLoaded;
        private List<ZoneData> _zones = new();
        private readonly Dictionary<string, List<ZoneData>> _allZones = new();
        private readonly ZoneRepository _zoneRepository = new(GlobalData.ZonesConfig);
        
        public void Enable()
        {
            _zones.Clear();
            CurrentRaidData = new ZoneTrackerData();
            CurrentZone = null;
            ZoneEntryTime = 0f;
            
            LeaderboardPlugin.Instance.FixedTick += CheckPlayerPosition;
            LoadZones();
            LoadZonesForCurrentMap();
        }

        private void LoadZonesForCurrentMap()
        {
            var mapName = DataUtils.GetPrettyMapName(DataUtils.GetRaidRawMap());
            if (_allZones.TryGetValue(mapName, out var mapZones) && mapZones != null)
            {
                _zones = mapZones;
                Logger.LogWarning($"[ZoneTracker] Loaded {_zones.Count} zones for map {mapName}");
            }
            else
            {
                _allZones.Clear();
                Logger.LogWarning($"[ZoneTracker] For map {mapName} zones not found.");
            }
        }

        public void Disable()
        {
            LeaderboardPlugin.Instance.FixedTick -= CheckPlayerPosition;
        }

        public void LoadZones()
        {
            var loaded = _zoneRepository.LoadAllZones();
            _allZones.Clear();

            if (loaded != null)
            {
                foreach (var kvp in loaded)
                    _allZones[kvp.Key] = kvp.Value;
                
                OnZonesLoaded?.Invoke(_allZones);
            }
            else
            {
                _zones.Clear();
                Logger.LogDebugInfo("[ZoneTracker] no zones found.");
            }
        }

        public void CheckPlayerPosition()
        {
            var player = PlayerHelper.Instance.Player;
            if (player == null)
                return;

            CheckPlayerPosition(player.PlayerBones.transform.position);
        }

        public void CheckPlayerPosition(Vector3 pos)
        {
            if (_zones == null || _zones.Count == 0)
                return;

            ZoneData foundZone = FindZoneContainingPosition(pos, _zones);
            if (foundZone != null)
            {
                if (CurrentZone != foundZone)
                    EnterZone(foundZone);
                
                if (foundZone.SubZones != null && foundZone.SubZones.Count > 0)
                {
                    ZoneData foundSubZone = FindZoneContainingPosition(pos, foundZone.SubZones);
                    
                    if(CurrentSubZone != foundSubZone)
                        EnterSubZone(foundSubZone);
                }
                return;
            }

            ExitCurrentZone();
        }

        private ZoneData FindZoneContainingPosition(Vector3 pos, List<ZoneData> zones)
        {
            foreach (var zone in zones)
            {
                if (zone == null) continue;
                
                if (zone.GetBounds().Contains(pos))
                {
                    return zone;
                }
            }

            return null;
        }

        private void EnterZone(ZoneData newZone)
        {
            CurrentZone = newZone;
            ZoneEntryTime = Time.fixedTime;
            ZoneEntryPedometer = GetKilometer();
            ZoneMedicinesUsed = GetUsedMedicines();

            if (!CurrentRaidData.ZonesEntered.Contains(newZone.GUID))
                CurrentRaidData.ZonesEntered.Add(newZone.GUID);

            Logger.LogDebugWarning($"[ZoneTracker]: Enter zone {CurrentZone.Name}");
        }
        
        private void EnterSubZone(ZoneData newSubZone)
        {
            CurrentSubZone = newSubZone;
            SubZoneEntryTime = Time.fixedTime;
            SubZoneEntryPedometer = GetKilometer();
            SubZoneMedicinesUsed = GetUsedMedicines();

            if (!CurrentRaidData.ZonesEntered.Contains(newSubZone.GUID))
                CurrentRaidData.ZonesEntered.Add(newSubZone.GUID);

            Logger.LogDebugWarning($"[ZoneTracker]: Enter sub-zone {CurrentSubZone.Name}");
        }

        private void ExitCurrentZone()
        {
            if (CurrentZone == null || ZoneEntryTime <= 0f)
                return;
            
            float timeSpent = Time.fixedTime - ZoneEntryTime;
            if (!CurrentRaidData.TimeSpendInZones.ContainsKey(CurrentZone.GUID))
                CurrentRaidData.TimeSpendInZones[CurrentZone.GUID] = 0f;
            CurrentRaidData.TimeSpendInZones[CurrentZone.GUID] += timeSpent;
            
            float kilometerWalked = GetKilometer() - ZoneEntryPedometer;
            if (!CurrentRaidData.KilometerWalkedInZones.ContainsKey(CurrentZone.GUID))
                CurrentRaidData.KilometerWalkedInZones[CurrentZone.GUID] = 0f;
            CurrentRaidData.KilometerWalkedInZones[CurrentZone.GUID] += kilometerWalked;
            
            int medicinesUsed = GetUsedMedicines() - ZoneMedicinesUsed;
            if (!CurrentRaidData.MedicinesUsedInZones.ContainsKey(CurrentZone.GUID))
                CurrentRaidData.MedicinesUsedInZones[CurrentZone.GUID] = 0;
            CurrentRaidData.MedicinesUsedInZones[CurrentZone.GUID] += medicinesUsed;
            
            Logger.LogDebugWarning($"[ZoneTracker]: Exit zone {CurrentZone.Name},   " +
                                   $"total time: {CurrentRaidData.TimeSpendInZones[CurrentZone.GUID]:F1}s,   " +
                                   $"total kilometer walked: {CurrentRaidData.KilometerWalkedInZones[CurrentZone.GUID]:F1},   " +
                                   $"total medicines used: {CurrentRaidData.MedicinesUsedInZones[CurrentZone.GUID]}");

            ZoneEntryTime = 0f;
            ZoneEntryPedometer = 0f;
            ZoneMedicinesUsed = 0;
            CurrentZone = null;
            
            if (CurrentSubZone != null)
            {
                ExitCurrentSubZone();
            }

            OnZoneUpdated?.Invoke();
        }
        
        private void ExitCurrentSubZone()
        {
            if (CurrentSubZone == null || SubZoneEntryTime <= 0f)
                return;

            float timeSpent = Time.fixedTime - ZoneEntryTime;
            if (!CurrentRaidData.TimeSpendInZones.ContainsKey(CurrentSubZone.GUID))
                CurrentRaidData.TimeSpendInZones[CurrentSubZone.GUID] = 0f;
            CurrentRaidData.TimeSpendInZones[CurrentSubZone.GUID] += timeSpent;
            
            float kilometerWalked = GetKilometer() - ZoneEntryPedometer;
            if (!CurrentRaidData.KilometerWalkedInZones.ContainsKey(CurrentSubZone.GUID))
                CurrentRaidData.KilometerWalkedInZones[CurrentSubZone.GUID] = 0f;
            CurrentRaidData.KilometerWalkedInZones[CurrentSubZone.GUID] += kilometerWalked;
            
            int medicinesUsed = GetUsedMedicines() - ZoneMedicinesUsed;
            if (!CurrentRaidData.MedicinesUsedInZones.ContainsKey(CurrentSubZone.GUID))
                CurrentRaidData.MedicinesUsedInZones[CurrentSubZone.GUID] = 0;
            CurrentRaidData.MedicinesUsedInZones[CurrentSubZone.GUID] += medicinesUsed;
            
            Logger.LogDebugWarning($"[ZoneTracker]: Exit Sub-zone {CurrentSubZone.Name},   " +
                                   $"total time: {CurrentRaidData.TimeSpendInZones[CurrentSubZone.GUID]:F1}s,   " +
                                   $"total kilometer walked: {CurrentRaidData.KilometerWalkedInZones[CurrentSubZone.GUID]:F1},   " +
                                   $"total medicines used: {CurrentRaidData.MedicinesUsedInZones[CurrentSubZone.GUID]}");
            
            SubZoneEntryTime = 0f;
            SubZoneEntryPedometer = 0f;
            SubZoneMedicinesUsed = 0;
            CurrentSubZone = null;
            OnSubZoneUpdated?.Invoke();
        }

        #region Utils

        private int GetUsedMedicines()
        {
            return PlayerHelper.GetProfile().EftStats.SessionCounters
                .GetInt(SessionCounterTypesAbstractClass.Medicines);
        }
        private float GetKilometer()
        {
            return PlayerHelper.Instance.Player.Pedometer.GetDistance();
        }
        private IStatisticsManager GetStatisticsManager()
        {
            return PlayerHelper.Instance.Player.StatisticsManager;
        }

        public void OnEnemyKilledInZone(DamageInfoStruct damage, string role, float distance, EBodyPart bodyPart)
        {
            if (CurrentZone != null)
            {
                if (!CurrentRaidData.KillsInZones.ContainsKey(CurrentZone.GUID))
                    CurrentRaidData.KillsInZones[CurrentZone.GUID] = 0;
                CurrentRaidData.KillsInZones[CurrentZone.GUID]++;
                
                if (!CurrentRaidData.KillDetailsInZones.ContainsKey(CurrentZone.GUID))
                    CurrentRaidData.KillDetailsInZones[CurrentZone.GUID] = new List<KillInfo>();

                CurrentRaidData.KillDetailsInZones[CurrentZone.GUID].Add(new KillInfo
                {
                    Weapon = LocalizationService.GetLocaleName(damage.Weapon.ShortName),
                    Distance = distance,
                    Role = role,
                    BodyPart = bodyPart.ToString()
                });

                Logger.LogDebugWarning($"[ZoneTracker] Kill in zone {CurrentZone.Name}:" +
                                       $"Weapon={damage.Weapon.ShortName}, distance={distance:F1}m, BodyPart={bodyPart.ToString()}m");
            }
            
            if (CurrentSubZone != null)
            {
                if (!CurrentRaidData.KillsInZones.ContainsKey(CurrentSubZone.GUID))
                    CurrentRaidData.KillsInZones[CurrentSubZone.GUID] = 0;
                CurrentRaidData.KillsInZones[CurrentSubZone.GUID]++;
                
                if (!CurrentRaidData.KillDetailsInZones.ContainsKey(CurrentSubZone.GUID))
                    CurrentRaidData.KillDetailsInZones[CurrentSubZone.GUID] = new List<KillInfo>();

                CurrentRaidData.KillDetailsInZones[CurrentSubZone.GUID].Add(new KillInfo
                {
                    Weapon = LocalizationService.GetLocaleName(damage.Weapon.ShortName),
                    Distance = distance,
                    Role = role,
                    BodyPart = bodyPart.ToString()
                });

                Logger.LogDebugWarning($"[ZoneTracker] Kill in sub-zone {CurrentSubZone.Name}: " +
                                       $"Weapon={damage.Weapon.ShortName}, distance={distance:F1}m, BodyPart={bodyPart.ToString()}m");
            }
        }

        #endregion
    }
}