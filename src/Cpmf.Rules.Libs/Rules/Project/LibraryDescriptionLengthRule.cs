// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Diagnostics;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Project
{
    public class LibraryDescriptionLengthRule : ProjectRule
    {
        private const int MaxLength = 500;

        protected override string Id => "CPMF-N006";
        protected override string Name => "Library Description Length";
        protected override TraceLevel DefaultSeverity => System.Diagnostics.TraceLevel.Warning;
        protected override string[] TargetOutputTypes => new[] { "Library" };
        protected override string Recommendation =>
            $"Shorten the project description to {MaxLength} characters or fewer. UiPath enforces this limit for library projects.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-N006";

        protected override InspectionResult Inspect(IProjectModel model, Rule rule)
        {
            var ctx = ProjectContext.From(model);
            var desc = ctx.Description ?? string.Empty;
            if (desc.Length > MaxLength)
                return Fail(rule, $"Project description is {desc.Length} characters; maximum is {MaxLength}.");
            return Pass();
        }
    }
}
