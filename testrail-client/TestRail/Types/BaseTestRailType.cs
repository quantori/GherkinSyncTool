using Newtonsoft.Json.Linq;

namespace TestRail.Types
{
    /// <summary>This is the base class for all Test Rail types.</summary>
    public abstract class BaseTestRailType
    {
        /// <summary>Raw JSON received from API.</summary>
        public JObject JsonFromResponse { get; set; }
    }
}
