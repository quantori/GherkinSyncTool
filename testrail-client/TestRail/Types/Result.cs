using Newtonsoft.Json.Linq;
using System;
using TestRail.Utils;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about the result of a test</summary>
    public class Result : BaseTestRailType
    {
        #region Public Properties
        /// <summary>ID of the result</summary>
        public ulong Id { get; set; }

        /// <summary>ID of the test</summary>
        public ulong TestId { get; set; }

        /// <summary>case ID of the test</summary>
        public ulong CaseId { get; set; }

        /// <summary>ID of the test status</summary>
        public ulong? StatusId { get; set; }

        /// <summary>created by</summary>
        public ulong? CreatedBy { get; set; }

        /// <summary>result created on</summary>
        public DateTime? CreatedOn { get; set; }

        /// <summary>the ID of the user the test should be assigned to</summary>
        public ulong? AssignedToId { get; set; }

        /// <summary>the comment /description for the test result</summary>
        public string Comment { get; set; }

        /// <summary>the version or build tested against</summary>
        public string Version { get; set; }

        /// <summary>the time it took to execute the test</summary>
        public TimeSpan? Elapsed { get; set; }

        /// <summary>a comma-separated list of defects to link to the test result</summary>
        public string Defects { get; set; }
        #endregion Public Properties

        #region Public Methods
        /// <summary>string representation of the object</summary>
        /// <returns>string representation of the object</returns>
        public override string ToString()
        {
            return $"{Id}:{Comment}";
        }

        /// <summary>Parse the JSON into a Result</summary>
        /// <param name="json">json object to parse</param>
        /// <returns>a Result</returns>
        public static Result Parse(JObject json)
        {
            json.TryGetValue("case_id", out var caseIDToken);
            var result = new Result
            {
                JsonFromResponse = json,
                Id = (ulong)json["id"],
                TestId = (ulong)json["test_id"],
                CaseId = caseIDToken != null ? caseIDToken.Value<ulong>() : default,
                StatusId = (ulong?)json["status_id"],
                CreatedBy = (ulong?)json["created_by"],
                CreatedOn = null == (int?)json["created_on"] ? (DateTime?)null : new DateTime(1970, 1, 1).AddSeconds((int)json["created_on"]),
                AssignedToId = (ulong?)json["assignedto_id"],
                Comment = (string)json["comment"],
                Version = (string)json["version"],
                Defects = (string)json["defects"]
            };

            // separate for easier debugging if necessary
            result.Elapsed = TimeSpanUtility.FromString((string)json["elapsed"]);

            return result;
        }

        /// <summary>Returns a json object that represents this class</summary>
        /// <returns>json object that represents this class</returns>
        public virtual JObject GetJson()
        {
            dynamic jsonParams = new JObject();

            if (TestId > 0)
            {
                jsonParams.test_id = TestId;
            }

            if (CaseId > 0)
            {
                jsonParams.case_id = CaseId;
            }

            if (null != StatusId)
            {
                jsonParams.status_id = (int)StatusId;
            }

            if (null != Comment)
            {
                jsonParams.comment = Comment;
            }

            if (null != Version)
            {
                jsonParams.version = Version;
            }

            if (null != Elapsed)
            {
                jsonParams.elapsed = $"{Elapsed.Value.Days}d {Elapsed.Value.Hours}h {Elapsed.Value.Minutes}m {Elapsed.Value.Seconds}s";
            }

            if (null != Defects)
            {
                jsonParams.defects = Defects;
            }

            if (null != AssignedToId)
            {
                jsonParams.assignedto_id = AssignedToId.Value;
            }

            return jsonParams;
        }
        #endregion Public Methods
    }
}
