using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using TestRail.Enums;
using TestRail.Types;
using TestRail.Utils;

namespace TestRail
{
    /// <summary>client used to access test case data in testrail</summary>
    public class TestRailClient
    {
        /// <summary>base url for testrail</summary>
        protected string BaseUrl;

        /// <summary>base 64 string of the given credentials</summary>
        protected string AuthInfo;

        /// <summary>projects in the test rail database</summary>
        public IList<Project> Projects => _projects.Value;

        /// <summary>list of projects in the current testrail instance</summary>
        private readonly Lazy<IList<Project>> _projects;

        /// <summary>dictionary of priority ID (from test rail) to priority levels(where Higher value means higher priority)</summary>
        private IDictionary<ulong, int> PriorityIdToLevel => LazyPriorityIdToLevel.Value;

        /// <summary>dictionary of priority ID (from test rail) to priority levels(where Higher value means higher priority)</summary>
        private Lazy<IDictionary<ulong, int>> LazyPriorityIdToLevel { get; }

        #region Constructor
        /// <summary>constructor</summary>
        /// <param name="baseUrl">base url for test rail</param>
        /// <param name="userName">user name</param>
        /// <param name="password">password</param>
        public TestRailClient(string baseUrl, string userName, string password)
        {
            BaseUrl = baseUrl;
            AuthInfo = Convert.ToBase64String(Encoding.Default.GetBytes($"{userName}:{password}"));

            _projects = new Lazy<IList<Project>>(_GetProjects);

            // set up the lazy loading of the priority dictionary (priority id to priority value)
            LazyPriorityIdToLevel = new Lazy<IDictionary<ulong, int>>(_CreatePrioritiesDict);
        }
        #endregion Constructor

        #region Public Methods
        /// <summary>Get the priority for the case if we can</summary>
        /// <param name="c">case to get the priority from</param>
        /// <returns>int value of priority if possible, null if not found</returns>
        public int? GetPriorityForCase(Case c)
        {
            int? priority = null;

            if (null != c?.PriorityId && null != PriorityIdToLevel && PriorityIdToLevel.ContainsKey(c.PriorityId.Value))
            {
                priority = PriorityIdToLevel[c.PriorityId.Value];
            }

            return priority;
        }

        #region Add Commands
        /// <summary>
        /// Adds a new test result, comment or assigns a test.
        /// It's recommended to use AddResults() instead if you plan to add results for multiple tests.
        /// </summary>
        /// <param name="testId">The ID of the test the result should be added to.</param>
        /// <param name="status">The test status.</param>
        /// <param name="comment">The comment/description for the test result.</param>
        /// <param name="version">The version or build you tested against.</param>
        /// <param name="elapsed">The time it took to execute the test, e.g. "30s" or "1m 45s".</param>
        /// <param name="defects">A comma-separated list of defects to link to the test result.</param>
        /// <param name="assignedToId">The ID of a user the test should be assigned to.</param>
        /// <param name="customs">Custom fields are supported as well and must be submitted with their system name, prefixed with 'custom_', e.g. custom_comment</param>
        /// <returns>If successful, this method will return the new test result.</returns>
        public RequestResult<Result> AddResult(ulong testId, ResultStatus? status, string comment = null,
            string version = null, TimeSpan? elapsed = null, string defects = null, ulong? assignedToId = null, JObject customs = null)
        {
            var uri = _CreateUri_(CommandType.Add, CommandAction.Result, testId);

            var result = new Result
            {
                TestId = testId,
                StatusId = (ulong?)status,
                Comment = comment,
                Version = version,
                Elapsed = elapsed,
                Defects = defects,
                AssignedToId = assignedToId
            };

            var jsonParams = JsonUtility.Merge(result.GetJson(), customs);

            return _SendPostCommand<Result>(uri, jsonParams);
        }

        /// <summary>
        /// Adds one or more new test results, comments or assigns one or more tests.
        /// Ideal for test automation to bulk-add multiple test results in one step.
        /// </summary>
        /// <param name="runId">The ID of the test run the results should be added to.</param>
        /// <param name="results">Bulk results to submit, please note that all referenced tests must belong to the same test run.</param>
        /// <returns>If successful, this method returns the new test results in the same order as the list of the request.</returns>
        public RequestResult<IList<Result>> AddResults(ulong runId, BulkResults results)
        {
            var uri = _CreateUri_(CommandType.Add, CommandAction.Results, runId);

            return _SendPostCommand<IList<Result>>(uri, results.GetJson());
        }

        /// <summary>
        /// Adds a new test result, comment or assigns a test.
        /// It's recommended to use AddResultsForCases() instead if you plan to add results for multiple test cases.
        /// </summary>
        /// <param name="runId">The ID of the test run.</param>
        /// <param name="caseId">The ID of the test case.</param>
        /// <param name="status">The test status.</param>
        /// <param name="comment">The comment/description for the test result.</param>
        /// <param name="version">The version or build you tested against.</param>
        /// <param name="elapsed">The time it took to execute the test, e.g. "30s" or "1m 45s".</param>
        /// <param name="defects">A comma-separated list of defects to link to the test result.</param>
        /// <param name="assignedToId">The ID of a user the test should be assigned to.</param>
        /// <param name="customs">Custom fields are supported as well and must be submitted with their system name, prefixed with 'custom_', e.g. custom_comment</param>
        /// <returns>If successful, this method will return the new test result.</returns>
        public RequestResult<Result> AddResultForCase(ulong runId, ulong caseId, ResultStatus? status, string comment = null,
            string version = null, TimeSpan? elapsed = null, string defects = null, ulong? assignedToId = null, JObject customs = null)
        {
            var uri = _CreateUri_(CommandType.Add, CommandAction.ResultForCase, runId, caseId);

            var result = new Result
            {
                StatusId = (ulong?)status,
                Comment = comment,
                Version = version,
                Elapsed = elapsed,
                Defects = defects,
                AssignedToId = assignedToId
            };

            var jsonParams = JsonUtility.Merge(result.GetJson(), customs);

            return _SendPostCommand<Result>(uri, jsonParams);
        }

