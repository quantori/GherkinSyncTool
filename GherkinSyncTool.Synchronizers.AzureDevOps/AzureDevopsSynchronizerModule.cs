using Autofac;
using GherkinSyncTool.Synchronizers.AzureDevOps.Client;
using Microsoft.TeamFoundation.TestManagement.WebApi;

namespace GherkinSyncTool.Synchronizers.AzureDevOps
{
    public class AzureDevopsSynchronizerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AzureDevopsClient>().SingleInstance();
            builder.RegisterType<Content.CaseContentBuilder>().SingleInstance();
            builder.RegisterType<TestBaseHelper>().As<ITestBaseHelper>();
        }
    }
}