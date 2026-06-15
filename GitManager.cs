using System;
using System.Diagnostics;
using System.IO;

namespace VersionManager
{
    public static class GitManager
    {
        public static bool IsGitInstalled()
        {
            try
            {
                var result = RunGitCommand(AppDomain.CurrentDomain.BaseDirectory, "--version");
                return result.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsGitRepository(string projectPath)
        {
            string gitDir = Path.Combine(projectPath, ".git");
            return Directory.Exists(gitDir);
        }

        public static void InitializeRepository(string projectPath)
        {
            RunGitCommand(projectPath, "init");
            
            // Configure local settings so git commit does not fail if global configs are missing
            RunGitCommand(projectPath, "config --local user.name \"Version Manager\"");
            RunGitCommand(projectPath, "config --local user.email \"versionmanager@local\"");
        }

        public static bool HasChanges(string projectPath)
        {
            var result = RunGitCommand(projectPath, "status --porcelain");
            return !string.IsNullOrWhiteSpace(result.StandardOutput);
        }

        public static void StageAll(string projectPath)
        {
            RunGitCommand(projectPath, "add .");
        }

        public static void Commit(string projectPath, string comment)
        {
            string safeComment = comment.Replace("\"", "\\\"");
            RunGitCommand(projectPath, $"commit -m \"{safeComment}\"");
        }

        public static void ResetHard(string projectPath)
        {
            RunGitCommand(projectPath, "reset --hard");
        }

        public static void CleanFd(string projectPath)
        {
            RunGitCommand(projectPath, "clean -fd");
        }

        public struct ProcessResult
        {
            public int ExitCode;
            public string StandardOutput;
            public string StandardError;
        }

        private static ProcessResult RunGitCommand(string workingDirectory, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    Logger.LogError($"Git command 'git {arguments}' in '{workingDirectory}' failed with exit code {process.ExitCode}. Error: {error}");
                }
                else
                {
                    Logger.LogInfo($"Git command 'git {arguments}' executed successfully.");
                }

                return new ProcessResult
                {
                    ExitCode = process.ExitCode,
                    StandardOutput = output,
                    StandardError = error
                };
            }
        }
    }
}
