﻿using System;
using GherkinSyncTool.Models.Configuration;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Model
{
    public class AzureDevopsConfigs : IConfigs
    { 
        public AzureDevopsSettings AzureDevopsSettings { get; set; }
        
        public void ValidateConfigs()
        {
            if (AzureDevopsSettings is null) throw new ArgumentException("Please, init TestRail configuration");
            
            if (string.IsNullOrWhiteSpace(AzureDevopsSettings.BaseUrl) ||
                !Uri.IsWellFormedUriString(AzureDevopsSettings.BaseUrl, UriKind.Absolute))
            {
                throw new ArgumentException(
                    "Azure DevOps BaseUrl parameter is empty or not valid. Please, check configuration.");
            }
            
            if (string.IsNullOrWhiteSpace(AzureDevopsSettings.PersonalAccessToken))
            {
                throw new ArgumentException("Azure DevOps PersonalAccessToken parameter is empty. Please, check configuration.");
            }
        }

    }

    public class AzureDevopsSettings
    {
        public string BaseUrl { get; set; }
        public string PersonalAccessToken { get; set; }
        public string Project { get; set; }
    }
}