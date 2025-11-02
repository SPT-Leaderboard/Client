using System;
using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.InventoryLogic;

namespace SPTLeaderboard.Utils;

// Taken from https://github.com/HiddenCirno/ShowLootValue
public class TrackingLoot
{
    public HashSet<string> LootedIds = new HashSet<string>();
    public HashSet<string> PreRaidIds = new HashSet<string>();
    public int PreRaidLootValue { get; private set; } = 0;
    
    public int PostRaidEquipValue { get; private set; } = 0;
    public int PostRaidLootValue { get; private set; } = 0;

    public void Add(Item item)
    {
        if (LootedIds.Add(item.TemplateId.ToString()))
        {
#if DEBUG
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Add] Item TemplateId {item.TemplateId.ToString()}");
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Add] Item Id {item.Id}");
#endif
        }
    }

    public void Remove(Item item)
    {
        if (PreRaidIds.Remove(item.TemplateId.ToString()))
        {
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Remove][PreRaidEquipment] Item {item.TemplateId.ToString()}");
        }
        
        if (LootedIds.Remove(item.TemplateId.ToString()))
        {
#if DEBUG
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Remove] Item {item.TemplateId.ToString()}");
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Remove] Item Id {item.Id}");
#endif
        }
    }

    private void Clear()
    {
        PreRaidIds.Clear();
        LootedIds.Clear();
    }

    public void OnStartRaid(ESideType sideType)
    {
        Clear();
        
        PreRaidIds = PlayerHelper.GetEquipmentItemsTemplateId(sideType).ToHashSet();
        DataUtils.GetPriceItems(PlayerHelper.GetEquipmentItemsTemplateId(sideType), value =>
        {
            PreRaidLootValue = value;
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][OnStartRaid] Cost Equipment = {PreRaidLootValue}");
        });
    }

    public void OnEndRaid(ESideType sideType, Action callback)
    {
        PostRaidEquipValue = 0;
        PostRaidLootValue = 0;
        
        if (sideType == ESideType.Pmc)
        {
            DataUtils.GetPriceItems(PreRaidIds.ToList(), equipmentValue =>
            {
                PostRaidEquipValue = equipmentValue;
                LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][OnEndRaid] PMC Equipment = {equipmentValue}");
                
                DataUtils.GetPriceItems(LootedIds.ToList(), lootValue =>
                {
                    PostRaidLootValue = lootValue;
                    LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][OnEndRaid] PMC Loot = {lootValue}");
                    LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][OnEndRaid] All price requests completed. Final PostRaidLootValue = {PostRaidLootValue}");
                    callback?.Invoke();
                });
            });
        }
        else
        {
            DataUtils.GetPriceItems(PreRaidIds.ToList(), equipmentValue =>
            {
                PostRaidEquipValue = equipmentValue;
                LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][OnEndRaid] SCAV Equipment = {equipmentValue}");
                
                DataUtils.GetPriceItems(LootedIds.ToList(), lootValue =>
                {
                    PostRaidLootValue = lootValue;
                    LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][OnEndRaid] SCAV Loot = {lootValue}");
                    LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][OnEndRaid] All price requests completed. Final PostRaidLootValue = {PostRaidLootValue}");
                    callback?.Invoke();
                });
            });
        }
    }
}