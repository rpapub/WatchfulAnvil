using System.IO;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Workflow
{
    public class WorkflowNameRule : WorkflowRule
    {
        protected override string Id => "CPMF-N002";
        protected override string Name => "Workflow Filename Is Valid .NET Identifier";
        protected override string Recommendation =>
            "The workflow filename (without extension) must be a valid .NET PascalCase identifier: " +
            "start with an uppercase letter, contain only letters, digits, and underscores, " +
            "and not start with a digit. " +
            "UiPath Coded Workflows generate a partial base class named after the filename.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-N002";

        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
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
                Messages = new System.Collections.Generic.List<string>
                {
                    $"Workflow filename validation failed for '{relativePath}': {error}"
                },
                ErrorLevel = rule.DefaultErrorLevel
            };
        }
    }
}