        /// <summary>
        /// Adds one or more new test results, comments or assigns one or more tests (using the case IDs).
        /// Ideal for test automation to bulk-add multiple test results in one step.
        /// </summary>
        /// <param name="runId">The ID of the test run the results should be added to.</param>
        /// <param name="results">Bulk results to submit, please note that all referenced tests must belong to the same test run.</param>
        /// <returns>If successful, this method returns the new test results in the same order as the list of the request.</returns>
        public RequestResult<IList<Result>> AddResultsForCases(ulong runId, BulkResults results)
        {
            var uri = _CreateUri_(CommandType.Add, CommandAction.ResultsForCases, runId);

            return _SendPostCommand<IList<Result>>(uri, results.GetJson());
        }

        /// <summary>Creates a new test run.</summary>
        /// <param name="projectId">The ID of the project the test run should be added to.</param>
        /// <param name="suiteId">The ID of the test suite for the test run (optional if the project is operating in single suite mode, required otherwise).</param>
        /// <param name="name">	The name of the test run.</param>
        /// <param name="description">The description of the test run.</param>
        /// <param name="milestoneId">The ID of the milestone to link to the test run.</param>
        /// <param name="assignedToId">The ID of the user the test run should be assigned to.</param>
        /// <param name="caseIds">An array of case IDs for the custom case selection.</param>
        /// <param name="customs">Custom fields are supported as well and must be submitted with their system name, prefixed with 'custom_', e.g. custom_comment</param>
        /// <returns>If successful, this method returns the new test run.</returns>
        public RequestResult<Run> AddRun(ulong projectId, ulong suiteId, string name, string description, ulong? milestoneId = null,
            ulong? assignedToId = null, HashSet<ulong> caseIds = null, JObject customs = null)
        {
            var includeAll = true;

            // validates whether we are in include all or custom case selection mode
            if (null != caseIds)
            {
                var atLeastOneCaseFoundInSuite = _CasesFoundInSuite(projectId, suiteId, caseIds);

                if (atLeastOneCaseFoundInSuite)
                {
                    includeAll = false;
                }

                else
                {
                    return new RequestResult<Run>(HttpStatusCode.BadRequest, thrownException: new Exception("Case ids not found in the Suite"));
                }
            }

            var uri = _CreateUri_(CommandType.Add, CommandAction.Run, projectId);

            var run = new Run
            {
                SuiteId = suiteId,
                Name = name,
                Description = description,
                MilestoneId = milestoneId,
                AssignedTo = assignedToId,
                IncludeAll = includeAll,
                CaseIds = caseIds
            };

            var jsonParams = JsonUtility.Merge(run.GetJson(), customs);

            return _SendPostCommand<Run>(uri, jsonParams);
        }

        /// <summary>Creates a new test case.</summary>
        /// <param name="sectionId">The ID of the section the test case should be added to.</param>
        /// <param name="title">The title of the test case (required).</param>
        /// <param name="typeId">The ID of the case type.</param>
        /// <param name="priorityId">The ID of the case priority.</param>
        /// <param name="estimate">The estimate, e.g. "30s" or "1m 45s".</param>
        /// <param name="milestoneId">The ID of the milestone to link to the test case.</param>
        /// <param name="refs">A comma-separated list of references/requirements.</param>
        /// <param name="customFields">Custom fields are supported as well and must be submitted with their system name, prefixed with 'custom_', e.g. custom_preconds</param>
        /// <param name="templateId">The ID of the template (field layout) (requires <b>TestRail 5.2</b> or later).</param>
        /// <returns>If successful, this method returns the new test case.</returns>
        public RequestResult<Case> AddCase(ulong sectionId, string title, ulong? typeId = null, ulong? priorityId = null,
            string estimate = null, ulong? milestoneId = null, string refs = null, JObject customFields = null, ulong? templateId = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return new RequestResult<Case>(HttpStatusCode.BadRequest, thrownException: new ArgumentNullException(nameof(title)));
            }

            var uri = _CreateUri_(CommandType.Add, CommandAction.Case, sectionId);

            var tmpCase = new Case
            {
                Title = title,
                TypeId = typeId,
                PriorityId = priorityId,
                Estimate = estimate,
                MilestoneId = milestoneId,
                References = refs,
                TemplateId = templateId
            };

            var jsonParams = JsonUtility.Merge(tmpCase.GetJson(), customFields);

            return _SendPostCommand<Case>(uri, jsonParams);
        }

        /// <summary>Creates a new project (admin status required).</summary>
        /// <param name="projectName">The name of the project (required).</param>
        /// <param name="announcement">The description of the project.</param>
        /// <param name="showAnnouncement">True if the announcement should be displayed on the project's overview page and false otherwise.</param>
        /// <returns>If successful, this method returns the new project.</returns>
        public RequestResult<Project> AddProject(string projectName, string announcement = null, bool? showAnnouncement = null)
        {
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return new RequestResult<Project>(HttpStatusCode.BadRequest, thrownException: new ArgumentNullException(nameof(projectName)));
            }

            var uri = _CreateUri_(CommandType.Add, CommandAction.Project);

            var project = new Project
            {
                Name = projectName,
                Announcement = announcement,
                ShowAnnouncement = showAnnouncement
            };

