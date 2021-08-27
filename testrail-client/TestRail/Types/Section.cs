using Newtonsoft.Json.Linq;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about a section</summary>
    public class Section : BaseTestRailType
    {
        #region Public Properties
        /// <summary>id of the section</summary>
        public ulong? Id { get; set; }

        /// <summary>name of the section</summary>
        public string Name { get; set; }

        /// <summary>description of the section</summary>
        public string Description { get; set; }

        /// <summary>id of the parent section of the section</summary>
        public ulong? ParentId { get; set; }

        /// <summary>depth of the section</summary>
        public uint? Depth { get; set; }

        /// <summary>display order of the section</summary>
        public uint? DisplayOrder { get; set; }

        /// <summary>id of the suite associated with the section</summary>
        public ulong? SuiteId { get; set; }
        #endregion Public Properties

        #region Public Methods
        /// <summary>string representation of the object</summary>
        /// <returns>string representation of the object</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>parses json into a section</summary>
        /// <param name="json">json to parse</param>
        /// <returns>section corresponding to the json</returns>
        public static Section Parse(JObject json)
        {
            var section = new Section
            {
                JsonFromResponse = json,
                Id = (ulong?)json["id"],
                Name = (string)json["name"],
                Description = (string)json["description"],
                ParentId = (ulong?)json["parent_id"],
                Depth = (uint?)json["depth"],
                DisplayOrder = (uint?)json["display_order"],
                SuiteId = (ulong?)json["suite_id"]
            };

            return section;
        }

        /// <summary>Creates a json object for this class</summary>
        /// <returns>json object that represents this class</returns>
        public JObject GetJson()
        {
            dynamic jsonParams = new JObject();
            if (null != SuiteId) { jsonParams.suite_id = SuiteId.Value; }
            if (null != ParentId) { jsonParams.parent_id = ParentId.Value; }
            if (!string.IsNullOrWhiteSpace(Name)) { jsonParams.name = Name; }
            if (!string.IsNullOrWhiteSpace(Description)) { jsonParams.description = Description; }
            return jsonParams;
        }
        #endregion Public Methods
    }
}
