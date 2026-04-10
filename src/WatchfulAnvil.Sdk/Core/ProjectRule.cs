using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Core;

/// <summary>Base class for rules that inspect the full project model.</summary>
public abstract class ProjectRule : ScopedRule<IProjectModel>
{
}
