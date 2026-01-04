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
    private List<string> _preRaidIdItems = new();

    public void Add(Item item)
    {
        // Check if item with this ID already exists in PreRaidItems
        var existingPreRaidItem = PreRaidItems.FirstOrDefault(x => x.Id == item.Id);
        if (existingPreRaidItem != null)
        {
            existingPreRaidItem.Amount = item.StackObjectsCount;
            Logger.LogDebugInfo($"[TrackingLoot][Update][PreRaidEquipment] Item TemplateId: {existingPreRaidItem.TemplateId}, Id: {existingPreRaidItem.Id}, New Amount: {existingPreRaidItem.Amount}");

            return;
        }
        
        // Check if item with this ID already exists in LootedItems
        var existingLootedItem = LootedItems.FirstOrDefault(x => x.Id == item.Id);
        if (existingLootedItem != null)
        {
            existingLootedItem.Amount = item.StackObjectsCount;
            LeaderboardPlugin.Instance.ZoneTracker?.OnItemUpdated(item);
            Logger.LogDebugInfo($"[TrackingLoot][Update] Item TemplateId: {existingLootedItem.TemplateId}, Id: {existingLootedItem.Id}, New Amount: {existingLootedItem.Amount}");

            return;
        }
        
        // If item not found, add new one
        if (_preRaidIdItems.Contains(item.Id))
        {
            var preItemData = new ItemData(
                item.Id,
                item.TemplateId.ToString(),
                item.StackObjectsCount,
                item.BackgroundColor.ToString()
            );
            
            PreRaidItems.Add(preItemData);
            
            Logger.LogDebugInfo($"[TrackingLoot][Add][PreRaidEquipment] Item TemplateId: {preItemData.TemplateId}, Id: {preItemData.Id}, Amount: {preItemData.Amount}");
        }
        else
        {
            var itemData = new ItemData(
                item.Id,
                item.TemplateId.ToString(),
                item.StackObjectsCount,
                item.BackgroundColor.ToString()
            );

            LootedItems.Add(itemData);
            LeaderboardPlugin.Instance.ZoneTracker?.OnItemAdded(item);

            Logger.LogDebugInfo($"[TrackingLoot][Add] Item TemplateId: {itemData.TemplateId}, Id: {itemData.Id}, Amount: {itemData.Amount}");
        }
    }

    public void Remove(Item item)
    {
        var preRaidItem = PreRaidItems.FirstOrDefault(x => x.Id == item.Id);
        if (preRaidItem != null)
        {
            PreRaidItems.Remove(preRaidItem);
            Logger.LogDebugInfo($"[TrackingLoot][Remove][PreRaidEquipment] Item TemplateId: {preRaidItem.TemplateId}, Id: {preRaidItem.Id}");
            
            return;
        }
        
        var lootedItem = LootedItems.FirstOrDefault(x => x.Id == item.Id);
        if (lootedItem != null)
        {
            LootedItems.Remove(lootedItem);
            LeaderboardPlugin.Instance.ZoneTracker?.OnItemRemoved(item);
            Logger.LogDebugInfo($"[TrackingLoot][Remove] Item TemplateId: {lootedItem.TemplateId}, Id: {lootedItem.Id}");
        }
    }

    private void Clear()
    {
        PreRaidItems.Clear();
        _preRaidIdItems.Clear();
        LootedItems.Clear();
    }

    public void OnStartRaid(ESideType sideType)
    {
        Clear();
        PreRaidItems = PlayerHelper.GetEquipmentItems(sideType);
        _preRaidIdItems = PreRaidItems.Select(item => item.Id).ToList();
    }
}