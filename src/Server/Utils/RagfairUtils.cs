using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services.Ragfair;

namespace SPTLeaderboard.Server.Utils;
[Injectable(InjectionType.Singleton)]
public class RagfairUtils(RagfairOfferService ragfairOfferService, ItemHelper itemHelper)
{
    public double GetLowestItemPrice(MongoId templateId)
    {
        var offers = ragfairOfferService.GetOffersOfType(templateId);
        if (offers == null)
        {
            return 0;
        }

        offers = offers.ToArray();
        if (!offers.Any())
        {
            return 0;
        }

        offers = offers.Where(o => o.User?.MemberType != MemberCategory.Trader
                                && o.Requirements?.First().TemplateId == Money.ROUBLES
                                && o.Items?.FirstOrDefault() is { } item
                                && itemHelper.GetItemQualityModifier(item) == 1.0).ToArray();
        if (!offers.Any())
        {
            return 0;
        }

        var lowestOffer = offers.Min(o => o.SummaryCost);
        if (lowestOffer == null)
        {
            return 0;
        }
        return lowestOffer.Value;
    }
}
