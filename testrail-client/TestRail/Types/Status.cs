using Newtonsoft.Json.Linq;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about a status</summary>
    public class Status : BaseTestRailType
    {
        #region Public Properties
        /// <summary>id of the status</summary>
        public ulong Id { get; private set; }

        /// <summary>name of the status</summary>
        public string Name { get; private set; }

        /// <summary>display name of the current status</summary>
        public string Label { get; private set; }

        /// <summary>the 'dark' color being use for the current status</summary>
        public ulong ColorDark { get; private set; }

        /// <summary>the 'medium' color being use for the current status</summary>
        public ulong ColorMedium { get; private set; }

        /// <summary>the 'bright' color being use for the current status</summary>
        public ulong ColorBright { get; private set; }

        /// <summary>whether or not the current instance is a system status</summary>
        public bool IsSystem { get; private set; }

        /// <summary>whether or not the current instance is untested</summary>
        public bool IsUntested { get; private set; }

        /// <summary>whether or not the current instance is finalized</summary>
        public bool IsFinal { get; private set; }
        #endregion Public Properties

        #region Public Methods
        /// <summary>string representation of the object</summary>
        /// <returns>string representation of the object</returns>	
        public override string ToString()
        {
            return Name;
        }

        /// <summary>Parses json into a Status object</summary>
        /// <param name="json">json to parse</param>
        /// <returns>Status object corresponding to the json</returns>
        public static Status Parse(JObject json)
        {
            var status = new Status
            {
                JsonFromResponse = json,
                Id = (ulong)json["id"],
                Name = (string)json["name"],
                Label = (string)json["label"],
                ColorDark = (ulong)json["color_dark"],
                ColorMedium = (ulong)json["color_medium"],
                ColorBright = (ulong)json["color_bright"],
                IsSystem = (bool)json["is_system"],
                IsUntested = (bool)json["is_untested"],
                IsFinal = (bool)json["is_final"]
            };

            return status;
        }
        #endregion Public Methods
    }
}
