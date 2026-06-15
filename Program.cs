using System;

namespace VersionManager
{
    class Program
    {
        static void Main(string[] args)
        {
            if (ConfigurationManager.ProjectsRoots.Length == 0 ||
                ConfigurationManager.SourcesRoots.Length == 0)
            {
                Console.WriteLine("Administrator Configuration Error: ProjectsRoot or SourcesRoot is not configured in App.config.");
                return;
            }

            if (TryParseVersionCommand(args, out string projectName, out ArchiveMode archiveMode))
            {
                VersionManager.CreateVersion(projectName, archiveMode);
                return;
            }

            if (args.Length == 1 && IsHelp(args[0]))
            {
                PrintUsage();
                return;
            }

            if (args.Length == 2)
            {
                string command = args[0].ToLowerInvariant();
                string name = args[1];

                switch (command)
                {
                    case "list":
                        RestoreManager.ListVersions(name);
                        break;
                    case "restore":
                        RestoreManager.RestoreVersion(name);
                        break;
                    case "snapshot":
                        VersionManager.CreateSnapshot(name);
                        break;
                    default:
                        PrintUsage();
                        break;
                }
                return;
            }

            PrintUsage();
        }

        private static bool TryParseVersionCommand(string[] args, out string projectName, out ArchiveMode archiveMode)
        {
            projectName = null;
            archiveMode = ArchiveMode.None;

            if (args.Length == 0 || args.Length > 3)
                return false;

            if (args.Length == 1)
            {
                if (IsHelp(args[0]))
                    return false;

                projectName = args[0];
                return true;
            }

            if (args.Length == 2 && IsKnownCommand(args[0]))
                return false;

            string project = null;
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (TryParseArchiveTypeFlag(arg, out ArchiveMode mode))
                {
                    archiveMode = mode;
                    continue;
                }

                if (arg.Equals("-archive", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && TryParseArchiveTypeValue(args[i + 1], out ArchiveMode typeMode))
                    {
                        archiveMode = typeMode;
                        i++;
                    }
                    continue;
                }

                if (!IsHelp(arg))
                    project = arg;
            }

            if (string.IsNullOrEmpty(project))
                return false;

            projectName = project;
            return true;
        }

        private static bool TryParseArchiveTypeFlag(string arg, out ArchiveMode mode)
        {
            if (arg.Equals("-archive-git", StringComparison.OrdinalIgnoreCase))
            {
                mode = ArchiveMode.Git;
                return true;
            }

            if (arg.Equals("-archive-project", StringComparison.OrdinalIgnoreCase))
            {
                mode = ArchiveMode.Project;
                return true;
            }

            mode = ArchiveMode.None;
            return false;
        }

        private static bool TryParseArchiveTypeValue(string arg, out ArchiveMode mode)
        {
            if (arg.Equals("git", StringComparison.OrdinalIgnoreCase))
            {
                mode = ArchiveMode.Git;
                return true;
            }

            if (arg.Equals("project", StringComparison.OrdinalIgnoreCase))
            {
                mode = ArchiveMode.Project;
                return true;
            }

            mode = ArchiveMode.None;
            return false;
        }

