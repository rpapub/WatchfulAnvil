using System.Diagnostics;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.LocalizationResources;
using WatchfulAnvil.Sdk.Core;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Noop
{
    public class NullOperationWorkflowRule : WorkflowRule
    {
        protected override string Id => "CPRIMA-NOOP-002";
        protected override string Name => Strings.CPRIMA_NOOP_002_Name;
        protected override string Recommendation => Strings.CPRIMA_NOOP_002_Recommendation;
        protected override TraceLevel DefaultSeverity => TraceLevel.Info;
        protected override string? DocumentationLink => "https://github.com/rpapub/WatchfulAnvil";

        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            return new InspectionResult { HasErrors = false };
        }
    }
}
