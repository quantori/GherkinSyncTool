using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using TestRail.Utils;

namespace TestRail.Types
{
    /// <inheritdoc />
    /// <summary>stores information about a milestone</summary>
    public class Milestone : BaseTestRailType
    {
        #region Public Properties
        /// <summary>id of the milestone</summary>
        public ulong Id { get; private set; }

        /// <summary>name of the milestone</summary>
        public string Name { get; set; }

        /// <summary>description of the milestone</summary>
        public string Description { get; set; }

        /// <summary>id of the parent milestone of the sub-milestone</summary>
        public ulong? ParentId { private get; set; }

        /// <summary>list of the sub-milestones</summary>
        public List<Milestone> Milestones { get; set; }

        /// <summary>true if the milestone is completed</summary>
        public bool? IsCompleted { get; set; }

        /// <summary>true if the milestone is started</summary>
        public bool? IsStarted { get; set; }

        /// <summary>date on which the milestone is due</summary>
        public DateTime? DueOn { get; set; }

        /// <summary>date on which the milestone was completed</summary>
        public DateTime? CompletedOn { get; private set; }

        /// <summary>id of the project with which the milestone is associated</summary>
        public ulong ProjectId { get; private set; }

        /// <summary>the url for to view the milestone</summary>
        public string Url { get; private set; }
        #endregion Public Properties

        #region Public Methods
        /// <summary>string representation of the object</summary>
        /// <returns>string representation of the object</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>parses json into a milestone</summary>
        /// <param name="json">json to parse</param>
        /// <returns>milestone corresponding to the json</returns>
        public static Milestone Parse(JObject json)
        {
            var milestone = new Milestone
            {
                JsonFromResponse = json,
                Id = (ulong)json["id"],
                Name = (string)json["name"],
                Description = (string)json["description"],
                IsCompleted = (bool?)json["is_completed"],
                IsStarted = (bool?)json["is_started"],
                DueOn = null == (int?)json["due_on"] ? (DateTime?)null : new DateTime(1970, 1, 1).AddSeconds((int)json["due_on"]),
                CompletedOn = null == (int?)json["completed_on"] ? (DateTime?)null : new DateTime(1970, 1, 1).AddSeconds((int)json["completed_on"]),
                ProjectId = (ulong)json["project_id"],
                Url = (string)json["url"]
            };

            var jarray = json["milestones"] as JArray;
            if (null != jarray)
            {
                milestone.Milestones = JsonUtility.ConvertJArrayToList(jarray, Parse);
            }

            return milestone;
        }

        /// <summary>Creates a json object for this class</summary>
        /// <returns>json object that represents this class</returns>
        public JObject GetJson()
        {
            dynamic jsonParams = new JObject();

            if (!string.IsNullOrWhiteSpace(Name)) { jsonParams.name = Name; }
            if (!string.IsNullOrWhiteSpace(Description)) { jsonParams.description = Description; }
            if (null != ParentId) { jsonParams.parent_id = ParentId.Value; }
            if (null != DueOn) { jsonParams.dueOn = DueOn.Value.ToUnixTimestamp(); }
            if (null != IsCompleted) { jsonParams.is_completed = IsCompleted; }

            return jsonParams;
        }
        #endregion Public Methods
    }
}
