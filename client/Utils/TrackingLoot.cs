using System;
using System.Collections.Generic;
using EFT.InventoryLogic;

namespace SPTLeaderboard.Utils;

// Taken from https://github.com/HiddenCirno/ShowLootValue
public class TrackingLoot
{
    public HashSet<string> TrackedIds = new HashSet<string>();
    
    public int PreRaidLootValue = 0;

    public bool Add(Item item)
    {
        if (TrackedIds.Add(item.TemplateId.ToString()))
        {
#if DEBUG
            LeaderboardPlugin.logger.LogWarning($"[TrackingLoot][Add] Item TemplateId {item.TemplateId.ToString()}");
            LeaderboardPlugin.logger.LogWarning($"[TrackingLoot][Add] Item Id {item.Id.ToString()}");
#endif
            return true;
        }
        return false;
    }

    public bool Remove(Item item)
    {
        if (TrackedIds.Remove(item.TemplateId.ToString()))
        {
#if DEBUG
            LeaderboardPlugin.logger.LogWarning($"[TrackingLoot][Remove] Item {item.TemplateId.ToString()}");
            LeaderboardPlugin.logger.LogWarning($"[TrackingLoot][Remove] Item Id {item.Id.ToString()}");
#endif
            return true;
        }
        return false;
    }

    private void Clear() => TrackedIds.Clear();

    public void OnStartRaid()
    {
        Clear();
        PreRaidLootValue = 0;
        
        LeaderboardPlugin.Instance.BeforeRaidPlayerEquipment.Clear();
        
        var listItems = PlayerHelper.GetEquipmentItemsTemplateId();
        PreRaidLootValue = DataUtils.GetPriceItems(listItems);
        
        foreach (var item in PlayerHelper.GetEquipmentItemsIds())
        {
            LeaderboardPlugin.Instance.BeforeRaidPlayerEquipment.Add(item);
        }
    }
}