using System;
using System.Collections.Generic;
using EFT;
using EFT.InventoryLogic;

namespace SPTLeaderboard.Utils;

// Taken from https://github.com/HiddenCirno/ShowLootValue
public class TrackingLoot
{
    public HashSet<string> TrackedIds = new HashSet<string>();
    
    public int PreRaidLootValue { get; private set; } = 0;
    public int PostRaidLootValue { get; private set; } = 0;

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

    public void OnStartRaid(ESideType sideType)
    {
        PreRaidLootValue = DataUtils.GetPriceItems(PlayerHelper.GetEquipmentItemsTemplateId(sideType));
    }

    public void OnEndRaid(ESideType sideType)
    {
        PostRaidLootValue = DataUtils.GetPriceItems(PlayerHelper.GetEquipmentItemsTemplateId(sideType));
    }
}