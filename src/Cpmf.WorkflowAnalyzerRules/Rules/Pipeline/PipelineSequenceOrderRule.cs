using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.Rules.Pipeline
{
    public class PipelineSequenceOrderRule : IRegisterAnalyzerConfiguration
    {
        private const string RuleId = "CPMF-PLN-001";
        private const string StagesKey = "Stages";
        private const string DefaultStages = "Initialize,Ingest,Enrich,Decide,Execute,Complete,Finalize";

        public void Initialize(IAnalyzerConfigurationService api)
        {
            // AnnotationText requires WorkflowAnalyzerV9 (sdk-capabilities: 21.4.1+).
            if (!api.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9))
                return; // Studio < 21.4 — rule cannot function without AnnotationText.

            api.AddRule<IWorkflowModel>(Get());
        }

        public Rule<IWorkflowModel> Get()
        {
            var rule = new Rule<IWorkflowModel>("Pipeline Structure", RuleId, Inspect)
            {
                RecommendationMessage =
                    "Workflows annotated @pipeline must: " +
                    "(1) declare an In argument 'in_TransactionItem' of type UiPath.Core.QueueItem; " +
                    "(2) contain the configured pipeline stages as direct children of the root, in that order.",
                DefaultErrorLevel = TraceLevel.Error,
                DocumentationLink = "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-PLN-001"
            };
            rule.Parameters.Add(StagesKey, new Parameter
            {
                Key = StagesKey,
                DefaultValue = DefaultStages,
                Value = DefaultStages,
                LocalizedDisplayName = "Pipeline Stages (ordered, comma-separated)"
            });
            return rule;
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

        private static InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            if (workflow.Root == null)
                return new InspectionResult { HasErrors = false };

            var annotation = workflow.Root.AnnotationText;
            if (string.IsNullOrWhiteSpace(annotation) || !annotation.Contains("@pipeline"))
                return new InspectionResult { HasErrors = false };

            var expectedOrder = ParseStages(rule);
            var expectedSet = new HashSet<string>(expectedOrder);

            // Check in_TransactionItem argument
            var transactionArg = workflow.Arguments == null ? null :
                System.Linq.Enumerable.FirstOrDefault(workflow.Arguments, a =>
                    a.DisplayName == "in_TransactionItem" &&
                    a.Direction == ArgumentDirection.In &&
                    a.Type == "UiPath.Core.QueueItem");

            var messages = new List<string>();

            if (transactionArg == null)
                messages.Add("Missing required In argument 'in_TransactionItem' of type UiPath.Core.QueueItem.");

            // Direct children that match a pipeline stage name, preserving document order
            var pipelineChildren = (workflow.Root.Children ?? (IEnumerable<IActivityModel>)new IActivityModel[0])
                .Where(c => expectedSet.Contains(c.DisplayName))
                .Select(c => c.DisplayName)
                .ToList();

            // Missing stages
            foreach (var stage in expectedOrder)
            {
                if (!pipelineChildren.Contains(stage))
                    messages.Add($"Missing required pipeline stage: '{stage}'.");
            }

            // Order check
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
    }
}
