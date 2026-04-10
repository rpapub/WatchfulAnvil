// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
using System.Collections.Generic;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using WatchfulAnvil.Sdk.Common;
using WatchfulAnvil.Sdk.Core;

namespace Cpmf.Rules.Pipeline
{
    public class PipelineDomainModelRule : WorkflowRule
    {
        private const string DomainModelKey = "@domain-model";

        protected override string Id => "CPMF-F003";
        protected override string Name => "Pipeline Domain Model";
        protected override string? RequiredFeature => DesignFeatureKeys.WorkflowAnalyzerV9;
        protected override string Recommendation =>
            "Workflows annotated @pipeline must declare their domain model on a second annotation line: " +
            "@domain-model:FullyQualifiedTypeName";
        protected override string? DocumentationLink =>
            "https://github.com/rpapub/WatchfulAnvil/wiki/Rule-Documentation-CPMF-F003";
        protected override string[] RequiresAnyTag => new[] { "@pipeline" };

        protected override InspectionResult Inspect(IWorkflowModel workflow, Rule rule)
        {
            if (workflow.Root == null)
            {
                return new InspectionResult { HasErrors = false };
            }

            var annotation = workflow.Root.AnnotationText;
            var typeName = AnnotationReader.GetTagValue(annotation, DomainModelKey);
            var messages = new List<string>();

            if (typeName == null)
            {
                messages.Add(
                    $"Workflow annotated @pipeline is missing the required '@domain-model:TypeName' " +
                    $"annotation line. Add '@domain-model:FullyQualifiedTypeName' as a second line " +
                    $"of the workflow annotation.");
            }
            else if (string.IsNullOrWhiteSpace(typeName))
            {
                messages.Add(
                    $"The '@domain-model:' annotation is present but the TypeName is empty. " +
                    $"Specify the fully qualified type name, e.g. '@domain-model:MyNamespace.MyDomainModel'.");
            }

            if (messages.Count > 0)
            {
                return new InspectionResult
                {
                    HasErrors = true,
                    RecommendationMessage = rule.RecommendationMessage,
                    Messages = messages,
                    ErrorLevel = rule.DefaultErrorLevel,
                };
            }

            return new InspectionResult { HasErrors = false };
        }
    }
}
