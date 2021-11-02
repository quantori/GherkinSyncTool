#nullable enable
using System;

namespace GherkinSyncTool.Synchronizers.TestRail.Exceptions
{
    public class TestRailException : Exception
    {
        public TestRailException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}