// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Project
{
    /// <summary>Asserts the project is configured as a Library output type.</summary>
    /// <remarks>
    /// No-op when the SDK filter is working correctly: TargetOutputTypes limits
    /// execution to Library projects, so IsLibrary is always true inside Inspect.
    /// The inner check is a belt-and-suspenders guard against filter bypasses.
    /// </remarks>
    public class IsLibraryRule : ProjectRule
    {
        protected override string Id => "CPMF-G001";
        protected override string Name => "Project Is Library";
        protected override string[] TargetOutputTypes => new[] { "Library" };
        protected override bool IsEnabledByDefault => false;
        protected override string Recommendation =>
            "This project is not configured as a Library output type. " +
            "Update the output type in project settings, or disable this rule.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-G001";

        protected override InspectionResult Inspect(IProjectModel model, Rule rule)
        {
            var ctx = ProjectContext.From(model);
            if (!ctx.IsLibrary)
                return Fail(rule, $"Project '{ctx.Name}' has output type '{ctx.OutputType ?? "unknown"}'; expected Library.");
            return Pass();
        }
    }
}
