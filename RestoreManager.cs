using System;
using System.Collections.Generic;
using System.IO;

namespace VersionManager
{
    public static class RestoreManager
    {
        public static void ListVersions(string projectName)
        {
            string storagePath = ProjectManager.GetStoragePath(projectName);
            var versions = GetAvailableVersions(storagePath);
            if (versions.Count == 0)
            {
                Logger.LogOperation(projectName, "Versions listed", "no versions found");
                Console.WriteLine("No versions found.");
                return;
            }

            PrintVersionListHeader(projectName);
            foreach (var entry in versions)
            {
                PrintVersionLine(entry);
            }

            Logger.LogOperation(projectName, "Versions listed", $"{versions.Count} version(s)");
        }

        public static void RestoreVersion(string projectName)
        {
            ConsoleUI.PrintHeader();
            ConsoleUI.PrintProjectInfo(projectName);

            ConsoleUI.PrintStep("Checking project...");
            string projectPath = ProjectManager.GetProjectPath(projectName);
            if (!ProjectManager.ProjectExists(projectName))
            {
                Logger.LogOperation(projectName, "Restore failed", "project not found");
                ConsoleUI.PrintErrorProjectMissing(projectName, ProjectManager.GetExpectedProjectLocations(projectName));
                return;
            }
            ConsoleUI.PrintStepResult("OK");

            ConsoleUI.PrintStep("Checking Git...");
            if (!GitManager.IsGitInstalled())
            {
                ConsoleUI.PrintErrorGitMissing();
                return;
            }
            ConsoleUI.PrintStepResult("OK");

            string storagePath = ProjectManager.GetStoragePath(projectName);
            var versions = GetAvailableVersions(storagePath);
            var restorableVersions = new Dictionary<int, HistoryManager.VersionEntry>();
            foreach (var entry in versions)
            {
                if (!string.IsNullOrEmpty(entry.ArchivePath) && entry.ArchiveMode == ArchiveMode.Git)
                    restorableVersions[entry.Sequence] = entry;
            }

            if (restorableVersions.Count == 0)
            {
                Logger.LogOperation(projectName, "Restore failed", "no git-archived versions available");
                Console.WriteLine("No git-archived versions available for restoration.");
                return;
            }

            PrintVersionListHeader(projectName);
            foreach (var entry in restorableVersions.Values)
            {
                PrintVersionLine(entry);
            }

            string choice = ConsoleUI.PromptChooseVersion();
            if (!int.TryParse(choice, out int versionIndex) || !restorableVersions.ContainsKey(versionIndex))
            {
                Logger.LogOperation(projectName, "Restore failed", "invalid version choice");
                Console.WriteLine("Invalid version choice.");
                return;
            }

            var selectedVersion = restorableVersions[versionIndex];

            ConsoleUI.PrintStep("Restoring version...");
            try
            {
                string gitPath = Path.Combine(projectPath, ".git");
                if (Directory.Exists(gitPath))
                {
                    DeleteDirectory(gitPath);
                }

                ArchiveManager.ExtractArchive(selectedVersion.ArchivePath, projectPath);

                GitManager.ResetHard(projectPath);
                GitManager.CleanFd(projectPath);

                ConsoleUI.PrintStepResult("Success");
                Console.WriteLine();
                Console.WriteLine("Project restored.");
                Logger.LogOperation(projectName, $"Version {versionIndex:D3} restored", selectedVersion.ArchivePath);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Project '{projectName}' - Failed to restore version {versionIndex}", ex);
                Console.WriteLine("Unable to restore project.");
            }
        }

        private static void PrintVersionListHeader(string projectName)
        {
            Console.WriteLine("---------------------------------------");
            Console.WriteLine(projectName);
            Console.WriteLine("---------------------------------------");
        }

        private static void PrintVersionLine(HistoryManager.VersionEntry entry)
        {
            string dateText = entry.Date == DateTime.MinValue
                ? "unknown"
                : entry.Date.ToString("yyyy-MM-dd HH:mm:ss");
            string comment = string.IsNullOrEmpty(entry.Comment) ? "(no comment)" : entry.Comment;
            string archiveLabel = GetArchiveLabel(entry);
            Console.WriteLine($"{entry.Sequence:D3}  {dateText}  {archiveLabel}  {comment}");
        }

        private static string GetArchiveLabel(HistoryManager.VersionEntry entry)
        {
            if (entry.ArchiveMode == ArchiveMode.Git)
                return "[git]";
            if (entry.ArchiveMode == ArchiveMode.Project)
                return "[project]";
            if (!string.IsNullOrEmpty(entry.ArchivePath))
                return "[git]";
            return "[none]";
        }

        private static List<HistoryManager.VersionEntry> GetAvailableVersions(string storagePath)
        {
            var entries = HistoryManager.ReadEntries(storagePath);
            if (entries.Count > 0)
                return entries;

            return GetVersionsFromArchivesOnly(storagePath);
        }

        private static List<HistoryManager.VersionEntry> GetVersionsFromArchivesOnly(string storagePath)
        {
            var versions = new List<HistoryManager.VersionEntry>();
            if (!Directory.Exists(storagePath))
                return versions;

            foreach (string file in Directory.GetFiles(storagePath, "Version*.zip"))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                ArchiveMode mode;
                int sequence;

                if (fileName.StartsWith("VersionProject", StringComparison.Ordinal) && fileName.Length > 14 &&
                    int.TryParse(fileName.Substring(14), out sequence))
                {
                    mode = ArchiveMode.Project;
                }
                else if (fileName.StartsWith("Version", StringComparison.Ordinal) && fileName.Length > 7 &&
                         int.TryParse(fileName.Substring(7), out sequence))
                {
                    mode = ArchiveMode.Git;
                }
                else
                {
                    continue;
                }

                versions.Add(new HistoryManager.VersionEntry
                {
                    Sequence = sequence,
                    Date = File.GetLastWriteTime(file),
                    Comment = string.Empty,
                    ArchiveMode = mode,
                    ArchivePath = file
                });
            }

            versions.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
            return versions;
        }

        private static void DeleteDirectory(string targetDir)
        {
            string[] files = Directory.GetFiles(targetDir);
            string[] dirs = Directory.GetDirectories(targetDir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(targetDir, false);
        }
    }
}
