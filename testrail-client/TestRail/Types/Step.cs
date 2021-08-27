using Newtonsoft.Json.Linq;
using TestRail.Enums;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about a step</summary>
    public class Step : BaseTestRailType
    {
        #region Public Fields
        /// <summary>description of the step</summary>
        public string Description;

        /// <summary>expected result for the step</summary>
        public string Expected;

        /// <summary>actual result for the step</summary>
        public string Actual;

        /// <summary>result of the step</summary>
        public ResultStatus? Status;
        #endregion Public Fields

        #region Public Methods
        /// <summary>parses json into a step</summary>
        /// <param name="json">json to parse</param>
        /// <returns>step corresponding to the json</returns>
        public static Step Parse(JObject json)
        {
            var step = new Step
            {
                JsonFromResponse = json,
                Description = (string)json["content"],
                Expected = (string)json["expected"],
                Actual = (string)json["actual"],
                Status = null == (int?)json["status_id"] ? (ResultStatus?)null : (ResultStatus)(int)json["status_id"]
            };

            return step;
        }

        /// <summary>Get the json object that describes this class</summary>
        /// <returns>json object for this class</returns>
        public JObject GetJsonObject()
        {
            dynamic json = new JObject();

            if (!string.IsNullOrWhiteSpace(Description))
            {
                json.content = Description;
            }

            if (!string.IsNullOrWhiteSpace(Expected))
            {
                json.expected = Expected;
            }

            if (!string.IsNullOrWhiteSpace(Actual))
            {
                json.actual = Actual;
            }

            if (null != Status)
            {
                json.status_id = (int)Status;
            }

            return json;
        }
        #endregion Public Methods
    }
}
