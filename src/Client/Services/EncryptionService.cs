using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SPT.Common.Http;
using SPTLeaderboard.Configuration;
using SPTLeaderboard.Data;
using SPTLeaderboard.Utils;

namespace SPTLeaderboard.Services
{
    public class EncryptionService
    {
        public static EncryptionService Instance { get; private set; }
        
        private string _token = "";
        private string _password = "";
        
        public string Token => _token;
        public string ServerToken { get; }
        public string Password => _password;
        
        private EncryptionService()
        {
            ServerToken = JsonConvert.DeserializeObject<string>(RequestHandler.GetJson("/SPTLB/GetServerToken"));
            if (string.IsNullOrWhiteSpace(ServerToken))
            {
                throw new InvalidOperationException("Failed to load server token. Check that the SPT Leaderboard server mod is installed and up to date.");
            }

            try
            {
                if (!File.Exists(GlobalData.PathToken))
                {
                    //Migration block
                    if (File.Exists(GlobalData.PathMigrationToken))
                    {
                        File.Copy(GlobalData.PathMigrationToken, GlobalData.PathToken);
                        LoadToken();
                        
                        Logger.LogWarning(
                            "Migrated token from server mod. WARNING: DO NOT SHARE IT WITH ANYONE! If you lose it, you will lose access to the Leaderboard until next season!");
                    }
                    else
                    {
                        _token = GenerateToken();
                        WriteTokenToFile(_token);

                        Logger.LogWarning(
                            "Generated your secret token, see mod directory. WARNING: DO NOT SHARE IT WITH ANYONE! If you lose it, you will lose access to the Leaderboard until next season!");
                    }
                }
                else
                {
                    Logger.LogWarning(
                        "Your secret token was initialized by the mod. Remember to never show it to anyone!");
                    LoadToken();
                }
            }
            catch (Exception e)
            {
                Logger.LogError(
                    $"Error handling token file: ${e.Message}");
                _token = GenerateToken();
            }
            
            try
            {
                if (File.Exists(GlobalData.PathPassword))
                {
                    Logger.LogWarning(
                        "Your password was initialized by the mod. Remember to never show it to anyone!");
                    LoadPassword();
                }
            }
            catch (Exception e)
            {
                Logger.LogError(
                    $"Error handling password file: ${e.Message}");
            }
        }
        
        private void LoadToken()
        {
            _token = File.ReadAllText(GlobalData.PathToken);
        }
        
        private void LoadPassword()
        {
            _password = File.ReadAllText(GlobalData.PathPassword) ?? "";
        }
        
        private string GenerateToken()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[32];
                rng.GetBytes(tokenData);
                return BitConverter.ToString(tokenData).Replace("-", "").ToLower();
            }
        }
        
        private void WriteTokenToFile(string token)
        {
            _token = token;
            File.WriteAllText(GlobalData.PathToken, token);
        }
        
        public static EncryptionService Create()
        {
            if (Instance != null)
            {
                return Instance;
            }
            return Instance = new EncryptionService();
        }
        
        public string GetHashMod()
        {
            #if DEBUG
            if (Settings.Instance.Debug.Value)
            {
                return "203923c227b2f564302c84b9e6aaeaa792e849a7dbd4a0a19554edb17f87a1f8";
            }
            #endif
            
            try
            {
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                byte[] dllBytes = File.ReadAllBytes(assemblyLocation);
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(dllBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch (Exception)
            {
                Logger.LogError("Error check integrity");
                return "ERROR CHECK INTEGRITY";
            }
        }
    }
}
