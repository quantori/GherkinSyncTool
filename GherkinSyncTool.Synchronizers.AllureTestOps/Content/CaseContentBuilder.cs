using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.AllureTestOps.Client;
using GherkinSyncTool.Synchronizers.AllureTestOps.Model;
using NLog;
using Quantori.AllureTestOpsClient.Model;
using Scenario = Gherkin.Ast.Scenario;

namespace GherkinSyncTool.Synchronizers.AllureTestOps.Content;

public class CaseContentBuilder
{
    private readonly AllureTestOpsSettings _allureTestOpsSettings =
        ConfigurationManager.GetConfiguration<AllureTestOpsConfigs>().AllureTestOpsSettings;

    private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);

    private readonly AllureClient _allureClient;
    private readonly Context _context;
    private List<WorkflowSchema> _workflowSchemas;
    private Item _automatedWorkflowId;
    private Item _manualWorkflowId;

    public List<WorkflowSchema> WorkflowSchemas =>
        _workflowSchemas ??= _allureClient.GetAllWorkflowSchemas(_allureTestOpsSettings.ProjectId).ToList();

    private List<WorkflowContent> _workflows;
    public List<WorkflowContent> Workflows => _workflows ??= _allureClient.GetAllWorkflows().ToList();

    public Item AutomatedWorkflow =>
        _automatedWorkflowId ??= WorkflowSchemas.FirstOrDefault(schema => schema.Type.Equals(TestType.Automated))!.Workflow;

    public Item ManualWorkflow => _manualWorkflowId ??= WorkflowSchemas.FirstOrDefault(schema => schema.Type.Equals(TestType.Manual))!.Workflow;

    public CaseContentBuilder(AllureClient allureClient, Context context)
    {
        _allureClient = allureClient;
        _context = context;
    }

    public CreateTestCaseRequest BuildCaseRequest(Scenario scenario, IFeatureFile featureFile)
    {
        var caseRequest = new CreateTestCaseRequest
        {
            Name = scenario.Name,
            ProjectId = _allureTestOpsSettings.ProjectId,
            Automated = IsAutomated(scenario, featureFile),
            StatusId = AddStatus(scenario, featureFile),
            WorkflowId = AddWorkflow(scenario, featureFile)
        };

        return caseRequest;
    }

    private long AddWorkflow(Scenario scenario, IFeatureFile featureFile)
    {
        return IsAutomated(scenario, featureFile) ? AutomatedWorkflow.Id : ManualWorkflow.Id;
    }

    private long? AddStatus(Scenario scenario, IFeatureFile featureFile)
    {
        var allTags = GherkinHelper.GetAllTags(scenario, featureFile);
        var statusTag = allTags.LastOrDefault(tag => tag.Name.Contains(TagsConstants.Status, StringComparison.InvariantCultureIgnoreCase));

        var automated = IsAutomated(scenario, featureFile);

        var manualStatuses = Workflows.FirstOrDefault(workflow => workflow.Id == ManualWorkflow.Id)!.Statuses;
        var autoStatuses = Workflows.FirstOrDefault(workflow => workflow.Id == AutomatedWorkflow.Id)!.Statuses;

        if (statusTag is null)
        {
            if (automated)
            {
                return autoStatuses.FirstOrDefault()!.Id;
            }

            return manualStatuses.FirstOrDefault()!.Id;
        }

        var statusString = statusTag.Name.Replace(TagsConstants.Status, "", StringComparison.InvariantCultureIgnoreCase);


        if (IsAutomated(scenario, featureFile))
        {
            try
            {
                return autoStatuses.First(status => status.Name.Equals(statusString, StringComparison.InvariantCultureIgnoreCase)).Id;
            }
            catch (InvalidOperationException e)
            {
                var statusNames = string.Join(", ", autoStatuses.Select(status => status.Name));
                Log.Error(e,
                    $"'{statusString}' is incorrect option for scenario: '{scenario.Name}'. Valid options are: '{statusNames}'. Workflow: '{AutomatedWorkflow.Name}'");
                _context.IsRunSuccessful = false;
                return null;
            }
        }

        try
        {
            return manualStatuses.First(status => status.Name.Equals(statusString, StringComparison.InvariantCultureIgnoreCase)).Id;
        }
        catch (InvalidOperationException e)
        {
            var statusNames = string.Join(", ", manualStatuses.Select(status => status.Name));
            Log.Error(e,
                $"'{statusString}' is incorrect option for scenario: '{scenario.Name}'. Valid options are: '{statusNames}'. Workflow: '{ManualWorkflow.Name}'");
            _context.IsRunSuccessful = false;
            return null;
        }
    }

    private bool IsAutomated(Scenario scenario, IFeatureFile featureFile)
    {
        var allTags = GherkinHelper.GetAllTags(scenario, featureFile);
        return allTags.Exists(tag => tag.Name.Contains(TagsConstants.Automated, StringComparison.InvariantCultureIgnoreCase));
    }
}