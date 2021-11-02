using System;
using System.Collections.Generic;
using System.Reflection;
using Gherkin;
using GherkinSyncTool.Models;
using NLog;

namespace GherkinSyncTool.FeatureParser
{
    public class FeatureParser
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        private readonly Context _context;

        public FeatureParser(Context context)
        {
            _context = context;
        }

        public List<IFeatureFile> Parse(IEnumerable<string> gherkinFilePaths)
        {
            List<IFeatureFile> featureFiles = new List<IFeatureFile>();
            var parser = new Parser();
            foreach (var gherkinFilePath in gherkinFilePaths)
            {
                try
                {
                    var file = new FeatureFile(parser.Parse(gherkinFilePath), gherkinFilePath);
                    featureFiles.Add(file);
                }
                catch (CompositeParserException e)
                {
                    Log.Error(e, $"The file has not been parsed: {gherkinFilePath}");
                    _context.IsRunSuccessful = false;
                }
                catch (Exception e)
                {
                    Log.Fatal(e, $"The file has not been parsed: {gherkinFilePath}");
                    throw;
                }
            }

            return featureFiles;
        }
    }
}