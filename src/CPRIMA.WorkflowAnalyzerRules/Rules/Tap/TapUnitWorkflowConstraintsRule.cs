using System;
using System.Collections.Generic;
using System.Linq;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Analyzer.Models;
using CPRIMA.WorkflowAnalyzerRules.Common;
using System.Diagnostics;

public class TapUnitWorkflowConstraintsRule : IRegisterAnalyzerConfiguration
{
    private const string RuleId = "CPRIMA-TAP-UNITCONSTRAINTS-001";

    public void Initialize(IAnalyzerConfigurationService config)
    {
        RuleLogger.LogAndReturn("AnalyzerInit", "TapUnitWorkflowConstraintsRule registered.");
        config.AddRule<IWorkflowModel>(Get());
    }

    public Rule<IWorkflowModel> Get() =>
        new Rule<IWorkflowModel>("Unit Workflow Constraints", RuleId, InspectWorkflow)
        {
            RecommendationMessage = "Workflows tagged with @unit must only have primitive input arguments and exactly one of: (1) output 'Result' (Dictionary<string, object>), or (2) InOut 'TransactionItem' (QueueItem).",
            DefaultErrorLevel = TraceLevel.Error
        };

    private InspectionResult InspectWorkflow(IWorkflowModel workflow, Rule rule)
    {
        RuleLogger.LogAndReturn("UnitWorkflowCheck", $"Checking workflow: {workflow.DisplayName}");

        if (workflow.Root == null)
        {
            RuleLogger.LogAndReturn("UnitWorkflowCheck", "Root is null — skipping.");
            return new InspectionResult { HasErrors = false };
        }

        RuleLogger.LogAndReturn("UnitWorkflowCheck", $"AnnotationText: {workflow.Root.AnnotationText}");

        if (string.IsNullOrWhiteSpace(workflow.Root.AnnotationText) || !workflow.Root.AnnotationText.Contains("@unit"))
        {
            RuleLogger.LogAndReturn("UnitWorkflowCheck", "Not tagged with @unit — skipping.");
            return new InspectionResult { HasErrors = false };
        }

        var messages = new List<string>();

        // Step 1: Composite input arguments
        var inputArgs = workflow.Arguments.Where(arg => arg.Direction == ArgumentDirection.In).ToList();
        RuleLogger.LogAndReturn("UnitWorkflowCheck", $"In arguments count: {inputArgs.Count}");

        var nonPrimitives = inputArgs.Where(arg => IsCompositeType(arg.Type)).ToList();

        foreach (var arg in nonPrimitives)
        {
            string msg = $"@unit workflow '{workflow.DisplayName}' has non-primitive input argument '{arg.DisplayName}' of type '{arg.Type}'";
            RuleLogger.LogAndReturn("CompositeInput", msg);
            messages.Add(msg);
        }

        // Step 2: Output mode checks
        var resultArg = workflow.Arguments.FirstOrDefault(arg =>
            arg.DisplayName == "Result" &&
            arg.Direction == ArgumentDirection.Out &&
            IsDictionaryOfStringObject(arg.Type));

        var transactionArg = workflow.Arguments.FirstOrDefault(arg =>
            arg.DisplayName == "TransactionItem" &&
            arg.Direction == ArgumentDirection.InOut &&
            arg.Type == "UiPath.Core.QueueItem");

        RuleLogger.LogAndReturn("OutputCheck", $"HasResult: {resultArg != null}");
        RuleLogger.LogAndReturn("OutputCheck", $"HasTransactionItem: {transactionArg != null}");

        if (resultArg != null && transactionArg != null)
        {
            string msg = $"@unit workflow '{workflow.DisplayName}' must not define both 'Result' and 'TransactionItem'. Only one is allowed.";
            RuleLogger.LogAndReturn("Conflict", msg);
            messages.Add(msg);
        }
        else if (resultArg == null && transactionArg == null)
        {
            string msg = $"@unit workflow '{workflow.DisplayName}' must define exactly one of: (1) input 'Result' of type Dictionary<string, object>, or (2) InOut 'TransactionItem' of type UiPath.Core.QueueItem.";
            RuleLogger.LogAndReturn("MissingOutput", msg);
            messages.Add(msg);
        }

        // Step 3: Optional 'Status' output
        var statusArg = workflow.Arguments.FirstOrDefault(arg =>
            arg.DisplayName == "Status" &&
            arg.Direction == ArgumentDirection.Out);

        if (statusArg != null)
        {
            RuleLogger.LogAndReturn("StatusCheck", $"Status type: {statusArg.Type}");

            if (!IsDictionaryOfStringObject(statusArg.Type))
            {
                string msg = $"@unit workflow '{workflow.DisplayName}' has a 'Status' argument that must be of type Dictionary<string, object>.";
                RuleLogger.LogAndReturn("StatusInvalidType", msg);
                messages.Add(msg);
            }

            if (resultArg == null && transactionArg == null)
            {
                string msg = $"@unit workflow '{workflow.DisplayName}' includes 'Status' without defining 'Result' or 'TransactionItem'. 'Status' is only valid as a secondary output.";
                RuleLogger.LogAndReturn("StatusWithoutPrimary", msg);
                messages.Add(msg);
            }
        }

        if (messages.Any())
        {
            var combined = rule.RecommendationMessage + " — " + string.Join(" | ", messages);

            return new InspectionResult
            {
                HasErrors = true,
                RecommendationMessage = combined,
                ErrorLevel = rule.DefaultErrorLevel
            };
        }

        return new InspectionResult { HasErrors = false };
    }

    private bool IsDictionaryOfStringObject(string type)
    {
        return type == "System.Collections.Generic.Dictionary<System.String, System.Object>";
    }

    private bool IsCompositeType(string type)
    {
        if (string.IsNullOrWhiteSpace(type)) return false;

        var primitives = new[]
        {
            "System.String", "System.Boolean", "System.Int32", "System.Double", "System.DateTime"
        };

        return !primitives.Contains(type);
    }
}
