using System;

namespace GherkinSyncTool.Exceptions
{
    /// <summary>
    /// Thrown when TestRail does not have a test case with provided Id
    /// </summary>
    public class TestRailNoCaseException : TestRailException
    {
        public TestRailNoCaseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}