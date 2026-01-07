using System;
using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using SPTLeaderboard.Data;
using SPTLeaderboard.Utils;
using SPTLeaderboard.Utils.Zones;
using UnityEngine;
using Logger = SPTLeaderboard.Utils.Logger;

// ReSharper disable InconsistentNaming

namespace SPTLeaderboard.Services
{
    public class ZoneTrackerService : MonoBehaviour
    {
        public ZoneData CurrentZone { get; private set; }
        public ZoneData CurrentSubZone { get; private set; }

        public float ZoneEntryTime;
        public float ZoneEntryPedometer;
        public int ZoneMedicinesUsed;
        public float ZoneHealthHealed;
        public float ZoneDamageToPlayer;
        public float ZoneDamageToEnemy;

        public float SubZoneEntryTime;
        public float SubZoneEntryPedometer;
        public int SubZoneMedicinesUsed;
        public float SubZoneHealthHealed;
        public float SubZoneDamageToPlayer;
        public float SubZoneDamageToEnemy;
        
        public Action OnZoneUpdated;
        public Action OnSubZoneUpdated;

        public ZoneTrackerData CurrentRaidData { get; set; } = new();
        
        public Dictionary<string, List<ZoneData>> AllZones => _allZones;

        public Action<Dictionary<string, List<ZoneData>>> OnZonesLoaded;
        private List<ZoneData> _zones = new();
        private Dictionary<string, List<ZoneData>> _allZones = new();

        private List<string> _lootedContainers = new();

        public void Enable()
        {
            _zones.Clear();
            CurrentRaidData = new ZoneTrackerData();
            _lootedContainers.Clear();
            CurrentZone = null;
            CurrentSubZone = null;

            ZoneEntryTime = 0f;
            ZoneEntryPedometer = 0f;
            ZoneMedicinesUsed = 0;
            ZoneHealthHealed = 0f;
            ZoneDamageToPlayer = 0f;
            ZoneDamageToEnemy = 0f;

            SubZoneEntryTime = 0f;
            SubZoneEntryPedometer = 0f;
            SubZoneMedicinesUsed = 0;
            SubZoneHealthHealed = 0f;
            SubZoneDamageToPlayer = 0f;
            SubZoneDamageToEnemy = 0f;

            LeaderboardPlugin.Instance.FixedTick += CheckPlayerPosition;
            LoadZones();
            LoadZonesForCurrentMap();
        }

        public void LoadZonesForCurrentMap()
        {
            var mapName = DataUtils.GetPrettyMapName(DataUtils.GetRaidRawMap().ToLower());
            if (_allZones.TryGetValue(mapName, out var mapZones) && mapZones != null)
            {
                _zones = mapZones;
                Logger.LogWarning($"[ZoneTracker] Loaded {_zones.Count} zones for map {mapName}");
            }
            else
            {
                _allZones.Clear();
                Logger.LogWarning($"[ZoneTracker] For map {DataUtils.GetRaidRawMap().ToLower()} zones not found.");
            }
        }

        public void Disable()
        {
            LeaderboardPlugin.Instance.FixedTick -= CheckPlayerPosition;

            ExitCurrentZone();

            _zones.Clear();
            _lootedContainers.Clear();
            _allZones.Clear();

            CurrentZone = null;
            CurrentSubZone = null;

            ZoneEntryTime = 0f;
            ZoneEntryPedometer = 0f;
            ZoneMedicinesUsed = 0;
            ZoneHealthHealed = 0f;
            ZoneDamageToPlayer = 0f;
            ZoneDamageToEnemy = 0f;

            SubZoneEntryTime = 0f;
            SubZoneEntryPedometer = 0f;
            SubZoneMedicinesUsed = 0;
            SubZoneHealthHealed = 0f;
            SubZoneDamageToPlayer = 0f;
            SubZoneDamageToEnemy = 0f;
        }

