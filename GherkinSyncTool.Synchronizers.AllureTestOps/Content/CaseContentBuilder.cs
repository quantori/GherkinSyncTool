using System;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.AllureTestOps.Model;
using Quantori.AllureTestOpsClient.Model;
using Scenario = Gherkin.Ast.Scenario;

namespace GherkinSyncTool.Synchronizers.AllureTestOps.Content;

public class CaseContentBuilder
{
    private readonly AllureTestOpsSettings _allureTestOpsSettings =
        ConfigurationManager.GetConfiguration<AllureTestOpsConfigs>().AllureTestOpsSettings;

    public CreateTestCaseRequest BuildCaseRequest(Scenario scenario, IFeatureFile featureFile)
    {
        var caseRequest = new CreateTestCaseRequest
        {
            Name = scenario.Name,
            ProjectId = _allureTestOpsSettings.ProjectId,
            Automated = IsAutomated(scenario, featureFile)
        };

        return caseRequest;
    }

    private bool IsAutomated(Scenario scenario, IFeatureFile featureFile)
    {
        var allTags = GherkinHelper.GetAllTags(scenario, featureFile);
        return allTags.Exists(tag => tag.Name.Contains(TagsConstants.Automated, StringComparison.InvariantCultureIgnoreCase));
    }
}