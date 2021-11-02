using Autofac;
using GherkinSyncTool.Synchronizers.TestRail.Client;
using GherkinSyncTool.Synchronizers.TestRail.Content;

namespace GherkinSyncTool.Synchronizers.TestRail
{
    public class TestRailSynchronizerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<TestRailClientWrapper>().SingleInstance();
            builder.RegisterType<CaseContentBuilder>().SingleInstance();
            builder.RegisterType<SectionSynchronizer>().SingleInstance();
        }
    }
}