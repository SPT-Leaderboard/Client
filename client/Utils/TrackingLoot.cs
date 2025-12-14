using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.InventoryLogic;
using SPTLeaderboard.Data;

namespace SPTLeaderboard.Utils;

// Taken from https://github.com/HiddenCirno/ShowLootValue
public class TrackingLoot
{
    public List<ItemData> LootedItems = new();
    public List<ItemData> PreRaidItems = new();
    public List<string> PreRaidIdItems = new();

    public void Add(Item item)
    {
        OverlayDebug.DebugGetProperties(item);
        if (PreRaidIdItems.Contains(item.Id))
        {
            var preItemData = new ItemData(
                item.Id,
                item.TemplateId.ToString(),
                item.StackObjectsCount
            );
            
            PreRaidItems.Add(preItemData);
            
#if DEBUG
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Add][PreRaidEquipment] Item TemplateId {preItemData.TemplateId}, Id {preItemData.Id}, Amount {preItemData.Amount}");
#endif
        }
        else
        {
            var itemData = new ItemData(
                item.Id,
                item.TemplateId.ToString(),
                item.StackObjectsCount
            );

            LootedItems.Add(itemData);

#if DEBUG
            LeaderboardPlugin.logger.LogInfo(
                $"[TrackingLoot][Add] Item TemplateId {itemData.TemplateId}, Id {itemData.Id}, Amount {itemData.Amount}");
#endif
        }
    }

    public void Remove(Item item)
    {
        var preRaidItem = PreRaidItems.FirstOrDefault(x => x.Id == item.Id);
        if (preRaidItem != null)
        {
            PreRaidItems.Remove(preRaidItem);
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Remove][PreRaidEquipment] Item TemplateId {preRaidItem.TemplateId}, Id {preRaidItem.Id}");
            return;
        }
        
        var lootedItem = LootedItems.FirstOrDefault(x => x.Id == item.Id);
        if (lootedItem != null)
        {
            LootedItems.Remove(lootedItem);
#if DEBUG
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Remove] Item TemplateId {lootedItem.TemplateId}, Id {lootedItem.Id}");
#endif
        }
    }

    private void Clear()
    {
        PreRaidItems.Clear();
        PreRaidIdItems.Clear();
        LootedItems.Clear();
    }

    public void OnStartRaid(ESideType sideType)
    {
        Clear();
        PreRaidItems = PlayerHelper.GetEquipmentItems(sideType);
        PreRaidIdItems = PreRaidItems.Select(item => item.Id).ToList();
    }
}