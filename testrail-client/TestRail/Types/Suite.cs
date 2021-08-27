using Newtonsoft.Json.Linq;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about a suite</summary>
    public class Suite : BaseTestRailType
    {
        #region Public Properties
        /// <summary>id of the suite</summary>
        public ulong? Id { get; set; }

        /// <summary>name of the suite</summary>
        public string Name { get; set; }

        /// <summary>description of the suite</summary>
        public string Description { get; set; }

        /// <summary>id of the project associated with the suite</summary>
        public ulong? ProjectId { get; set; }

        /// <summary>url to view the suite</summary>
        public string Url { get; set; }
        #endregion Public Properties

        #region Public Methods
        /// <summary>string representation of the object</summary>
        /// <returns>string representation of the object</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>parses json into a suite</summary>
        /// <param name="json">json to parse</param>
        /// <returns>suite corresponding to the json</returns>
        public static Suite Parse(JObject json)
        {
            var suite = new Suite
            {
                JsonFromResponse = json,
                Id = (ulong?)json["id"],
                Name = (string)json["name"],
                Description = (string)json["description"],
                ProjectId = (ulong?)json["project_id"],
                Url = (string)json["url"]
            };

            return suite;
        }

        /// <summary>Creates a json object for this class</summary>
        /// <returns>json object that represents this class</returns>
        public JObject GetJson()
        {
            dynamic jsonParams = new JObject();

            if (!string.IsNullOrWhiteSpace(Name))
            {
                jsonParams.name = Name;
            }

            if (!string.IsNullOrWhiteSpace(Description))
            {
                jsonParams.description = Description;
            }

            return jsonParams;
        }
        #endregion Public Methods
    }
}
