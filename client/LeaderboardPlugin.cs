using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Timers;
using Cysharp.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using EFT;
using EFT.Communications;
using Newtonsoft.Json;
using SPTLeaderboard.Data;
using SPTLeaderboard.Enums;
using SPTLeaderboard.Models;
using SPTLeaderboard.Patches;
using SPTLeaderboard.Utils;
using UnityEngine;
using Timer = System.Timers.Timer;

namespace SPTLeaderboard
{
    [BepInDependency("com.arys.unitytoolkit", "2.0.1")]
    [BepInPlugin("harmonyzt.SPTLeaderboard", "SPTLeaderboard", "5.0.5")]
    public class LeaderboardPlugin : BaseUnityPlugin
    {
        public static LeaderboardPlugin Instance { get; private set; }
        
        private SettingsModel _settings;
        private LocalizationModel _localization;
        private EncryptionModel _encrypt;
        private IconSaver _iconSaver;
        private TrackingLoot _trackingLoot = new();
        
        private Timer _inRaidHeartbeatTimer;
        private Timer _preRaidCheckTimer;
        
        public bool canPreRaidCheck = true;
        public bool cachedPlayerModelPreview;
        public bool engLocaleLoaded;
        public bool configUpdated;

        public static ManualLogSource logger;
        
        private static readonly object _raidDataLock = new();
        private static bool _isSendingRaidData;
        private static string _lastSentDataHash;
        private static DateTime _lastSentDataTime = DateTime.MinValue;
        private const int HASH_EXPIRY_SECONDS = 120;
        public static bool IsDebugLogsEnabled = false;

        public RaidSettingsData SavedRaidSettingsData = new();

