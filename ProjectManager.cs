using System;
using System.IO;
using System.Linq;

namespace VersionManager
{
    public static class ProjectManager
    {
        public static string GetProjectPath(string projectName)
        {
            foreach (string root in ConfigurationManager.ProjectsRoots)
            {
                string path = Path.Combine(root, projectName);
                if (Directory.Exists(path))
                    return path;
            }
            return string.Empty;
        }

        public static string[] GetExpectedProjectLocations(string projectName)
        {
            return ConfigurationManager.ProjectsRoots
                .Select(root => Path.Combine(root, projectName))
                .ToArray();
        }

        public static string GetStoragePath(string projectName)
        {
            foreach (string root in ConfigurationManager.SourcesRoots)
            {
                string path = Path.Combine(root, projectName);
                if (Directory.Exists(path))
                    return path;
            }

            if (ConfigurationManager.SourcesRoots.Length > 0)
                return Path.Combine(ConfigurationManager.SourcesRoots[0], projectName);

            return string.Empty;
        }

        public static bool ProjectExists(string projectName)
        {
            return !string.IsNullOrEmpty(GetProjectPath(projectName));
        }

        public static void EnsureStorageExists(string projectName)
        {
            string storagePath = GetStoragePath(projectName);
            if (!string.IsNullOrEmpty(storagePath) && !Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }
        }
    }
}
