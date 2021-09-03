using Autofac;
using GherkinSyncTool.FeatureParser;
using GherkinSyncTool.Interfaces;
using GherkinSyncTool.Models;
using GherkinSyncTool.Synchronizers.TestRailSynchronizer;
using GherkinSyncTool.Synchronizers.TestRailSynchronizer.Client;
using GherkinSyncTool.Synchronizers.TestRailSynchronizer.Content;

namespace GherkinSyncTool.DI
{
    public class GherkinSyncToolModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<FeatureFilesGrabber>().As<IFeatureFilesGrabber>().SingleInstance();
            builder.RegisterType<TestRailSynchronizer>().As<ISynchronizer>().SingleInstance();
            builder.RegisterType<FeatureParser.FeatureParser>().SingleInstance();
            builder.RegisterType<TestRailClientWrapper>().SingleInstance();
            builder.RegisterType<SectionSynchronizer>().SingleInstance();
            builder.RegisterType<CaseContentBuilder>().SingleInstance();
            builder.RegisterType<Context>().SingleInstance();
        }
    }
}