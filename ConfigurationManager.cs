using System;
using System.Collections.Generic;
using System.Linq;

namespace VersionManager
{
    public static class ConfigurationManager
    {
        public static string[] ProjectsRoots { get; private set; }
        public static string[] SourcesRoots { get; private set; }
        public static string ArchivePassword { get; private set; }
        public static string LogFilePath { get; private set; }

        static ConfigurationManager()
        {
            try
            {
                ProjectsRoots = ParsePaths(System.Configuration.ConfigurationManager.AppSettings["ProjectsRoot"]);
                SourcesRoots = ParsePaths(System.Configuration.ConfigurationManager.AppSettings["SourcesRoot"]);
                ArchivePassword = System.Configuration.ConfigurationManager.AppSettings["ArchivePassword"];
                LogFilePath = EnvironmentManager.Expand(System.Configuration.ConfigurationManager.AppSettings["LogFilePath"]);
            }
            catch (Exception)
            {
                ProjectsRoots = new string[0];
                SourcesRoots = new string[0];
                ArchivePassword = string.Empty;
                LogFilePath = string.Empty;
            }
        }

        private static string[] ParsePaths(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new string[0];

            var paths = new List<string>();
            foreach (string part in value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string expanded = EnvironmentManager.Expand(part.Trim());
                if (!string.IsNullOrEmpty(expanded))
                    paths.Add(expanded);
            }
            return paths.ToArray();
        }
    }
}
