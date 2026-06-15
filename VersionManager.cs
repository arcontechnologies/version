using System;
using System.IO;

namespace VersionManager
{
    public static class VersionManager
    {
        public static void CreateVersion(string projectName, ArchiveMode archiveMode)
        {
            ConsoleUI.PrintHeader();
            ConsoleUI.PrintProjectInfo(projectName);

            ConsoleUI.PrintStep("Checking project...");
            string projectPath = ProjectManager.GetProjectPath(projectName);
            if (!ProjectManager.ProjectExists(projectName))
            {
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

            ConsoleUI.PrintStep("Detecting changes...");
            if (!GitManager.IsGitRepository(projectPath))
            {
                GitManager.InitializeRepository(projectPath);
            }

            if (!GitManager.HasChanges(projectPath))
            {
                ConsoleUI.PrintErrorNoChanges();
                return;
            }
            ConsoleUI.PrintStepResult("Changes detected");

            string comment = ConsoleUI.PromptComment();
            if (string.IsNullOrWhiteSpace(comment))
            {
                comment = "Version created by Version Manager";
            }

            ConsoleUI.PrintStep("Creating version...");
            try
            {
                GitManager.StageAll(projectPath);
                GitManager.Commit(projectPath, comment);

                ProjectManager.EnsureStorageExists(projectName);
                string storagePath = ProjectManager.GetStoragePath(projectName);
                string seq = HistoryManager.GetNextVersionSequence(storagePath);
                DateTime versionDate = DateTime.Now;

                HistoryManager.AppendEntry(storagePath, projectName, seq, versionDate, comment, archiveMode);

                if (archiveMode == ArchiveMode.Git)
                {
                    string zipFileName = HistoryManager.GetGitArchiveFileName(seq);
                    string zipFilePath = Path.Combine(storagePath, zipFileName);
                    ArchiveManager.CreateVersionArchive(projectPath, zipFilePath);
                    ConsoleUI.PrintStepResult("Success");
                    ConsoleUI.PrintSuccess(zipFileName);
                }
                else if (archiveMode == ArchiveMode.Project)
                {
                    string zipFileName = HistoryManager.GetProjectArchiveFileName(seq);
                    string zipFilePath = Path.Combine(storagePath, zipFileName);
                    ArchiveManager.CreateSnapshotArchive(projectPath, zipFilePath);
                    ConsoleUI.PrintStepResult("Success");
                    ConsoleUI.PrintSuccess(zipFileName);
                }
                else
                {
                    ConsoleUI.PrintStepResult("Success");
                    ConsoleUI.PrintSuccessCommit(seq);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to create version", ex);
                ConsoleUI.PrintErrorArchiveFailed();
            }
        }

        public static void CreateSnapshot(string projectName)
        {
            ConsoleUI.PrintHeader();
            ConsoleUI.PrintProjectInfo(projectName);

            ConsoleUI.PrintStep("Checking project...");
            string projectPath = ProjectManager.GetProjectPath(projectName);
            if (!ProjectManager.ProjectExists(projectName))
            {
                ConsoleUI.PrintErrorProjectMissing(projectName, ProjectManager.GetExpectedProjectLocations(projectName));
                return;
            }
            ConsoleUI.PrintStepResult("OK");

            ConsoleUI.PrintStep("Creating snapshot...");
            try
            {
                ProjectManager.EnsureStorageExists(projectName);
                string storagePath = ProjectManager.GetStoragePath(projectName);

                string seq = GetNextSnapshotSequence(storagePath);
                string zipFileName = $"Snapshot{seq}.zip";
                string zipFilePath = Path.Combine(storagePath, zipFileName);

                ArchiveManager.CreateSnapshotArchive(projectPath, zipFilePath);
                ConsoleUI.PrintStepResult("Success");

                ConsoleUI.PrintSuccess(zipFileName);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to create snapshot", ex);
                ConsoleUI.PrintErrorArchiveFailed();
            }
        }

        private static string GetNextSnapshotSequence(string storagePath)
        {
            int maxSeq = 0;
            if (Directory.Exists(storagePath))
            {
                string[] files = Directory.GetFiles(storagePath, "Snapshot*.zip");
                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.Length > 8 && int.TryParse(fileName.Substring(8), out int num) && num > maxSeq)
                        maxSeq = num;
                }
            }
            return (maxSeq + 1).ToString("D3");
        }
    }
}
