using System.Collections.Generic;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Pipeline
{
    public class PipelineSequenceOrderRule : WorkflowRule
    {
        private const string StagesKey = "Stages";
        private const string DefaultStages = "Initialize,Ingest,Enrich,Decide,Execute,Complete,Finalize";

        protected override string Id => "CPMF-PLN-001";
        protected override string Name => "Pipeline Structure";
        protected override string? RequiredFeature => DesignFeatureKeys.WorkflowAnalyzerV9;
        protected override string Recommendation =>
            "Workflows annotated @pipeline must: " +
            "(1) declare an In argument 'in_TransactionItem' of type UiPath.Core.QueueItem; " +
            "(2) contain the configured pipeline stages as direct children of the root, in that order.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-PLN-001";

        protected override void ConfigureParameters(Rule<IWorkflowModel> rule)
        {
            rule.Parameters.Add(StagesKey, new Parameter
            {
                Key = StagesKey,
                DefaultValue = DefaultStages,
                Value = DefaultStages,
                LocalizedDisplayName = "Pipeline Stages (ordered, comma-separated)"
            });
        }

        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            if (workflow.Root == null)
                return new InspectionResult { HasErrors = false };

            if (!AnnotationReader.HasTag(workflow.Root.AnnotationText, "@pipeline"))
                return new InspectionResult { HasErrors = false };

            var expectedOrder = ParseStages(rule);
            var expectedSet = new HashSet<string>(expectedOrder);

            var transactionArg = workflow.Arguments == null ? null :
                System.Linq.Enumerable.FirstOrDefault(workflow.Arguments, a =>
                    a.DisplayName == "in_TransactionItem" &&
                    a.Direction == ArgumentDirection.In &&
                    a.Type == "UiPath.Core.QueueItem");

            var messages = new List<string>();

            if (transactionArg == null)
                messages.Add("Missing required In argument 'in_TransactionItem' of type UiPath.Core.QueueItem.");

            var pipelineChildren = (workflow.Root.Children ?? (IEnumerable<IActivityModel>)new IActivityModel[0])
                .Where(c => expectedSet.Contains(c.DisplayName))
                .Select(c => c.DisplayName)
                .ToList();

            foreach (var stage in expectedOrder)
            {
                if (!pipelineChildren.Contains(stage))
                    messages.Add($"Missing required pipeline stage: '{stage}'.");
            }

            var expectedPresent = expectedOrder.Where(e => pipelineChildren.Contains(e)).ToList();
            if (!pipelineChildren.SequenceEqual(expectedPresent))
            {
                messages.Add(
                    $"Pipeline stage order is incorrect. " +
                    $"Found: [{string.Join(", ", pipelineChildren)}]. " +
                    $"Expected order: [{string.Join(", ", expectedOrder)}].");
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

        private static string[] ParseStages(Rule rule)
        {
            var raw = rule.Parameters[StagesKey]?.Value;
            if (string.IsNullOrWhiteSpace(raw))
                raw = DefaultStages;
            return raw.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                      .Select(s => s.Trim())
                      .Where(s => s.Length > 0)
                      .ToArray();
        }
    }
}
