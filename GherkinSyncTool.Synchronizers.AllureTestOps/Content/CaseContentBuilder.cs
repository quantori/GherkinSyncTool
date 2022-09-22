﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.AllureTestOps.Client;
using GherkinSyncTool.Synchronizers.AllureTestOps.Model;
using NLog;
using Quantori.AllureTestOpsClient.Model;
using Refit;
using AllureTestCaseStep = Quantori.AllureTestOpsClient.Model.Step;
using AllureScenario = Quantori.AllureTestOpsClient.Model.Scenario;
using Scenario = Gherkin.Ast.Scenario;
using Step = Gherkin.Ast.Step;
using Tag = Quantori.AllureTestOpsClient.Model.Tag;

namespace GherkinSyncTool.Synchronizers.AllureTestOps.Content;

public class CaseContentBuilder
{
    private readonly AllureTestOpsSettings _allureTestOpsSettings =
        ConfigurationManager.GetConfiguration<AllureTestOpsConfigs>().AllureTestOpsSettings;

    private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);

    private readonly AllureClientWrapper _allureClientWrapper;
    private readonly Context _context;
    private List<WorkflowSchema> _workflowSchemas;
    private Item _automatedWorkflowId;
    private Item _manualWorkflowId;
    private List<Tag> _testTags;

    public List<WorkflowSchema> WorkflowSchemas =>
        _workflowSchemas ??= _allureClientWrapper.GetAllWorkflowSchemas(_allureTestOpsSettings.ProjectId).ToList();

    private List<WorkflowContent> _workflows;
    public List<WorkflowContent> Workflows => _workflows ??= _allureClientWrapper.GetAllWorkflows().ToList();

    public Item AutomatedWorkflow =>
        _automatedWorkflowId ??= WorkflowSchemas.FirstOrDefault(schema => schema.Type.Equals(TestType.Automated))!.Workflow;

    public Item ManualWorkflow => _manualWorkflowId ??= WorkflowSchemas.FirstOrDefault(schema => schema.Type.Equals(TestType.Manual))!.Workflow;
    public  List<Tag> AllureTestTags => _testTags ??= _allureClientWrapper.GetAllTestTags();

    public CaseContentBuilder(AllureClientWrapper allureClientWrapper, Context context)
    {
        _allureClientWrapper = allureClientWrapper;
        _context = context;
    }

    public CreateTestCaseRequestExtended BuildCaseRequest(Scenario scenario, IFeatureFile featureFile)
    {
        var createTestCaseRequestExtended = new CreateTestCaseRequestExtended
        {
            CreateTestCaseRequest =
            {
                Name = scenario.Name,
                ProjectId = _allureTestOpsSettings.ProjectId,
                Automated = IsAutomated(scenario, featureFile),
                StatusId = AddStatus(scenario, featureFile),
                WorkflowId = AddWorkflow(scenario, featureFile),
                Description = AddDescription(scenario, featureFile),
                Scenario = AddScenario(scenario, featureFile),
                Tags = AddTags(scenario, featureFile)
            },
            StepsAttachments = AddStepAttachments(scenario, featureFile)
        };

        return createTestCaseRequestExtended;
    }

    private List<Tag> AddTags(Scenario scenario, IFeatureFile featureFile)
    {
        var allTags = GherkinHelper.GetAllTags(scenario, featureFile);
        //Remove tags that will duplicate existing fields
        RemoveTags(allTags, TagsConstants.Reference, TagsConstants.Automated, TagsConstants.Status);
        
        var result = new List<Tag>();
        if (allTags.Any())
        {
            foreach (var tag in allTags)
            {
                var tagName = tag.Name.Replace("@", "");
                var allureTag = AllureTestTags.FirstOrDefault(t => t.Name.Equals(tagName, StringComparison.InvariantCultureIgnoreCase));
                if (allureTag is null)
                {
                    var newTag = _allureClientWrapper.AddTestTags(tagName);
                    AllureTestTags.Add(newTag);
                    result.Add(newTag);
                    continue;
                }
                result.Add(new Tag {Id = allureTag.Id, Name = tagName});
            }
        }

        return result;
    }

    private void RemoveTags(List<Gherkin.Ast.Tag> allTags, params string[] tagsToRemove)
    {
        foreach (var tagToRemove in tagsToRemove)
        {
            allTags.RemoveAll(tag => tag.Name.Contains(tagToRemove, StringComparison.InvariantCultureIgnoreCase));    
        }
    }

    private Dictionary<int, ByteArrayPart> AddStepAttachments(Scenario scenario, IFeatureFile featureFile)
    {
        var attachments = ExtractAttachments(scenario.Steps.ToList());

        var background = featureFile.Document.Feature.Children.OfType<Background>().FirstOrDefault();
        if (background is not null)
        {
            var backgroundStepCount = background.Steps.Count();
            var attachmentsShifted = attachments.ToDictionary(attachment => attachment.Key + backgroundStepCount, attachment => attachment.Value);

            var backgroundAttachments = ExtractAttachments(background.Steps.ToList());
            return backgroundAttachments.Concat(attachmentsShifted).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        return attachments;
    }

    private Dictionary<int, ByteArrayPart> ExtractAttachments(List<Step> steps)
    {
        var result = new Dictionary<int, ByteArrayPart>();

        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];

            switch (step.Argument)
            {
                case DocString docString:

                    var textBytes = Encoding.ASCII.GetBytes(docString.Content);
                    result.Add(index, new ByteArrayPart(textBytes, "Text", "text/plain"));
                    break;

                case DataTable table:

                    var csvBytes = Encoding.ASCII.GetBytes(ConvertToCsvTable(table.Rows.ToList()));
                    result.Add(index, new ByteArrayPart(csvBytes, "Table", "text/csv"));
                    break;
            }
        }

        return result;
    }

    private AllureScenario AddScenario(Scenario scenario, IFeatureFile featureFile)
    {
        var steps = GetSteps(scenario, featureFile);
        if (!steps.Any()) return null;

        var allureScenario = new AllureScenario
        {
            Steps = new List<AllureTestCaseStep>()
        };
        allureScenario.Steps.AddRange(steps);
        return allureScenario;
    }

    private List<AllureTestCaseStep> GetSteps(Scenario scenario, IFeatureFile featureFile)
    {
        var scenarioSteps = ExtractSteps(scenario.Steps);

        var background = featureFile.Document.Feature.Children.OfType<Background>().FirstOrDefault();
        if (background is not null)
        {
            var backgroundSteps = ExtractSteps(background.Steps);
            return backgroundSteps.Concat(scenarioSteps).ToList();
        }

        return scenarioSteps;
    }

    private List<AllureTestCaseStep> ExtractSteps(IEnumerable<Step> steps)
    {
        return steps.Select(step => new AllureTestCaseStep { Keyword = step.Keyword.Trim(), Name = step.Text }).ToList();
    }

    private string ConvertToCsvTable(List<TableRow> tableRows)
    {
        var table = new StringBuilder();

        //Header
        table.AppendLine(string.Join(",", tableRows.First().Cells.Select(cell => cell.Value)));

        //Table body
        for (var i = 1; i < tableRows.Count; i++)
        {
            table.AppendLine(string.Join(",", tableRows[i].Cells.Select(cell => cell.Value)));
        }

        return table.ToString();
    }

    private string AddDescription(Scenario scenario, IFeatureFile featureFile)
    {
        var description = new StringBuilder();
        description.AppendLine(featureFile.Document.Feature.Description);
        description.AppendLine(scenario.Description);

        var background = featureFile.Document.Feature.Children.OfType<Background>().FirstOrDefault();
        if (background is not null && (!string.IsNullOrWhiteSpace(background.Name) || !string.IsNullOrWhiteSpace(background.Description)))
        {
            description.AppendLine($"**{background.Keyword}:** {background.Name}");
            if (!string.IsNullOrWhiteSpace(background.Description)) description.AppendLine(background.Description);
        }

        description.AppendLine($"**Feature file:** {featureFile.RelativePath}");

        var examples = scenario.Examples.ToList();

        if (examples.Any())
        {
            foreach (var example in examples)
            {
                description.AppendLine($"**{example.Keyword}:** {example.Name}");
                if (!string.IsNullOrEmpty(example.Description))
                {
                    description.AppendLine(example.Description);
                }

                if (example.TableHeader is null) continue;

                var tableRows = new List<TableRow> { example.TableHeader };
                tableRows.AddRange(example.TableBody);
                description.AppendLine(ConvertToMarkdownTable(tableRows));
            }
        }

        return description.ToString();
    }

    private string ConvertToMarkdownTable(List<TableRow> tableRows)
    {
        var table = new StringBuilder();
        table.AppendLine();
        table.Append("|");

        //Header
        foreach (var cell in tableRows.First().Cells)
        {
            table.Append($"{cell.Value}|");
        }

        table.AppendLine();
        table.Append("|");
        //Header delimiter
        foreach (var unused in tableRows.First().Cells)
        {
            table.Append("---|");
        }

        table.AppendLine();

        //Table body
        for (int i = 1; i < tableRows.Count; i++)
        {
            table.Append("|");

            var row = tableRows[i];
            foreach (var cell in row.Cells)
            {
                table.Append($"{cell.Value}|");
            }

            table.AppendLine();
        }

        return table.ToString();
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
                return autoStatuses.FirstOrDefault()!.Id;
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
            return manualStatuses.FirstOrDefault()!.Id;
        }
    }

    private bool IsAutomated(Scenario scenario, IFeatureFile featureFile)
    {
        var allTags = GherkinHelper.GetAllTags(scenario, featureFile);
        return allTags.Exists(tag => tag.Name.Contains(TagsConstants.Automated, StringComparison.InvariantCultureIgnoreCase));
    }
}