using System.Collections.Generic;
using System.Linq;
using EFT;
using EFT.InventoryLogic;
using SPTLeaderboard.Data;

namespace SPTLeaderboard.Utils;

public class TrackingLoot
{
    public List<ItemData> LootedItems = new();
    public List<ItemData> PreRaidItems = new();
    public List<string> PreRaidIdItems = new();

    public void Add(Item item)
    {
        // Check if item with this ID already exists in PreRaidItems
        var existingPreRaidItem = PreRaidItems.FirstOrDefault(x => x.Id == item.Id);
        if (existingPreRaidItem != null)
        {
            existingPreRaidItem.Amount = item.StackObjectsCount;
#if DEBUG
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Update][PreRaidEquipment] Item TemplateId: {existingPreRaidItem.TemplateId}, Id: {existingPreRaidItem.Id}, New Amount: {existingPreRaidItem.Amount}");
#endif
            return;
        }
        
        // Check if item with this ID already exists in LootedItems
        var existingLootedItem = LootedItems.FirstOrDefault(x => x.Id == item.Id);
        if (existingLootedItem != null)
        {
            existingLootedItem.Amount = item.StackObjectsCount;
#if DEBUG
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Update] Item TemplateId: {existingLootedItem.TemplateId}, Id: {existingLootedItem.Id}, New Amount: {existingLootedItem.Amount}");
#endif
            return;
        }
        
        // If item not found, add new one
        if (PreRaidIdItems.Contains(item.Id))
        {
            var preItemData = new ItemData(
                item.Id,
                item.TemplateId.ToString(),
                item.StackObjectsCount
            );
            
            PreRaidItems.Add(preItemData);
            
#if DEBUG
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Add][PreRaidEquipment] Item TemplateId: {preItemData.TemplateId}, Id: {preItemData.Id}, Amount: {preItemData.Amount}");
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
                $"[TrackingLoot][Add] Item TemplateId: {itemData.TemplateId}, Id: {itemData.Id}, Amount: {itemData.Amount}");
#endif
        }
    }

    public void Remove(Item item)
    {
        var preRaidItem = PreRaidItems.FirstOrDefault(x => x.Id == item.Id);
        if (preRaidItem != null)
        {
            PreRaidItems.Remove(preRaidItem);
#if DEBUG
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Remove][PreRaidEquipment] Item TemplateId: {preRaidItem.TemplateId}, Id: {preRaidItem.Id}");
#endif
            return;
        }
        
        var lootedItem = LootedItems.FirstOrDefault(x => x.Id == item.Id);
        if (lootedItem != null)
        {
            LootedItems.Remove(lootedItem);
#if DEBUG
            LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][Remove] Item TemplateId: {lootedItem.TemplateId}, Id: {lootedItem.Id}");
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