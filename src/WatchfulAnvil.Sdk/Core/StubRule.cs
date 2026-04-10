// Compiled for net461 only — see WatchfulAnvil.Sdk.csproj.
// Provides stub rule registrations for Studio Windows-Legacy (net461).
// Rule source files are not compiled for net461; rules appear in Studio but always pass.
using UiPath.Studio.Activities.Api;
using UiPath.Studio.Activities.Api.Analyzer;
using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Core
{
    /// <summary>
    /// Factory for stub <see cref="IRegisterAnalyzerConfiguration"/> instances used in
    /// net461 delivery package builds. Each stub registers the rule so it appears and
    /// is configurable in Studio, but the inspect delegate always returns pass.
    /// </summary>
    public static class StubRule
    {
        /// <summary>Stub for a rule scoped to <see cref="IWorkflowModel"/>.</summary>
        public static IRegisterAnalyzerConfiguration WorkflowRule(
            string id, string name, string recommendation)
            => new StubRegistration<IWorkflowModel>(id, name, recommendation);

        /// <summary>Stub for a rule scoped to <see cref="IActivityModel"/>.</summary>
        public static IRegisterAnalyzerConfiguration ActivityRule(
            string id, string name, string recommendation)
            => new StubRegistration<IActivityModel>(id, name, recommendation);

        /// <summary>Stub for a rule scoped to <see cref="IProjectModel"/>.</summary>
        public static IRegisterAnalyzerConfiguration ProjectRule(
            string id, string name, string recommendation)
            => new StubRegistration<IProjectModel>(id, name, recommendation);

        /// <summary>Stub for a rule scoped to <see cref="IProjectSummary"/>.</summary>
        public static IRegisterAnalyzerConfiguration SummaryRule(
            string id, string name, string recommendation)
            => new StubRegistration<IProjectSummary>(id, name, recommendation);
    }
}
