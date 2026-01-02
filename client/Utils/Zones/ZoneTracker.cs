using System;
using System.Collections.Generic;
using SPTLeaderboard.Data;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace SPTLeaderboard.Utils.Zones
{
    public class ZoneTracker : MonoBehaviour
    {
        public ZoneData CurrentZone { get; private set; }
        public ZoneData CurrentSubZone { get; private set; }
        
        public float ZoneEntryTime;
        public float SubZoneEntryTime;
        public float ZoneEntryPedometer;
        public float SubZoneEntryPedometer;
        
        public Action OnZoneTimeUpdated;
        public Action OnSubZoneTimeUpdated;
        
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
            ZoneEntryPedometer = PlayerHelper.Instance.Player.Pedometer.GetDistance();

            if (!CurrentRaidData.ZonesEntered.Contains(newZone.GUID))
                CurrentRaidData.ZonesEntered.Add(newZone.GUID);

            Logger.LogDebugWarning($"ZoneTracker: Enter zone {CurrentZone.Name}");
        }

        private void ExitCurrentZone()
        {
            if (CurrentZone == null || ZoneEntryTime <= 0f)
                return;

            float timeSpent = Time.fixedTime - ZoneEntryTime;
            float kilometerWalked = PlayerHelper.Instance.Player.Pedometer.GetDistance() - ZoneEntryPedometer;
            if (!CurrentRaidData.ZonesTimesSpend.ContainsKey(CurrentZone.GUID))
                CurrentRaidData.ZonesTimesSpend[CurrentZone.GUID] = 0f;
            
            if (!CurrentRaidData.ZonesKilometerWalked.ContainsKey(CurrentZone.GUID))
                CurrentRaidData.ZonesKilometerWalked[CurrentZone.GUID] = 0f;

            CurrentRaidData.ZonesTimesSpend[CurrentZone.GUID] += timeSpent;
            CurrentRaidData.ZonesKilometerWalked[CurrentZone.GUID] += kilometerWalked;
            
            Logger.LogDebugWarning($"ZoneTracker: Exit zone {CurrentZone.Name}, total time: {CurrentRaidData.ZonesTimesSpend[CurrentZone.GUID]:F1}s, total kilometer walked: {CurrentRaidData.ZonesKilometerWalked[CurrentZone.GUID]:F1}");

            ZoneEntryTime = 0f;
            CurrentZone = null;

            // Also exit any sub-zone when exiting the main zone
            if (CurrentSubZone != null)
            {
                ExitCurrentSubZone();
            }

            OnZoneTimeUpdated?.Invoke();
        }

        private void EnterSubZone(ZoneData newSubZone)
        {
            CurrentSubZone = newSubZone;
            SubZoneEntryTime = Time.fixedTime;
            SubZoneEntryPedometer = PlayerHelper.Instance.Player.Pedometer.GetDistance();

            if (!CurrentRaidData.ZonesEntered.Contains(newSubZone.GUID))
                CurrentRaidData.ZonesEntered.Add(newSubZone.GUID);

            Logger.LogDebugWarning($"ZoneTracker: Enter sub-zone {CurrentSubZone.Name}");
        }

        private void ExitCurrentSubZone()
        {
            if (CurrentSubZone == null || SubZoneEntryTime <= 0f)
                return;

            float timeSpent = Time.fixedTime - SubZoneEntryTime;
            float kilometerWalked = PlayerHelper.Instance.Player.Pedometer.GetDistance() - SubZoneEntryPedometer;
            if (!CurrentRaidData.ZonesTimesSpend.ContainsKey(CurrentSubZone.GUID))
                CurrentRaidData.ZonesTimesSpend[CurrentSubZone.GUID] = 0f;
            
            if (!CurrentRaidData.ZonesKilometerWalked.ContainsKey(CurrentSubZone.GUID))
                CurrentRaidData.ZonesKilometerWalked[CurrentSubZone.GUID] = 0f;

            CurrentRaidData.ZonesTimesSpend[CurrentSubZone.GUID] += timeSpent;
            CurrentRaidData.ZonesKilometerWalked[CurrentSubZone.GUID] += kilometerWalked;
            Logger.LogDebugWarning($"ZoneTracker: Exit sub-zone {CurrentSubZone.Name}, total time: {CurrentRaidData.ZonesTimesSpend[CurrentSubZone.GUID]:F1}s");

            SubZoneEntryTime = 0f;
            CurrentSubZone = null;
            OnZoneTimeUpdated?.Invoke();
        }
    }
}