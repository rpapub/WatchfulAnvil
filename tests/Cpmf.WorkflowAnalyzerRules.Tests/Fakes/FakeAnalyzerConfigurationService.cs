using System.Collections.Generic;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Fakes
{
    /// <summary>
    /// Hand-written fake (no mocking library) for IAnalyzerConfigurationService.
    /// Collects registered rules so tests can assert the manifest without deploying to Studio.
    /// HasFeature always returns true so feature gates do not suppress registration.
    /// </summary>
    internal sealed class FakeAnalyzerConfigurationService : IAnalyzerConfigurationService
    {
        private readonly List<Rule> _rules;

        public FakeAnalyzerConfigurationService(List<Rule> rules) => _rules = rules;

        public void AddRule<T>(Rule<T> rule) where T : IInspectionObject
            => _rules.Add(rule);

        public bool HasFeature(string featureKey) => true;

        public void AddCounter<T>(Counter<T> counter) where T : IInspectionObject { }
        public void AddMetadata<T>(Metadata<T> metadata) where T : IInspectionObject { }
        public string ActiveProfile => "Studio";
    }
}
