using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Core;

/// <summary>Base class for rules that inspect the project summary.</summary>
public abstract class SummaryRule : ScopedRule<IProjectSummary>
{
}
