using System.Collections.Generic;
using Cpmf.Rules.Pipeline;
using Moq;
using UiPath.Studio.Analyzer.Models;
using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Core
{
    /// <summary>
    /// Suppression of PROJECT-scoped rules.
    ///
    /// A project carries no annotation of its own, so @nocheck and @suppress:RULE-ID used
    /// to be silently inert at project scope — there was no way to suppress a project rule
    /// at all. ScopedRule now joins every workflow's root annotation for IProjectModel.
    ///
    /// CPMF-N003 (ProjectNameRule) is used as the subject because it fires on the project
    /// name alone, so suppression is the only variable.
    /// </summary>
    public class ProjectScopeSuppressionTests
    {
        private const string RuleId = "CPMF-N003";
        private const string BadName = "1_invalid name";   // violates on its own

        private static Mock<IProjectModel> Project(string displayName, params string?[] workflowAnnotations)
        {
            var workflows = new List<IWorkflowModel>();
            foreach (var annotation in workflowAnnotations)
            {
                var root = new Mock<IActivityModel>();
                root.Setup(a => a.AnnotationText).Returns(annotation!);

                var wf = new Mock<IWorkflowModel>();
                wf.Setup(w => w.Root).Returns(root.Object);
                workflows.Add(wf.Object);
            }

            var project = new Mock<IProjectModel>();
            project.Setup(p => p.DisplayName).Returns(displayName);
            project.Setup(p => p.Workflows).Returns(workflows);
            return project;
        }

        private static bool Inspect(Mock<IProjectModel> project)
        {
            var rule = new ProjectNameRule();
            return rule.Get().Inspect(project.Object, rule.Get()).HasErrors;
        }

        [Fact]
        public void Violates_WhenNothingSuppressesIt()
        {
            Assert.True(Inspect(Project(BadName, "just a note")));
        }

        [Fact]
        public void Suppressed_ByDirectiveOnAnyWorkflowRoot()
        {
            // The directive is on the SECOND workflow: the join must cover all of them,
            // not just the first.
            Assert.False(Inspect(Project(BadName, "unrelated note", "@suppress:" + RuleId)));
        }

        [Fact]
        public void Suppressed_ByNoCheckOnAnyWorkflowRoot()
        {
            Assert.False(Inspect(Project(BadName, "unrelated note", "@nocheck")));
        }

        [Fact]
        public void NotSuppressed_ByADirectiveForADifferentRule()
        {
            Assert.True(Inspect(Project(BadName, "@suppress:CPMF-SOMETHING-ELSE")));
        }

        [Fact]
        public void Survives_AProjectWithNoWorkflowsAtAll()
        {
            // Workflows is null on an unpopulated project model; dereferencing it here
            // would throw before the rule ever runs.
            var project = new Mock<IProjectModel>();
            project.Setup(p => p.DisplayName).Returns(BadName);
            project.Setup(p => p.Workflows).Returns((IReadOnlyCollection<IWorkflowModel>)null!);

            Assert.True(Inspect(project));
        }

        [Fact]
        public void Survives_AWorkflowWithNoRootAnnotation()
        {
            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Root).Returns((IActivityModel)null!);

            var project = new Mock<IProjectModel>();
            project.Setup(p => p.DisplayName).Returns(BadName);
            project.Setup(p => p.Workflows).Returns(new List<IWorkflowModel> { wf.Object });

            Assert.True(Inspect(project));
        }
    }
}
