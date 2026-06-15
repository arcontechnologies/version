using System;
using System.Text.RegularExpressions;

namespace VersionManager
{
    public static class EnvironmentManager
    {
        private static readonly Regex EnvVarRegex = new Regex(@"%([^%]+)%", RegexOptions.Compiled);

        public static string Expand(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // 1. Try standard process-level expansion
            string expanded = Environment.ExpandEnvironmentVariables(value);

            // 2. If '%' symbols remain, check User and Machine registry-level variables
            if (expanded.Contains("%"))
            {
                expanded = EnvVarRegex.Replace(expanded, match =>
                {
                    string varName = match.Groups[1].Value;

                    // Try User level
                    string val = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.User);

                    // Try Machine level
                    if (string.IsNullOrEmpty(val))
                    {
                        val = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Machine);
                    }

                    // Return expanded value if found, otherwise keep original
                    return val ?? match.Value;
                });
            }

            return expanded;
        }
    }
}
