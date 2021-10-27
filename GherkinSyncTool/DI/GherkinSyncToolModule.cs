using Autofac;
using GherkinSyncTool.FeatureParser;
using GherkinSyncTool.Models;
using GherkinSyncTool.Synchronizers.AzureDevOps.Client;
using GherkinSyncTool.Synchronizers.TestRail.Client;
using GherkinSyncTool.Synchronizers.TestRail.Content;

namespace GherkinSyncTool.DI
{
    public class GherkinSyncToolModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<FeatureFilesGrabber>().As<IFeatureFilesGrabber>().SingleInstance();
            builder.RegisterType<FeatureParser.FeatureParser>().SingleInstance();
            builder.RegisterType<TestRailClientWrapper>().SingleInstance();
            builder.RegisterType<SectionSynchronizer>().SingleInstance();
            builder.RegisterType<CaseContentBuilder>().SingleInstance();
            //TODO: move to the AzureDevOps project
            builder.RegisterType<Synchronizers.AzureDevOps.Content.CaseContentBuilder>().SingleInstance();
            builder.RegisterType<Context>().SingleInstance();
            builder.RegisterType<AzureDevopsClient>().SingleInstance();
        }
    }
}