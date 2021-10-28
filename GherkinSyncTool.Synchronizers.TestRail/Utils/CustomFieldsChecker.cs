using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Synchronizers.TestRail.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace GherkinSyncTool.Synchronizers.TestRail.Utils
{
    public class CustomFieldsChecker
    {
        private static readonly TestRailSettings _testRailSettings = ConfigurationManager.GetConfiguration<TestRailConfigs>().TestRailSettings;

        public void CheckCustomFields()
        {
            var actualCustomFields = GetActualCustomFields();
            var expectedCustomFields = GetExpectedCustomFields();
            foreach (var customField in expectedCustomFields.Where(customField => !actualCustomFields.Contains(customField)))
            {
                throw new ArgumentException(
                    $"\r\nOne of the required custom fields is missing: \"{customField}\". Please check your TestRail case fields in customization menu\r\n");
            }
        }

        private IEnumerable<string> GetActualCustomFields()
        {
            var client = new RestClient(_testRailSettings.BaseUrl);
            var request = new RestRequest("index.php?/api/v2/get_case_fields", Method.GET)
            {
                RequestFormat = DataFormat.Json
            };
            client.Authenticator = new HttpBasicAuthenticator(_testRailSettings.UserName, _testRailSettings.Password);

            
            var response = client.Execute(request).Content;
            var root = (JContainer)JToken.Parse(response);
            var listOfCustomFields = root.DescendantsAndSelf().OfType<JProperty>()
                .Where(p => p.Name == "system_name").Select(p => p.Value.Value<string>());

            return listOfCustomFields;
        }

        private IEnumerable<string> GetExpectedCustomFields() => typeof(CaseCustomFields).GetProperties()
            .Select(p => p.GetCustomAttribute<JsonPropertyAttribute>())
            .Select(jp => jp.PropertyName).ToList();
    }
}