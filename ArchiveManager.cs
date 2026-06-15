using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace VersionManager
{
    public static class ArchiveManager
    {
        public static void CreateVersionArchive(string projectPath, string zipFilePath)
        {
            string gitPath = Path.Combine(projectPath, ".git");
            if (!Directory.Exists(gitPath))
            {
                throw new DirectoryNotFoundException($"Git repository not found at {gitPath}");
            }

            // Ensure destination directory exists
            string destDir = Path.GetDirectoryName(zipFilePath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            using (ZipOutputStream s = new ZipOutputStream(File.Create(zipFilePath)))
            {
                s.SetLevel(9); // Highest compression
                if (!string.IsNullOrEmpty(ConfigurationManager.ArchivePassword))
                {
                    s.Password = ConfigurationManager.ArchivePassword;
                }

                // Add the .git folder files recursively
                string[] files = Directory.GetFiles(gitPath, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    // Compute path relative to projectPath so it starts with ".git/"
                    string relativePath = file.Substring(projectPath.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace(Path.DirectorySeparatorChar, '/');

                    ZipEntry entry = new ZipEntry(relativePath)
                    {
                        DateTime = File.GetLastWriteTime(file)
                    };

                    s.PutNextEntry(entry);

                    using (FileStream fs = File.OpenRead(file))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            s.Write(buffer, 0, bytesRead);
                        }
                    }
                    s.CloseEntry();
                }
                s.Finish();
            }
        }

        public static void CreateSnapshotArchive(string projectPath, string zipFilePath)
        {
            // Ensure destination directory exists
            string destDir = Path.GetDirectoryName(zipFilePath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            using (ZipOutputStream s = new ZipOutputStream(File.Create(zipFilePath)))
            {
                s.SetLevel(9); // Highest compression
                if (!string.IsNullOrEmpty(ConfigurationManager.ArchivePassword))
                {
                    s.Password = ConfigurationManager.ArchivePassword;
                }

                // Add all project files recursively
                string[] files = Directory.GetFiles(projectPath, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    // Prevent zipping the archive file itself if it is stored inside the project folder
                    if (Path.GetFullPath(file).Equals(Path.GetFullPath(zipFilePath), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Compute path relative to projectPath so it is at the root of the ZIP archive
                    string relativePath = file.Substring(projectPath.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace(Path.DirectorySeparatorChar, '/');

                    ZipEntry entry = new ZipEntry(relativePath)
                    {
                        DateTime = File.GetLastWriteTime(file)
                    };

                    s.PutNextEntry(entry);

                    using (FileStream fs = File.OpenRead(file))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            s.Write(buffer, 0, bytesRead);
                        }
                    }
                    s.CloseEntry();
                }
                s.Finish();
            }
        }

        public static void ExtractArchive(string zipFilePath, string destinationPath)
        {
            if (!File.Exists(zipFilePath))
            {
                throw new FileNotFoundException($"Archive not found at {zipFilePath}");
            }

            using (ZipInputStream s = new ZipInputStream(File.OpenRead(zipFilePath)))
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.ArchivePassword))
                {
                    s.Password = ConfigurationManager.ArchivePassword;
                }

                ZipEntry theEntry;
                while ((theEntry = s.GetNextEntry()) != null)
                {
                    string entryName = theEntry.Name;
                    if (string.IsNullOrEmpty(entryName))
                    {
                        continue;
                    }

                    // Clean and build local target path
                    string cleanedEntryName = entryName.Replace('/', Path.DirectorySeparatorChar);
                    string targetFilePath = Path.Combine(destinationPath, cleanedEntryName);

                    // Zip Slip Prevention: ensure path resolves inside the destination folder
                    string absoluteDestinationPath = Path.GetFullPath(destinationPath);
                    string absoluteTargetFilePath = Path.GetFullPath(targetFilePath);
                    if (!absoluteTargetFilePath.StartsWith(absoluteDestinationPath, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"Security Violation: Path traversal attempt detected in entry '{entryName}'");
                    }

                    // Create folder structures if they do not exist
                    string targetDirectory = Path.GetDirectoryName(targetFilePath);
                    if (!string.IsNullOrEmpty(targetDirectory) && !Directory.Exists(targetDirectory))
                    {
                        Directory.CreateDirectory(targetDirectory);
                    }

                    // Write file
                    if (!theEntry.IsDirectory && !string.IsNullOrEmpty(Path.GetFileName(targetFilePath)))
                    {
                        using (FileStream streamWriter = File.Create(targetFilePath))
                        {
                            byte[] buffer = new byte[4096];
                            int bytesRead;
                            while ((bytesRead = s.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                streamWriter.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                    s.CloseEntry();
                }
            }
        }
    }
}
