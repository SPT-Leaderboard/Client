using System;
using Comfort.Common;
using EFT.UI;
using Newtonsoft.Json;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Data;
using SPTLeaderboard.Enums;
using SPTLeaderboard.Services;

namespace SPTLeaderboard.Utils
{
    public static class HeartbeatSender
    {
        private static DateTime _lastSendTime = DateTime.MinValue;
        private static PlayerState _lastSentState;

        public static void Send(PlayerState playerState)
        {
            if (Singleton<PreloaderUI>.Instantiated)
                {
                    var now = DateTime.UtcNow;
                    var timeSinceLastSend = (now - _lastSendTime).TotalSeconds;

                    if (timeSinceLastSend < GlobalData.HeartbeatCooldownSeconds && _lastSentState.Equals(playerState))
                    {
                        return;
                    }

                    var session = PlayerHelper.GetSession();
                    if (session?.Profile == null)
                        return;

                    var request = NetworkApiRequest.Create(GlobalData.HeartbeatUrl);

                    request.OnSuccess = (response, code) =>
                    {
                        Logger.LogWarning($"[HeartbeatSender] OnSuccess {response}");
                    };

                    request.OnFail = (error, code) => { ServerErrorHandler.HandleError(error, code); };

                    var data = new PlayerHeartbeatData
                    {
                        Type = DataUtils.GetPlayerState(playerState),
                        Timestamp = DataUtils.CurrentTimestamp,
                        Version = GlobalData.Version,
                        SessionId = session.Profile.Id,
                        Token = EncryptionService.Instance.Token
                    };

                    string jsonBody = JsonConvert.SerializeObject(data);

#if DEBUG
                    Logger.LogDebugWarning($"Request Data {jsonBody}");
#endif

                    request.SetData(jsonBody);
                    request.Send();

                    _lastSendTime = now;
                    _lastSentState = playerState;
                }
        }
        
        public static void SendInRaid(PlayerState playerState = PlayerState.IN_RAID)
        {
            if (Singleton<PreloaderUI>.Instantiated)
            {

                var session = PlayerHelper.GetSession();
                if (session?.Profile == null)
                    return;

                var request = NetworkApiRequest.Create(GlobalData.HeartbeatUrl);

                request.OnSuccess = (response, code) =>
                {
                    Logger.LogWarning($"[HeartbeatSender] SendInRaid OnSuccess {response}");
                };

                request.OnFail = (error, code) => { ServerErrorHandler.HandleError(error, code); };

                var data = new PlayerHeartbeatRaidData
                {
                    Type = DataUtils.GetPlayerState(playerState),
                    Timestamp = DataUtils.CurrentTimestamp,
                    Version = GlobalData.Version,
                    SessionId = session.Profile.Id,
                    Map = DataUtils.GetRaidRawMap(),
                    Side = DataUtils.GetRaidPlayerSide(),
                    GameTime = DataUtils.GetRaidGameTime(),
                    Token = EncryptionService.Instance.Token
                };

                string jsonBody = JsonConvert.SerializeObject(data);

#if DEBUG
                if (Settings.Instance.Debug.Value)
                {
                    Logger.LogWarning($"[HeartbeatSender] SendInRaid Data {jsonBody}");
                }
#endif

                request.SetData(jsonBody);
                request.Send();

                _lastSentState = playerState;
            }
        }
    }
}
