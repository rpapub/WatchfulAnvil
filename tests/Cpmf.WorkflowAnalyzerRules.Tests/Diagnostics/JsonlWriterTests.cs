// <copyright file="JsonlWriterTests.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using WatchfulAnvil.Sdk.Diagnostics;

using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Diagnostics
{
    /// <summary>
    /// Integration tests for <see cref="JsonlWriter"/> using the real <see cref="RunContext.Instance"/>.
    /// These tests write to <c>%LOCALAPPDATA%\WatchfulAnvil\runs\{runId}\</c> and assert on the files
    /// produced there. Because <see cref="RunContext"/> is a process-lifetime singleton, tests in this
    /// class share the same run directory and are not run in parallel.
    /// </summary>
    [Collection("RunContext")]
    public sealed class JsonlWriterTests
    {
        private static readonly RunContext Run = RunContext.Instance;

        // ── AppendActivity ───────────────────────────────────────────────────

        [Fact]
        public void AppendActivity_CreatesActivityJsonl_InRunDirectory()
        {
            var before = LinesIn("activity.jsonl");
            JsonlWriter.AppendActivity("{\"tap_test\":\"act\"}", Run, null, null);
            var after = LinesIn("activity.jsonl");
            Assert.Equal(before + 1, after);
        }

        [Fact]
        public void AppendActivity_AppendedLine_IsValidJson()
        {
            JsonlWriter.AppendActivity("{\"check\":99}", Run, null, null);

            var lastLine = File.ReadAllLines(Path.Combine(Run.RunDirectory, "activity.jsonl")).Last();
            var doc = JsonDocument.Parse(lastLine);
            Assert.Equal(99, doc.RootElement.GetProperty("check").GetInt32());
        }

        [Fact]
        public void AppendActivity_FansOut_ToByPackageDirectory()
        {
            const string pkg = "test.pkg.unit";
            const string ver = "0.0.1";
            JsonlWriter.AppendActivity("{\"fan\":true}", Run, pkg, ver);

            var packagePath = Path.Combine(Run.GetByPackagePath(pkg, ver), "activity.jsonl");
            Assert.True(File.Exists(packagePath), $"Expected by-package file at {packagePath}");

            var lastLine = File.ReadAllLines(packagePath).Last();
            Assert.Contains("\"fan\"", lastLine);
        }

        [Fact]
        public void AppendActivity_IsThreadSafe_AllRecordsLanded()
        {
            const int count = 30;
            var before = LinesIn("activity.jsonl");
            var json = "{\"parallel\":true}";

            Parallel.For(0, count, _ => JsonlWriter.AppendActivity(json, Run, null, null));

            var after = LinesIn("activity.jsonl");
            Assert.Equal(before + count, after);
        }

        [Fact]
        public void AppendActivity_NoOp_WhenJsonLineIsEmpty()
        {
            var before = LinesIn("activity.jsonl");
            JsonlWriter.AppendActivity(string.Empty, Run, null, null);
            var after = LinesIn("activity.jsonl");
            Assert.Equal(before, after);
        }

        // ── AppendWorkflow ───────────────────────────────────────────────────

        [Fact]
        public void AppendWorkflow_CreatesWorkflowJsonl_InRunDirectory()
        {
            var before = LinesIn("workflow.jsonl");
            JsonlWriter.AppendWorkflow("{\"tap_test\":\"wfl\"}", Run);
            var after = LinesIn("workflow.jsonl");
            Assert.Equal(before + 1, after);
        }

        [Fact]
        public void AppendWorkflow_AppendedLine_IsValidJson()
        {
            JsonlWriter.AppendWorkflow("{\"wfcheck\":77}", Run);

            var lastLine = File.ReadAllLines(Path.Combine(Run.RunDirectory, "workflow.jsonl")).Last();
            var doc = JsonDocument.Parse(lastLine);
            Assert.Equal(77, doc.RootElement.GetProperty("wfcheck").GetInt32());
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static int LinesIn(string fileName)
        {
            var path = Path.Combine(Run.RunDirectory, fileName);
            return File.Exists(path) ? File.ReadAllLines(path).Length : 0;
        }
    }
}