        private static bool IsKnownCommand(string arg)
        {
            switch (arg.ToLowerInvariant())
            {
                case "list":
                case "restore":
                case "snapshot":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsHelp(string arg)
        {
            return arg.Equals("/?", StringComparison.OrdinalIgnoreCase) ||
                   arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                   arg.Equals("--help", StringComparison.OrdinalIgnoreCase);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("======================================================================");
            Console.WriteLine("Version Manager - Help & Documentation");
            Console.WriteLine("======================================================================");
            Console.WriteLine("Version Manager is a thin abstraction layer over Git that provides");
            Console.WriteLine("a simple versioning system for non-technical users.");
            Console.WriteLine();
            Console.WriteLine("All Git commands are executed internally and hidden from the user.");
            Console.WriteLine();
            Console.WriteLine("ADMINISTRATOR CONFIGURATION:");
            Console.WriteLine("  Configuration is loaded from App.config. It contains:");
            Console.WriteLine("    - ProjectsRoot    : Semicolon-separated root directories containing user projects.");
            Console.WriteLine("    - SourcesRoot     : Semicolon-separated root directories where version archives are stored.");
            Console.WriteLine("    - ArchivePassword : Password used to encrypt/decrypt ZIP archives.");
            Console.WriteLine();
            Console.WriteLine("COMMANDS:");
            Console.WriteLine();
            Console.WriteLine("  1. Create a New Version");
            Console.WriteLine("     Usage: Version.exe [ProjectName]");
            Console.WriteLine("            Version.exe -archive-git [ProjectName]");
            Console.WriteLine("            Version.exe -archive-project [ProjectName]");
            Console.WriteLine("            Version.exe [ProjectName] -archive git");
            Console.WriteLine("            Version.exe [ProjectName] -archive project");
            Console.WriteLine("     Description:");
            Console.WriteLine("       - Locates the project folder by scanning each configured ProjectsRoot.");
            Console.WriteLine("       - Initializes Git internally if not already present.");
            Console.WriteLine("       - Detects if there are any modified, deleted, or untracked files.");
            Console.WriteLine("         If no changes are detected, the process exits without saving.");
            Console.WriteLine("       - Prompts the user to enter a version Comment.");
            Console.WriteLine("       - Internally stages and commits the changes.");
            Console.WriteLine("       - By default, only the Git repository is updated (no ZIP archive).");
            Console.WriteLine("       - With -archive-git (or -archive git), also compresses only the");
            Console.WriteLine("         .git directory into Version00x.zip.");
            Console.WriteLine("       - With -archive-project (or -archive project), also compresses the");
            Console.WriteLine("         entire project folder into VersionProject00x.zip.");
            Console.WriteLine("       - Appends version history data (Version index, Date/Time, Comment)");
            Console.WriteLine("         to 'History.txt' in the project's archive storage directory.");
            Console.WriteLine();
            Console.WriteLine("  2. List Available Versions");
            Console.WriteLine("     Usage: Version.exe list [ProjectName]");
            Console.WriteLine("     Description:");
            Console.WriteLine("       - Reads version history from History.txt across configured SourcesRoot paths.");
            Console.WriteLine("       - Displays all saved versions with their sequential index,");
            Console.WriteLine("         date/time of creation, archive type, and commit comment.");
            Console.WriteLine();
            Console.WriteLine("  3. Restore a Version");
            Console.WriteLine("     Usage: Version.exe restore [ProjectName]");
            Console.WriteLine("     Description:");
            Console.WriteLine("       - Displays archived versions available for restoration.");
            Console.WriteLine("       - Prompts the user to enter the number (index) of the version");
            Console.WriteLine("         they wish to restore.");
            Console.WriteLine("       - Deletes the current .git folder in the active project directory.");
            Console.WriteLine("       - Extracts the selected version's archived .git directory.");
            Console.WriteLine("       - Performs a hard reset and clean to discard all uncommitted");
            Console.WriteLine("         modifications and restore files to the exact state they were in.");
            Console.WriteLine("       - Only versions archived with -archive-git can be restored.");
            Console.WriteLine();
            Console.WriteLine("  4. Create a Snapshot (Full Backup)");
            Console.WriteLine("     Usage: Version.exe snapshot [ProjectName]");
            Console.WriteLine("     Description:");
            Console.WriteLine("       - Compresses the entire project directory from the located ProjectsRoot.");
            Console.WriteLine("         into a single password-protected ZIP archive named Snapshot00x.zip.");
            Console.WriteLine("       - Stored directly in the project's archive directory.");
            Console.WriteLine("       - Excludes nothing, serving as a raw filesystem backup.");
            Console.WriteLine();
            Console.WriteLine("GENERAL HELP:");
            Console.WriteLine("  Show this help screen:");
            Console.WriteLine("    Version.exe -h");
            Console.WriteLine("    Version.exe --help");
            Console.WriteLine("    Version.exe /?");
            Console.WriteLine("======================================================================");
        }
    }
}
