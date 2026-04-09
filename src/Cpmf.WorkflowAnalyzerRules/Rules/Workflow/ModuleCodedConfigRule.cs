using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.Rules.Workflow
{
    public class ModuleCodedConfigRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPMF-WFL-003";
        private const string TypeSimpleName = "CodedConfig";

        public void Initialize(IAnalyzerConfigurationService api)
        {
            // AnnotationText requires WorkflowAnalyzerV9 (sdk-capabilities: 21.4.1+).
            if (!api.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9))
                return; // Studio < 21.4 — rule cannot function without AnnotationText.

            api.AddRule<IWorkflowModel>(Get());
        }

        public Rule<IWorkflowModel> Get() =>
            new Rule<IWorkflowModel>("CodedConfig Argument", RuleId, Inspect)
            {
                RecommendationMessage =
                    "Workflows annotated @module or @pipeline must declare an In argument of type CodedConfig " +
                    "(namespace may vary, e.g. MyProject.Config.CodedConfig).",
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-003"
            };

        private static InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            if (workflow.Root == null)
                return new InspectionResult { HasErrors = false };

            var annotation = workflow.Root.AnnotationText;
            var isModule = annotation.Contains("@module");
            var isPipeline = annotation.Contains("@pipeline");
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
                Messages = new List<string>
                {
                    $"Workflow annotated @module is missing a required In argument of type '{TypeSimpleName}'. " +
                    $"The namespace may vary (e.g. MyProject.Config.{TypeSimpleName})."
                },
                ErrorLevel = rule.DefaultErrorLevel
            };
        }

        /// <summary>
        /// Matches any type whose simple name is "CodedConfig", regardless of namespace.
        /// Examples that match: "CodedConfig", "MyProject.Config.CodedConfig"
        /// </summary>
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
