using System;
using System.IO;

namespace GherkinSyncTool.Models.Configuration
{
    public class GherkinSyncToolConfig : IConfigs
    {
        public string BaseDirectory { get; set; }
        public string TagIdPrefix { get; set; } = "@tc:";
        public FormattingSettings FormattingSettings { get; set; }
        public void ValidateConfigs()
        {
            if (string.IsNullOrEmpty(BaseDirectory))
                throw new ArgumentException(
                    "Parameter BaseDirectory must not be empty! Please check your settings");
            var info = new DirectoryInfo(BaseDirectory);
            if (!info.Exists) throw new DirectoryNotFoundException($"Directory {BaseDirectory} not found, please, check the path");
        }
    }
    public class FormattingSettings
    {
        private int _tagIndentation = 2;

        public string TagIndentation
        {
            get => new(' ', _tagIndentation);
            set => _tagIndentation = Convert.ToInt32(value);
        }
    }
}