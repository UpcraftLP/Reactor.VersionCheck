using System;
using System.IO;

namespace Reactor.VersionCheck.util
{
    public static class DotEnv
    {
        public static void Load(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            foreach (var line in File.ReadAllLines(filePath))
            {
                var parts = line.Split(
                    '=', 2,
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                    continue;

                Environment.SetEnvironmentVariable(parts[0], parts[1]);
            }
        }

        public static string Get(string key, string defaultValue, EnvironmentVariableTarget? target = null)
        {
            return (target.HasValue
                ? Environment.GetEnvironmentVariable(key, target.Value)
                : Environment.GetEnvironmentVariable(key)) ?? defaultValue;
        }
    }
}