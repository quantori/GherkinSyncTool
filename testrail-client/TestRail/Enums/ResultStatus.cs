namespace TestRail.Enums
{
    /// <summary>the enumeration represents the status of a test result</summary>
    public enum ResultStatus
    {
        /// <summary>the test has not been run</summary>
        Untested = 3,

        /// <summary>the test passed</summary>
        Passed = 1,

        /// <summary>the test is blocked</summary>
        Blocked = 2,

        /// <summary>the test needs to be rerun</summary>
        Retest = 4,

        /// <summary>the test failed</summary>
        Failed = 5,

        /// <summary>custom status 1</summary>
        CustomStatus1 = 6,

        /// <summary>custom status 2</summary>
        CustomStatus2 = 7,

        /// <summary>custom status 3</summary>
        CustomStatus3 = 8,

        /// <summary>custom status 4</summary>
        CustomStatus4 = 9,

        /// <summary>custom status 5</summary>
        CustomStatus5 = 10,

        /// <summary>custom status 6</summary>
        CustomStatus6 = 11,

        /// <summary>custom status 7</summary>
        CustomStatus7 = 12,
    }
}
