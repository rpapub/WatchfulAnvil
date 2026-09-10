// <copyright file="Fake.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System.Collections.Generic;

using Moq;

using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Testing
{
    /// <summary>
    /// Builders for the analyzer model interfaces a rule inspects.
    /// </summary>
    /// <remarks>
    /// Returns the model interfaces rather than <c>Mock&lt;T&gt;</c>, so consumers need no
    /// mocking library of their own — Moq is an implementation detail here.
    ///
    /// Collections default to empty rather than null. Rules iterate them, and a null
    /// collection produces a NullReferenceException that looks like a rule bug when it is
    /// really a fixture bug. Pass null explicitly to exercise null handling.
    ///
    /// Note the trap this hides: an argument's NAME is exposed as
    /// <c>IArgumentModel.DisplayName</c>, not a <c>Name</c> property.
    /// </remarks>
    public static class Fake
    {
        /// <summary>An activity property, as read via <c>DefinedExpression</c>.</summary>
        public static IPropertyModel Property(string name, string expression)
        {
            var m = new Mock<IPropertyModel>();
            m.Setup(p => p.DisplayName).Returns(name);
            m.Setup(p => p.DefinedExpression).Returns(expression);
            return m.Object;
        }

        /// <summary>
        /// A workflow argument. <paramref name="name"/> populates <c>DisplayName</c>,
        /// which is where the analyzer API exposes an argument's name.
        /// </summary>
        public static IArgumentModel Argument(
            string name,
            ArgumentDirection direction = ArgumentDirection.In,
            string type = "String")
        {
            var m = new Mock<IArgumentModel>();
            m.Setup(a => a.DisplayName).Returns(name);
            m.Setup(a => a.Direction).Returns(direction);
            m.Setup(a => a.Type).Returns(type);
            return m.Object;
        }

        /// <summary>An activity, optionally annotated and with children.</summary>
        /// <param name="toolboxName">Matched by rules that identify activities by kind, e.g. "Log Message".</param>
        /// <param name="annotation">Raw annotation text, e.g. "@unit" or "@suppress:RULE-ID".</param>
        public static IActivityModel Activity(
            string toolboxName = null,
            string displayName = null,
            string annotation = null,
            IReadOnlyCollection<IPropertyModel> properties = null,
            IReadOnlyCollection<IActivityModel> children = null,
            IReadOnlyCollection<IArgumentModel> arguments = null)
        {
            var m = new Mock<IActivityModel>();
            m.Setup(a => a.ToolboxName).Returns(toolboxName);
            m.Setup(a => a.DisplayName).Returns(displayName ?? toolboxName);
            m.Setup(a => a.AnnotationText).Returns(annotation);
            m.Setup(a => a.Properties).Returns(properties ?? new List<IPropertyModel>());
            m.Setup(a => a.Children).Returns(children ?? new List<IActivityModel>());
            m.Setup(a => a.Arguments).Returns(arguments ?? new List<IArgumentModel>());
            return m.Object;
        }

        /// <summary>
        /// A workflow whose root carries <paramref name="annotation"/>.
        /// </summary>
        /// <remarks>
        /// A workflow's annotation lives on its ROOT activity, not on the workflow — that
        /// indirection is where rules gated on <c>@unit</c> or <c>@module</c> most often go
        /// wrong, so this builder does it for you.
        /// </remarks>
        public static IWorkflowModel Workflow(
            string annotation = null,
            IReadOnlyCollection<IActivityModel> children = null,
            IReadOnlyCollection<IArgumentModel> arguments = null,
            string relativePath = null)
        {
            var root = Activity(annotation: annotation, children: children);

            var m = new Mock<IWorkflowModel>();
            m.Setup(w => w.Root).Returns(root);
            m.Setup(w => w.Arguments).Returns(arguments ?? new List<IArgumentModel>());
            m.Setup(w => w.RelativePath).Returns(relativePath);
            return m.Object;
        }

        /// <summary>A workflow with an explicit root, when the root itself matters.</summary>
        public static IWorkflowModel WorkflowWithRoot(
            IActivityModel root,
            IReadOnlyCollection<IArgumentModel> arguments = null,
            string relativePath = null)
        {
            var m = new Mock<IWorkflowModel>();
            m.Setup(w => w.Root).Returns(root);
            m.Setup(w => w.Arguments).Returns(arguments ?? new List<IArgumentModel>());
            m.Setup(w => w.RelativePath).Returns(relativePath);
            return m.Object;
        }

        /// <summary>
        /// A project. <paramref name="projectFilePath"/> points at a project.json on disk —
        /// pair it with <see cref="ProjectJsonFixture"/> to exercise ProjectContext.
        /// </summary>
        public static IProjectModel Project(
            string displayName = null,
            IReadOnlyCollection<IWorkflowModel> workflows = null,
            string projectFilePath = null,
            string outputType = null,
            string profileType = null,
            string expressionLanguage = null)
        {
            var m = new Mock<IProjectModel>();
            m.Setup(p => p.DisplayName).Returns(displayName);
            m.Setup(p => p.Workflows).Returns(workflows ?? new List<IWorkflowModel>());
            m.Setup(p => p.ProjectFilePath).Returns(projectFilePath);
            m.Setup(p => p.ProjectOutputType).Returns(outputType);
            m.Setup(p => p.ProjectProfileType).Returns(profileType);
            m.Setup(p => p.ExpressionLanguage).Returns(expressionLanguage);
            return m.Object;
        }
    }
}
