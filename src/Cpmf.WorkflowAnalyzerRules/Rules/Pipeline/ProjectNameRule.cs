using System.Collections.Generic;
using System.Diagnostics;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.Rules.Pipeline
{
    public class ProjectNameRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPMF-PLN-003";

        public void Initialize(IAnalyzerConfigurationService api)
        {
            // IInspectionObject.DisplayName is available from WorkflowAnalyzerV4 (sdk-capabilities: 20.4.0+).
            api.AddRule<IProjectModel>(Get());
        }

        public Rule<IProjectModel> Get() =>
            new Rule<IProjectModel>("Project Name Is Valid .NET Identifier", RuleId, Inspect)
            {
                RecommendationMessage =
                    "The project name must be a valid .NET PascalCase identifier: " +
                    "start with an uppercase letter, contain only letters, digits, and underscores, " +
                    "and not start with a digit. The project name is used as the root namespace.",
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-PLN-003"
            };

        private static InspectionResult Inspect(IProjectModel project, Rule rule)
        {
            var name = project.DisplayName;
            var error = DotNetIdentifierValidator.Validate(name);

            if (error == null)
                return new InspectionResult { HasErrors = false };

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = rule.RecommendationMessage,
                Messages = new List<string> { $"Project name validation failed: {error}" },
                ErrorLevel = rule.DefaultErrorLevel
            };
        }
    }
}
