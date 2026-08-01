using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace SPTLeaderboard.Server
{
    public record ModMetadata : IModMetadata
    {
        public string ModGuid { get; init; } = "github.SPT-Leaderboard.Server";
        public string Name { get; init; } = "SPTLeaderboard.Server";
        public string Author { get; init; } = "Harmony";
        public List<string>? Contributors { get; init; } = ["Katrin0522", "yuyui.moe", "RuKira"];
        public SemanticVersioning.Version Version { get; init; } = new("4.2.3");
        public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");

        public bool HasPrepatcher { get; init; } = false;
        public List<string>? Incompatibilities { get; init; }
        public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
        public string? Url { get; init; } = "https://sptlb.yuyui.moe";
        public string License { get; init; } = "MPL 2.0";
    }

    [Injectable(TypePriority = OnLoadOrder.Preload + 1)]
    public class Logging(ISptLogger<Logging> logger) : IOnLoad
    {
        public Task OnLoadAsync(CancellationToken cancellationToken)
        {
            logger.Success("[SPT Leaderboard] Server mod loaded.");
            return Task.CompletedTask;
        }
    }
}
