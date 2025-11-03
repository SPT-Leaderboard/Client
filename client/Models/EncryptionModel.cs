using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using SPTLeaderboard.Data;

namespace SPTLeaderboard.Models
{
    public class EncryptionModel
    {
        public static EncryptionModel Instance { get; private set; }
        
        private string _token = "";
        
        public string Token => _token;
        
        private EncryptionModel()
        {
            try
            {
                if (!File.Exists(GlobalData.PathToken))
                {
                    //Migration block
                    if (File.Exists(GlobalData.PathMigrationToken))
                    {
                        File.Copy(GlobalData.PathMigrationToken, GlobalData.PathToken);
                        LoadToken();
                        
                        LeaderboardPlugin.logger.LogWarning(
                            $"Migrated token from server mod. WARNING: DO NOT SHARE IT WITH ANYONE! If you lose it, you will lose access to the Leaderboard until next season!");
                    }
                    else
                    {
                        _token = GenerateToken();
                        WriteTokenToFile(_token);

                        LeaderboardPlugin.logger.LogWarning(
                            $"Generated your secret token, see mod directory. WARNING: DO NOT SHARE IT WITH ANYONE! If you lose it, you will lose access to the Leaderboard until next season!");
                    }
                }
                else
                {
                    LeaderboardPlugin.logger.LogWarning(
                        $"Your secret token was initialized by the mod. Remember to never show it to anyone!");
                    LoadToken();
                }
            }
            catch (Exception e)
            {
                LeaderboardPlugin.logger.LogError(
                    $"Error handling token file: ${e.Message}");
                _token = GenerateToken();
            }
        }
        
        private void LoadToken()
        {
            _token = File.ReadAllText(GlobalData.PathToken);
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
        
        public static EncryptionModel Create()
        {
            if (Instance != null)
            {
                return Instance;
            }
            return Instance = new EncryptionModel();
        }
        
        public string GetHashMod()
        {
            #if DEBUG
            if (SettingsModel.Instance.Debug.Value)
            {
                return "";
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
                LeaderboardPlugin.logger.LogError($"Error check integrity mod");
                return "ERROR CHECK INTEGRITY";
            }
        }
    }
}