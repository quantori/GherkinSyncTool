using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace GherkinSyncTool.Models.Configuration
{
    public static class ConfigurationManager
    {
        public static IConfiguration Config { get; private set; }

        public static T GetConfiguration<T>() where T : IConfigs
        {
            if (Config is null) throw new ArgumentException("Please, init configuration");
            
            var config = Config.Get<T>();
            config.ValidateConfigs();

            return config;
        }

        public static void InitConfiguration(string[] commandLineArguments)
        {
            Config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("TMS")}.json", true, true)
                .AddEnvironmentVariables()
                .AddCommandLine(commandLineArguments)
                .Build();
        }
    }
}