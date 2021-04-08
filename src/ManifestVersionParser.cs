using System.IO;
using System.Text.RegularExpressions;
using DepotDownloader;
using Microsoft.Extensions.Logging;

namespace Reactor.VersionCheck.util
{
    public static class ManifestVersionParser
    {
        public static ulong Parse(string manifestDir, ILogger<Program> logger)
        {
            var files = Directory.EnumerateFiles(manifestDir, "*.txt");
            var pattern = @"manifest_(\d+)_(\d+)\.txt";
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var match = Regex.Match(fileName, pattern);
                if (match.Groups.Count > 2) return ulong.Parse(match.Groups[2].Value);
            }
            return ContentDownloader.INVALID_MANIFEST_ID;
        }
    }
}