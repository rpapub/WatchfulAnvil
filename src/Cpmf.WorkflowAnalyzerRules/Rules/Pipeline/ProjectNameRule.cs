using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Pipeline
{
    public class ProjectNameRule : ProjectRule
    {
        protected override string Id => "CPMF-PLN-003";
        protected override string Name => "Project Name Is Valid .NET Identifier";
        protected override string Recommendation =>
            "The project name must be a valid .NET PascalCase identifier: " +
            "start with an uppercase letter, contain only letters, digits, and underscores, " +
            "and not start with a digit. The project name is used as the root namespace.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-PLN-003";

        protected override InspectionResult Inspect(IProjectModel project, Rule rule)
        {
            var name = project.DisplayName;
            var error = DotNetIdentifierValidator.Validate(name);

            if (error == null)
                return new InspectionResult { HasErrors = false };

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = rule.RecommendationMessage,
                Messages = new System.Collections.Generic.List<string>
                {
                    $"Project name validation failed: {error}"
                },
                ErrorLevel = rule.DefaultErrorLevel
            };
        }
    }
}
