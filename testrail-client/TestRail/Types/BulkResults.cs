using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about the result of a test</summary>
    public class BulkResults : BaseTestRailType
    {
        #region Public Properties
        /// <summary>list of results</summary>
        public IList<Result> Results { get; set; }
        #endregion Public Properties

        #region Public Methods
        /// <summary>string representation of the object</summary>
        /// <returns>string representation of the object</returns>
        public override string ToString()
        {
            var builder = new StringBuilder("[");

            for (var index = 0; index < Results.Count; index++)
            {
                var result = Results[index];

                builder.Append($"{result.Id}:{result.Comment}");

                if (index + 1 < Results.Count)
                {
                    builder.Append(", ");
                }
            }

            builder.Append("]");

            return builder.ToString();
        }

        /// <summary>Returns a json object that represents this class</summary>
        /// <returns>json object that represents this class</returns>
        public virtual JObject GetJson()
        {
            dynamic jsonParams = new JObject();

            jsonParams.results = new List<JObject>();

            foreach (var result in Results)
            {
                jsonParams.Add(result.GetJson());
            }

            return jsonParams;
        }
        #endregion Public Methods
    }
}
