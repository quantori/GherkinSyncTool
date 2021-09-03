using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Autofac;
using GherkinSyncTool.DI;
using GherkinSyncTool.Interfaces;
using NLog;

namespace GherkinSyncTool
{
    class Program
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        public static string[] CommandLineArguments { get; private set; }

        private static int Main(string[] args)
        {
            Log.Info("GherkinSyncTool v.{0}{1}",
                Assembly.GetExecutingAssembly().GetName().Version,
                Environment.NewLine);
            
            try
            {
                CommandLineArguments = args;
                
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
