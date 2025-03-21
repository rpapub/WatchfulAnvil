using System;
using System.Diagnostics;
using System.Threading;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.LocalizationResources;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Noop
{
    public class WaitBeforeExecutionRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPM_NOOP_004";

        public Rule<IProjectModel> Get()
        {
            return new Rule<IProjectModel>(Strings.CPM_WAIT_001_Name, RuleId, InspectProject)
            {
                RecommendationMessage = Strings.CPM_WAIT_001_Recommendation,
                DefaultErrorLevel = TraceLevel.Info,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPM-NOOP-004",
                DefaultIsEnabled = false
            };
        }

        public void Initialize(IAnalyzerConfigurationService workflowAnalyzerConfigService)
        {
            workflowAnalyzerConfigService.AddRule<IProjectModel>(Get());
        }

        private InspectionResult InspectProject(IProjectModel project, Rule configuredRule)
        {
            Console.WriteLine("Waiting 10 seconds for debugger attachment...");
            Thread.Sleep(10000); // Wait for 10 seconds
            Console.WriteLine("Continuing execution...");

            return new InspectionResult { HasErrors = false };
        }
    }
}
