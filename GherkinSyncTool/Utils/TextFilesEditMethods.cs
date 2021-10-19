using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GherkinSyncTool.Utils
{
    internal static class TextFilesEditMethods
    {
        internal static void InsertLineToTheFile(string path, int lineNumber, string text)
        {
            var featureFileLines = File.ReadAllLines(path).ToList();
            featureFileLines.Insert(lineNumber, text);
            File.WriteAllLines(path, featureFileLines);
        }
        internal static void InsertLineToTheFile(string path, Regex lineRegex, string text)
        {
            var featureFileLines = File.ReadAllLines(path).ToList();
            var lineNumber = featureFileLines.FindIndex(lineRegex.IsMatch);
            featureFileLines.Insert(lineNumber, text);
            File.WriteAllLines(path, featureFileLines);
        }

        internal static void ReplaceLineInTheFile(string path, int lineNumber, string newLine)
        {
            var featureFileLines = File.ReadAllLines(path).ToList();
            featureFileLines[lineNumber] = newLine;
            File.WriteAllLines(path, featureFileLines);
        }
    }
}