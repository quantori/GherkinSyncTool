using Newtonsoft.Json.Linq;
using System;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about a project</summary>
    public class Project : BaseTestRailType
    {
        #region Public Properties
        /// <summary>id of the project</summary>
        public ulong Id { get; private set; }

        /// <summary>name of the project</summary>
        public string Name { get; set; }

        /// <summary>url of the project</summary>
        public string Url { get; private set; }

        /// <summary>announcement associated with the project</summary>
        public string Announcement { get; set; }

        /// <summary>true if the announcement should be displayed on the project's overview page and false otherwise</summary>
        public bool? ShowAnnouncement { get; set; }

        /// <summary>true if the project has been completed</summary>
        public bool? IsCompleted { get; set; }

        /// <summary>date on which the milestone was completed</summary>
        public DateTime? CompletedOn { get; private set; }
        #endregion Public Properties

        #region Public Methods
        /// <summary>string representation of the object</summary>
        /// <returns>string representation of the object</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>parses json into a project</summary>
        /// <param name="json">json to parse</param>
        /// <returns>project corresponding to the json</returns>
        public static Project Parse(JObject json)
        {
            var project = new Project
            {
                JsonFromResponse = json,
                Id = (ulong)json["id"],
                Name = (string)json["name"],
                Announcement = (string)json["announcement"],
                ShowAnnouncement = (bool?)json["show_announcement"],
                IsCompleted = (bool?)json["is_completed"],
                Url = (string)json["url"],
                CompletedOn = null == (int?)json["completed_on"] ? (DateTime?)null : new DateTime(1970, 1, 1).AddSeconds((int)json["completed_on"])
            };

            return project;
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

            if (!string.IsNullOrWhiteSpace(Announcement))
            {
                jsonParams.announcement = Announcement;
            }

            if (null != ShowAnnouncement)
            {
                jsonParams.show_announcement = ShowAnnouncement.Value;
            }

            if (null != IsCompleted)
            {
                jsonParams.is_completed = IsCompleted.Value;
            }

            return jsonParams;
        }
        #endregion Public Methods
    }
}
