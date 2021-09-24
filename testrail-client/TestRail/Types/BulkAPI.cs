using System.Collections.Generic;
using Newtonsoft.Json;

namespace TestRail.Types
{
    /// <summary>This is the base class for all Bulk API Endpoints</summary>
    public class BulkAPI : BaseTestRailType
    {
        #region Public Properties

        /// <summary>Offset of the response</summary>
        public ulong Offset { get; set; }

        /// <summary>Limit of the response</summary>
        public ulong Limit { get; set; }

        /// <summary>Size of the response</summary>
        public ulong Size { get; set; }

        /// <summary>URI links of the response</summary>
        public Links _Links { get; set; }

        /// <summary>Json Arroy of data from the response </summary>
        [JsonExtensionData]
        public Dictionary<string, object> DataItems { get; set; }

        #endregion Public Properties
    }

    /// <summary>This is the class for Links</summary>
    public class Links
    {
        #region Public Properties

        /// <summary>Next uri</summary>
        public string Next { get; set; }

        /// <summary>Prev uri</summary>
        public string Prev { get; set; }

        #endregion Public Properties
    }
}