using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Synchronizers.TestRail.Client;
using GherkinSyncTool.Synchronizers.TestRail.Model;
using Newtonsoft.Json;

namespace GherkinSyncTool.Synchronizers.TestRail.Utils
{
    public class CustomFieldsChecker
    {
        private readonly TestRailClientWrapper _testRailClientWrapper;

        public CustomFieldsChecker(TestRailClientWrapper testRailClientWrapper)
        {
            _testRailClientWrapper = testRailClientWrapper;
        }

        public void CheckCustomFields()
        {
            var actualCustomFields = _testRailClientWrapper.GetCaseFields().Select(f => f.SystemName);
            var expectedCustomFields = GetExpectedCustomFields();
            foreach (var customField in expectedCustomFields.Where(customField => !actualCustomFields.Contains(customField)))
            {
                throw new ArgumentException(
                    $"\r\nOne of the required custom fields is missing: \"{customField}\". Please check your TestRail case fields in customization menu\r\n");
            }
        }

        private IEnumerable<string> GetExpectedCustomFields() => typeof(CaseCustomFields).GetProperties()
            .Select(p => p.GetCustomAttribute<JsonPropertyAttribute>())
            .Select(jp => jp.PropertyName).ToList();
    }
}