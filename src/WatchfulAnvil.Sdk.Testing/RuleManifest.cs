// <copyright file="RuleManifest.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer.Rules;

namespace WatchfulAnvil.Sdk.Testing
{
    /// <summary>
    /// Compares what a pack's central registration class actually registers against what
    /// its rule classes declare, so a rule that exists but was never wired up is caught.
    /// </summary>
    /// <remarks>
    /// The expected set is discovered by reflection rather than hardcoded, so adding a
    /// rule updates both sides at once and only *forgetting to register it* fails.
    ///
    /// Returns plain lists and throws nothing on mismatch: the assertion library is the
    /// consumer's choice, so this package takes no test-framework dependency.
    /// </remarks>
    public static class RuleManifest
    {
        /// <summary>
        /// Rules registered by the pack's central <see cref="IRegisterAnalyzerConfiguration"/>.
        /// </summary>
        public static IReadOnlyList<Rule> Registered(IRegisterAnalyzerConfiguration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            var service = new FakeAnalyzerConfigurationService();
            registration.Initialize(service);
            return service.Rules;
        }

        /// <summary>
        /// Rules declared by every individual rule class in the same assembly as
        /// <paramref name="registration"/>, found by reflection.
        /// </summary>
        /// <param name="registration">The pack's central registration class. Excluded from the scan.</param>
        /// <param name="namespacePrefix">
        /// Restricts the scan to rule classes under this namespace. Required: without it the
        /// scan also picks up rule types the pack merely references — anything in the SDK,
        /// for instance — and the comparison becomes meaningless.
        /// </param>
        public static IReadOnlyList<Rule> Declared(
            IRegisterAnalyzerConfiguration registration,
            string namespacePrefix)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            if (string.IsNullOrWhiteSpace(namespacePrefix))
            {
                throw new ArgumentException(
                    "A namespace prefix is required, otherwise referenced assemblies' rules are swept in too.",
                    nameof(namespacePrefix));
            }

            var rootType = registration.GetType();
            var service = new FakeAnalyzerConfigurationService();

            var ruleTypes = rootType.Assembly.GetTypes()
                .Where(t =>
                    !t.IsAbstract &&
                    typeof(IRegisterAnalyzerConfiguration).IsAssignableFrom(t) &&
                    t != rootType &&
                    t.Namespace != null &&
                    t.Namespace.StartsWith(namespacePrefix, StringComparison.Ordinal) &&
                    t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var type in ruleTypes)
            {
                var instance = (IRegisterAnalyzerConfiguration)Activator.CreateInstance(type);
                instance.Initialize(service);
            }

            return service.Rules;
        }

        /// <summary>Sorted ids of <see cref="Registered"/>. Compare against <see cref="DeclaredIds"/>.</summary>
        public static IReadOnlyList<string> RegisteredIds(IRegisterAnalyzerConfiguration registration)
            => Registered(registration).Select(r => r.Id).OrderBy(id => id, StringComparer.Ordinal).ToList();

        /// <summary>Sorted ids of <see cref="Declared"/>. Compare against <see cref="RegisteredIds"/>.</summary>
        public static IReadOnlyList<string> DeclaredIds(
            IRegisterAnalyzerConfiguration registration,
            string namespacePrefix)
            => Declared(registration, namespacePrefix).Select(r => r.Id).OrderBy(id => id, StringComparer.Ordinal).ToList();

        /// <summary>
        /// Rule ids declared by a rule class but never registered — the failure this exists
        /// to catch. Empty when the manifest is complete.
        /// </summary>
        public static IReadOnlyList<string> Unregistered(
            IRegisterAnalyzerConfiguration registration,
            string namespacePrefix)
        {
            var registered = new HashSet<string>(RegisteredIds(registration), StringComparer.Ordinal);
            return DeclaredIds(registration, namespacePrefix).Where(id => !registered.Contains(id)).ToList();
        }
    }
}
