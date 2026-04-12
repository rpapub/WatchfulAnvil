// <copyright file="JsonlWriter.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.IO;
using System.Text;

namespace WatchfulAnvil.Sdk.Diagnostics;

/// <summary>
/// Thread-safe JSONL (JSON Lines) writer for TAP diagnostic output.
/// Maintains a single global file lock shared across all writers in the process.
/// </summary>
/// <remarks>
/// Design contract:
/// <list type="bullet">
///   <item>The main stream (<c>activity.jsonl</c> / <c>workflow.jsonl</c>) is the authoritative record.</item>
///   <item>The by-package fan-out is best-effort. A failure there never prevents the main-stream write.</item>
///   <item>Each call appends exactly one newline-terminated JSON line.</item>
///   <item>All writes are protected by a single static lock; the lock is held as briefly as possible.</item>
/// </list>
/// </remarks>
public static class JsonlWriter
{
    private static readonly object s_lock = new();

    // ── Public entry points ─────────────────────────────────────────────────

    /// <summary>
    /// Appends one JSON record to <c>activity.jsonl</c> in the run directory,
    /// then fans out to the by-package file derived from <paramref name="sourceId"/>
    /// and <paramref name="sourceVersion"/>.
    /// </summary>
    /// <param name="jsonLine">A complete, single-line JSON object (no embedded newlines).</param>
    /// <param name="run">The current run context supplying directory paths.</param>
    /// <param name="sourceId">NuGet package ID (lowercase). Used to compute by-package path.</param>
    /// <param name="sourceVersion">Package version string.</param>
    public static void AppendActivity(
        string jsonLine,
        RunContext run,
        string? sourceId,
        string? sourceVersion)
    {
        if (run is null) throw new ArgumentNullException(nameof(run));
        if (string.IsNullOrEmpty(jsonLine)) return;

        var mainPath = Path.Combine(run.RunDirectory, "activity.jsonl");
        var line = jsonLine.EndsWith("\n", StringComparison.Ordinal)
                         ? jsonLine
                         : jsonLine + "\n";

        // Step 1 — write to main stream.
        bool mainOk = AppendLine(mainPath, line, run);
        if (!mainOk) return; // do not attempt by-package if main failed

        // Step 2 — fan-out to by-package (best-effort; failure does NOT affect main).
        if (!string.IsNullOrEmpty(sourceId))
        {
            var packageDir = run.GetByPackagePath(sourceId, sourceVersion);
            var packagePath = Path.Combine(packageDir, "activity.jsonl");
            AppendLine(packagePath, line, run); // ignore return value
        }
    }

    /// <summary>
    /// Appends one JSON record to <c>workflow.jsonl</c> in the run directory.
    /// Workflow records are not fanned out to by-package.
    /// </summary>
    /// <param name="jsonLine">A complete, single-line JSON object (no embedded newlines).</param>
    /// <param name="run">The current run context supplying directory paths.</param>
    public static void AppendWorkflow(string jsonLine, RunContext run)
    {
        if (run is null) throw new ArgumentNullException(nameof(run));
        if (string.IsNullOrEmpty(jsonLine)) return;

        var mainPath = Path.Combine(run.RunDirectory, "workflow.jsonl");
        var line = jsonLine.EndsWith("\n", StringComparison.Ordinal)
            ? jsonLine
            : jsonLine + "\n";

        AppendLine(mainPath, line, run);
    }

    // ── Internal write primitive ─────────────────────────────────────────────

    /// <summary>
    /// Acquires the global lock, ensures the directory exists, and appends
    /// <paramref name="line"/> to <paramref name="filePath"/>.
    /// On failure, increments <see cref="RunContext.IncrementWriteFailures"/> and returns <c>false</c>.
    /// </summary>
    private static bool AppendLine(string filePath, string line, RunContext run)
    {
        lock (s_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(filePath, line, Encoding.UTF8);
                return true;
            }
            catch
            {
                run.IncrementWriteFailures();
                return false;
            }
        }
    }
}
