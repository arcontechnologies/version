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
            var restorableVersions = BuildRestorableVersions(projectPath, versions);

            if (restorableVersions.Count == 0)
            {
                Logger.LogOperation(projectName, "Restore failed", "no versions available for restoration");
                Console.WriteLine("No versions available for restoration.");
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
                if (CanRestoreFromArchive(selectedVersion))
                {
                    RestoreFromArchive(projectPath, selectedVersion);
                    Logger.LogOperation(projectName, $"Version {versionIndex:D3} restored from archive", selectedVersion.ArchivePath);
                }
                else if (!string.IsNullOrEmpty(selectedVersion.CommitHash))
                {
                    RestoreFromCommit(projectPath, selectedVersion.CommitHash);
                    Logger.LogOperation(projectName, $"Version {versionIndex:D3} restored from git", selectedVersion.CommitHash);
                }
                else
                {
                    Logger.LogOperation(projectName, "Restore failed", $"version {versionIndex:D3} could not be resolved");
                    Console.WriteLine("Unable to restore project.");
                    return;
                }

                ConsoleUI.PrintStepResult("Success");
                Console.WriteLine();
                Console.WriteLine("Project restored.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Project '{projectName}' - Failed to restore version {versionIndex}", ex);
                Console.WriteLine("Unable to restore project.");
            }
        }

        private static Dictionary<int, HistoryManager.VersionEntry> BuildRestorableVersions(
            string projectPath,
            List<HistoryManager.VersionEntry> versions)
        {
            var restorableVersions = new Dictionary<int, HistoryManager.VersionEntry>();
            List<string> commitHashes = GitManager.IsGitRepository(projectPath)
                ? GitManager.GetCommitHashesOldestFirst(projectPath)
                : new List<string>();

            foreach (var entry in versions)
            {
                if (entry.ArchiveMode == ArchiveMode.Project)
                    continue;

                if (CanRestoreFromArchive(entry))
                {
                    restorableVersions[entry.Sequence] = entry;
                    continue;
                }

                var resolved = entry;
                if (string.IsNullOrEmpty(resolved.CommitHash) &&
                    resolved.ArchiveMode == ArchiveMode.None &&
                    resolved.Sequence > 0 &&
                    resolved.Sequence <= commitHashes.Count)
                {
                    resolved.CommitHash = commitHashes[resolved.Sequence - 1];
                }

                if (!string.IsNullOrEmpty(resolved.CommitHash))
                    restorableVersions[resolved.Sequence] = resolved;
            }

            return restorableVersions;
        }

        private static bool CanRestoreFromArchive(HistoryManager.VersionEntry entry)
        {
            return !string.IsNullOrEmpty(entry.ArchivePath) && entry.ArchiveMode == ArchiveMode.Git;
        }

        private static void RestoreFromArchive(string projectPath, HistoryManager.VersionEntry version)
        {
            string gitPath = Path.Combine(projectPath, ".git");
            if (Directory.Exists(gitPath))
                DeleteDirectory(gitPath);

            ArchiveManager.ExtractArchive(version.ArchivePath, projectPath);
            GitManager.ResetHard(projectPath);
            GitManager.CleanFd(projectPath);
        }

        private static void RestoreFromCommit(string projectPath, string commitHash)
        {
            if (!GitManager.IsGitRepository(projectPath))
                throw new InvalidOperationException("Git repository not found in project folder.");

            GitManager.ResetHard(projectPath, commitHash);
            GitManager.CleanFd(projectPath);
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
            if (!string.IsNullOrEmpty(entry.CommitHash))
                return "[local]";
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
