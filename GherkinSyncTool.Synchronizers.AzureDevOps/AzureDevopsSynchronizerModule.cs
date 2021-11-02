using Autofac;
using GherkinSyncTool.Synchronizers.AzureDevOps.Client;

namespace GherkinSyncTool.Synchronizers.AzureDevOps
{
    public class AzureDevopsSynchronizerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AzureDevopsClient>().SingleInstance();
            builder.RegisterType<Content.CaseContentBuilder>().SingleInstance();
        }
    }
}