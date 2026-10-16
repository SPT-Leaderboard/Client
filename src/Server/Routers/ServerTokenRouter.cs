using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;
using SPTLeaderboard.Server.Services;

namespace SPTLeaderboard.Server.Routers;

[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
public class ServerTokenRouter(JsonUtil jsonUtil, HttpResponseUtil httpResponseUtil, ServerTokenService serverTokenService)
    : StaticRouter(jsonUtil,
    [
        new RouteAction("/SPTLB/GetServerToken", (url, data, sessionId, output, cancellationToken) =>
            new ValueTask<object>(httpResponseUtil.NoBody(serverTokenService.Token)))
    ]);
