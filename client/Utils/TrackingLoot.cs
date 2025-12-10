using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
    
    public async UniTask OnEndRaidAsync(ESideType sideType)
    {
        PostRaidEquipValue = 0;
        PostRaidLootValue = 0;
        
        var equipmentTask = GetPriceItemsAsync(PreRaidIds.ToList());
        var lootTask = GetPriceItemsAsync(LootedIds.ToList());
        
        PostRaidEquipValue = await equipmentTask;
        PostRaidLootValue = await lootTask;
        
        LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][OnEndRaid] {(sideType == ESideType.Pmc ? "PMC" : "SCAV")} Equipment = {PostRaidEquipValue}");
        LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][OnEndRaid] {(sideType == ESideType.Pmc ? "PMC" : "SCAV")} Loot = {PostRaidLootValue}");
        LeaderboardPlugin.logger.LogInfo($"[TrackingLoot][OnEndRaid] All price requests completed. Final PostRaidLootValue = {PostRaidLootValue}");
    }

    private UniTask<int> GetPriceItemsAsync(List<string> items)
    {
        var tcs = new UniTaskCompletionSource<int>();
        
        DataUtils.GetPriceItems(items, value =>
        {
            tcs.TrySetResult(value);
        });
        
        return tcs.Task;
    }
}