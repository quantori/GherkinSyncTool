using Autofac;
using GherkinSyncTool.Synchronizers.AllureTestOps.Client;
using GherkinSyncTool.Synchronizers.AllureTestOps.Content;

namespace GherkinSyncTool.Synchronizers.AllureTestOps
{
    public class AllureSynchronizerModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AllureClient>().SingleInstance();
            builder.RegisterType<CaseContentBuilder>().SingleInstance();
        }
    }
}