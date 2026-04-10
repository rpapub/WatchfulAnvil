using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Workflow
{
    public class ModuleCodedConfigRule : WorkflowRule
    {
        private const string TypeSimpleName = "CodedConfig";

        protected override string Id => "CPMF-WFL-003";
        protected override string Name => "CodedConfig Argument";
        protected override string? RequiredFeature => DesignFeatureKeys.WorkflowAnalyzerV9;
        protected override string Recommendation =>
            "Workflows annotated @module or @pipeline must declare an In argument of type CodedConfig " +
            "(namespace may vary, e.g. MyProject.Config.CodedConfig).";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-003";

        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            if (workflow.Root == null)
                return new InspectionResult { HasErrors = false };

            var annotation = workflow.Root.AnnotationText;
            var isModule = AnnotationReader.HasTag(annotation, "@module");
            var isPipeline = AnnotationReader.HasTag(annotation, "@pipeline");
            if (!isModule && !isPipeline)
                return new InspectionResult { HasErrors = false };

            var hasCodedConfig = workflow.Arguments != null &&
                System.Linq.Enumerable.Any(workflow.Arguments, a =>
                    a.Direction == ArgumentDirection.In &&
                    IsCodedConfigType(a.Type));

            if (hasCodedConfig)
                return new InspectionResult { HasErrors = false };

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = rule.RecommendationMessage,
                Messages = new System.Collections.Generic.List<string>
                {
                    $"Workflow annotated @module is missing a required In argument of type '{TypeSimpleName}'. " +
                    $"The namespace may vary (e.g. MyProject.Config.{TypeSimpleName})."
                },
                ErrorLevel = rule.DefaultErrorLevel
            };
        }

        private static bool IsCodedConfigType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return false;
            var dot = type.LastIndexOf('.');
            var simpleName = dot >= 0 ? type.Substring(dot + 1) : type;
            return simpleName == TypeSimpleName;
        }
    }
}
