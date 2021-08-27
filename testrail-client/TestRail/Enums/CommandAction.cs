using System.ComponentModel;

namespace TestRail.Enums
{
    /// <summary>Command actions available.</summary>
    public enum CommandAction
    {
        /// <summary>Used to access an individual case.</summary>
        [Description("case")]
        Case,

        /// <summary>Used to access a set of cases.</summary>
        [Description("cases")]
        Cases,

        /// <summary>Used to access the different case types.</summary>
        [Description("case_types")]
        CaseTypes,

        /// <summary>Used to access the different case fields.</summary>
        [Description("case_fields")]
        CaseFields,

        /// <summary>Used to access configurations.</summary>
        [Description("configs")]
        Configs,
        
        /// <summary>Used to move test case from one section to another.</summary>
        [Description("cases_to_section")]
        CasesToSection,
        
        /// <summary>Used to access an individual milestone.</summary>
        [Description("milestone")]
        Milestone,

        /// <summary>Used to access a set of milestones.</summary>
        [Description("milestones")]
        Milestones,

        /// <summary>Used to access an individual plan.</summary>
        [Description("plan")]
        Plan,

        /// <summary>Used to access a set of plans.</summary>
        [Description("plans")]
        Plans,

        /// <summary>Used to access an individual plan entry.</summary>
        [Description("plan_entry")]
        PlanEntry,

        /// <summary>Used to access a set of priorities.</summary>
        [Description("priorities")]
        Priorities,

        /// <summary>Used to access an individual project.</summary>
        [Description("project")]
        Project,

        /// <summary>Used to access a set of projects.</summary>
        [Description("projects")]
        Projects,

        /// <summary>Used to access an individual result.</summary>
        [Description("result")]
        Result,

        /// <summary>Used to access a set of results.</summary>
        [Description("results")]
        Results,

        /// <summary>Used to access an individual result for a case.</summary>
        [Description("result_for_case")]
        ResultForCase,

        /// <summary>Used to access the results for an individual case.</summary>
        [Description("results_for_case")]
        ResultsForCase,

        /// <summary>Used to access the results for a group of cases.</summary>
        [Description("results_for_cases")]
        ResultsForCases,

        /// <summary>Used to access the test results for a run.</summary>
        [Description("results_for_run")]
        ResultsForRun,

        /// <summary>Used to access an individual run.</summary>
        [Description("run")]
        Run,

        /// <summary>Used to access a set of runs.</summary>
        [Description("runs")]
        Runs,

        /// <summary>Used to access an individual section.</summary>
        [Description("section")]
        Section,

        /// <summary>Used to access a set of sections.</summary>
        [Description("sections")]
        Sections,

        /// <summary>Used to access an individual suite.</summary>
        [Description("suite")]
        Suite,

        /// <summary>Used to access a set of suites.</summary>
        [Description("suites")]
        Suites,

        /// <summary>Used to access a set of statuses.</summary>
        [Description("statuses")]
        Statuses,

        /// <summary>Used to access an individual test.</summary>
        [Description("test")]
        Test,

        /// <summary>Used to access a set of tests.</summary>
        [Description("tests")]
        Tests,

        /// <summary>Used to access an individual user.</summary>
        [Description("user")]
        User,

        /// <summary>Used to access a set of users.</summary>
        [Description("users")]
        Users,

        /// <summary>Used to access a user by email address.</summary>
        [Description("user_by_email")]
        UserByEmail
    }
}
