using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core;
using CommandLine;
using GherkinSyncTool.DI;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Synchronizers.AzureDevOps;
using GherkinSyncTool.Synchronizers.AzureDevOps.Model;
using GherkinSyncTool.Synchronizers.TestRail;
using GherkinSyncTool.Synchronizers.TestRail.Model;
using NLog;

namespace GherkinSyncTool
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        static readonly ContainerBuilder ContainerBuilder = new();
        
        private static int Main(string[] args)
        {
            Log.Info("GherkinSyncTool v.{0}{1}",
                Assembly.GetExecutingAssembly().GetName().Version,
                Environment.NewLine);
            try
            {
                ConfigurationManager.InitConfiguration(args);
                Parser.Default.ParseArguments(args, LoadCliVerbsTypes())
                    .WithParsed(RegisterSynchronizer)
                    .WithNotParsed(_ => Environment.Exit(1));

                ContainerBuilder.RegisterModule<GherkinSyncToolModule>();
                var container = ContainerBuilder.Build();
                
                var stopwatch = Stopwatch.StartNew();
                //Parse files
                List<IFeatureFile> featureFiles = ParseFeatureFiles(container);
                if (featureFiles.Count == 0)
                {
                    Log.Info("No files were found for synchronization");
                    return 0;
                }
                
                //Start sync process with a target system
                if (!container.IsRegistered<ISynchronizer>())
                {
                    throw new DependencyResolutionException($"{nameof(ISynchronizer)} is not registered");
                }
                var synchronizer = container.Resolve<ISynchronizer>();
                synchronizer.Sync(featureFiles);
                Log.Info(@$"Synchronization finished in: {stopwatch.Elapsed:mm\:ss\.fff}");
                
                var context = container.Resolve<Context>();
                if (!context.IsRunSuccessful)
                {
                    Log.Fatal($"GherkinSyncTool did not complete successfully. Please check errors in the log.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                if (Log.IsFatalEnabled)
                {
                    Log.Fatal(ex, $"GherkinSyncTool did not complete successfully {Environment.NewLine}");
                }

                return 1;
            }
            return 0;
        }

        private static void RegisterSynchronizer(object obj)
        {
            var configurationSections = ConfigurationManager.Config.GetChildren();
            if (configurationSections is null) throw new ArgumentException("Please, init configuration");

            foreach (var section in configurationSections)
            {
                if(section.Key.Contains(nameof(TestRailSettings)))
                {
                    ContainerBuilder.RegisterType<TestRailSynchronizer>().As<ISynchronizer>().SingleInstance();
                    break;
                }
                if(section.Key.Contains(nameof(AzureDevopsSettings)))
                {
                    ContainerBuilder.RegisterType<AzureDevopsSynchronizer>().As<ISynchronizer>().SingleInstance();
                    break;
                }
            }
        }

        private static List<IFeatureFile> ParseFeatureFiles(IContainer container)
        {
            if (!container.IsRegistered<IFeatureFilesGrabber>())
            {
                throw new DependencyResolutionException($"{nameof(IFeatureFilesGrabber)} is not registered");
            }
            var featureFilesGrabber = container.Resolve<IFeatureFilesGrabber>();
            var parseFilesStopwatch = Stopwatch.StartNew();
            var featureFiles = featureFilesGrabber.TakeFiles();
            Log.Info(@$"{featureFiles.Count} file(s) parsed in {parseFilesStopwatch.Elapsed:mm\:ss\.fff}");
            return featureFiles;
        }
        
        private	static Type[] LoadCliVerbsTypes()
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.GetCustomAttribute<VerbAttribute>() != null).ToArray();		 
        }
    }
}