using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;
using SPTLeaderboard.Server.Callbacks;
using SPTLeaderboard.Server.Models.Requests;

namespace SPTLeaderboard.Server.Routers;

[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
public class ItemRouter : StaticRouter
{
   private static ItemCallbacks _callbacks = null!;

   public ItemRouter(JsonUtil jsonUtil, ItemCallbacks callbacks) : base(jsonUtil, GetRoutes())
   {
      _callbacks = callbacks;
   }

   private static List<RouteAction> GetRoutes()
      => [
         new RouteAction<ItemPricesRequestData>("/SPTLB/GetItemPrices", async (url, data, sessionId, output, cancellationToken) => await _callbacks.HandleItemPrices(data))
      ];
}
