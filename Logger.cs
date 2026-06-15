using System;
using System.IO;

namespace VersionManager
{
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version_manager.log");

        public static void LogError(string message, Exception ex = null)
        {
            try
            {
                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}";
                if (ex != null)
                {
                    logLine += Environment.NewLine + ex.ToString();
                }
                File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
            }
            catch
            {
                // Fail silently to prevent logger crashes
            }
        }

        public static void LogInfo(string message)
        {
            try
            {
                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}";
                File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
            }
            catch
            {
                // Fail silently
            }
        }
    }
}
