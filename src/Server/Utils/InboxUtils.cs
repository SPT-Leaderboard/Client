using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;
using SPTLeaderboard.Server.Models.Responses;

namespace SPTLeaderboard.Server.Utils;

[Injectable(InjectionType.Singleton)]
public class InboxUtils(
    ItemUtils itemUtils,
    JsonUtil jsonUtil,
    MailSendService mailSendService,
    ILogger<InboxUtils> logger
)
{
    private static readonly HttpClient HttpClient = new();

    public async Task<bool> CheckInbox(MongoId sessionId)
    {
        try
        {
            UriBuilder uri = new UriBuilder("https://sptlb.katrinfoxvr.com")
            {
                Path = "/api/main/inbox/checkInbox.php",
                Query = $"sessionId={sessionId}",
            };

            HttpResponseMessage request = await HttpClient.GetAsync(uri.Uri.AbsoluteUri);
            if (!request.IsSuccessStatusCode)
            {
                logger.LogError(
                    "[SPTLeaderboard] Inbox check failed with status {ResponseStatusCode}",
                    request.StatusCode
                );
                return false;
            }

            string response = await request.Content.ReadAsStringAsync();
            CheckInboxResponseData? data = jsonUtil.Deserialize<CheckInboxResponseData>(
                response
            );
            if (data == null)
            {
                logger.LogError("[SPTLeaderboard] Inbox response JSON deserialized to NULL");
                return false;
            }

            if (data.Status != "success")
            {
                logger.LogDebug("[SPTLeaderboard] No new messages for SessionID {SessionId}", sessionId);
                return true;
            }

            var generatedItems = itemUtils.GetItemInstancesAsFiR(data.Items ?? []);
            mailSendService.SendDirectNpcMessageToPlayer(
                sessionId,
                "54cb50c76803fa8b248b4571", // Prapor
                MessageType.MessageWithItems,
                data.Message ?? "[MESSAGE WITH NO CONTENTS]",
                generatedItems.ToList()
            );

            return true;
        }
        catch (Exception e)
        {
            logger.LogError(
                "[SPTLeaderboard] Error when trying to check player's inbox: {Message} (SessionID: {sessionID}) \n {stacktrace} \n Send this to SPTLeaderboard developers",
                [e.Message, sessionId, e.StackTrace]
            );
            return false;
        }
    }
}
