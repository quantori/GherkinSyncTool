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
    }
}