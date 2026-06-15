using System;

namespace VersionManager
{
    public static class ConsoleUI
    {
        public static void PrintHeader()
        {
            Console.WriteLine("====================================");
            Console.WriteLine("Version Manager");
            Console.WriteLine("====================================");
        }

        public static void PrintProjectInfo(string projectName)
        {
            Console.WriteLine("Project");
            Console.WriteLine(projectName);
            Console.WriteLine();
        }

        public static void PrintStep(string message)
        {
            Console.WriteLine(message);
        }

        public static void PrintStepResult(string resultText)
        {
            Console.WriteLine($"✓ {resultText}");
        }

        public static void PrintSuccess(string archiveName)
        {
            Console.WriteLine("Archive");
            Console.WriteLine(archiveName);
            Console.WriteLine();
            Console.WriteLine("Completed successfully.");
        }

        public static void PrintErrorProjectMissing(string projectName, string[] expectedLocations)
        {
            Console.WriteLine($"Project \"{projectName}\" was not found.");
            Console.WriteLine();
            Console.WriteLine("Searched locations:");
            foreach (string location in expectedLocations)
            {
                Console.WriteLine($"  - {location}");
            }
        }

        public static void PrintErrorGitMissing()
        {
            Console.WriteLine("Git is not installed or not available in PATH.");
        }

        public static void PrintErrorNoChanges()
        {
            Console.WriteLine("No changes detected.");
            Console.WriteLine();
            Console.WriteLine("No new version created.");
        }

        public static void PrintSuccessCommit(string versionSeq)
        {
            Console.WriteLine("Version");
            Console.WriteLine(versionSeq);
            Console.WriteLine();
            Console.WriteLine("Committed successfully.");
        }

        public static void PrintErrorArchiveFailed()
        {
            Console.WriteLine("Unable to create archive.");
            Console.WriteLine();
            Console.WriteLine("The Git repository remains valid.");
        }

        public static string PromptComment()
        {
            Console.Write("Comment: ");
            return Console.ReadLine() ?? string.Empty;
        }

        public static string PromptChooseVersion()
        {
            Console.Write("Choose version : ");
            return Console.ReadLine() ?? string.Empty;
        }
    }
}
