using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DepotDownloader;
using Karambolo.Extensions.Logging.File;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Reactor.VersionCheck.util;

namespace Reactor.VersionCheck
{
    public class Program
    {
        private const int AppID = 945360;
        private const int DepotID = 945361;

        private readonly ILogger<Program> logger;
        private readonly string workingDir;

        public ILogger<Program> GetLogger()
        {
            return logger;
        }

        private Program(string wd)
        {
            workingDir = wd;
            var logDir = Path.Combine(workingDir, "logs");
            Directory.CreateDirectory(logDir);
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                    .AddFile(options =>
                    {
                        options.RootPath = logDir;
                        //options.Files = new[] {new LogFileOptions {Path = "latest.log"}};
                        options.MaxFileSize = 10_000_000;
                        options.FileAccessMode = LogFileAccessMode.KeepOpenAndAutoFlush;
                    })
                    .AddConsole();
            });
            logger = loggerFactory.CreateLogger<Program>();
        }

        private static void Main(string[] args)
        {
            var workingDir = Directory.GetCurrentDirectory();
            DotEnv.Load(Path.Combine(workingDir, ".env")); // load .env file into system variables
            new Program(workingDir).Start();
        }

        private void Start()
        {
            File.Create(Path.Combine(Directory.GetCurrentDirectory(), "test.txt"));
            logger.LogInformation("Reactor Version Checker starting...");
            var downloadDir = Path.GetFullPath(DotEnv.Get("DOWNLOAD_DIRECTORY", Path.Combine(workingDir, ".download")));
            if (!Directory.Exists(downloadDir))
            {
                logger.LogDebug("creating download directory at {}", downloadDir);
            }
            var depotDir = Path.Combine(downloadDir, "depot");
            if (Directory.Exists(depotDir))
            {
                Directory.Delete(depotDir, true);
            }
            Directory.CreateDirectory(depotDir);

            var manifestDir = Path.Join(depotDir, "manifests");
            Directory.CreateDirectory(manifestDir);

            // setup done
            var configDir = DotEnv.Get("CONFIG_DIRECTORY", Path.Join(workingDir, "config"));
            var accountCfgPath = Path.GetFullPath(Path.Join(configDir, "steam_account_config"));

            AccountSettingsStore.LoadFromFile(accountCfgPath);
            ContentDownloader.Config.RememberPassword = true;
            string[] steam = CredentialHelper.GetSteamLogin();
            ContentDownloader.InitializeSteam3(steam[0], steam[1]);

            const string gameVersionFile = "Among Us_Data/globalgamemanagers";
            const string branch = ContentDownloader.DEFAULT_BRANCH; // public

            // step 1: download manifest
            ContentDownloader.Config.InstallDirectory = manifestDir;
            ContentDownloader.Config.DownloadManifestOnly = true;
            ContentDownloader.DownloadAppAsync(AppID, DepotID, ContentDownloader.INVALID_MANIFEST_ID, branch).Wait();
            ContentDownloader.Config.DownloadManifestOnly = false;

            // step 2: download file that contains game version
            ContentDownloader.Config.InstallDirectory = depotDir;
            var toDownload = new List<string> {gameVersionFile.Replace("/", "\\")};
            ContentDownloader.Config.UsingFileList = true;
            ContentDownloader.Config.FilesToDownload = toDownload;
            ContentDownloader.Config.FilesToDownloadRegex = new List<Regex>();
            ContentDownloader.DownloadAppAsync(AppID, DepotID, ContentDownloader.INVALID_MANIFEST_ID, branch).Wait();

            // close steam3 connection
            ContentDownloader.ShutdownSteam3();

            var readableVersion = GameVersionParser.Parse(Path.Join(depotDir, gameVersionFile));
            logger.LogInformation("Among Us Version {Version}", readableVersion);

            var manifestVersion = ManifestVersionParser.Parse(manifestDir, logger);
            logger.LogInformation("Manifest Version {ManifestVersion}", manifestVersion);
        }
    }
}