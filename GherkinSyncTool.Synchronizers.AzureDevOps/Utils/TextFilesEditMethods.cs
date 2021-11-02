using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Utils
{
    internal static class TextFilesEditMethods
    {
        internal static void InsertLineToTheFile(string path, Regex lineRegex, string text)
        {
            var featureFileLines = File.ReadAllLines(path).ToList();
            var lineNumber = featureFileLines.FindIndex(lineRegex.IsMatch);
            featureFileLines.Insert(lineNumber, text);
            File.WriteAllLines(path, featureFileLines);
        }
    }
}