        public void LoadZones()
        {
            LeaderboardPlugin.Instance.ZoneRepository ??= new ZoneRepository();
            
            var loaded = LeaderboardPlugin.Instance.ZoneRepository.LoadAllZones();
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

            var foundZone = FindZoneContainingPosition(pos, _zones);
            if (foundZone != null)
            {
                if (CurrentZone != foundZone)
                    EnterZone(foundZone);

                if (foundZone.SubZones != null && foundZone.SubZones.Count > 0)
                {
                    ZoneData foundSubZone = FindZoneContainingPosition(pos, foundZone.SubZones);
                    if (foundSubZone != null)
                    {
                        if (CurrentSubZone != foundSubZone)
                            EnterSubZone(foundSubZone);
                    }
                    else
                    {
                        if (CurrentSubZone != null)
                        {
                            ExitCurrentSubZone();
                        }
                    }
                }

                return;
            }

            ExitCurrentZone();
        }

        private ZoneData FindZoneContainingPosition(Vector3 pos, List<ZoneData> zones)
        {
            return zones.Where(zone => zone != null).FirstOrDefault(zone => zone.GetBounds().Contains(pos));
        }

        private void EnterZone(ZoneData newZone)
        {
            CurrentZone = newZone;
            ZoneEntryTime = Time.fixedTime;
            ZoneEntryPedometer = GetKilometer();
            ZoneMedicinesUsed = GetUsedMedicines();
            ZoneHealthHealed = GetHealthHealed();
            ZoneDamageToPlayer = GetDamageToPlayer();
            ZoneDamageToEnemy = GetDamageToEnemy();

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
            SubZoneHealthHealed = GetHealthHealed();
            SubZoneDamageToPlayer = GetDamageToPlayer();
            SubZoneDamageToEnemy = GetDamageToEnemy();

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
            
            float healthHealed = GetHealthHealed() - ZoneHealthHealed;
            if (!CurrentRaidData.HealthHealedUsedInZones.ContainsKey(CurrentZone.GUID))
                CurrentRaidData.HealthHealedUsedInZones[CurrentZone.GUID] = 0;
            CurrentRaidData.HealthHealedUsedInZones[CurrentZone.GUID] += healthHealed;

            float damageToPlayer = GetDamageToPlayer() - ZoneDamageToPlayer;
            if (!CurrentRaidData.DamageToPlayerInZones.ContainsKey(CurrentZone.GUID))
                CurrentRaidData.DamageToPlayerInZones[CurrentZone.GUID] = 0;
            CurrentRaidData.DamageToPlayerInZones[CurrentZone.GUID] += damageToPlayer;
            
            float damageToEnemy = GetDamageToEnemy() - ZoneDamageToEnemy;
            if (!CurrentRaidData.DamageToEnemyInZones.ContainsKey(CurrentZone.GUID))
                CurrentRaidData.DamageToEnemyInZones[CurrentZone.GUID] = 0;
            CurrentRaidData.DamageToEnemyInZones[CurrentZone.GUID] += damageToEnemy;

            Logger.LogDebugWarning($"[ZoneTracker]: Exit Sub-zone {CurrentZone.Name},   " +
                                   $"total time: {CurrentRaidData.TimeSpendInZones[CurrentZone.GUID]:F1}s,   " +
                                   $"total kilometer walked: {CurrentRaidData.KilometerWalkedInZones[CurrentZone.GUID]:F1},   " +
                                   $"total medicines used: {CurrentRaidData.MedicinesUsedInZones[CurrentZone.GUID]},   " +
                                   $"total health healed: {CurrentRaidData.HealthHealedUsedInZones[CurrentZone.GUID]:F1},   " +
                                   $"total damage to player: {CurrentRaidData.DamageToPlayerInZones[CurrentZone.GUID]:F1}    " +
                                   $"total damage to enemy: {CurrentRaidData.DamageToEnemyInZones[CurrentZone.GUID]:F1}");

            ZoneEntryTime = 0f;
            ZoneEntryPedometer = 0f;
            ZoneMedicinesUsed = 0;
            ZoneHealthHealed = 0f;
            ZoneDamageToPlayer = 0f;
            ZoneDamageToEnemy = 0f;
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

            float timeSpent = Time.fixedTime - SubZoneEntryTime;
            if (!CurrentRaidData.TimeSpendInZones.ContainsKey(CurrentSubZone.GUID))
                CurrentRaidData.TimeSpendInZones[CurrentSubZone.GUID] = 0f;
            CurrentRaidData.TimeSpendInZones[CurrentSubZone.GUID] += timeSpent;

            float kilometerWalked = GetKilometer() - SubZoneEntryPedometer;
            if (!CurrentRaidData.KilometerWalkedInZones.ContainsKey(CurrentSubZone.GUID))
                CurrentRaidData.KilometerWalkedInZones[CurrentSubZone.GUID] = 0f;
            CurrentRaidData.KilometerWalkedInZones[CurrentSubZone.GUID] += kilometerWalked;

            int medicinesUsed = GetUsedMedicines() - SubZoneMedicinesUsed;
            if (!CurrentRaidData.MedicinesUsedInZones.ContainsKey(CurrentSubZone.GUID))
                CurrentRaidData.MedicinesUsedInZones[CurrentSubZone.GUID] = 0;
            CurrentRaidData.MedicinesUsedInZones[CurrentSubZone.GUID] += medicinesUsed;

            float healthHealed = GetHealthHealed() - SubZoneHealthHealed;
            if (!CurrentRaidData.HealthHealedUsedInZones.ContainsKey(CurrentSubZone.GUID))
                CurrentRaidData.HealthHealedUsedInZones[CurrentSubZone.GUID] = 0;
            CurrentRaidData.HealthHealedUsedInZones[CurrentSubZone.GUID] += healthHealed;
            
            float damageToPlayer = GetDamageToPlayer() - SubZoneDamageToPlayer;
            if (!CurrentRaidData.DamageToPlayerInZones.ContainsKey(CurrentSubZone.GUID))
                CurrentRaidData.DamageToPlayerInZones[CurrentSubZone.GUID] = 0;
            CurrentRaidData.DamageToPlayerInZones[CurrentSubZone.GUID] += damageToPlayer;
            
            float damageToEnemy = GetDamageToEnemy() - SubZoneDamageToEnemy;
            if (!CurrentRaidData.DamageToEnemyInZones.ContainsKey(CurrentSubZone.GUID))
                CurrentRaidData.DamageToEnemyInZones[CurrentSubZone.GUID] = 0;
            CurrentRaidData.DamageToEnemyInZones[CurrentSubZone.GUID] += damageToEnemy;

            Logger.LogDebugWarning($"[ZoneTracker]: Exit Sub-zone {CurrentSubZone.Name},   " +
                                   $"total time: {CurrentRaidData.TimeSpendInZones[CurrentSubZone.GUID]:F1}s,   " +
                                   $"total kilometer walked: {CurrentRaidData.KilometerWalkedInZones[CurrentSubZone.GUID]:F1},   " +
                                   $"total medicines used: {CurrentRaidData.MedicinesUsedInZones[CurrentSubZone.GUID]},   " +
                                   $"total health healed: {CurrentRaidData.HealthHealedUsedInZones[CurrentSubZone.GUID]:F1},   " +
                                   $"total damage to player: {CurrentRaidData.DamageToPlayerInZones[CurrentSubZone.GUID]:F1}    " +
                                   $"total damage to enemy: {CurrentRaidData.DamageToEnemyInZones[CurrentSubZone.GUID]:F1}");

            SubZoneEntryTime = 0f;
            SubZoneEntryPedometer = 0f;
            SubZoneMedicinesUsed = 0;
            SubZoneHealthHealed = 0f;
            SubZoneDamageToPlayer = 0f;
            SubZoneDamageToEnemy = 0f;
            CurrentSubZone = null;
            OnSubZoneUpdated?.Invoke();
        }

