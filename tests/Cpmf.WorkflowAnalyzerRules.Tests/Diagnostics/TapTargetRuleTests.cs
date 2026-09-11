// <copyright file="TapTargetRuleTests.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System.Diagnostics;
using System.Linq;

using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;

using WatchfulAnvil.Sdk.Diagnostics;
using WatchfulAnvil.Sdk.Testing;

using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Diagnostics
{
    /// <summary>
    /// Exercises <see cref="TapTargetRule"/> through the same registration path the analyzer
    /// host uses, then invokes the registered inspector directly.
    /// </summary>
    /// <remarks>
    /// The rule is the developer-facing half of the diagnostics story, so "does it fire on an
    /// annotated activity" has to be answerable without standing up uipcli. An end-to-end run
    /// cannot distinguish a rule that never registered from one that registered and passed;
    /// this can.
    /// </remarks>
    /// <remarks>
    /// In the RunContext collection because the @tap:dump case writes a real record to the
    /// process-lifetime run directory that JsonlWriterTests counts lines in. Run in
    /// parallel, that test sees two appended lines where it made one call.
    /// </remarks>
    [Collection("RunContext")]
    public sealed class TapTargetRuleTests
    {
        private static Rule<IActivityModel> Registered()
        {
            var api = new FakeAnalyzerConfigurationService();
            new TapTargetRule().Initialize(api);
            return Assert.IsType<Rule<IActivityModel>>(
                Assert.Single(api.Rules.Where(r => r.Id == "CPMF-TAP-TGT-001")));
        }

        [Fact]
        public void Initialize_RegistersTheRule()
        {
            var rule = Registered();
            Assert.Equal("Tap Target (Diagnostics)", rule.Name);
            Assert.Equal(TraceLevel.Info, rule.DefaultErrorLevel);
        }

        [Fact]
        public void Rule_IsEnabledByDefault()
        {
            // The @tap annotation is the opt-in; a disabled-by-default rule would need a
            // governance policy before it could answer the question it exists to answer.
            Assert.True(Registered().DefaultIsEnabled);
        }

        [Fact]
        public void Inspect_ActivityWithoutTapAnnotation_Passes()
        {
            var rule = Registered();
            var result = rule.Inspect(
                Fake.Activity(toolboxName: "Log Message", annotation: "@unit"), rule);
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void Inspect_ActivityWithTapAnnotation_Reports()
        {
            var rule = Registered();
            var result = rule.Inspect(
                Fake.Activity(toolboxName: "Kill Process", annotation: "@tap"), rule);

            Assert.True(result.HasErrors);
            Assert.Equal(TraceLevel.Info, result.ErrorLevel);
            Assert.Contains(result.Messages, m => m.Contains("Kill Process"));
        }

        [Fact]
        public void Inspect_TapDumpValue_StillReports()
        {
            var rule = Registered();
            var result = rule.Inspect(
                Fake.Activity(toolboxName: "Kill Process", annotation: "@tap:dump"), rule);
            Assert.True(result.HasErrors);
        }
    }
}
