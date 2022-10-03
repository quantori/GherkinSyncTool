using System;
using System.Collections.Generic;
using GherkinSyncTool.Models.Configuration;

namespace GherkinSyncTool.Synchronizers.AllureTestOps.Model
{
    public class AllureTestOpsConfigs : IConfigs
    {
        public AllureTestOpsSettings AllureTestOpsSettings { get; set; }

        public void ValidateConfigs()
        {
            if (AllureTestOpsSettings is null) throw new ArgumentException("Please, init configuration");

            if (string.IsNullOrWhiteSpace(AllureTestOpsSettings.BaseUrl) ||
                !Uri.IsWellFormedUriString(AllureTestOpsSettings.BaseUrl, UriKind.Absolute))
            {
                throw new ArgumentException(
                    "BaseUrl parameter is empty or not valid. Please, check configuration.");
            }

            if (string.IsNullOrWhiteSpace(AllureTestOpsSettings.AccessToken))
            {
                throw new ArgumentException("AccessToken parameter is empty. Please, check configuration.");
            }

            if (string.IsNullOrWhiteSpace(AllureTestOpsSettings.GherkinSyncToolId))
            {
                throw new ArgumentException("GherkinSyncToolId parameter is empty. Please, check configuration.");
            }

            if (AllureTestOpsSettings.ProjectId == -1)
            {
                throw new ArgumentException("ProjectId parameter is not specify. Please, check configuration.");
            }
        }
    }

    public class AllureTestOpsSettings
    {
        public string BaseUrl { get; set; }
        public string AccessToken { get; set; }
        public int ProjectId { get; set; } = -1;
        public string GherkinSyncToolId { get; set; }
        public string TestLayer { get; set; }
        public List<CustomField> CustomFields { get; set; }
        public bool BackgroundToPrecondition { get; set; }
    }

    public class CustomField
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}