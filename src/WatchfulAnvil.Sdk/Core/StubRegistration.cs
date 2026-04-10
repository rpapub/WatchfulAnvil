// Compiled for net461 only — see WatchfulAnvil.Sdk.csproj.
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Core
{
    internal sealed class StubRegistration<T> : IRegisterAnalyzerConfiguration
        where T : IInspectionObject
    {
        private readonly string _id;
        private readonly string _name;
        private readonly string _recommendation;

        internal StubRegistration(string id, string name, string recommendation)
        {
            _id = id;
            _name = name;
            _recommendation = recommendation;
        }

        public void Initialize(IAnalyzerConfigurationService api)
        {
            var rule = new Rule<T>(_name, _id,
                (model, r) => new InspectionResult { HasErrors = false });
            rule.RecommendationMessage = _recommendation;
            api.AddRule<T>(rule);
        }
    }
}
