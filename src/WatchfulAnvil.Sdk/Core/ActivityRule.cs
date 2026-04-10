using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Core;

/// <summary>Base class for rules that inspect individual activities.</summary>
public abstract class ActivityRule : ScopedRule<IActivityModel>
{
}
