using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace VersionManager
{
    public static class HistoryManager
    {
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        private const string LegacyDateFormat = "yyyy-MM-dd";

        public struct VersionEntry
        {
            public int Sequence;
            public DateTime Date;
            public string Comment;
            public ArchiveMode ArchiveMode;
            public string ArchivePath;
        }

        public static void AppendEntry(string storagePath, string projectName, string versionSeq, DateTime date, string comment, ArchiveMode archiveMode)
        {
            string historyFilePath = Path.Combine(storagePath, "History.txt");
            StringBuilder sb = new StringBuilder();

            if (!File.Exists(historyFilePath))
            {
                sb.AppendLine($"Project : {projectName}");
                sb.AppendLine("-----------------------------------");
            }

            sb.AppendLine($"Version {versionSeq}");
            sb.AppendLine("Date");
            sb.AppendLine(date.ToString(DateTimeFormat));
            sb.AppendLine("Comment");
            sb.AppendLine(comment);
            if (archiveMode != ArchiveMode.None)
            {
                sb.AppendLine("Archive");
                sb.AppendLine(archiveMode == ArchiveMode.Git ? "git" : "project");
            }
            sb.AppendLine("-----------------------------------");

            File.AppendAllText(historyFilePath, sb.ToString());
        }

        public static List<VersionEntry> ReadEntries(string storagePath)
        {
            var entries = new List<VersionEntry>();
            string historyFilePath = Path.Combine(storagePath, "History.txt");
            if (!File.Exists(historyFilePath))
                return entries;

            string[] lines = File.ReadAllLines(historyFilePath);
            int i = 0;
            while (i < lines.Length)
            {
                if (!lines[i].StartsWith("Version ", StringComparison.Ordinal))
                {
                    i++;
                    continue;
                }

                string seqPart = lines[i].Substring("Version ".Length).Trim();
                if (!int.TryParse(seqPart, out int sequence))
                {
                    i++;
                    continue;
                }

                DateTime date = DateTime.MinValue;
                string comment = string.Empty;
                ArchiveMode archiveMode = ArchiveMode.None;

                i++;
                if (i < lines.Length && lines[i].Equals("Date", StringComparison.Ordinal))
                {
                    i++;
                    if (i < lines.Length)
                        date = ParseDate(lines[i]);
                }

                i++;
                if (i < lines.Length && lines[i].Equals("Comment", StringComparison.Ordinal))
                {
                    i++;
                    if (i < lines.Length)
                        comment = lines[i];
                }

                i++;
                if (i < lines.Length && lines[i].Equals("Archive", StringComparison.Ordinal))
                {
                    i++;
                    if (i < lines.Length)
                        archiveMode = ParseArchiveMode(lines[i]);
                }

                string archivePath = FindArchivePath(storagePath, sequence, archiveMode);
                entries.Add(new VersionEntry
                {
                    Sequence = sequence,
                    Date = date,
                    Comment = comment,
                    ArchiveMode = archiveMode,
                    ArchivePath = archivePath
                });

                i++;
            }

            return entries.OrderBy(e => e.Sequence).ToList();
        }

        public static string GetNextVersionSequence(string storagePath)
        {
            int maxSeq = 0;

            foreach (var entry in ReadEntries(storagePath))
            {
                if (entry.Sequence > maxSeq)
                    maxSeq = entry.Sequence;
            }

            if (Directory.Exists(storagePath))
            {
                foreach (string file in Directory.GetFiles(storagePath, "Version*.zip"))
                {
                    int? seq = ParseVersionSequence(Path.GetFileNameWithoutExtension(file));
                    if (seq.HasValue && seq.Value > maxSeq)
                        maxSeq = seq.Value;
                }
            }

            return (maxSeq + 1).ToString("D3");
        }

        public static string GetGitArchiveFileName(string sequence)
        {
            return $"Version{sequence}.zip";
        }

        public static string GetProjectArchiveFileName(string sequence)
        {
            return $"VersionProject{sequence}.zip";
        }

        private static string FindArchivePath(string storagePath, int sequence, ArchiveMode archiveMode)
        {
            if (archiveMode == ArchiveMode.Project)
            {
                string projectArchive = Path.Combine(storagePath, GetProjectArchiveFileName(sequence.ToString("D3")));
                if (File.Exists(projectArchive))
                    return projectArchive;
            }

            string gitArchive = Path.Combine(storagePath, GetGitArchiveFileName(sequence.ToString("D3")));
            if (File.Exists(gitArchive))
                return gitArchive;

            return string.Empty;
        }

        private static int? ParseVersionSequence(string fileName)
        {
            if (fileName.StartsWith("VersionProject", StringComparison.Ordinal) && fileName.Length > 14)
            {
                if (int.TryParse(fileName.Substring(14), out int projectSeq))
                    return projectSeq;
            }

            if (fileName.StartsWith("Version", StringComparison.Ordinal) && fileName.Length > 7)
            {
                if (int.TryParse(fileName.Substring(7), out int gitSeq))
                    return gitSeq;
            }

            return null;
        }

        private static ArchiveMode ParseArchiveMode(string value)
        {
            if (value.Equals("git", StringComparison.OrdinalIgnoreCase))
                return ArchiveMode.Git;
            if (value.Equals("project", StringComparison.OrdinalIgnoreCase))
                return ArchiveMode.Project;
            return ArchiveMode.None;
        }

        private static DateTime ParseDate(string value)
        {
            if (DateTime.TryParseExact(value, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result;

            if (DateTime.TryParseExact(value, LegacyDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                return result;

            return DateTime.MinValue;
        }
    }
}
