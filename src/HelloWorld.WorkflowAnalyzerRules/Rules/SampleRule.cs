using System.Diagnostics;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Core;

namespace ACME.HelloWorld.WorkflowAnalyzerRules.Rules.Sample
{
    public class SampleRule : ActivityRule
    {
        protected override string Id => "HWR-SAMPLE-001";
        protected override string Name => "Sample rule title";
        protected override string Recommendation => "Replace this sample rule with a real implementation.";
        protected override TraceLevel DefaultSeverity => TraceLevel.Info;
        protected override string? DocumentationLink => "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-HWR-SAMPLE-001";

        protected override InspectionResult Inspect(IActivityModel activity, Rule rule)
        {
            return new InspectionResult { HasErrors = false };
        }
    }
}
