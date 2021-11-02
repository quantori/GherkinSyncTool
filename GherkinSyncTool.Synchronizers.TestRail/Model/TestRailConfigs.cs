using System;
using GherkinSyncTool.Models.Configuration;

namespace GherkinSyncTool.Synchronizers.TestRail.Model
{
    public class TestRailConfigs : IConfigs
    {
        public TestRailSettings TestRailSettings { get; set; }

        public void ValidateConfigs()
        {
            if (TestRailSettings is null) throw new ArgumentException("Please, init TestRail configuration");
            
            if (TestRailSettings.RetriesCount < 0)
            {
                throw new ArgumentException("Retries count must be a positive number. Please, check configuration.");
            }

            if (TestRailSettings.PauseBetweenRetriesSeconds < 0)
            {
                throw new ArgumentException(
                    "Pause between requests must be a positive number. Please, check configuration.");
            }

            if (TestRailSettings.ProjectId <= 0)
            {
                throw new ArgumentException(
                    "TestRail project ID parameter is not valid. It must be a positive number. Please, check configuration.");
            }

            if (TestRailSettings.SuiteId <= 0)
            {
                throw new ArgumentException(
                    "TestRail suite ID parameter is not valid. It must be a positive number. Please, check configuration.");
            }

            if (TestRailSettings.TemplateId <= 0)
            {
                throw new ArgumentException(
                    "TestRail template ID parameter is not valid. It must be a positive number. Please, check configuration.");
            }

            if (string.IsNullOrWhiteSpace(TestRailSettings.BaseUrl) ||
                !Uri.IsWellFormedUriString(TestRailSettings.BaseUrl, UriKind.Absolute))
            {
                throw new ArgumentException(
                    "TestRail BaseUrl parameter is empty or not valid. Please, check configuration.");
            }

            if (string.IsNullOrWhiteSpace(TestRailSettings.UserName))
            {
                throw new ArgumentException("TestRail username parameter is empty. Please, check configuration.");
            }

            if (string.IsNullOrWhiteSpace(TestRailSettings.Password))
            {
                throw new ArgumentException("TestRail password parameter is empty. Please, check configuration.");
            }

            if (string.IsNullOrWhiteSpace(TestRailSettings.GherkinSyncToolId))
            {
                throw new ArgumentException(
                    "TestRail GherkinSyncToolId parameter is empty. Please, check configuration.");
            }
        }

    }
    public class TestRailSettings 
    {
        public ulong ProjectId { get; set; }
        public ulong SuiteId { get; set; }
        public ulong TemplateId { get; set; }
        public string GherkinSyncToolId { get; set; }
        public string BaseUrl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int RetriesCount { get; set; } = 3;
        public int PauseBetweenRetriesSeconds { get; set; } = 5;
        public string ArchiveSectionName { get; set; } = "Archive";
    }
}