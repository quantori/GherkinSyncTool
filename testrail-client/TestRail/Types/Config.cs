using Newtonsoft.Json.Linq;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about a config</summary>
    public class Config : BaseTestRailType
    {
        #region Public Properties
        /// <summary>Options for this configuration</summary>
        public ConfigOption Option { get; private set; }

        /// <summary>Guid unique identifier as a string</summary>
        public string Id { get; private set; }

        /// <summary>Configuration context</summary>
        public ConfigContext Context { get; private set; }
        #endregion Public Properties

        #region Public Methods
        /// <summary>Constructor</summary>
        /// <param name="json">json object to parse into a Config</param>
        public static Config Parse(JObject json)
        {
            var config = new Config
            {
                JsonFromResponse = json,
                Option = ConfigOption.Parse((JObject)json["options"]),
                Context = ConfigContext.Parse((JObject)json["context"]),
                Id = (string)json["id"]
            };

            return config;
        }
        #endregion Public Methods
    }
}
