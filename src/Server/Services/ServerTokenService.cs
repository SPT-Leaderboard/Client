using System.Reflection;
using System.Security.Cryptography;
using SPTarkov.DI.Annotations;
using SPTarkov.Common.Models.Logging;

namespace SPTLeaderboard.Server.Services;

[Injectable(InjectionType.Singleton)]
public class ServerTokenService
{
    public string Token { get; }

    public ServerTokenService(ISptLogger<ServerTokenService> logger)
    {
        var modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var tokenPath = Path.Combine(modDirectory, "server.token");

        if (File.Exists(tokenPath))
        {
            Token = File.ReadAllText(tokenPath).Trim();
            if (string.IsNullOrWhiteSpace(Token))
            {
                throw new InvalidOperationException("[SPT Leaderboard] server.token is empty. Restore it or delete it to generate a new server identity.");
            }
            logger.Success("[SPT Leaderboard] Server token loaded successfully.");
        }
        else
        {
            Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
            File.WriteAllText(tokenPath, Token);
            logger.Success($"[SPT Leaderboard] New server token created and saved to: {tokenPath}");
            logger.Warning("[SPT Leaderboard] Back up server.token and keep the backup in a safe place so you can restore it if lost. Losing this file changes your server identity.");
        }
    }
}
