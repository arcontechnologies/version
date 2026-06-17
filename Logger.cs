using System;
using System.IO;
using System.Text.RegularExpressions;

namespace VersionManager
{
    public static class Logger
    {
        private static readonly Regex EmailRegex = new Regex(
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static void LogOperation(string projectName, string operation, string details = null)
        {
            string message = $"Project '{projectName}' - {operation}";
            if (!string.IsNullOrEmpty(details))
                message += $" - {details}";

            LogInfo(message);
        }

        public static void LogError(string message, Exception ex = null)
        {
            try
            {
                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {Sanitize(message)}";
                if (ex != null)
                    logLine += Environment.NewLine + Sanitize(ex.ToString());

                AppendLine(logLine);
            }
            catch
            {
            }
        }

        public static void LogInfo(string message)
        {
            try
            {
                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {Sanitize(message)}";
                AppendLine(logLine);
            }
            catch
            {
            }
        }

        private static void AppendLine(string logLine)
        {
            string logFilePath = GetLogFilePath();
            string directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.AppendAllText(logFilePath, logLine + Environment.NewLine);
        }

        private static string GetLogFilePath()
        {
            if (!string.IsNullOrEmpty(ConfigurationManager.LogFilePath))
                return ConfigurationManager.LogFilePath;

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version_manager.log");
        }

        public static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string sanitized = EmailRegex.Replace(text, "[redacted]");
            sanitized = Regex.Replace(
                sanitized,
                @"(user\.email\s+"")[^""]*("")",
                "$1[redacted]$2",
                RegexOptions.IgnoreCase);

            return sanitized;
        }
    }
}
