using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Core;

/// <summary>
/// Base class for Rule&lt;T&gt; implementations.
/// Handles registration via api.AddRule&lt;T&gt;() with optional feature gating.
/// </summary>
public abstract class RuleBase<T> : AnalyzerBase where T : IInspectionObject
{
    public abstract Rule<T> Get();

    public override void Initialize(IAnalyzerConfigurationService api)
    {
        if (!IsFeatureAvailable(api))
            return;

        api.AddRule<T>(Get());
    }
}
