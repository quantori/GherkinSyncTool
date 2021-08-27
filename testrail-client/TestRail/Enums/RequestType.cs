using System.ComponentModel;

namespace TestRail.Enums
{
    /// <summary>These enums are used to label what kind of request you are building.</summary>
    public enum RequestType
    {
        /// <summary>Used for GET requests.</summary>
        [Description("GET")]
        Get,

        /// <summary>Used for POST requests.</summary>
        [Description("POST")]
        Post
    }
}
