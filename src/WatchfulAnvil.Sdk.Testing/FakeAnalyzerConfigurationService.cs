// <copyright file="FakeAnalyzerConfigurationService.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;

using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Testing
{
    /// <summary>
    /// Recording stand-in for <see cref="IAnalyzerConfigurationService"/>. Captures what a
    /// rule or a pack's registration class registers, so registration can be asserted
    /// without deploying to Studio.
    /// </summary>
    /// <remarks>
    /// Hand-written rather than mocked: the recording behaviour is the whole point, and a
    /// mock setup for it is more code than the class.
    ///
    /// Counters are recorded separately from rules. They are a distinct analyzer concept
    /// and a counter routed into the rules collection is exactly the kind of mistake worth
    /// catching in a test.
    /// </remarks>
    public sealed class FakeAnalyzerConfigurationService : IAnalyzerConfigurationService
    {
        private readonly List<Rule> _rules = new List<Rule>();
        private readonly List<object> _counters = new List<object>();
        private readonly List<object> _metadata = new List<object>();

        /// <summary>Creates a service that reports every feature as available.</summary>
        public FakeAnalyzerConfigurationService()
        {
        }

        /// <summary>
        /// Creates a service whose feature availability is decided by <paramref name="hasFeature"/>.
        /// </summary>
        /// <remarks>
        /// Feature gates silently skip registration when the feature is absent, so a rule
        /// gated on a level the host does not offer simply never appears. Passing a
        /// predicate here is the only way to test that path — with the default, a gate can
        /// never suppress anything and the behaviour is untestable.
        /// </remarks>
        public FakeAnalyzerConfigurationService(Func<string, bool> hasFeature)
        {
            FeaturePredicate = hasFeature ?? throw new ArgumentNullException(nameof(hasFeature));
        }

        /// <summary>Rules captured via <see cref="AddRule{T}"/>, in registration order.</summary>
        public IReadOnlyList<Rule> Rules => _rules;

        /// <summary>Counters captured via <see cref="AddCounter{T}"/>, in registration order.</summary>
        public IReadOnlyList<object> Counters => _counters;

        /// <summary>Metadata captured via <see cref="AddMetadata{T}"/>, in registration order.</summary>
        public IReadOnlyList<object> Metadata => _metadata;

        /// <summary>Decides <see cref="HasFeature"/>. Defaults to every feature available.</summary>
        public Func<string, bool> FeaturePredicate { get; set; } = _ => true;

        /// <summary>Value reported as the active profile. Defaults to <c>"Studio"</c>.</summary>
        public string ActiveProfile { get; set; } = "Studio";

        /// <summary>Ids of the captured rules, in registration order.</summary>
        public IReadOnlyList<string> RuleIds
        {
            get
            {
                var ids = new List<string>(_rules.Count);
                foreach (var rule in _rules)
                {
                    ids.Add(rule.Id);
                }

                return ids;
            }
        }

        public void AddRule<T>(Rule<T> rule)
            where T : IInspectionObject
            => _rules.Add(rule);

        public void AddCounter<T>(Counter<T> counter)
            where T : IInspectionObject
            => _counters.Add(counter);

        public void AddMetadata<T>(Metadata<T> metadata)
            where T : IInspectionObject
            => _metadata.Add(metadata);

        public bool HasFeature(string featureKey) => FeaturePredicate(featureKey);
    }
}
