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
        public IReadOnlyList<ZoneData> Zones => _zones;
        public IReadOnlyDictionary<string, float> ZoneTimes => _zoneTimes;
        public IReadOnlyDictionary<string, List<ZoneData>> AllZones => _allZones;
        public ZoneRepository ZoneRepository => _zoneRepository;

        private readonly Dictionary<string, List<ZoneData>> _allZones = new();
        private List<ZoneData> _zones = new();
        private readonly Dictionary<string, float> _zoneTimes = new();
        private readonly List<string> _zonesEntered = new();

        public float ZoneEntryTime;
        private readonly ZoneRepository _zoneRepository = new(GlobalData.ZonesConfig);

        public Action<Dictionary<string, List<ZoneData>>> OnZonesLoaded;
        public Action OnZoneTimeUpdated;
        

        public void Enable()
        {
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
                Logger.LogWarning($"[ZoneTracker] Loaded {Zones.Count} zones for map {mapName}");
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
#if DEBUG || BETA
            
#endif
            
            _zones.Clear();
            _zoneTimes.Clear();
            _zonesEntered.Clear();
            CurrentZone = null;
            ZoneEntryTime = 0f;
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
            if (Zones == null || Zones.Count == 0)
                return;

            foreach (var zone in Zones)
            {
                if (zone == null) continue;

                if (zone.GetBounds().Contains(pos))
                {
                    if (CurrentZone != zone)
                        EnterZone(zone);
                    
                    return;
                }
            }
            
            ExitCurrentZone();
        }

        private void EnterZone(ZoneData newZone)
        {
            ExitCurrentZone();

            CurrentZone = newZone;
            ZoneEntryTime = Time.fixedTime;

            if (!_zonesEntered.Contains(newZone.GUID))
                _zonesEntered.Add(newZone.GUID);

            Logger.LogDebugWarning($"ZoneTracker: Enter {CurrentZone.Name}");
        }

        private void ExitCurrentZone()
        {
            if (CurrentZone == null || ZoneEntryTime <= 0f)
                return;

            float timeSpent = Time.fixedTime - ZoneEntryTime;
            if (!ZoneTimes.ContainsKey(CurrentZone.GUID))
                _zoneTimes[CurrentZone.GUID] = 0f;

            _zoneTimes[CurrentZone.GUID] += timeSpent;
            Logger.LogDebugWarning($"ZoneTracker: Exit {CurrentZone.Name}, total time: {ZoneTimes[CurrentZone.GUID]:F1}s");

            ZoneEntryTime = 0f;
            CurrentZone = null;
            OnZoneTimeUpdated?.Invoke();
        }
    }
}