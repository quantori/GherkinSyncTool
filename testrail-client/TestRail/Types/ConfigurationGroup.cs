using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using TestRail.Utils;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>store information about a configuration group in testrail</summary>
    public class ConfigurationGroup : BaseTestRailType
    {
        #region Public Properties
        /// <summary>id of the current configuration group</summary>
        public ulong Id { get; set; }

        /// <summary>project id associated with the current configuration group</summary>
        public ulong ProjectId { get; set; }

        /// <summary>name of the currenct configuration group</summary>
        public string Name { get; set; }

        /// <summary>list of configuations associated with the current configuration group</summary>
        public List<Configuration> Configurations { get; set; }
        #endregion Public Properties

        #region Public Methods
        /// <summary>parses json into a ConfigurationGroup</summary>
        /// <param name="json">json to parse</param>
        /// <returns>ConfigurationGroup corresponding to the json</returns>
        public static ConfigurationGroup Parse(JObject json)
        {
            var configurationGroup = new ConfigurationGroup
            {
                JsonFromResponse = json,
                Id = (ulong)json["id"],
                Name = (string)json["name"],
                ProjectId = (ulong)json["project_id"]
            };

            var jarray = json["configs"] as JArray;

            if (null != jarray)
            {
                configurationGroup.Configurations = JsonUtility.ConvertJArrayToList(jarray, Configuration.Parse);
            }

            return configurationGroup;
        }
        #endregion Public Methods
    }
}
