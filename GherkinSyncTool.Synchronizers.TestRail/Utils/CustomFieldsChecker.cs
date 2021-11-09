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
        private static readonly TestRailSettings _testRailSettings = ConfigurationManager.GetConfiguration<TestRailConfigs>().TestRailSettings;

        public CustomFieldsChecker(TestRailClientWrapper testRailClientWrapper)
        {
            _testRailClientWrapper = testRailClientWrapper;
        }

        public void CheckCustomFields()
        {
            var caseFields = _testRailClientWrapper.GetCaseFields();
            var actualCustomFieldNames = caseFields.Select(f => f.SystemName);
            var expectedCustomFields = GetExpectedCustomFields();
            foreach (var customField in actualCustomFieldNames)
            {
                if (!expectedCustomFields.Contains(customField))
                {
                    throw new ArgumentException(
                        $"\r\nOne of the required custom fields is missing: \"{customField}\". Please check your TestRail case fields in customization menu\r\n");
                }
            }

            foreach (var field in caseFields.Where(f => expectedCustomFields.Contains(f.SystemName)))
            {
                var context = field.JsonFromResponse.ToObject<CustomFieldsModel>().Configs.First().Context;

                if (!context.IsGlobal && !context.ProjectIds.Contains(_testRailSettings.ProjectId))
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