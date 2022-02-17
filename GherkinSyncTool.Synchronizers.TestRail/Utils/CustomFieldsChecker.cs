using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Synchronizers.TestRail.Client;
using GherkinSyncTool.Synchronizers.TestRail.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GherkinSyncTool.Synchronizers.TestRail.Utils
{
    public class CustomFieldsChecker
    {
        private readonly TestRailClientWrapper _testRailClientWrapper;
        private readonly TestRailSettings _testRailSettings = ConfigurationManager.GetConfiguration<TestRailConfigs>().TestRailSettings;

        public CustomFieldsChecker(TestRailClientWrapper testRailClientWrapper)
        {
            _testRailClientWrapper = testRailClientWrapper;
        }

        public void CheckCustomFields()
        {
            var caseFields = _testRailClientWrapper.GetCaseFields().ToList();
            var actualCustomFieldNames = caseFields.Select(f => f.SystemName).ToList();
            var expectedCustomFields = GetExpectedCustomFields().ToList();
            foreach (var expectedCustomField in expectedCustomFields)
            {
                if (!actualCustomFieldNames.Contains(expectedCustomField))
                {
                    throw new ArgumentException(
                        $"\r\nOne of the required custom fields is missing: \"{expectedCustomField}\". Please check your TestRail case fields in customization menu\r\n");
                }
            }

            foreach (var field in caseFields.Where(f => expectedCustomFields.Contains(f.SystemName)))
            {
                var contexts = field.JsonFromResponse.ToObject<CustomFieldsModel>().Configs.Select(config => config.Context).ToList();

                if (contexts.Exists(context => context.IsGlobal))
                {
                    continue;
                }

                var projectIds = contexts.SelectMany(context => context.ProjectIds);

                if (!projectIds.Contains(_testRailSettings.ProjectId))
                {
                    throw new ArgumentException(
                        $"\r\nOne of the required fields: \"{field.SystemName}\" should be global or attached to the project with id: {_testRailSettings.ProjectId}\r\n");
                }
            }
        }

        private IEnumerable<string> GetExpectedCustomFields() => typeof(CaseCustomFields).GetProperties()
            .Select(p => p.GetCustomAttribute<JsonPropertyAttribute>())
            .Select(jp => jp.PropertyName);
    }
}