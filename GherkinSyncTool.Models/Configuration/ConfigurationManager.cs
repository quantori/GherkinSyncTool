using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace GherkinSyncTool.Models.Configuration
{
    public static class ConfigurationManager
    {
        private static IConfiguration _configuration;
        public static T GetConfiguration<T>() where T : IConfigs
        {
            if (_configuration is null) throw new ArgumentException("Please, init configuration");
            
            var config = _configuration.Get<T>();
            config.ValidateConfigs();

            return config;
        }

        public static void InitConfiguration(string[] commandLineArguments)
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables()
                .AddCommandLine(commandLineArguments)
                .Build();
        }
    }
}