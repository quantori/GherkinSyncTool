using Autofac;
using GherkinSyncTool.FeatureParser;
using GherkinSyncTool.Interfaces;
using GherkinSyncTool.Models;
using GherkinSyncTool.Synchronizers.AzureDevopsSynchronizer;
using GherkinSyncTool.Synchronizers.AzureDevopsSynchronizer.Client;
using GherkinSyncTool.Synchronizers.TestRailSynchronizer.Client;
using GherkinSyncTool.Synchronizers.TestRailSynchronizer.Content;

namespace GherkinSyncTool.DI
{
    public class GherkinSyncToolModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<FeatureFilesGrabber>().As<IFeatureFilesGrabber>().SingleInstance();
            //TODO:builder.RegisterType<TestRailSynchronizer>().As<ISynchronizer>().SingleInstance();
            builder.RegisterType<AzureDevopsSynchronizer>().As<ISynchronizer>().SingleInstance();
            builder.RegisterType<FeatureParser.FeatureParser>().SingleInstance();
            builder.RegisterType<TestRailClientWrapper>().SingleInstance();
            builder.RegisterType<SectionSynchronizer>().SingleInstance();
            builder.RegisterType<CaseContentBuilder>().SingleInstance();
            builder.RegisterType<GherkinSyncTool.Synchronizers.AzureDevopsSynchronizer.Content.CaseContentBuilder>().SingleInstance();
            builder.RegisterType<Context>().SingleInstance();
            builder.RegisterType<AzureDevopsClient>().SingleInstance();
        }
    }
}