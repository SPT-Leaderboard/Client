namespace SPTLeaderboard.Utils;

/// <summary>
/// Centralized logging that routes all logs to LeaderboardPlugin.logger.
/// Supports two types of logs: basic (always shown) and debug (shown only when debug toggle is enabled).
/// </summary>
public class Logger
{
    private Logger() { }
    
    /// <summary>
    /// Check if debug logging is enabled
    /// </summary>
    private static bool IsDebugEnabled
    {
        get
        {
#if DEBUG
            return true;
#else
            return LeaderboardPlugin.IsDebugLogsEnabled;
#endif
        }
    }
    
    #region Basic Logs (Always Shown)
    
    /// <summary>
    /// Log a basic informational message (always shown)
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void LogInfo(object message)
    {
        if (LeaderboardPlugin.logger != null)
        {
            LeaderboardPlugin.logger.LogInfo(message);
        }
    }
    
    /// <summary>
    /// Log a basic warning message (always shown)
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void LogWarning(object message)
    {
        if (LeaderboardPlugin.logger != null)
        {
            LeaderboardPlugin.logger.LogWarning(message);
        }
    }
    
    /// <summary>
    /// Log a basic error message (always shown)
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void LogError(object message)
    {
        if (LeaderboardPlugin.logger != null)
        {
            LeaderboardPlugin.logger.LogError(message);
        }
    }
    
    #endregion
    
    #region Debug Logs (Shown Only When Debug Toggle is Enabled)
    
    /// <summary>
    /// Log a debug informational message (shown only when debug toggle is enabled)
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void LogDebugInfo(object message)
    {
        if (!IsDebugEnabled || LeaderboardPlugin.logger == null)
        {
            return;
        }
        
        LeaderboardPlugin.logger.LogInfo($"{message}");
    }
    
    /// <summary>
    /// Log a debug warning message (shown only when debug toggle is enabled)
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void LogDebugWarning(object message)
    {
        if (!IsDebugEnabled || LeaderboardPlugin.logger == null)
        {
            return;
        }
        
        LeaderboardPlugin.logger.LogWarning($"{message}");
    }
    
    /// <summary>
    /// Log a debug error message (shown only when debug toggle is enabled)
    /// </summary>
    /// <param name="message">Message to log</param>
    public static void LogDebugError(object message)
    {
        if (!IsDebugEnabled || LeaderboardPlugin.logger == null)
        {
            return;
        }
        
        LeaderboardPlugin.logger.LogError($"{message}");
    }
    
    #endregion
}

