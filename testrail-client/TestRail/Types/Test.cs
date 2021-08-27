using Newtonsoft.Json.Linq;
using TestRail.Enums;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about a test</summary>
    public class Test : BaseTestRailType
    {
        #region Public Properties
        /// <summary>id of the test</summary>
        public ulong? Id { get; private set; }

        /// <summary>id of the test case</summary>
        public ulong? CaseId { get; set; }

        /// <summary>id of the test run</summary>
        public ulong? RunId { get; private set; }

        /// <summary>test rail status id</summary>
        public ResultStatus? Status { get; private set; }

        /// <summary>id of the user the test is assigned to</summary>
        public ulong? AssignedToId { get; private set; }

        /// <summary>title of the test</summary>
        public string Title;
        #endregion Public Properties

        #region Public Methods
        /// <summary>string representation of the object</summary>
        /// <returns>string representation of the object</returns>
        public override string ToString()
        {
            return Title;
        }

        /// <summary>parses a test from the supplied json</summary>
        /// <param name="json">json for the test</param>
        /// <returns>test corresponding to the json</returns>
        public static Test Parse(JObject json)
        {
            var test = new Test
            {
                JsonFromResponse = json,
                Id = (ulong?)json["id"],
                CaseId = (ulong?)json["case_id"],
                RunId = (ulong?)json["run_id"],
                Status = (ResultStatus?)(int)json["status_id"],
                AssignedToId = (ulong?)json["assignedto_id"],
                Title = (string)json["title"]
            };

            return test;
        }
        #endregion Public Methods
    }
}
