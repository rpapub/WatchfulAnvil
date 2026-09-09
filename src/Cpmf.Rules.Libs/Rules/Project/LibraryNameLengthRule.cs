// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Project
{
    public class LibraryNameLengthRule : ProjectRule
    {
        private const int MaxLength = 128;

        protected override string Id => "CPMF-N004";
        protected override string Name => "Library Name Length";
        protected override string[] TargetOutputTypes => new[] { "Library" };
        protected override string Recommendation =>
            $"Shorten the project name to {MaxLength} characters or fewer. UiPath enforces this limit for library projects.";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-N004";

        protected override InspectionResult Inspect(IProjectModel model, Rule rule)
        {
            var ctx = ProjectContext.From(model);
            if (ctx.Name.Length > MaxLength)
                return Fail(rule, $"Project name '{ctx.Name}' is {ctx.Name.Length} characters; maximum is {MaxLength}.");
            return Pass();
        }
    }
}
