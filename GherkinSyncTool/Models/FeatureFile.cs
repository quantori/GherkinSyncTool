using System.IO;
using Gherkin.Ast;
using GherkinSyncTool.Configuration;
using GherkinSyncTool.Interfaces;

namespace GherkinSyncTool.Models
{
    public class FeatureFile : IFeatureFile
    {
        public GherkinDocument Document { get; init; }
        public string AbsolutePath { get; }
        public string RelativePath { get; }
        
        public FeatureFile(GherkinDocument document, string path)
        {
            if (!new FileInfo(path).Exists)
                throw new DirectoryNotFoundException($"File {path} not found");
            Document = document;
            AbsolutePath = Path.GetFullPath(path);
            var baseDirectory = new DirectoryInfo(ConfigurationManager.GetConfiguration().BaseDirectory);
            var relativeToDirectory = baseDirectory?.Parent?.FullName ?? 
                                      throw new DirectoryNotFoundException($"Base directory {baseDirectory} does not have a parent");
            RelativePath = Path.GetRelativePath(relativeToDirectory, AbsolutePath);
        }
    }
}