using System.IO;
using System.Linq;

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

        internal static void ReplaceLineInTheFile(string path, int lineNumber, string newLine)
        {
            var featureFileLines = File.ReadAllLines(path).ToList();
            featureFileLines[lineNumber] = newLine;
            File.WriteAllLines(path, featureFileLines);
        }
    }
}