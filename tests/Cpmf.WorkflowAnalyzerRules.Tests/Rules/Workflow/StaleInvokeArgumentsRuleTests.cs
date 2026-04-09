using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Cpmf.Rules.Workflow;
using Moq;
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Activities.Api.Analyzer.Rules;
using UiPath.Studio.Analyzer.Models;
using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Rules.Workflow
{
    public class StaleInvokeArgumentsRuleTests
    {
        private readonly StaleInvokeArgumentsRule _rule = new StaleInvokeArgumentsRule();

        private const string UiNs = "http://schemas.uipath.com/workflow/activities";
        private const string Sap2010Ns = "http://schemas.microsoft.com/netfx/2010/xaml/activities/presentation";
        private const string McIgnorable = "http://schemas.openxmlformats.org/markup-compatibility/2006";

        // --- Registration ---

        [Fact]
        public void Initialize_RegistersRule_OnV9()
        {
            var api = new Mock<IAnalyzerConfigurationService>();
            api.Setup(a => a.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9)).Returns(true);
            _rule.Initialize(api.Object);
            api.Verify(s => s.AddRule(
                It.Is<Rule<IWorkflowModel>>(r =>
                    r.Id == "CPMF-WFL-008" &&
                    r.DefaultErrorLevel == TraceLevel.Error)));
        }

        [Fact]
        public void Initialize_DoesNotRegister_OnPreV9()
        {
            var api = new Mock<IAnalyzerConfigurationService>();
            api.Setup(a => a.HasFeature(DesignFeatureKeys.WorkflowAnalyzerV9)).Returns(false);
            _rule.Initialize(api.Object);
            api.Verify(s => s.AddRule(It.IsAny<Rule<IWorkflowModel>>()), Times.Never);
        }

        // --- Helpers ---

        private static string BuildBoundArgsXaml(string activityId, int boundArgCount, string workflowFileName)
        {
            var boundArgs = new System.Text.StringBuilder();
            for (int i = 0; i < boundArgCount; i++)
                boundArgs.AppendLine(
                    $"  <InArgument x:TypeArguments=\"x:String\" x:Key=\"arg{i}\">[val{i}]</InArgument>");

            return $@"<Activity mc:Ignorable=""sap2010""
    xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
    xmlns:mc=""{McIgnorable}""
    xmlns:sap2010=""{Sap2010Ns}""
    xmlns:ui=""{UiNs}""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Sequence sap2010:WorkflowViewState.IdRef=""Sequence_1"">
    <ui:InvokeWorkflowFile sap2010:WorkflowViewState.IdRef=""{activityId}""
        WorkflowFileName=""{workflowFileName}"">
      <ui:InvokeWorkflowFile.Arguments>
{boundArgs}      </ui:InvokeWorkflowFile.Arguments>
    </ui:InvokeWorkflowFile>
  </Sequence>
</Activity>";
        }

        private static string BuildEmptyDictXaml(string activityId, string workflowFileName)
        {
            return $@"<Activity mc:Ignorable=""sap2010""
    xmlns=""http://schemas.microsoft.com/netfx/2009/xaml/activities""
    xmlns:mc=""{McIgnorable}""
    xmlns:sap2010=""{Sap2010Ns}""
    xmlns:scg=""clr-namespace:System.Collections.Generic;assembly=System.Private.CoreLib""
    xmlns:ui=""{UiNs}""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <Sequence sap2010:WorkflowViewState.IdRef=""Sequence_1"">
    <ui:InvokeWorkflowFile sap2010:WorkflowViewState.IdRef=""{activityId}""
        WorkflowFileName=""{workflowFileName}"">
      <ui:InvokeWorkflowFile.Arguments>
        <scg:Dictionary x:TypeArguments=""x:String, Argument"" />
      </ui:InvokeWorkflowFile.Arguments>
    </ui:InvokeWorkflowFile>
  </Sequence>
</Activity>";
        }

        private static Mock<IWorkflowModel> BuildWorkflow(
            string projectDir, string relPath, string invokeId,
            string targetRelPath, bool literalFileName, int targetArgCount, int boundArgCount,
            bool useEmptyDict = false)
        {
            var xamlPath = Path.Combine(projectDir, relPath);
            if (useEmptyDict)
            {
                var content = BuildEmptyDictXaml(invokeId, targetRelPath);
                File.WriteAllText(xamlPath, content);
            }
            else
            {
                var content = BuildBoundArgsXaml(invokeId, boundArgCount, targetRelPath);
                File.WriteAllText(xamlPath, content);
            }

            // Target workflow
            var targetArgs = new List<IArgumentModel>();
            for (int i = 0; i < targetArgCount; i++)
            {
                var arg = new Mock<IArgumentModel>();
                arg.Setup(a => a.DisplayName).Returns($"arg{i}");
                targetArgs.Add(arg.Object);
            }
            var target = new Mock<IWorkflowModel>();
            target.Setup(w => w.RelativePath).Returns(targetRelPath);
            target.Setup(w => w.Arguments).Returns(targetArgs);

            // Project
            var project = new Mock<IProjectModel>();
            project.Setup(p => p.Directory).Returns(projectDir);
            project.Setup(p => p.Workflows).Returns(new List<IWorkflowModel> { target.Object });

            // Invoke activity argument "Workflow file name"
            var fileNameArg = new Mock<IArgumentModel>();
            fileNameArg.Setup(a => a.DisplayName).Returns("Workflow file name");
            fileNameArg.Setup(a => a.HasLiteralExpression).Returns(literalFileName);
            fileNameArg.Setup(a => a.DefinedExpression).Returns($"\"{targetRelPath}\"");

            var invokeArgs = new List<IArgumentModel> { fileNameArg.Object };

            var invokeActivity = new Mock<IActivityModel>();
            invokeActivity.Setup(a => a.ToolboxName).Returns("InvokeWorkflowFile");
            invokeActivity.Setup(a => a.DisplayName).Returns("Test Invoke");
            invokeActivity.Setup(a => a.Id).Returns(invokeId);
            invokeActivity.Setup(a => a.Arguments).Returns(invokeArgs);
            invokeActivity.Setup(a => a.Children).Returns((IReadOnlyCollection<IActivityModel>)null);

            var root = new Mock<IActivityModel>();
            root.Setup(a => a.ToolboxName).Returns("Sequence");
            root.Setup(a => a.Children).Returns(new List<IActivityModel> { invokeActivity.Object });

            var workflow = new Mock<IWorkflowModel>();
            workflow.Setup(w => w.RelativePath).Returns(relPath);
            workflow.Setup(w => w.Project).Returns(project.Object);
            workflow.Setup(w => w.Root).Returns(root.Object);

            return workflow;
        }

        // --- Pass cases ---

        [Fact]
        public void Pass_WhenArgumentCountMatches()
        {
            var dir = Path.GetTempPath();
            var wf = BuildWorkflow(dir, "Caller_match.xaml", "IWF_match", "Target.xaml", true, 3, 3);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenBoundArgCountIsZeroAndTargetHasZeroArgs()
        {
            var dir = Path.GetTempPath();
            var wf = BuildWorkflow(dir, "Caller_zero.xaml", "IWF_zero", "Target.xaml", true, 0, 0, useEmptyDict: true);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenInvocationIsDynamic()
        {
            var dir = Path.GetTempPath();
            var wf = BuildWorkflow(dir, "Caller_dyn.xaml", "IWF_dyn", "Target.xaml", false, 3, 1);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        [Fact]
        public void Pass_WhenProjectDirectoryIsNull()
        {
            var project = new Mock<IProjectModel>();
            project.Setup(p => p.Directory).Returns((string)null);
            var wf = new Mock<IWorkflowModel>();
            wf.Setup(w => w.Project).Returns(project.Object);
            wf.Setup(w => w.Root).Returns((IActivityModel)null);
            Assert.False(_rule.Get().Inspect(wf.Object, _rule.Get()).HasErrors);
        }

        // --- Fail cases ---

        [Fact]
        public void Fail_WhenCallerBindsFewerArgumentsThanTargetDeclares()
        {
            var dir = Path.GetTempPath();
            var wf = BuildWorkflow(dir, "Caller_fewer.xaml", "IWF_fewer", "Target.xaml", true, 3, 1);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("3") && m.Contains("1"));
        }

        [Fact]
        public void Fail_WhenCallerBindsMoreArgumentsThanTargetDeclares()
        {
            var dir = Path.GetTempPath();
            var wf = BuildWorkflow(dir, "Caller_more.xaml", "IWF_more", "Target.xaml", true, 2, 4);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
            Assert.Contains(result.Messages, m => m.Contains("2") && m.Contains("4"));
        }

        [Fact]
        public void Fail_WhenCallerBindsZeroButTargetDeclaresArgs()
        {
            var dir = Path.GetTempPath();
            var wf = BuildWorkflow(dir, "Caller_none.xaml", "IWF_none", "Target.xaml", true, 2, 0, useEmptyDict: true);
            var result = _rule.Get().Inspect(wf.Object, _rule.Get());
            Assert.True(result.HasErrors);
        }
    }
}
