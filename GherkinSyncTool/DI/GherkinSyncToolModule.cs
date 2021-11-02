using Autofac;
using GherkinSyncTool.FeatureParser;
using GherkinSyncTool.Models;
using GherkinSyncTool.Synchronizers.AzureDevOps;
using GherkinSyncTool.Synchronizers.TestRail;

namespace GherkinSyncTool.DI
{
    public class GherkinSyncToolModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<FeatureFilesGrabber>().As<IFeatureFilesGrabber>().SingleInstance();
            builder.RegisterType<FeatureParser.FeatureParser>().SingleInstance();
            builder.RegisterType<Context>().SingleInstance();
            builder.RegisterModule<TestRailSynchronizerModule>();
            builder.RegisterModule<AzureDevopsSynchronizerModule>();
        }
    }
}