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
          
        private static int Main(string[] args)
        {
            Log.Info("GherkinSyncTool v.{0}{1}",
                Assembly.GetExecutingAssembly().GetName().Version,
                Environment.NewLine);
            
            try
            {
                var builder = new ContainerBuilder();
                builder.RegisterModule<GherkinSyncToolModule>();
                var container = builder.Build();
                //Parse files
                var parseFilesStopwatch = Stopwatch.StartNew();
                List<IFeatureFile> featureFiles = ParseFeatureFiles(container);
                if (featureFiles.Count == 0)
                {
                    Log.Info("No files were found for synchronization");
                    return 0;
                }
                Log.Info(@$"{featureFiles.Count} file(s) found in {parseFilesStopwatch.Elapsed:mm\:ss\.fff}");

                //Push to sync target system
                var synchronizer = container.Resolve<ISynchronizer>();
                synchronizer.Sync(featureFiles);
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
            var featureFiles = featureFilesGrabber.TakeFiles();

            return featureFiles;
        }
    }
}
