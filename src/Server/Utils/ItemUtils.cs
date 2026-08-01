using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;

namespace SPTLeaderboard.Server.Utils;
[Injectable(InjectionType.Singleton)]
public class ItemUtils(ItemHelper itemHelper, RagfairUtils ragfairUtils)
{
    public double GetTotalFleaPrice(MongoId[] templateIds)
    {
        if (templateIds.Length == 0)
        {
            return 0;
        }

        return templateIds.Sum(ragfairUtils.GetLowestItemPrice);
    }

    public double GetTotalHandBookPrice(MongoId[] templateIds)
    {
        if (templateIds.Length == 0)
        {
            return 0;
        }
        return templateIds.Sum(itemHelper.GetItemPrice) ?? 0;
    }

    public IEnumerable<Item> GetItemInstancesAsFiR(IEnumerable<MongoId> templateIds)
    { 
        var items = templateIds.Select(item => new Item() { Template = item, Id = new MongoId()}).ToList();
        itemHelper.SetFoundInRaid(items);
        return items;
    }
}
