using System;

namespace GherkinSyncTool.Configuration
{
    public class GherkinSyncToolConfig
    {
        public string BaseDirectory { get; set; }
        public string TagIdPrefix { get; set; } = "@tc:";
        public FormattingSettings FormattingSettings { get; set; }
        public TestRailSettings TestRailSettings { get; set; }
    }

    public class TestRailSettings
    {
        public ulong ProjectId { get; set; }
        public ulong SuiteId { get; set; }
        public ulong TemplateId { get; set; }
        public string GherkinSyncToolId { get; set; }
        public string BaseUrl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int RetriesCount { get; set; } = 3;
        public int PauseBetweenRetriesSeconds { get; set; } = 5;
        public string ArchiveSectionName { get; set; } = "Archive";
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