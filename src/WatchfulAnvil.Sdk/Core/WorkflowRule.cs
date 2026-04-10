using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Core;

/// <summary>Base class for rules that inspect a single workflow file.</summary>
public abstract class WorkflowRule : ScopedRule<IWorkflowModel>
{
}
