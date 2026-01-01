using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Data;
using SPTLeaderboard.Utils;
using UnityEngine;
using UnityEngine.Networking;
using Logger = SPTLeaderboard.Utils.Logger;

namespace SPTLeaderboard.Services
{
    /// <summary>
    /// Handles HTTP network requests (GET/POST) with optional JSON payload, retries on timeout,
    /// and callbacks for success or failure.
    /// </summary>
    public class NetworkApiRequest : MonoBehaviour
    {
        private string _url;
        private string _jsonBody;
        private string _httpMethod = UnityWebRequest.kHttpVerbPOST;

        public Action<string, long> OnSuccess;
        public Action<string, long> OnFail;
        
        private bool _isComplete;
        
        private int _retryCount;
        private int _maxRetries = 2;
        
        private static readonly Dictionary<string, (string hash, DateTime time)> _sentDataHashes = new();
        private static readonly object _hashLock = new();
        private const int HASH_EXPIRY_SECONDS = 120;
        
        /// <summary>
        /// Sets the maximum number of retries when a request times out.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts.</param>
        public void SetMaxRetries(int maxRetries)
        {
            _maxRetries = maxRetries;
        }

        /// <summary>
        /// Factory method to create a POST request instance.
        /// </summary>
        /// <param name="url">The URL to send the request to.</param>
        /// <returns>A new <see cref="NetworkApiRequest"/> configured for POST.</returns>
        public static NetworkApiRequest Create(string url)
        {
            Logger.LogDebugWarning($"[POST] Request Url -> '{url}'");

            var obj = new GameObject("[SPTLeaderboard] NetworkRequest");
            DontDestroyOnLoad(obj);
            var request = obj.AddComponent<NetworkApiRequest>();
            request._url = url;
            request._httpMethod = UnityWebRequest.kHttpVerbPOST;
            return request;
        }

        /// <summary>
        /// Factory method to create a GET request instance.
        /// </summary>
        /// <param name="url">The URL to send the request to.</param>
        /// <returns>A new <see cref="NetworkApiRequest"/> configured for GET.</returns>
        public static NetworkApiRequest CreateGet(string url)
        {
            Logger.LogDebugWarning($"[GET] Request Url -> '{url}'");

            var obj = new GameObject("[SPTLeaderboard] NetworkRequest");
            DontDestroyOnLoad(obj);
            var request = obj.AddComponent<NetworkApiRequest>();
            request._url = url;
            request._httpMethod = UnityWebRequest.kHttpVerbGET;
            return request;
        }

        /// <summary>
        /// Sets the JSON payload for a POST request.
        /// </summary>
        /// <param name="jsonBody">The JSON string to send in the request body.</param>
        public void SetData(string jsonBody)
        {
            _jsonBody = jsonBody;
        }

        /// <summary>
        /// Starts sending the request. Handles retries on timeout automatically.
        /// </summary>
        public void Send()
        {
            RunBaseRequestAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>
        /// Internal async method that executes the HTTP request and handles success, failure, and retries.
        /// </summary>
        /// <remarks>
        /// - Calls <see cref="OnSuccess"/> if the request succeeds.  
        /// - Calls <see cref="OnFail"/> if the request fails or exceeds retry attempts.  
        /// - Automatically destroys the GameObject after completion.
        /// </remarks>
        private async UniTaskVoid RunBaseRequestAsync(CancellationToken cancellationToken = default)
        {
            if (_httpMethod == UnityWebRequest.kHttpVerbPOST && string.IsNullOrEmpty(_jsonBody))
            {
                Logger.LogWarning("Data is null or empty, skipping POST request");
                return;
            }
            
            if (_isComplete)
            {
                return;
            }
            
            if (_httpMethod == UnityWebRequest.kHttpVerbPOST && !string.IsNullOrEmpty(_jsonBody) && 
                _url == GlobalData.ProfileUrl)
            {
                string dataHash = await DataUtils.ComputeHashAsync(_jsonBody, cancellationToken);
                string hashKey = $"{_url}:{dataHash}";
                
                lock (_hashLock)
                {
                    if (_sentDataHashes.TryGetValue(hashKey, out var hashInfo))
                    {
                        bool isHashExpired = (DateTime.Now - hashInfo.time).TotalSeconds > HASH_EXPIRY_SECONDS;
                        
                        if (!isHashExpired)
                        {
                            Logger.LogWarning($"NetworkApiRequestModel: Duplicate data detected for URL {_url}, skipping send (same data already sent recently)");
                            Destroy(gameObject);
                            return;
                        }

                        _sentDataHashes.Remove(hashKey);
                    }
                }
            }
            
            _isComplete = true;
            
            UnityWebRequest request;

            if (_httpMethod == UnityWebRequest.kHttpVerbPOST)
            {
                request = new UnityWebRequest(_url, UnityWebRequest.kHttpVerbPOST);
                var bodyRaw = Encoding.UTF8.GetBytes(_jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
            }
            else // GET
            {
                request = UnityWebRequest.Get(_url);
            }

            request.downloadHandler ??= new DownloadHandlerBuffer();
            request.SetRequestHeader("X-SPT-Mod", "SPTLeaderboard");
            
            var reqId = Guid.NewGuid().ToString();
            Logger.LogDebugWarning($"Request ID = {reqId}");

            request.timeout = Settings.Instance.ConnectionTimeout.Value;

            // Start the request
            var operation = request.SendWebRequest();
            
            while (!operation.isDone && !cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(cancellationToken);
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                request.Dispose();
                Destroy(gameObject);
                return;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (_httpMethod == UnityWebRequest.kHttpVerbPOST && !string.IsNullOrEmpty(_jsonBody) && 
                    _url.Contains("/v2/v2.php"))
                {
                    string dataHash = await DataUtils.ComputeHashAsync(_jsonBody, cancellationToken);
                    string hashKey = $"{_url}:{dataHash}";
                    
                    lock (_hashLock)
                    {
                        _sentDataHashes[hashKey] = (dataHash, DateTime.Now);
                    }
                }
                
                OnSuccess?.Invoke(request.downloadHandler.text, request.responseCode);
                request.Dispose();
                Destroy(gameObject);
            }
            else
            {
                bool isTimeout = request.error != null && request.error.ToLower().Contains("timeout");
                
                if (isTimeout && _retryCount < _maxRetries)
                {
                    _retryCount++;
                    Logger.LogDebugWarning($"Timeout, retrying {_retryCount}/{_maxRetries}...");
                    _isComplete = false;
                    request.Dispose();
                    await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
                    RunBaseRequestAsync(cancellationToken).Forget();
                }
                else
                {
                    if (_retryCount >= _maxRetries && isTimeout)
                    {
                        Logger.LogDebugWarning("After five tries, nothing came out");
                    }
                    
                    if (_httpMethod == UnityWebRequest.kHttpVerbPOST && !string.IsNullOrEmpty(_jsonBody) && 
                        _url.Contains("/v2/v2.php"))
                    {
                        string dataHash = await DataUtils.ComputeHashAsync(_jsonBody, cancellationToken);
                        string hashKey = $"{_url}:{dataHash}";
                        
                        lock (_hashLock)
                        {
                            _sentDataHashes.Remove(hashKey);
                        }
                    }
                    
  
                    Logger.LogWarning($"OnFail response {request.downloadHandler.text}");

                    string errorData = !string.IsNullOrEmpty(request.downloadHandler?.text) 
                        ? request.downloadHandler.text 
                        : request.error ?? "Unknown error";
                    OnFail?.Invoke(errorData, request.responseCode);
                    request.Dispose();
                    Destroy(gameObject);
                }
            }
        }
    }
}