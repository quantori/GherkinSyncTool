using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GherkinSyncTool.Configuration;
using GherkinSyncTool.Interfaces;
using NLog;

namespace GherkinSyncTool.FeatureParser
{
    public class FeatureFilesGrabber : IFeatureFilesGrabber
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        private readonly FeatureParser _featureParser;

        public FeatureFilesGrabber(FeatureParser parser)
        {
            _featureParser = parser;
        }

        public List<IFeatureFile> TakeFiles()
        {
            var config = ConfigurationManager.GetConfiguration();
            var baseDirectory = config.BaseDirectory;
            var gherkinFilePaths = Directory.EnumerateFiles(baseDirectory, "*.feature",
                SearchOption.AllDirectories);
            Log.Info($"# Scanning for feature files in {baseDirectory}");
            return _featureParser.Parse(gherkinFilePaths);
        }
    }
}