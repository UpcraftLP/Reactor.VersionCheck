using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DepotDownloader;
using Karambolo.Extensions.Logging.File;
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

        private Program(string wd)
        {
            workingDir = wd;
            var logDir = Path.Join(workingDir, "logs");
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

        public ILogger<Program> GetLogger()
        {
            return logger;
        }

        private static void Main(string[] args)
        {
            var workingDir = Directory.GetCurrentDirectory();
            DotEnv.Load(Path.Join(workingDir, ".env")); // load .env file into system variables
            new Program(workingDir).Start();
        }

        private void Start()
        {
            logger.LogInformation("Reactor Version Checker starting...");
            var downloadDir = Path.GetFullPath(DotEnv.Get("DOWNLOAD_DIRECTORY", Path.Join(workingDir, ".download")));
            if (!Directory.Exists(downloadDir))
                logger.LogDebug("creating download directory at {DownloadDirectory}", downloadDir);
            var depotDir = Path.Join(downloadDir, "depot");
            if (Directory.Exists(depotDir)) Directory.Delete(depotDir, true);

            var manifestDir = Path.Join(depotDir, "manifests");
            Directory.CreateDirectory(manifestDir);

            // setup done
            AccountSettingsStore.LoadFromFile("steam_account.config");
            ContentDownloader.Config.RememberPassword = true;
            string[] steam = CredentialHelper.GetSteamCredentials();
            ContentDownloader.InitializeSteam3(steam[0], steam[1]);

            const string gameVersionFile = "Among Us_Data/globalgamemanagers";
            const string branch = ContentDownloader.DEFAULT_BRANCH; // public

            // step 1: download manifest
            ContentDownloader.Config.InstallDirectory = manifestDir;
            ContentDownloader.Config.DownloadManifestOnly = true;
            ContentDownloader.DownloadAppAsync(AppID, DepotID).Wait();
            ContentDownloader.Config.DownloadManifestOnly = false;

            // parse manifest
            var manifestVersion = ManifestVersionParser.Parse(manifestDir, logger);

            // step 2: download file that contains game version
            ContentDownloader.Config.InstallDirectory = depotDir;
            var toDownload = new List<string> {gameVersionFile, gameVersionFile.Replace("/", "\\")};
            ContentDownloader.Config.UsingFileList = true;
            ContentDownloader.Config.FilesToDownload = toDownload;
            ContentDownloader.Config.FilesToDownloadRegex = new List<Regex>();
            ContentDownloader.DownloadAppAsync(AppID, DepotID, manifestVersion).Wait();

            // close steam3 connection
            ContentDownloader.ShutdownSteam3();

            var readableVersion = GameVersionParser.Parse(Path.Join(depotDir, gameVersionFile));

            logger.LogInformation("Manifest Version {ManifestVersion}", manifestVersion);
            logger.LogInformation("Among Us Version {Version}", readableVersion);
        }
    }
}