        #region Utils

        public int GetUsedMedicines()
        {
            return PlayerHelper.GetProfile().EftStats.SessionCounters
                .GetInt(SessionCounterTypesAbstractClass.Medicines);
        }

        public float GetHealthHealed()
        {
            return PlayerHelper.GetProfile().EftStats.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.Heal);
        }

        public float GetKilometer()
        {
            return PlayerHelper.Instance.Player.Pedometer.GetDistance() / 1000;
        }

        public float GetDamageToPlayer()
        {
            return PlayerHelper.GetProfile().EftStats.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.CombatDamage);
        }
        
        public float GetDamageToEnemy()
        {
            return PlayerHelper.GetProfile().EftStats.SessionCounters.GetFloat(SessionCounterTypesAbstractClass.CauseBodyDamage);
        }
        
        #endregion
        
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
            else
            {
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
        }
        public void OnEnemyDamage(DamageInfoStruct damage)
        {
            if (CurrentZone != null)
            {
                if (!CurrentRaidData.DamageToEnemyInZones.ContainsKey(CurrentZone.GUID))
                    CurrentRaidData.DamageToEnemyInZones[CurrentZone.GUID] = 0f;
                CurrentRaidData.DamageToEnemyInZones[CurrentZone.GUID] += damage.Damage;
            }
            else
            {
                if (CurrentSubZone != null)
                {
                    if (!CurrentRaidData.DamageToEnemyInZones.ContainsKey(CurrentSubZone.GUID))
                        CurrentRaidData.DamageToEnemyInZones[CurrentSubZone.GUID] = 0f;
                    CurrentRaidData.DamageToEnemyInZones[CurrentSubZone.GUID] += damage.Damage;
                }
            }
        }
        public void OnItemAdded(Item item)
        {
            if (CurrentZone != null)
            {
                if (CurrentRaidData.LootedItemsInZones.TryGetValue(CurrentZone.GUID, out var zone))
                {
                    zone.Add(new ItemData(
                        item.Id,
                        item.TemplateId.ToString(),
                        item.StackObjectsCount,
                        item.BackgroundColor.ToString()
                    ));
                }
                else
                {
                    CurrentRaidData.LootedItemsInZones[CurrentZone.GUID] = new List<ItemData>();
                }
            }
            else
            {
                if (CurrentSubZone != null)
                {
                    if (CurrentRaidData.LootedItemsInZones.TryGetValue(CurrentSubZone.GUID, out var zone))
                    {
                        zone.Add(new ItemData(
                            item.Id,
                            item.TemplateId.ToString(),
                            item.StackObjectsCount,
                            item.BackgroundColor.ToString()
                        ));
                    }
                    else
                    {
                        CurrentRaidData.LootedItemsInZones[CurrentSubZone.GUID] = new List<ItemData>();
                    }
                }
            }
        }
        public void OnItemUpdated(Item item)
        {
            if (CurrentZone != null)
            {
                if (CurrentRaidData.LootedItemsInZones.TryGetValue(CurrentZone.GUID, out var zone))
                {
                    var existingLootedItem = zone.FirstOrDefault(x => x.Id == item.Id);
                    if (existingLootedItem != null) existingLootedItem.Amount = item.StackObjectsCount;
                }
                else
                {
                    CurrentRaidData.LootedItemsInZones[CurrentZone.GUID] = new List<ItemData>();
                }
            }
            else
            {
                if (CurrentSubZone != null)
                {
                    if (CurrentRaidData.LootedItemsInZones.TryGetValue(CurrentSubZone.GUID, out var zone))
                    {
                        var existingLootedItem = zone.FirstOrDefault(x => x.Id == item.Id);
                        if (existingLootedItem != null) existingLootedItem.Amount = item.StackObjectsCount;
                    }
                    else
                    {
                        CurrentRaidData.LootedItemsInZones[CurrentSubZone.GUID] = new List<ItemData>();
                    }
                }
            }
        }
        public void OnItemRemoved(Item item)
        {
            if (CurrentZone != null)
            {
                if (CurrentRaidData.LootedItemsInZones.TryGetValue(CurrentZone.GUID, out var zone))
                {
                    var existingLootedItem = zone.FirstOrDefault(x => x.Id == item.Id);
                    if (existingLootedItem != null) zone.Remove(existingLootedItem);
                }
                else
                {
                    CurrentRaidData.LootedItemsInZones[CurrentZone.GUID] = new List<ItemData>();
                }
            }
            else
            {
                if (CurrentSubZone != null)
                {
                    if (CurrentRaidData.LootedItemsInZones.TryGetValue(CurrentSubZone.GUID, out var zone))
                    {
                        var existingLootedItem = zone.FirstOrDefault(x => x.Id == item.Id);
                        if (existingLootedItem != null) zone.Remove(existingLootedItem);
                    }
                    else
                    {
                        CurrentRaidData.LootedItemsInZones[CurrentSubZone.GUID] = new List<ItemData>();
                    }
                }
            }
        }
        public void OnStashOpened(CompoundItem item)
        {
            if(_lootedContainers.Contains(item.Parent.GetOwner().ID)) return;
            
            if (CurrentZone != null)
            {
                if (!CurrentRaidData.AmountContainersOpenedInZones.ContainsKey(CurrentZone.GUID))
                    CurrentRaidData.AmountContainersOpenedInZones[CurrentZone.GUID] = 0;
                CurrentRaidData.AmountContainersOpenedInZones[CurrentZone.GUID]++;

                if (item.TemplateId == GlobalData.ComputerContainerKey)
                {
                    if (!CurrentRaidData.AmountComputersOpenedInZones.ContainsKey(CurrentZone.GUID))
                        CurrentRaidData.AmountComputersOpenedInZones[CurrentZone.GUID] = 0;
                    CurrentRaidData.AmountComputersOpenedInZones[CurrentZone.GUID]++;
                }
                
                if (item.TemplateId == GlobalData.SafeContainerKey)
                {
                    if (!CurrentRaidData.AmountSafeOpenedInZones.ContainsKey(CurrentZone.GUID))
                        CurrentRaidData.AmountSafeOpenedInZones[CurrentZone.GUID] = 0;
                    CurrentRaidData.AmountSafeOpenedInZones[CurrentZone.GUID]++;
                }

                _lootedContainers.Add(item.Parent.GetOwner().ID);
            }
            else
            {
                if (CurrentSubZone != null)
                {
                    if (!CurrentRaidData.AmountContainersOpenedInZones.ContainsKey(CurrentSubZone.GUID))
                        CurrentRaidData.AmountContainersOpenedInZones[CurrentSubZone.GUID] = 0;
                    CurrentRaidData.AmountContainersOpenedInZones[CurrentSubZone.GUID]++;

                    if (item.TemplateId == GlobalData.ComputerContainerKey)
                    {
                        if (!CurrentRaidData.AmountComputersOpenedInZones.ContainsKey(CurrentSubZone.GUID))
                            CurrentRaidData.AmountComputersOpenedInZones[CurrentSubZone.GUID] = 0;
                        CurrentRaidData.AmountComputersOpenedInZones[CurrentSubZone.GUID]++;
                    }
                
                    if (item.TemplateId == GlobalData.SafeContainerKey)
                    {
                        if (!CurrentRaidData.AmountSafeOpenedInZones.ContainsKey(CurrentSubZone.GUID))
                            CurrentRaidData.AmountSafeOpenedInZones[CurrentSubZone.GUID] = 0;
                        CurrentRaidData.AmountSafeOpenedInZones[CurrentSubZone.GUID]++;
                    }

                    _lootedContainers.Add(item.Parent.GetOwner().ID);
                }
            }
        }
    }
}