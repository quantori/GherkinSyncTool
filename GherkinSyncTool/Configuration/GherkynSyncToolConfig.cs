using System;
using System.IO;

namespace GherkinSyncTool.Configuration
{
    public class GherkynSyncToolConfig
    {
        public string BaseDirectory
        {
            get => _directory;
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException(
                        "Parameter BaseDirectory must not be empty! Please check your settings");
                var info = new DirectoryInfo(value);
                if (!info.Exists)
                    throw new DirectoryNotFoundException($"Directory {value} not found, please, check the path");
                _directory = value;
            }
        }

        public string TagIdPrefix { get; set; } = "@tc:";


        public FormattingSettings FormattingSettings { get; set; }
        public TestRailSettings TestRailSettings { get; set; }

        private string _directory;
    }

    public class TestRailSettings
    {
        private int? _pauseBetweenRetriesSeconds;
        private int? _retriesCount;

        public ulong ProjectId { get; set; }
        public ulong SuiteId { get; set; }
        public ulong TemplateId { get; set; }
        public string BaseUrl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public int? RetriesCount
        {
            get => _retriesCount;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Attempts count must be a positive number");
                _retriesCount = value ?? 3;
            }
        }

        public int? PauseBetweenRetriesSeconds
        {
            get => _pauseBetweenRetriesSeconds;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Pause between requests must be a positive number");
                _pauseBetweenRetriesSeconds = value ?? 5;
            }
        }
    }

    public class FormattingSettings
    {
        public int TagIndentation { get; set; } = 2;
    }
}