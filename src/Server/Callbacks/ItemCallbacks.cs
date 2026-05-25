using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;
using SPTLeaderboard.Server.Models.Requests;
using SPTLeaderboard.Server.Utils;

namespace SPTLeaderboard.Server.Callbacks;

[Injectable]
public class ItemCallbacks(HttpResponseUtil httpResponseUtil, ItemUtils itemUtils)
{
    public ValueTask<string> HandleItemPrices(ItemPricesRequestData requestData)
    {
        return new ValueTask<string>(httpResponseUtil.NoBody(itemUtils.GetTotalHandBookPrice(requestData.TemplateIds)));
    }
}