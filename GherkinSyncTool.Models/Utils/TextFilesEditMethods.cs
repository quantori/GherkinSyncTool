using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GherkinSyncTool.Models.Utils
{
    public static class TextFilesEditMethods
    {
        public static void InsertLineToTheFile(string path, int lineNumber, string text)
        {
            var featureFileLines = File.ReadAllLines(path).ToList();
            featureFileLines.Insert(lineNumber, text);
            File.WriteAllLines(path, featureFileLines);
        }

        public static void ReplaceLineInTheFile(string path, int lineNumber, string newLine)
        {
            var featureFileLines = File.ReadAllLines(path).ToList();
            featureFileLines[lineNumber] = newLine;
            File.WriteAllLines(path, featureFileLines);
        }

        public static void ReplaceTextInTheFileRegex(string path, string regexPattern, string newValue = "")
        {
            var featureFileLines = File.ReadAllLines(path).ToList();
            
            var linesToRemove = new List<int>();

            for (var i = 0; i < featureFileLines.Count; i++)
            {
                if (Regex.IsMatch(featureFileLines[i], regexPattern))
                {
                    featureFileLines[i] = Regex.Replace(featureFileLines[i], regexPattern, newValue);
                    if (string.IsNullOrWhiteSpace(featureFileLines[i]))
                    {
                        linesToRemove.Add(i);
                    }
                }
            }
            
            linesToRemove.ForEach(i => featureFileLines.RemoveAt(i));

            File.WriteAllLines(path, featureFileLines);
        }
    }
}