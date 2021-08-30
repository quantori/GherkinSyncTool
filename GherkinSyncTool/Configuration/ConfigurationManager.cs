using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace GherkinSyncTool.Configuration
{
    public static class ConfigurationManager
    {
        private static GherkynSyncToolConfig _configurationModel;

        public static GherkynSyncToolConfig GetConfiguration()
        {
            if (_configurationModel == null)
            {
                var config = GetIConfiguration().Get<GherkynSyncToolConfig>();
                ValidateConfig(config);

                _configurationModel = config;
            }

            return _configurationModel;
        }

        private static void ValidateConfig(GherkynSyncToolConfig config)
        {
            ValidateBaseDirectory(config);
            ValidateTestRailSettings(config);
        }

        private static void ValidateTestRailSettings(GherkynSyncToolConfig config)
        {
            if (config.TestRailSettings.RetriesCount < 0)
                throw new ArgumentException("Retries count must be a positive number. Please, check configuration.");

            if (config.TestRailSettings.PauseBetweenRetriesSeconds < 0)
                throw new ArgumentException("Pause between requests must be a positive number. Please, check configuration.");

            if (config.TestRailSettings.ProjectId <= 0)
            {
                throw new ArgumentException(
                    "TestRail project ID parameter is not valid. It must be a positive number. Please, check configuration.");
            }

            if (config.TestRailSettings.SuiteId <= 0)
            {
                throw new ArgumentException(
                    "TestRail suite ID parameter is not valid. It must be a positive number. Please, check configuration.");
            }

            if (config.TestRailSettings.TemplateId <= 0)
            {
                throw new ArgumentException(
                    "TestRail template ID parameter is not valid. It must be a positive number. Please, check configuration.");
            }

            if (string.IsNullOrWhiteSpace(config.TestRailSettings.BaseUrl) ||
                !Uri.IsWellFormedUriString(config.TestRailSettings.BaseUrl, UriKind.Absolute))
            {
                throw new ArgumentException("TestRail BaseUrl parameter is empty or not valid. Please, check configuration.");
            }

            if (string.IsNullOrWhiteSpace(config.TestRailSettings.UserName))
            {
                throw new ArgumentException("TestRail username parameter is empty. Please, check configuration.");
            }

            if (string.IsNullOrWhiteSpace(config.TestRailSettings.Password))
            {
                throw new ArgumentException("TestRail password parameter is empty. Please, check configuration.");
            }

            if (string.IsNullOrWhiteSpace(config.TestRailSettings.GherkinSyncToolId))
            {
                throw new ArgumentException("TestRail GherkinSyncToolId parameter is empty. Please, check configuration.");
            }
        }

        private static void ValidateBaseDirectory(GherkynSyncToolConfig config)
        {
            var baseDirectory = config.BaseDirectory;
            if (string.IsNullOrEmpty(baseDirectory))
                throw new ArgumentException(
                    "Parameter BaseDirectory must not be empty! Please check your settings");
            var info = new DirectoryInfo(baseDirectory);
            if (!info.Exists)
                throw new DirectoryNotFoundException($"Directory {baseDirectory} not found, please, check the path");
        }

        private static IConfiguration GetIConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .AddCommandLine(Program.CommandLineArguments)
                .Build();
        }
    }
}