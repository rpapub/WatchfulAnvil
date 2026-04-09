using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.Rules.Workflow
{
    public class WorkflowNameRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPMF-WFL-007";

        public void Initialize(IAnalyzerConfigurationService api)
        {
            // IFileModel.RelativePath is available from WorkflowAnalyzerV4 (sdk-capabilities: 20.4.0+).
            api.AddRule<IWorkflowModel>(Get());
        }

        public Rule<IWorkflowModel> Get() =>
            new Rule<IWorkflowModel>("Workflow Filename Is Valid .NET Identifier", RuleId, Inspect)
            {
                RecommendationMessage =
                    "The workflow filename (without extension) must be a valid .NET PascalCase identifier: " +
                    "start with an uppercase letter, contain only letters, digits, and underscores, " +
                    "and not start with a digit. " +
                    "UiPath Coded Workflows generate a partial base class named after the filename.",
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-WFL-007"
            };

        private static InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            var relativePath = workflow.RelativePath;
            if (string.IsNullOrWhiteSpace(relativePath))
                return new InspectionResult { HasErrors = false };

            var stem = Path.GetFileNameWithoutExtension(relativePath);
            var error = DotNetIdentifierValidator.Validate(stem);

            if (error == null)
                return new InspectionResult { HasErrors = false };

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = rule.RecommendationMessage,
                Messages = new List<string>
                {
                    $"Workflow filename validation failed for '{relativePath}': {error}"
                },
                ErrorLevel = rule.DefaultErrorLevel
            };
        }
    }
}
