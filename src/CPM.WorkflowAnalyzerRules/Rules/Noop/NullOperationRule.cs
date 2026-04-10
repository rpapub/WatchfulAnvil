using System.Diagnostics;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPM.WorkflowAnalyzerRules.LocalizationResources;
using WatchfulAnvil.Sdk.Core;

namespace CPM.WorkflowAnalyzerRules.Rules.Noop
{
    public class NullOperationRule : ActivityRule
    {
        protected override string Id => "CPM-NOOP-000";
        protected override string Name => Strings.CPM_NOOP_000_Name;
        protected override string Recommendation => Strings.CPM_NOOP_000_Recommendation;
        protected override TraceLevel DefaultSeverity => TraceLevel.Info;
        protected override string? DocumentationLink => "https://github.com/rpapub/WatchfulAnvil";

        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            // This rule does nothing, always returns a successful check
            return new InspectionResult { HasErrors = false };
        }
    }
}
