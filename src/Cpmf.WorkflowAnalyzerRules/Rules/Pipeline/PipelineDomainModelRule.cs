using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.Rules.Pipeline
{
    public class PipelineDomainModelRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPMF-PLN-002";
        private const string DomainModelPrefix = "@domain-model:";

        public void Initialize(IAnalyzerConfigurationService api)
        {
            if (api.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9))
                api.AddRule<IWorkflowModel>(Get());
        }

        public Rule<IWorkflowModel> Get() =>
            new Rule<IWorkflowModel>("Pipeline Domain Model", RuleId, Inspect)
            {
                RecommendationMessage =
                    "Workflows annotated @pipeline must declare their domain model on a second annotation line: " +
                    "@domain-model:FullyQualifiedTypeName",
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-PLN-002"
            };

        private static InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            if (workflow.Root == null)
                return new InspectionResult { HasErrors = false };

            var annotation = workflow.Root.AnnotationText;
            if (string.IsNullOrWhiteSpace(annotation) || !annotation.Contains("@pipeline"))
                return new InspectionResult { HasErrors = false };

            var lines = annotation.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

            var domainModelLine = System.Linq.Enumerable.FirstOrDefault(lines,
                l => l.TrimStart().StartsWith(DomainModelPrefix));

            var messages = new List<string>();

            if (domainModelLine == null)
            {
                messages.Add(
                    $"Workflow annotated @pipeline is missing the required '@domain-model:TypeName' " +
                    $"annotation line. Add '@domain-model:FullyQualifiedTypeName' as a second line " +
                    $"of the workflow annotation.");
            }
            else
            {
                var typeName = domainModelLine.TrimStart().Substring(DomainModelPrefix.Length).Trim();
                if (string.IsNullOrWhiteSpace(typeName))
                    messages.Add(
                        $"The '@domain-model:' annotation is present but the TypeName is empty. " +
                        $"Specify the fully qualified type name, e.g. '@domain-model:MyNamespace.MyDomainModel'.");
            }

            if (messages.Count > 0)
                return new InspectionResult
                {
                    HasErrors = true,
                    RecommendationMessage = rule.RecommendationMessage,
                    Messages = messages,
                    ErrorLevel = rule.DefaultErrorLevel
                };

            return new InspectionResult { HasErrors = false };
        }
    }
}
