using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using TestRail.Utils;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about a case field</summary>
    public class CaseField : BaseTestRailType
    {
        #region Public Properties
        /// <summary>id of the field</summary>
        public ulong? Id { get; private set; }

        /// <summary>easy name of the custom case field</summary>
        public string Name { get; private set; }

        /// <summary>system name of the custom case field</summary>
        public string SystemName { get; private set; }

        /// <summary>entity id</summary>
        public ulong? EntityId { get; private set; }

        /// <summary>display label for the custom case field</summary>
        public string Label { get; private set; }

        /// <summary>description of the custom case field</summary>
        public string Description { get; private set; }

        /// <summary>type of custom case field as described by the case type</summary>
        public ulong? TypeId { get; private set; }

        /// <summary>location id</summary>
        public ulong? LocationId { get; private set; }

        /// <summary>display order</summary>
        public ulong? DisplayOrder { get; private set; }

        /// <summary>list of configurations for this case field</summary>
        public List<Config> Configs { get; private set; }

        /// <summary>is multi</summary>
        public bool? IsMulti { get; private set; }
        #endregion Public Properties

        #region Public Methods
        /// <summary>string representation of the object</summary>
        /// <returns>string representation of the object</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>Parses the json object into a CaseField object</summary>
        /// <param name="json">json to parse into a CaseField</param>
        /// <returns>CaseField corresponding to the json</returns>
        public static CaseField Parse(JObject json)
        {
            var caseField = new CaseField
            {
                JsonFromResponse = json,
                Id = (ulong?)json["id"],
                Name = (string)json["name"],
                SystemName = (string)json["system_name"],
                EntityId = (ulong?)json["entity_id"],
                Label = (string)json["label"],
                Description = (string)json["description"],
                TypeId = (ulong?)json["type_id"],
                LocationId = (ulong?)json["location_id"],
                DisplayOrder = (ulong?)json["display_order"]
            };

            var jarray = json["configs"] as JArray;

            if (null != jarray)
            {
                caseField.Configs = JsonUtility.ConvertJArrayToList(jarray, Config.Parse);
            }

            caseField.IsMulti = (bool?)json["is_multi"];

            return caseField;
        }
        #endregion Public Methods
    }
}
