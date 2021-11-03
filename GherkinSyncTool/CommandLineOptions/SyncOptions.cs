using CommandLine;

namespace GherkinSyncTool.CommandLineOptions
{
    [Verb("sync", true, HelpText = "Start synchronization with Test Management System.")]
    class SyncOptions
    {
        [Option("testrail", HelpText = "Synchronize with TestRail")]
        public bool Testrail { get; set; }

        [Option("azuredevops", HelpText = "Synchronize with Azure DevOps")]
        public bool AzureDevOps { get; set; }
    }
}