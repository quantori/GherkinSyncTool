using Autofac;
using GherkinSyncTool.FeatureParser;
using GherkinSyncTool.Models;
using GherkinSyncTool.Synchronizers.AzureDevOps;
using GherkinSyncTool.Synchronizers.AzureDevOps.Client;
using GherkinSyncTool.Synchronizers.TestRail;
using GherkinSyncTool.Synchronizers.TestRail.Client;
using GherkinSyncTool.Synchronizers.TestRail.Content;
using GherkinSyncTool.Synchronizers.TestRail.Utils;

namespace GherkinSyncTool.DI
{
    public class GherkinSyncToolModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<FeatureFilesGrabber>().As<IFeatureFilesGrabber>().SingleInstance();
            builder.RegisterType<TestRailSynchronizer>().As<ISynchronizer>().SingleInstance();
            //builder.RegisterType<AzureDevopsSynchronizer>().As<ISynchronizer>().SingleInstance();
            builder.RegisterType<FeatureParser.FeatureParser>().SingleInstance();
            builder.RegisterType<TestRailClientWrapper>().SingleInstance();
            builder.RegisterType<SectionSynchronizer>().SingleInstance();
            builder.RegisterType<CaseContentBuilder>().SingleInstance();
            builder.RegisterType<CustomFieldsChecker>().SingleInstance();
            //TODO:
            builder.RegisterType<GherkinSyncTool.Synchronizers.AzureDevOps.Content.CaseContentBuilder>().SingleInstance();
            builder.RegisterType<Context>().SingleInstance();
            builder.RegisterType<AzureDevopsClient>().SingleInstance();
        }
    }
}