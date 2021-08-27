using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using TestRail.Utils;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about a plan entry</summary>
    public class PlanEntry : BaseTestRailType
    {
        #region Public Properties
        /// <summary>Guid of the plan entry</summary>
        public string Id { get; set; }

        /// <summary>a list of test run ids associated with the current plan entry</summary>
        public List<ulong> RunIdList { get; private set; }

        /// <summary>a list of test runs associated with the current plan entry</summary>
        public List<Run> RunList { get; set; }

        /// <summary>the id of the test suite for the test run</summary>
        public ulong? SuiteId { get; set; }

        /// <summary>name of the test run</summary>
        public string Name { get; set; }

        /// <summary>the ID of the user the test run should be assigned to</summary>
        public ulong? AssignedToId { get; set; }

        /// <summary>true for including all test cases of the test suite, false for a custom case selection</summary>
        public bool? IncludeAll { get; private set; }

        /// <summary>an array of case IDs for the custom case selection</summary>
        public List<ulong> CaseIds { get; set; }

        /// <summary>an array of config IDs to allow for multiple test run configurations to be created</summary>
        public List<ulong> ConfigIDs { get; set; }
        #endregion Public Properties

        #region Public Methods
        /// <summary>Parse a json object to a PlanEntry</summary>
        /// <param name="json">json object to parse</param>
        /// <returns>PlanEntry corresponding to a json object</returns>
        public static PlanEntry Parse(JObject json)
        {
            var planEntry = new PlanEntry
            {
                JsonFromResponse = json,
                Id = (string)json["id"],
                SuiteId = (ulong?)json["suite_id"],
                Name = (string)json["name"],
                AssignedToId = (ulong?)json["assignedto_id"],
                IncludeAll = (bool?)json["include_all"],
                RunIdList = _ConvertToRunIDs(json["runs"] as JArray),
                CaseIds = _ConvertToCaseIDs(json["case_ids"] as JArray)
            };

            var jarray = json["runs"] as JArray;

            if (null != jarray)
            {
                planEntry.RunList = JsonUtility.ConvertJArrayToList(jarray, Run.Parse);
            }

            return planEntry;
        }

        /// <summary>Returns a json Object that represents this class</summary>
        /// <returns>Json object that corresponds to this class</returns>
        public JObject GetJson()
        {
            dynamic jsonParams = new JObject();

            if (null != SuiteId) { jsonParams.suite_id = SuiteId; }
            if (!string.IsNullOrWhiteSpace(Name)) { jsonParams.name = Name; }
            if (null != AssignedToId) { jsonParams.assignedto_id = AssignedToId.Value; }

            if (null != CaseIds && 0 < CaseIds.Count)
            {
                jsonParams.include_all = false;
                jsonParams.case_ids = JArray.FromObject(CaseIds);
            }

            else
            {
                jsonParams.include_all = true;
            }

            if (null != ConfigIDs && 0 < ConfigIDs.Count) { jsonParams.config_ids = JArray.FromObject(ConfigIDs); }
            if (null != RunList && 0 < RunList.Count) { jsonParams.runs = JArray.FromObject(RunList); }

            return jsonParams;
        }
        #endregion Public Methods

        #region Private Methods
        /// <summary>Convert the JArray to a list of Run IDs</summary>
        /// <param name="jarray">json to parse</param>
        /// <returns>a list of run IDs, list of size 0 if none exist</returns>
        private static List<ulong> _ConvertToRunIDs(JArray jarray)
        {
            var list = new List<ulong>();

            if (null != jarray)
            {
                // TODO - Convert to LINQ
                list.AddRange(from jt in jarray where null != (ulong?)jt["id"] select (ulong)jt["id"]);
            }

            return list;
        }

        /// <summary>Convert the Jarray to a list of case IDs</summary>
        /// <param name="jarray">json to parse</param>
        /// <returns>a list of case IDs, list of size 0 if none exist</returns>
        private static List<ulong> _ConvertToCaseIDs(JArray jarray)
        {
            var list = new List<ulong>();

            if (null != jarray)
            {
                list.AddRange(from JValue jsonItem in jarray select (ulong)jsonItem);
            }

            return list;
        }
        #endregion Private Methods
    }
}