            return _SendPostCommand<Project>(uri, project.GetJson());
        }

        /// <summary>Creates a new section.</summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="suiteId">The ID of the test suite (ignored if the project is operating in single suite mode, required otherwise).</param>
        /// <param name="name">The name of the section (required).</param>
        /// <param name="parentId">The ID of the parent section (to build section hierarchies).</param>
        /// <param name="description">The description of the section (added with TestRail 4.0).</param>
        /// <returns>If successful, this method returns the new section.</returns>
        public RequestResult<Section> AddSection(ulong projectId, ulong suiteId, string name, ulong? parentId = null, string description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new RequestResult<Section>(HttpStatusCode.BadRequest, thrownException: new ArgumentNullException(nameof(name)));
            }

            var uri = _CreateUri_(CommandType.Add, CommandAction.Section, projectId);

            var section = new Section
            {
                SuiteId = suiteId,
                ParentId = parentId,
                Name = name,
                Description = description
            };

            return _SendPostCommand<Section>(uri, section.GetJson());
        }

        /// <summary>Creates a new test suite.</summary>
        /// <param name="projectId">The ID of the project the test suite should be added to.</param>
        /// <param name="name">The name of the test suite (required).</param>
        /// <param name="description">The description of the test suite.</param>
        /// <returns>If successful, this method returns the new test suite.</returns>
        public RequestResult<Suite> AddSuite(ulong projectId, string name, string description = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new RequestResult<Suite>(HttpStatusCode.BadRequest, thrownException: new ArgumentNullException(nameof(name)));
            }

            var uri = _CreateUri_(CommandType.Add, CommandAction.Suite, projectId);

            var suite = new Suite
            {
                Name = name,
                Description = description
            };

            return _SendPostCommand<Suite>(uri, suite.GetJson());
        }

        /// <summary>Creates a new test plan.</summary>
        /// <param name="projectId">The ID of the project the test plan should be added to.</param>
        /// <param name="name">The name of the test plan (required).</param>
        /// <param name="description">The description of the test plan.</param>
        /// <param name="milestoneId">The ID of the milestone to link to the test plan.</param>
        /// <param name="entries">An array of objects describing the test runs of the plan.</param>
        /// <param name="customs">Custom fields are supported as well and must be submitted with their system name, prefixed with 'custom_', e.g. custom_comment</param>
        /// <returns>If successful, this method returns the new test plan.</returns>
        public RequestResult<Plan> AddPlan(ulong projectId, string name, string description = null, ulong? milestoneId = null,
            List<PlanEntry> entries = null, JObject customs = null)// TODO: - Add config ids here: , params ulong[] suiteIDs)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new RequestResult<Plan>(HttpStatusCode.BadRequest, thrownException: new ArgumentNullException(nameof(name)));
            }

            var uri = _CreateUri_(CommandType.Add, CommandAction.Plan, projectId);

            var plan = new Plan
            {
                Name = name,
                Description = description,
                MilestoneId = milestoneId,
                Entries = entries
            };

            var jsonParams = JsonUtility.Merge(plan.GetJson(), customs);

            return _SendPostCommand<Plan>(uri, jsonParams);
        }

        /// <summary>Adds one or more new test runs to a test plan.</summary>
        /// <param name="planId">The ID of the plan the test runs should be added to.</param>
        /// <param name="suiteId">The ID of the test suite for the test run(s) (required).</param>
        /// <param name="name">The name of the test run(s).</param>
        /// <param name="assignedToId">The ID of the user the test run(s) should be assigned to.</param>
        /// <param name="caseIds">An array of case IDs for the custom case selection.</param>
        /// <param name="customs">Custom fields are supported as well and must be submitted with their system name, prefixed with 'custom_', e.g. custom_comment</param>
        /// <returns>If successful, this method returns the new test plan entry including test runs.</returns>
        public RequestResult<PlanEntry> AddPlanEntry(ulong planId, ulong suiteId, string name = null, ulong? assignedToId = null,
            List<ulong> caseIds = null, JObject customs = null)
        {
            var uri = _CreateUri_(CommandType.Add, CommandAction.PlanEntry, planId);

            var planEntry = new PlanEntry
            {
                AssignedToId = assignedToId,
                SuiteId = suiteId,
                Name = name,
                CaseIds = caseIds
            };

            var jsonParams = JsonUtility.Merge(planEntry.GetJson(), customs);

            return _SendPostCommand<PlanEntry>(uri, jsonParams);
        }

        /// <summary>Creates a new milestone.</summary>
        /// <param name="projectId">The ID of the project the milestone should be added to.</param>
        /// <param name="name">The name of the milestone (required).</param>
        /// <param name="description">The description of the milestone.</param>
        /// <param name="parentId">The ID of the parent milestone, if any (for sub-milestones) (available since TestRail 5.3).</param>
        /// <param name="dueOn">The due date of the milestone (as UNIX timestamp).</param>
        /// <returns>If successful, this method returns the new milestone.</returns>
        public RequestResult<Milestone> AddMilestone(ulong projectId, string name, string description = null, ulong? parentId = null, DateTime? dueOn = null)
        {
            var uri = _CreateUri_(CommandType.Add, CommandAction.Milestone, projectId);

            var milestone = new Milestone
            {
                Name = name,
                Description = description,
                DueOn = dueOn,
                ParentId = parentId
            };

            return _SendPostCommand<Milestone>(uri, milestone.GetJson());
        }
        #endregion Add Commands

        #region Update Commands
        /// <summary>Updates an existing test case (partial updates are supported, i.e. you can submit and update specific fields only).</summary>
        /// <param name="caseId">The ID of the test case.</param>
        /// <param name="title">The title of the test case.</param>
        /// <param name="typeId">The ID of the test case type that is linked to the test case.</param>
        /// <param name="priorityId">The ID of the priority that is linked to the test case.</param>
        /// <param name="estimate">The estimate, e.g. "30s" or "1m 45s".</param>
        /// <param name="milestoneId">The ID of the milestone that is linked to the test case.</param>
        /// <param name="refs">A comma-separated list of references/requirements.</param>
        /// <param name="customs">Custom fields are also included in the response and use their system name prefixed with 'custom_' as their field identifier.</param>
        /// <param name="templateId">The ID of the template (field layout) (requires <b>TestRail 5.2</b> or later).</param>
        /// <returns>If successful, this method returns the updated test case.</returns>
        public RequestResult<Case> UpdateCase(ulong caseId, string title, ulong? typeId = null, ulong? priorityId = null, string estimate = null,
            ulong? milestoneId = null, string refs = null, JObject customs = null, ulong? templateId = null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return new RequestResult<Case>(HttpStatusCode.BadRequest, thrownException: new ArgumentNullException(nameof(title)));
            }

            var uri = _CreateUri_(CommandType.Update, CommandAction.Case, caseId);

            var tmpCase = new Case
            {
                Title = title,
                TypeId = typeId,
                PriorityId = priorityId,
                Estimate = estimate,
                MilestoneId = milestoneId,
                References = refs,
                TemplateId = templateId
            };

            var jsonParams = JsonUtility.Merge(tmpCase.GetJson(), customs);

            return _SendPostCommand<Case>(uri, jsonParams);
        }

        /// <summary>Updates an existing milestone (partial updates are supported, i.e. you can submit and update specific fields only).</summary>
        /// <param name="milestoneId">The ID of the milestone.</param>
        /// <param name="name">The name of the milestone (required).</param>
        /// <param name="description">The description of the milestone.</param>
        /// <param name="dueOn">The due date of the milestone (as UNIX timestamp).</param>
        /// <param name="isCompleted">True if a milestone is considered completed and false otherwise.</param>
        /// <returns>If successful, this method returns the updated milestone.</returns>
        public RequestResult<Milestone> UpdateMilestone(ulong milestoneId, string name = null, string description = null, DateTime? dueOn = null, bool? isCompleted = null)
        {
            var uri = _CreateUri_(CommandType.Update, CommandAction.Milestone, milestoneId);

            var milestone = new Milestone
            {
                Name = name,
                Description = description,
                DueOn = dueOn,
                IsCompleted = isCompleted
            };

            return _SendPostCommand<Milestone>(uri, milestone.GetJson());
        }

        /// <summary>Updates an existing test plan (partial updates are supported, i.e. you can submit and update specific fields only).</summary>
        /// <param name="planId">The ID of the test plan.</param>
        /// <param name="name">The name of the test plan (required).</param>
        /// <param name="description">The description of the test plan.</param>
        /// <param name="milestoneId">The ID of the milestone to link to the test plan.</param>
        /// <param name="customs">Custom fields are also included in the response and use their system name prefixed with 'custom_' as their field identifier.</param>
        /// <returns>If successful, this method returns the updated test plan.</returns>
        public RequestResult<Plan> UpdatePlan(ulong planId, string name = null, string description = null, ulong? milestoneId = null, JObject customs = null)
        {
            var uri = _CreateUri_(CommandType.Update, CommandAction.Plan, planId);

            var plan = new Plan
            {
                Name = name,
                Description = description,
                MilestoneId = milestoneId
            };

            var jsonParams = JsonUtility.Merge(plan.GetJson(), customs);

            return _SendPostCommand<Plan>(uri, jsonParams);
        }

        /// <summary>Updates one or more existing test runs in a plan (partial updates are supported, i.e. you can submit and update specific fields only).</summary>
        /// <param name="planId">The ID of the test plan.</param>
        /// <param name="entryId">The ID of the test plan entry (note: not the test run ID).</param>
        /// <param name="name">The name of the test run(s).</param>
        /// <param name="assignedToId">The ID of the user the test run(s) should be assigned to.</param>
        /// <param name="caseIds">An array of case IDs for the custom case selection.</param>
        /// <param name="customs">Custom fields are also included in the response and use their system name prefixed with 'custom_' as their field identifier.</param>
        /// <returns>If successful, this method returns the updated test plan entry including test runs.</returns>
        public RequestResult<PlanEntry> UpdatePlanEntry(ulong planId, string entryId, string name = null, ulong? assignedToId = null, List<ulong> caseIds = null, JObject customs = null)
        {
            var uri = _CreateUri_(CommandType.Update, CommandAction.PlanEntry, planId, null, null, entryId);

            var planEntry = new PlanEntry
            {
                AssignedToId = assignedToId,
                Name = name,
                CaseIds = caseIds
            };

            var jsonParams = JsonUtility.Merge(planEntry.GetJson(), customs);

            return _SendPostCommand<PlanEntry>(uri, jsonParams);
        }

        /// <summary>Updates an existing project (admin status required; partial updates are supported, i.e. you can submit and update specific fields only).</summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="projectName">The name of the project (required).</param>
        /// <param name="announcement">The description of the project.</param>
        /// <param name="showAnnouncement">True if the announcement should be displayed on the project's overview page and false otherwise.</param>
        /// <param name="isCompleted">Specifies whether a project is considered completed or not.</param>
        /// <returns>If successful, this method returns the updated project.</returns>
        public RequestResult<Project> UpdateProject(ulong projectId, string projectName, string announcement = null, bool? showAnnouncement = null, bool? isCompleted = null)
        {
            var uri = _CreateUri_(CommandType.Update, CommandAction.Project, projectId);

            var project = new Project
            {
                Name = projectName,
                Announcement = announcement,
                ShowAnnouncement = showAnnouncement,
                IsCompleted = isCompleted
            };

            return _SendPostCommand<Project>(uri, project.GetJson());
        }

        /// <summary>Updates an existing test run (partial updates are supported, i.e. you can submit and update specific fields only).</summary>
        /// <param name="runId">The ID of the test run.</param>
        /// <param name="name">The name of the test run.</param>
        /// <param name="description">The description of the test run.</param>
        /// <param name="milestoneId">The ID of the milestone to link to the test run.</param>
        /// <param name="caseIds">An array of case IDs for the custom case selection.</param>
        /// <param name="customs">Custom fields are also included in the response and use their system name prefixed with 'custom_' as their field identifier.</param>
        /// <returns>If successful, this method returns the updated test run.</returns>
        public RequestResult<Run> UpdateRun(ulong runId, string name = null, string description = null, ulong? milestoneId = null, HashSet<ulong> caseIds = null, JObject customs = null)
        {
            var includeAll = true;
            var existingRun = GetRun(runId).Payload;

            // validates whether we are in include all or custom case selection mode
            if (null != existingRun?.ProjectId && existingRun.SuiteId.HasValue && null != caseIds)
            {
                var atLeastOneCaseFoundInSuite = _CasesFoundInSuite(existingRun.ProjectId.Value, existingRun.SuiteId.Value, caseIds);

                if (atLeastOneCaseFoundInSuite)
                {
                    includeAll = false;
                }

                else
                {
                    return new RequestResult<Run>(HttpStatusCode.BadRequest, thrownException: new Exception("Case IDs not found in the Suite"));
                }
            }

            var uri = _CreateUri_(CommandType.Update, CommandAction.Run, runId);

            var newRun = new Run
            {
                Name = name,
                Description = description,
                MilestoneId = milestoneId,
                IncludeAll = includeAll,
                CaseIds = caseIds
            };

            var jsonParams = JsonUtility.Merge(newRun.GetJson(), customs);

            return _SendPostCommand<Run>(uri, jsonParams);
        }

        /// <summary>Updates an existing section (partial updates are supported, i.e. you can submit and update specific fields only).</summary>
        /// <param name="sectionId">The ID of the section.</param>
        /// <param name="name">The name of the section.</param>
        /// <param name="description">The description of the section (added with TestRail 4.0).</param>
        /// <param name="customs">Custom fields are also included in the response and use their system name prefixed with 'custom_' as their field identifier.</param>
        /// <returns>If successful, this method returns the updated section.</returns>
        public RequestResult<Section> UpdateSection(ulong sectionId, string name, string description = null, JObject customs = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new RequestResult<Section>(HttpStatusCode.BadRequest, thrownException: new ArgumentNullException(nameof(name)));
            }

            var uri = _CreateUri_(CommandType.Update, CommandAction.Section, sectionId);

            var section = new Section
            {
                Id = sectionId,
                Name = name
            };

            var jsonParams = JsonUtility.Merge(section.GetJson(), customs);

            return _SendPostCommand<Section>(uri, jsonParams);
        }

        /// <summary>Updates an existing test suite (partial updates are supported, i.e. you can submit and update specific fields only).</summary>
        /// <param name="suiteId">The ID of the test suite.</param>
        /// <param name="name">The name of the test suite (required).</param>
        /// <param name="description">The description of the test suite.</param>
        /// <param name="customs">Custom fields are also included in the response and use their system name prefixed with 'custom_' as their field identifier.</param>
        /// <returns>If successful, this method returns the updated test suite.</returns>
        public RequestResult<Suite> UpdateSuite(ulong suiteId, string name = null, string description = null, JObject customs = null)
        {
            var uri = _CreateUri_(CommandType.Update, CommandAction.Suite, suiteId);

            var s = new Suite
            {
                Name = name,
                Description = description
            };

            var jsonParams = JsonUtility.Merge(s.GetJson(), customs);

            return _SendPostCommand<Suite>(uri, jsonParams);
        }
        #endregion Update Commands

        #region Close Commands
        /// <summary>Closes an existing test plan and archives its test runs and results. Please note: Closing a test plan cannot be undone.</summary>
        /// <param name="planId">The ID of the test plan.</param>
        /// <returns>If successful, this method returns the closed test plan.</returns>
        public RequestResult<Plan> ClosePlan(ulong planId)
        {
            var uri = _CreateUri_(CommandType.Close, CommandAction.Plan, planId);

            return _SendPostCommand<Plan>(uri);
        }

        /// <summary>Closes an existing test run and archives its tests and results. Please note: Closing a test run cannot be undone.</summary>
        /// <param name="runId">The ID of the test run.</param>
        /// <returns>If successful, this method returns the closed test run.</returns>
        public RequestResult<Run> CloseRun(ulong runId)
        {
            var uri = _CreateUri_(CommandType.Close, CommandAction.Run, runId);

            return _SendPostCommand<Run>(uri);
        }
        #endregion Close Commands

        #region Delete Commands
        /// <summary>Deletes an existing milestone. This action requires the account to have permissions to delete.</summary>
        /// <param name="milestoneId">The ID of the milestone.</param>
        /// <returns>Please note: Deleting a milestone cannot be undone.</returns>
        public RequestResult<Milestone> DeleteMilestone(ulong milestoneId)
        {
            var uri = _CreateUri_(CommandType.Delete, CommandAction.Milestone, milestoneId);

            return _SendPostCommand<Milestone>(uri);
        }

        /// <summary>Deletes an existing test case. This action requires the account to have permissions to delete.</summary>
        /// <param name="caseId">The ID of the test case.</param>
        /// <returns>
        /// Please note: Deleting a test case cannot be undone and also permanently deletes all test results in active test runs (i.e. test runs that haven't been closed (archived) yet).
        /// </returns>
        public RequestResult<Case> DeleteCase(ulong caseId)
        {
            var uri = _CreateUri_(CommandType.Delete, CommandAction.Case, caseId);

            return _SendPostCommand<Case>(uri);
        }

        /// <summary>Deletes an existing test plan. This action requires the account to have permissions to delete.</summary>
        /// <param name="planId">The ID of the test plan.</param>
        /// <returns>Please note: Deleting a test plan cannot be undone and also permanently deletes all test runs and results of the test plan.</returns>
        public RequestResult<Plan> DeletePlan(ulong planId)
        {
            var uri = _CreateUri_(CommandType.Delete, CommandAction.Plan, planId);

            return _SendPostCommand<Plan>(uri);
        }

        /// <summary>Deletes one or more existing test runs from a plan. This action requires the account to have permissions to delete.</summary>
        /// <param name="planId">The ID of the test plan.</param>
        /// <param name="entryId">The ID of the test plan entry (note: not the test run ID).</param>
        /// <returns>Please note: Deleting a test run from a plan cannot be undone and also permanently deletes all related test results.</returns>
        public RequestResult<PlanEntry> DeletePlanEntry(ulong planId, string entryId)
        {
            var uri = _CreateUri_(CommandType.Delete, CommandAction.PlanEntry, planId, null, null, entryId);

            return _SendPostCommand<PlanEntry>(uri);
        }

        /// <summary>Deletes an existing project (admin status required). This action requires the account to have permissions to delete.</summary>
        /// <param name="projectId">The ID of the project.</param>
        /// <returns>
        /// Please note: Deleting a project cannot be undone and also permanently deletes all test suites and cases, test runs and results and everything else that is part of the project.
        /// </returns>
        public RequestResult<Project> DeleteProject(ulong projectId)
        {
            var uri = _CreateUri_(CommandType.Delete, CommandAction.Project, projectId);

            return _SendPostCommand<Project>(uri);
        }

        /// <summary>Deletes an existing section. This action requires the account to have permissions to delete.</summary>
        /// <param name="sectionId">The ID of the section.</param>
        /// <returns>
        /// Please note: Deleting a section cannot be undone and also deletes all related test cases as well as active tests and results, i.e. tests and results that weren't closed (archived) yet.
        /// </returns>
        public RequestResult<Section> DeleteSection(ulong sectionId)
        {
            var uri = _CreateUri_(CommandType.Delete, CommandAction.Section, sectionId);

            return _SendPostCommand<Section>(uri);
        }

        /// <summary>Deletes an existing test suite. This action requires the account to have permissions to delete.</summary>
        /// <param name="suiteId">The ID of the test suite.</param>
        /// <returns>
        /// Please note: Deleting a test suite cannot be undone and also deletes all active test runs and results, i.e. test runs and results that weren't closed (archived) yet.
        /// </returns>
        public RequestResult<Suite> DeleteSuite(ulong suiteId)
        {
            var uri = _CreateUri_(CommandType.Delete, CommandAction.Suite, suiteId);

            return _SendPostCommand<Suite>(uri);
        }

        /// <summary>Deletes an existing test run. This action requires the account to have permissions to delete.</summary>
        /// <param name="runId">The ID of the test run.</param>
        /// <returns>Please note: Deleting a test run cannot be undone and also permanently deletes all tests and results of the test run.</returns>
        public RequestResult<Result> DeleteRun(ulong runId)
        {
            var uri = _CreateUri_(CommandType.Delete, CommandAction.Run, runId);

            return _SendPostCommand<Result>(uri);
        }
        #endregion Delete Commands

        #region CustomCommands

        /// <summary>
        /// Moves caseIds to another section
        /// </summary>
        /// <param name="newSectionId">Id of the new section</param>
        /// <param name="caseIds">Array of caseIds to be moved</param>
        /// <returns></returns>
        public RequestResult<Result> MoveCases(ulong newSectionId, IEnumerable<ulong> caseIds)
        {
            var uri = _CreateUri_(CommandType.Move, CommandAction.CasesToSection, newSectionId);

            var cases = new JObject(new JProperty("case_ids", new JArray(caseIds.Select(i=>i.ToString()))));

            return _SendPostCommand<Result>(uri, cases);
        }

        #endregion

        #region Get Commands
        /// <summary>gets a test</summary>
        /// <param name="testId">id of the test</param>
        /// <returns>information about the test</returns>
        public RequestResult<Test> GetTest(ulong testId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Test, testId);

            return _SendGetCommand<Test>(uri);
        }

        /// <summary>gets tests associated with a run</summary>
        /// <param name="runId">id of the run</param>
        /// <returns>tests associated with the run</returns>
        public RequestResult<IList<Test>> GetTests(ulong runId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Tests, runId);

            return _SendGetCommand<IList<Test>>(uri);
        }

        /// <summary>gets a case</summary>
        /// <param name="caseId">id of the case</param>
        /// <returns>information about the case</returns>
        public RequestResult<Case> GetCase(ulong caseId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Case, caseId);

            return _SendGetCommand<Case>(uri);
        }

        /// <summary>gets cases associated with a suite</summary>
        /// <param name="projectId">id of the project</param>
        /// <param name="suiteId">id of the suite</param>
        /// <param name="sectionId">(optional) id of the section</param>
        /// <returns>cases associated with the suite</returns>
        public RequestResult<IList<Case>> GetCases(ulong projectId, ulong suiteId, ulong? sectionId = null)
        {
            var optionalSectionId = sectionId.HasValue ? $"&section_id={sectionId.Value}" : string.Empty;
            var options = $"&suite_id={suiteId}{optionalSectionId}";
            var uri = _CreateUri_(CommandType.Get, CommandAction.Cases, projectId, null, options);

            return _SendGetCommand<IList<Case>>(uri);
        }

        /// <summary>returns a list of available test case custom fields</summary>
        /// <returns>a list of custom field definitions</returns>
        public RequestResult<IList<CaseField>> GetCaseFields()
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.CaseFields);

            return _SendGetCommand<IList<CaseField>>(uri);
        }

        /// <summary>returns a list of available case types</summary>
        /// <returns>a list of test case types, each has a unique ID and a name.</returns>
        public RequestResult<IList<CaseType>> GetCaseTypes()
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.CaseTypes);

            return _SendGetCommand<IList<CaseType>>(uri);
        }

        /// <summary>gets a suite</summary>
        /// <param name="suiteId">id of the suite</param>
        /// <returns>information about the suite</returns>
        public RequestResult<Suite> GetSuite(ulong suiteId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Suite, suiteId);

            return _SendGetCommand<Suite>(uri);
        }

        /// <summary>gets suites associated with a project</summary>
        /// <param name="projectId">id of the project</param>
        /// <returns>suites associated with the project</returns>
        public RequestResult<IList<Suite>> GetSuites(ulong projectId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Suites, projectId);

            return _SendGetCommand<IList<Suite>>(uri);
        }

        /// <summary>gets a section</summary>
        /// <param name="sectionId">id of the section</param>
        /// <returns>information about the section</returns>
        public RequestResult<Section> GetSection(ulong sectionId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Section, sectionId);

            return _SendGetCommand<Section>(uri);
        }

        /// <summary>gets sections associated with a suite</summary>
        /// <param name="projectId">id of the project</param>
        /// <param name="suiteId">id of the suite</param>
        /// <returns>sections associated with the suite</returns>
        public RequestResult<IList<Section>> GetSections(ulong projectId, ulong? suiteId = null)
        {
            var options = suiteId.HasValue ? $"&suite_id={suiteId}" : string.Empty;
            var uri = _CreateUri_(CommandType.Get, CommandAction.Sections, projectId, null, options);

            return _SendGetCommand<IList<Section>>(uri);
        }

        /// <summary>gets a run</summary>
        /// <param name="runId">id of the run</param>
        /// <returns>information about the run</returns>
        public RequestResult<Run> GetRun(ulong runId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Run, runId);

            return _SendGetCommand<Run>(uri);
        }

        /// <summary>gets runs associated with a project</summary>
        /// <param name="projectId">id of the project</param>
        /// <param name="offset">Where to start counting the test cases from (the offset)</param>
        /// <returns>runs associated with the project</returns>
        public RequestResult<IList<Run>> GetRuns(ulong projectId, ulong offset = 0)
        {
            var options = offset > 0 ? $"&offset={offset}" : null;
            var uri = _CreateUri_(CommandType.Get, CommandAction.Runs, projectId, null, options);

            return _SendGetCommand<IList<Run>>(uri);
        }

        /// <summary>gets a plan</summary>
        /// <param name="planId">id of the plan</param>
        /// <returns>information about the plan</returns>
        public RequestResult<Plan> GetPlan(ulong planId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Plan, planId);

            return _SendGetCommand<Plan>(uri);
        }

        /// <summary>gets plans associated with a project</summary>
        /// <param name="projectId">id of the project</param>
        /// <returns>plans associated with the project</returns>
        public RequestResult<IList<Plan>> GetPlans(ulong projectId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Plans, projectId);

            return _SendGetCommand<IList<Plan>>(uri);
        }

        /// <summary>gets a milestone</summary>
        /// <param name="milestoneId">id of the milestone</param>
        /// <returns>information about the milestone</returns>
        public RequestResult<Milestone> GetMilestone(ulong milestoneId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Milestone, milestoneId);

            return _SendGetCommand<Milestone>(uri);
        }

        /// <summary>gets milestones associated with a project</summary>
        /// <param name="projectId">id of the project</param>
        /// <returns>milestone associated with project</returns>
        public RequestResult<IList<Milestone>> GetMilestones(ulong projectId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Milestones, projectId);

            return _SendGetCommand<IList<Milestone>>(uri);
        }

        /// <summary>gets a project</summary>
        /// <param name="projectId">id of the project</param>
        /// <returns>information about the project</returns>
        public RequestResult<Project> GetProject(ulong projectId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Project, projectId);

            return _SendGetCommand<Project>(uri);
        }

        /// <summary>gets all projects contained in the testrail instance</summary>
        /// <returns>list containing all the projects</returns>
        public RequestResult<IList<Project>> GetProjects()
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Projects);

            return _SendGetCommand<IList<Project>>(uri);
        }

        /// <summary>Get User for user id</summary>
        /// <param name="userId">user id to search for</param>
        /// <returns>a User object</returns>
        public RequestResult<User> GetUser(ulong userId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.User, userId);

            return _SendGetCommand<User>(uri);
        }

        /// <summary>Find a user by their email address</summary>
        /// <param name="email">email address of the user</param>
        /// <returns>user if found</returns>
        public RequestResult<User> GetUserByEmail(string email)
        {
            // validate the email string
            if (string.IsNullOrWhiteSpace(email))
            {
                return new RequestResult<User>(HttpStatusCode.BadRequest,
                    thrownException: new ArgumentException($"You must provide a valid string that is not null or white space for: {nameof(email)}"));
            }

            var optionalParam = $"&email={email}";
            var uri = _CreateUri_(CommandType.Get, CommandAction.UserByEmail, null, null, optionalParam);

            return _SendGetCommand<User>(uri);
        }

        /// <summary>Get a list of users in the testrail instance</summary>
        /// <returns>List of users</returns>
        public RequestResult<IList<User>> GetUsers()
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Users);

            return _SendGetCommand<IList<User>>(uri);
        }

        /// <summary>Returns a list of test results for a test</summary>
        /// <param name="testId">id of the test</param>
        /// <param name="limit">(optional) maximum amount of test results to return, latest first</param>
        /// <param name="statusIds">a list of status IDs to filter by</param>
        /// <returns>list containing the results for the given test</returns>
        public RequestResult<IList<Result>> GetResults(ulong testId, ulong? limit = null, IList<ResultStatus> statusIds = null)
        {
            var filters = new StringBuilder(string.Empty);

            if (statusIds != null && statusIds.Any())
            {
                filters.Append($"&status_id={string.Join(",", statusIds)}");
            }

            if (limit.HasValue)
            {
                filters.Append($"&limit={limit.Value}");
            }

            var uri = _CreateUri_(CommandType.Get, CommandAction.Results, testId, null, filters.ToString());

            return _SendGetCommand<IList<Result>>(uri);
        }

        /// <summary>Return the list of test results for a test run and the case combination</summary>
        /// <param name="runId">id of the test run</param>
        /// <param name="caseId">id of the test case</param>
        /// <param name="limit">(optional) maximum amount of test results to return, latest first</param>
        /// <param name="offset">(optional) offset of test results to return</param>
        /// <param name="statusIds">a list of status IDs to filter by</param>
        /// <returns>list of test results for a case</returns>
        public RequestResult<IList<Result>> GetResultsForCase(ulong runId, ulong caseId, ulong? limit = null, ulong? offset = null, IList<ResultStatus> statusIds = null)
        {
            var filters = new StringBuilder(string.Empty);

            if (statusIds != null && statusIds.Any())
            {
                filters.Append($"&status_id={string.Join(",", statusIds)}");
            }

            if (limit.HasValue)
            {
                filters.Append($"&limit={limit.Value}");
            }

            if (offset.HasValue)
            {
                filters.Append($"&offset={offset.Value}");
            }

            var uri = _CreateUri_(CommandType.Get, CommandAction.ResultsForCase, runId, caseId, filters.ToString());

            return _SendGetCommand<IList<Result>>(uri);
        }

        /// <summary>Return the list of test results for a test run</summary>
        /// <param name="runId">id of the rest run</param>
        /// <param name="limit">(optional) maximum amount of test results to return, latest first</param>
        /// <param name="offset">(optional) offset of test results to return</param>
        /// <param name="statusIds">a list of status IDs to filter by</param>
        /// <returns>list of test results for a test run</returns>
        public RequestResult<IList<Result>> GetResultsForRun(ulong runId, ulong? limit = null, ulong? offset = null, IList<ResultStatus> statusIds = null)
        {
            var filters = new StringBuilder(string.Empty);

            if (statusIds != null && statusIds.Any())
            {
                filters.Append($"&status_id={string.Join(",", statusIds)}");
            }

            if (limit.HasValue)
            {
                filters.Append($"&limit={limit.Value}");
            }

            if (offset.HasValue)
            {
                filters.Append($"&offset={offset.Value}");
            }

            var uri = _CreateUri_(CommandType.Get, CommandAction.ResultsForRun, runId, null, filters.ToString());

            return _SendGetCommand<IList<Result>>(uri);
        }

        /// <summary>Returns the list of statuses available to test rail</summary>
        /// <returns>list of possible statuses</returns>
        public RequestResult<IList<Status>> GetStatuses()
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Statuses);

            return _SendGetCommand<IList<Status>>(uri);
        }

        /// <summary>Get a list of all available priorities</summary>
        /// <returns>list of priorities</returns>
        public RequestResult<IList<Priority>> GetPriorities()
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Priorities);

            return _SendGetCommand<IList<Priority>>(uri);
        }

        /// <summary>Returns a list of Config Groups available in a Project</summary>
        /// <param name="projectId">ID of the Project to return the Config Groups for</param>
        /// <returns>list of ConfigurationGroup</returns>
        public RequestResult<IList<ConfigurationGroup>> GetConfigurationGroups(ulong projectId)
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Configs, projectId);

            return _SendGetCommand<IList<ConfigurationGroup>>(uri);
        }
        #endregion Get Commands
        #endregion Public Methods

        #region Protected Methods
        /// <summary>Creates a URI with the parameters given in the format</summary>
        /// <param name="commandType">the type of action the server is going to take (i.e. get, add, update, close)</param>
        /// <param name="actionName">the type of command the server is going to take (i.e. run, case, plan, etc)</param>
        /// <param name="id1">(optional)first id to include in the uri</param>
        /// <param name="id2">(optional)second id to include in the uri</param>
        /// <param name="options">(optional)additional options to include in the uri</param>
        /// <param name="id2Str">(optional)additional parameters to append to the uri</param>
        /// <returns>the newly created uri</returns>
        protected static string _CreateUri_(CommandType commandType, CommandAction actionName, ulong? id1 = null,
            ulong? id2 = null, string options = null, string id2Str = null)
        {
            var commandString = commandType.GetStringValue();
            var actionString = actionName.GetStringValue();

            var uri = $"?/api/v2/{commandString}_{actionString}{(id1.HasValue ? "/" + id1.Value : string.Empty)}{(id2.HasValue ? "/" + id2.Value : !string.IsNullOrWhiteSpace(id2Str) ? "/" + id2Str : string.Empty)}{(!string.IsNullOrWhiteSpace(options) ? options : string.Empty)}";

            return uri;
        }
        #endregion Protected Methods

        #region Private Methods
        /// <summary>Used to send a POST request.</summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="uri">The endpoint to send the request to.</param>
        /// <param name="jsonParams">JSON object to include in the request.</param>
        /// <returns>The result of the request.</returns>
        private RequestResult<T> _SendPostCommand<T>(string uri, JObject jsonParams = null)
        {
            return _SendCommand<T>(uri, RequestType.Post, jsonParams);
        }

        /// <summary>Used to send a GET request.</summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="uri">The endpoint to send the request to.</param>
        /// <returns>The result of the request.</returns>
        private RequestResult<T> _SendGetCommand<T>(string uri)
        {
            return _SendCommand<T>(uri, RequestType.Get);
        }

        /// <summary>Used to build a request.</summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="uri">The endpoint to send the request to.</param>
        /// <param name="type">The type of request.</param>
        /// <param name="jsonParams">JSON object to include in the request.</param>
        /// <returns>The result of the request.</returns>
        private RequestResult<T> _SendCommand<T>(string uri, RequestType type, JObject jsonParams = null)
        {
            try
            {
                return _CallEndpoint<T>(uri, type, jsonParams);
            }

            // If there is an error, will try to create a new result object
            // with the corresponding response code
            catch (WebException ex)
            {
                var response = (HttpWebResponse) ex.Response;
                using (var reader = new StreamReader(response.GetResponseStream() ?? Stream.Null))
                {
                    var json = reader.ReadToEnd();
                    return new RequestResult<T>(response.StatusCode, json, ex);
                }
            }
            catch (Exception thrownException)
            {
                var message = thrownException.Message;
                var statusCode = HttpStatusCode.InternalServerError; // 500 is the default in this case

                // Return a response object for the most popular errors
                if (message.Contains("400"))
                    statusCode = HttpStatusCode.BadRequest;

                if (message.Contains("401"))
                    statusCode = HttpStatusCode.Unauthorized;

                if (message.Contains("403"))
                    statusCode = HttpStatusCode.Forbidden;

                if (message.Contains("404"))
                    statusCode = HttpStatusCode.NotFound;

                if (message.Contains("502"))
                    statusCode = HttpStatusCode.BadGateway;

                if (message.Contains("503"))
                    statusCode = HttpStatusCode.ServiceUnavailable;

                if (message.Contains("504"))
                    statusCode = HttpStatusCode.GatewayTimeout;

                return new RequestResult<T>(statusCode, thrownException: thrownException);
            }
        }

        /// <summary>Constructs the request and sends it.</summary>
        /// <typeparam name="T">The type to deserialize the response to.</typeparam>
        /// <param name="uri">The uri of the endpoint.</param>
        /// <param name="type">The type of request to build: GEt, POST, etc.</param>
        /// <param name="json">Parameters to send formatted as a single JSON object.</param>
        /// <returns>Result of the call.</returns>
        private RequestResult<T> _CallEndpoint<T>(string uri, RequestType type, JObject json = null)
        {
            // Build full uri
            uri = BaseUrl + uri;

            // Build request
            var request = new TestRailRequest(uri, type.GetStringValue());

            request.AddHeaders(new Dictionary<string, string> { { "Authorization", $"Basic {AuthInfo}" } });
            request.Accepts("application/json");
            request.ContentType("application/json");

            // Add body
            if (json != null)
            {
                request.AddBody(json.ToString());
            }

            // Send request and return response
            return request.Execute<T>();
        }

        /// <summary>Determines if at least one of the case ids given is contained in the project and suite</summary>
        /// <param name="projectId">id of the project</param>
        /// <param name="suiteId">id of the suite</param>
        /// <param name="caseIds">collection of case ids to check</param>
        /// <returns>true if at least one case exists in the project and suite id combination, otherwise false</returns>
        private bool _CasesFoundInSuite(ulong projectId, ulong suiteId, ICollection<ulong> caseIds)
        {
            var validCases = GetCases(projectId, suiteId).Payload;

            return validCases.Any(tmpCase => tmpCase.Id.HasValue && caseIds.Contains(tmpCase.Id.Value));
        }

        /// <summary>Create a priority dictionary</summary>
        /// <returns>dictionary of priority ID (from test rail) to priority levels(where Higher value means higher priority)</returns>
        private IDictionary<ulong, int> _CreatePrioritiesDict()
        {
            var tmpDict = new Dictionary<ulong, int>();
            var priorityList = GetPriorities().Payload;

            foreach (var priority in priorityList.Where(priority => null != priority))
            {
                tmpDict[priority.Id] = priority.PriorityLevel;
            }

            return tmpDict;
        }

        private IList<Project> _GetProjects()
        {
            var uri = _CreateUri_(CommandType.Get, CommandAction.Projects);

            var items = _SendGetCommand<IList<Project>>(uri);

            return items.Payload;
        }
        #endregion Private Methods
    }
}
