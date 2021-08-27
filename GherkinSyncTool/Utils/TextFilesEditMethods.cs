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

        internal static void ReplaceLineInTheFile(string path, string oldLine, string newLine)
        {
            var featureFileLines = File.ReadAllLines(path).ToList();
            var index = featureFileLines.FindIndex(s=>s.Contains(oldLine));
            if(index >= 0)
            {
                featureFileLines[index] = featureFileLines[index].Replace(oldLine, newLine);
                File.WriteAllLines(path, featureFileLines);
            }
        }
    }
}