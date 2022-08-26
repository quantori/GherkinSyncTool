using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Synchronizers.AllureTestOps.Model;
using Quantori.AllureTestOpsClient.Model;
using Scenario = Gherkin.Ast.Scenario;
using Step = Gherkin.Ast.Step;

namespace GherkinSyncTool.Synchronizers.AllureTestOps.Content;

public class CaseContentBuilder
{
    private readonly AllureTestOpsSettings _allureTestOpsSettings = ConfigurationManager.GetConfiguration<AllureTestOpsConfigs>().AllureTestOpsSettings;
    
    public CreateTestCaseRequest BuildCaseRequest(Scenario scenario, IFeatureFile featureFile)
    {
        var steps = GetSteps(scenario, featureFile);

        var caseRequest = new CreateTestCaseRequest
        {
            Name = scenario.Name,
            ProjectId = _allureTestOpsSettings.ProjectId,
        };

        return caseRequest;
    }
    
    private List<string> GetSteps(Scenario scenario, IFeatureFile featureFile)
    {
        var scenarioSteps = ExtractSteps(scenario.Steps.ToList());

        var background = featureFile.Document.Feature.Children.OfType<Background>().FirstOrDefault();
        if (background is not null)
        {
            var backgroundSteps = ExtractSteps(background.Steps.ToList());
            return backgroundSteps.Concat(scenarioSteps).ToList();
        }

        return scenarioSteps;
    }

    private List<string> ExtractSteps(List<Step> steps)
    {
        List<string> resultSteps = new List<string>();
        foreach (var step in steps)
        {
            var keywordFormatted = $"**{step.Keyword.Trim()}** ";
            var stepFormatted = step.Text.Replace("<", "***").Replace(">", "***");

            var fullStep = keywordFormatted + stepFormatted;

            if (step.Argument is DocString docString)
            {
                fullStep += Environment.NewLine + docString.Content;
            }

            if (step.Argument is DataTable table)
            {
                fullStep += Environment.NewLine + ConvertToStringTable(table.Rows.ToList());
            }

            resultSteps.Add(fullStep);
        }

        return resultSteps;
    }
    
    private string ConvertToStringTable(List<TableRow> tableRows)
    {
        var table = new StringBuilder();
        table.Append("||");

        //Header
        foreach (var cell in tableRows.First().Cells)
        {
            table.Append($"|:{cell.Value}");
        }

        table.AppendLine();

        //Table body
        for (int i = 1; i < tableRows.Count; i++)
        {
            table.Append("|");

            var row = tableRows[i];
            foreach (var cell in row.Cells)
            {
                table.Append($"|{cell.Value}");
            }

            table.AppendLine();
        }

        return table.ToString();
    }
}