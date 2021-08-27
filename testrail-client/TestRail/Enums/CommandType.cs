using System.ComponentModel;

namespace TestRail.Enums
{
    /// <summary>Command types available</summary>
    public enum CommandType
    {
        /// <summary>For get commands.</summary>
        [Description("get")]
        Get,

        /// <summary>For add commands.</summary>
        [Description("add")]
        Add,

        /// <summary>For update commands.</summary>
        [Description("update")]
        Update,

        /// <summary>For delete commands.</summary>
        [Description("delete")]
        Delete,

        /// <summary>For close commands.</summary>
        [Description("close")]
        Close,
        
        /// <summary>For move commands.</summary>
        [Description("move")]
        Move
    }
}
