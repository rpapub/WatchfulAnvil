using System.Collections.Generic;
using System.Diagnostics;
using Cpmf.Rules.Pipeline;
using Moq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Rules.Pipeline
{
    public class PipelinePresenceCounterTests
    {
        private readonly PipelinePresenceCounter _rule = new PipelinePresenceCounter();

        private static Mock<IWorkflowModel> WorkflowWithAnnotation(string annotation)
        {
            var root = new Mock<IActivityModel>();
            root.Setup(r => r.AnnotationText).Returns(annotation);

            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Root).Returns(root.Object);
            return wf;
        }

        private static Mock<IProjectModel> Project(IEnumerable<IWorkflowModel> workflows)
        {
            var p = new Mock<IProjectModel>();
            p.Setup(x => x.Workflows).Returns(new List<IWorkflowModel>(workflows));
            return p;
        }

        [Fact]
        public void Initialize_RegistersRule_WithCorrectId()
        {
            var api = new Mock<IAnalyzerConfigurationService>();
            api.Setup(a => a.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9)).Returns(true);
            _rule.Initialize(api.Object);
            api.Verify(s => s.AddRule(
                It.Is<Rule<IProjectModel>>(r =>
                    r.Id == "CPMF-PLN-C001" &&
                    r.DefaultErrorLevel == TraceLevel.Error)));
        }

        [Fact]
        public void Pass_WhenOnePipelineWorkflowExists()
        {
            var project = Project(new[]
            {
                WorkflowWithAnnotation("@pipeline").Object,
                WorkflowWithAnnotation("@unit").Object,
            });
            Assert.False(_rule.Get().Inspect(project.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenMultiplePipelineWorkflowsExist()
        {
            var project = Project(new[]
            {
                WorkflowWithAnnotation("@pipeline").Object,
                WorkflowWithAnnotation("@pipeline").Object,
            });
            Assert.False(_rule.Get().Inspect(project.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Fail_WhenNoPipelineWorkflowExists()
        {
            var project = Project(new[]
            {
                WorkflowWithAnnotation("@unit").Object,
                WorkflowWithAnnotation("").Object,
            });
            var result = _rule.Get().Inspect(project.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("@pipeline"));
        }

        [Fact]
        public void Fail_WhenProjectHasNoWorkflows()
        {
            var project = Project(new List<IWorkflowModel>());
            var result = _rule.Get().Inspect(project.Object, _rule.Get());
            Assert.True(result.HasErrors);
        }

        [Fact]
        public void Pass_WhenWorkflowsIsNull()
        {
            var p = new Mock<IProjectModel>();
            p.Setup(x => x.Workflows).Returns((IReadOnlyCollection<IWorkflowModel>)null);
            Assert.False(_rule.Get().Inspect(p.Object, _rule.Get()).HasErrors);
        }
    }
}
