using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Autofac;
using GherkinSyncTool.DI;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using NLog;

namespace GherkinSyncTool
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        
        private static int Main(string[] args)
        {
            Log.Info("GherkinSyncTool v.{0}{1}",
                Assembly.GetExecutingAssembly().GetName().Version,
                Environment.NewLine);
            
            try
            {
                ConfigurationManager.InitConfiguration(args);
        
                var builder = new ContainerBuilder();
                builder.RegisterModule<GherkinSyncToolModule>();
                var container = builder.Build();
                
                var stopwatch = Stopwatch.StartNew();
                //Parse files
                List<IFeatureFile> featureFiles = ParseFeatureFiles(container);
                if (featureFiles.Count == 0)
                {
                    Log.Info("No files were found for synchronization");
                    return 0;
                }
                
                //Push to sync target system
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

        private static List<IFeatureFile> ParseFeatureFiles(IContainer container)
        {
            var featureFilesGrabber = container.Resolve<IFeatureFilesGrabber>();
            var parseFilesStopwatch = Stopwatch.StartNew();
            var featureFiles = featureFilesGrabber.TakeFiles();
            Log.Info(@$"{featureFiles.Count} file(s) parsed in {parseFilesStopwatch.Elapsed:mm\:ss\.fff}");
            return featureFiles;
        }
    }
}