        private void Awake()
        {
            logger = Logger;
            Utils.Logger.LogInfo("Loading...");
            
            #region Checking Headless
            
            bool isFikaHeadless = false;
            if (!DataUtils.IsCheckedFikaHeadless)
            {
                DataUtils.CheckFikaHeadless(found => { isFikaHeadless = found; });
            }
            else
            {
                isFikaHeadless = DataUtils.FikaHeadless != null;
            }

            if (isFikaHeadless)
            {
                Utils.Logger.LogWarning("FIKA HEADLESS is found. SPTLeaderboard initialization disabled");
                return;
            }
            
            #endregion
            
            _settings = SettingsModel.Create(Config);
            _encrypt = EncryptionModel.Create();
            _localization = LocalizationModel.Create();
            
            new LeaderboardVersionLabelPatch().Enable();
            new OpenMainMenuScreenPatch().Enable();
            new OpenInventoryScreenPatch().Enable();
            new OpenSelectSideScreenPatch().Enable();
            new OpenLoadingRaidScreenPatch().Enable();
            new OnStartRaidPatch().Enable();
            new OnEndRaidPatch().Enable();
            new HideoutAwakePatch().Enable();
            new OnApplyDamageInfoPatch().Enable();
            new OnInitPlayerPatch().Enable();
            new OnEnemyDamagePatch().Enable();
            new PlayerOnDeadPatch().Enable();
            new OnPlayerAddedItem().Enable();
            new OnPlayerRemovedItem().Enable();
            new RaidSettingsHookPatch().Enable();
            
            if (!DataUtils.IsCheckedFikaCore)
            {
                DataUtils.CheckFikaCore(callback =>
                {
                    if (!callback) return;

                    new OnCoopApplyShotFourPatch().Enable();
                    Utils.Logger.LogInfo("FIKA is found. Enable patch for hit hook");
                });
            }
            
#if DEBUG
            // Enable patches for overlay with hits
            new OnGameWorldStartPatch().Enable();
            new OnGameWorldDisposePatch().Enable();
#endif
            
            Instance = this;
            Utils.Logger.LogInfo("Successful loaded!");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        if (Input.GetKeyDown(KeyCode.Space))
                        {
                            IsDebugLogsEnabled = !IsDebugLogsEnabled;
                            LocalizationModel.NotificationWarning($"Advanced logs SPTLB is: {IsDebugLogsEnabled}");
                        }
                    }
                }
            }
        }

        #region Icons
        
        /// <summary>
        /// Capture icon preview with PMC full body
        /// </summary>
        public void CreateIconFullBodyPlayer()
        {
            if (!_iconSaver)
            {
                _iconSaver = gameObject.AddComponent<IconSaver>();
            }

            if (!_iconSaver.clonePlayerModelViewObj)
            {
                _iconSaver.clonePlayerModelViewObj = _iconSaver.CreateClonedPlayerModelView();
                _iconSaver.HidePlayerModelExtraElements();
            }
                
            _iconSaver.CreateFullBodyIcon();
        }
        
        /// <summary>
        /// Caching player model view for future actions
        /// </summary>
        public void CacheFullBodyPlayerModelView()
        {
            if (!_iconSaver)
            {
                _iconSaver = gameObject.AddComponent<IconSaver>();
            }

            _iconSaver.CachePlayerModelView();
        }

        /// <summary>
        /// Capture icon preview only face 
        /// </summary>
        public void CreateIconPlayer()
        {
            if (!_iconSaver)
            {
                _iconSaver = gameObject.AddComponent<IconSaver>();
            }
            
            _iconSaver.CreateIcon();
        }
        
        #endregion

        #region Network
        
        /// <summary>
        /// Sends the player's profile image to the server.
        /// </summary>
        /// <param name="texture">The texture containing the profile image to send.</param>
        /// <param name="isFullBody">
        /// Indicates whether the image is a full-body picture (<c>true</c>) or just an avatar/icon (<c>false</c>).
        /// </param>
        /// <remarks>
        /// The method encodes <paramref name="texture"/> to PNG, then to Base64, creates a JSON payload with 
        /// the player's ID and image type, and sends it to the server at <see cref="GlobalData.IconUrl"/>.
        /// On success, the <c>OnSuccess</c> callback is triggered; on failure, the <c>OnFail</c> callback is triggered.
        /// </remarks>
        public static void SendProfileIcon(Texture2D texture, bool isFullBody)
        {
            var request = NetworkApiRequestModel.Create(GlobalData.IconUrl);
            var session = PlayerHelper.GetSession();
            request.OnSuccess = (response, code) =>
            {
                Utils.Logger.LogInfo($"[SendProfileIcon] OnSuccess {response}");
            };

            request.OnFail = (error, code) =>
            {
                ServerErrorHandler.HandleError(error, code);
            };
                    
            byte[] imageData = texture.EncodeToPNG();
            var encodedImage = Convert.ToBase64String(imageData);
            var data = new ImageData
            {
                EncodedImage = encodedImage,
                PlayerId = session.Profile.Id,
                IsFullBody = isFullBody,
                Token = EncryptionModel.Instance.Token
            };
            string jsonBody = JsonConvert.SerializeObject(data);
                    
#if DEBUG
            var logData = new ImageData
            {
                EncodedImage = "VeryMoreDataForLogsBlaBla",
                PlayerId = data.PlayerId,
                IsFullBody = data.IsFullBody,
                Token = data.Token
            };
            string logJsonBody = JsonConvert.SerializeObject(logData);
            Utils.Logger.LogWarning($"[SendProfileIcon] Data {logJsonBody}");
#endif
            request.SetData(jsonBody);
            request.Send();
        }
        
        /// <summary>
        /// Sends the raid and profile data to the server.
        /// </summary>
        /// <param name="data">An object containing the profile and raid data to be serialized and sent.</param>
        /// <remarks>
        /// The method serializes <paramref name="data"/> to JSON and sends it to the server at 
        /// <see cref="GlobalData.ProfileUrl"/>.
        /// On success, the <c>OnSuccess</c> callback is triggered; on failure, the <c>OnFail</c> callback is triggered.
        /// </remarks>
        public static void SendRaidData(object data)
        {
            SendRaidDataAsync(data, CancellationToken.None).Forget();
        }
        
        /// <summary>
        /// Sends the raid and profile data to the server (async version).
        /// </summary>
        private static async UniTaskVoid SendRaidDataAsync(object data, CancellationToken cancellationToken)
        {
            // Serialize and compute hash in background thread to avoid blocking main thread
            string jsonBody;
            string dataHash;
            
            try
            {
                (jsonBody, dataHash) = await UniTask.RunOnThreadPool(() =>
                {
                    string json = JsonConvert.SerializeObject(data);
                    string hash = DataUtils.ComputeHash(json);
                    return (json, hash);
                }, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            
            lock (_raidDataLock)
            {
                bool isHashExpired = (DateTime.Now - _lastSentDataTime).TotalSeconds > HASH_EXPIRY_SECONDS;
                
                if (_lastSentDataHash == dataHash && !isHashExpired)
                {
                    Utils.Logger.LogWarning("SendRaidData: Duplicate data detected, skipping send (same data already sent recently)");
                    return;
                }
                
                if (_isSendingRaidData)
                {
                    Utils.Logger.LogWarning("SendRaidData: Request already in progress, skipping duplicate call");
                    return;
                }
                
                _isSendingRaidData = true;
                _lastSentDataHash = dataHash;
                _lastSentDataTime = DateTime.Now;
            }

            var request = NetworkApiRequestModel.Create(GlobalData.ProfileUrl);

            request.OnSuccess = (response, code) =>
            {
                lock (_raidDataLock)
                {
                    _isSendingRaidData = false;
                    
                    _lastSentDataTime = DateTime.Now;
                }
                
                Utils.Logger.LogInfo($"[SendRaidDataAsync] OnSuccess {response}");

                try
                {
                    var responseData = JsonConvert.DeserializeObject<ResponseRaidData>(response.ToString());
                    
                    if (responseData.Response == "success")
                    {
                        if (responseData.AddedToBalance > 0 && SettingsModel.Instance.ShowPointsNotification.Value)
                        {
                            LocalizationModel.Notification(LocalizationModel.Instance.GetLocaleCoin(responseData.AddedToBalance));
                        }

                        if (responseData.BattlePassEXP > 0 && SettingsModel.Instance.ShowExperienceNotification.Value)
                        {
                            LocalizationModel.Notification(LocalizationModel.Instance.GetLocaleExperience(responseData.BattlePassEXP));
                        }
                    }
                }
                catch (Exception)
                {
                    //
                }
            
            };

            request.OnFail = (error, code) =>
            {
                lock (_raidDataLock)
                {
                    _isSendingRaidData = false;

                    _lastSentDataHash = null;
                    _lastSentDataTime = DateTime.MinValue;
                }
                
                ServerErrorHandler.HandleError(error, code);
            };
            
#if DEBUG
            Utils.Logger.LogWarning($"[SendRaidDataAsync] Data {jsonBody}");
#endif
            
            request.SetData(jsonBody);
            request.Send();
        }
        
        /// <summary>
        /// Sends pre-raid data to the server.
        /// </summary>
        /// <param name="data">
        /// An object containing the pre-raid information to be serialized and sent, typically 
        /// an instance of <see cref="PreRaidData"/>.
        /// </param>
        /// <remarks>
        /// The method serializes <paramref name="data"/> to JSON and sends it to the server at 
        /// <see cref="GlobalData.PreRaidUrl"/>.
        /// On success, the <c>OnSuccess</c> callback is triggered; on failure, the <c>OnFail</c> callback is triggered.
        /// </remarks>
        public static void SendPreRaidData(object data)
        {
            var request = NetworkApiRequestModel.Create(GlobalData.PreRaidUrl);

            request.OnSuccess = (response, code) =>
            {
                Utils.Logger.LogInfo($"[SendPreRaidData] OnSuccess {response}");
            };

            request.OnFail = (error, code) =>
            {
                ServerErrorHandler.HandleError(error, code);
            };

            string jsonBody = JsonConvert.SerializeObject(data);
            
#if DEBUG
            Utils.Logger.LogWarning($"[SendPreRaidData] Data {jsonBody}");
#endif
            
            request.SetData(jsonBody);
            request.Send();
        }
        
        #endregion

        #region Timers
        
        /// <summary>
        /// Start the timer to update the heartbeat during the raid
        /// </summary>
        public void StartInRaidHeartbeat()
        {
            StopInRaidHeartbeat();
            HeartbeatSender.Send(PlayerState.IN_RAID);
        
            _inRaidHeartbeatTimer = new Timer(_settings.SupportInRaidConnectionTimer.Value * 1000);
            _inRaidHeartbeatTimer.Elapsed += (_, __) =>
            {
                if (PlayerHelper.HasRaidStarted())
                {
                    HeartbeatSender.SendInRaid();
                }
            };
            _inRaidHeartbeatTimer.AutoReset = true;
            _inRaidHeartbeatTimer.Start();
        }

        /// <summary>
        /// Stop the timer to update the heartbeat during the raid
        /// </summary>
        public void StopInRaidHeartbeat()
        {
            if (_inRaidHeartbeatTimer == null) return;
            
            _inRaidHeartbeatTimer.Stop();
            _inRaidHeartbeatTimer.Dispose();
            _inRaidHeartbeatTimer = null;
        }

        /// <summary>
        /// Start a delay for the preRaid check.
        /// </summary>
        public void StartPreRaidCheckTimer()
        {
            StopPreRaidCheckTimer();
            
            canPreRaidCheck = false;
            _preRaidCheckTimer = new Timer(10 * 60 * 1000);
            _preRaidCheckTimer.Elapsed += (sender, args) =>
            {
                canPreRaidCheck = true;
                _preRaidCheckTimer.Stop();
                _preRaidCheckTimer.Dispose();
                _preRaidCheckTimer = null;
            };
            _preRaidCheckTimer.AutoReset = false;
            _preRaidCheckTimer.Start();
        }
        
        /// <summary>
        /// Disable the delay for preRaid check
        /// </summary>
        public void StopPreRaidCheckTimer()
        {
            if (_preRaidCheckTimer == null) return;
            
            _preRaidCheckTimer.Stop();
            _preRaidCheckTimer.Dispose();
            _preRaidCheckTimer = null;
        }
        
        #endregion
        
        public TrackingLoot TrackingLoot => _trackingLoot;
    }
}
