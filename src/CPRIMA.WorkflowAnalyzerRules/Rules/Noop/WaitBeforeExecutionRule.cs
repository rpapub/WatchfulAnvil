using System;
using System.Diagnostics;
using System.Threading;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.LocalizationResources;
using WatchfulAnvil.Sdk.Core;

namespace CPRIMA.WorkflowAnalyzerRules.Rules.Noop
{
    public class WaitBeforeExecutionRule : ProjectRule
    {
        protected override string Id => "CPRIMA-NOOP-004";
        protected override string Name => Strings.CPRIMA_WAIT_001_Name;
        protected override string Recommendation => Strings.CPRIMA_WAIT_001_Recommendation;
        protected override TraceLevel DefaultSeverity => TraceLevel.Info;
        protected override string? DocumentationLink => "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPRIMA-NOOP-004";

        protected override void ConfigureParameters(Rule<IProjectModel> rule)
        {
            rule.DefaultIsEnabled = false;
        }

        protected override InspectionResult Inspect(IProjectModel project, Rule rule)
        {
            Console.WriteLine("Waiting 10 seconds for debugger attachment...");
            Thread.Sleep(10000); // Wait for 10 seconds
            Console.WriteLine("Continuing execution...");

            return new InspectionResult { HasErrors = false };
        }
    }
}
