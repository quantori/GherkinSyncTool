using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Utils
{
    public static class StringExtensions
    {
        public static string EncodeHtml(this string input)
        {
            input = HttpUtility.HtmlEncode(input);
            input = input.Replace("&#39;", "'");
            return input;
        }
        
        public static string FormatStringToCamelCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new ArgumentNullException(nameof(input));

            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            input = textInfo.ToTitleCase(input);
            
            input = Regex.Replace(input, @"\W+|\s", "");
            
            return input;
        }
        
        public static string RemoveWhitespaceCharacters(this string input)
        {
            input = Regex.Replace(input ?? string.Empty, @"\s", "");
            return input;
        }
    }
}