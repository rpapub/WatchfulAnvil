// <copyright file="RunContext.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

#pragma warning disable SA1402 // File may only contain a single type — intentional: internal record types co-located with their builder

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Diagnostics;

/// <summary>
/// Thread-safe singleton that manages the lifecycle of one TAP analysis run.
/// Created on first access; lives until the Workflow Analyzer child process exits.
/// </summary>
/// <remarks>
/// One RunContext per child-process lifetime. The Workflow Analyzer spawns a new
/// child process for each analysis run, so the Lazy singleton is effectively per-run.
/// </remarks>
public sealed class RunContext
{
    private static readonly Lazy<RunContext> _lazy =
        new(() => new RunContext(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Gets the singleton instance, creating it on first access.</summary>
    public static RunContext Instance => _lazy.Value;

    // ── Immutable run identity ──────────────────────────────────────────────

    /// <summary>
    /// Unique run identifier. Format: <c>yyyy-MM-ddTHH-mm-ss.fffZ+rrrrrrr</c> where
    /// the suffix is 6 hex characters of random entropy.
    /// </summary>
    public string RunId { get; }

    /// <summary>UTC timestamp when this RunContext was first constructed.</summary>
    public DateTime StartedAt { get; }

    /// <summary>Absolute path to the run-specific output directory.</summary>
    public string RunDirectory { get; }

    /// <summary>Absolute path to the parent <c>runs\</c> directory.</summary>
    public string RunsRoot { get; }

    // ── Mutable: populated when TapProjectRule fires ────────────────────────
    private string _projectFilePath = string.Empty;
    private string? _projectOutputType;
    private string? _projectProfileType;
    private string? _expressionLanguage;
    private bool? _hasModernBehavior;
    private string? _entryPointName;

    // ── Write health tracking ───────────────────────────────────────────────
    private int _writeFailures;
    private readonly Dictionary<string, string> _packagePathAliases = new();

    private readonly object _metaLock = new();

    // ── Version constants (captured once at class load) ─────────────────────
    private static readonly string s_tapRuleVersion =
        typeof(RunContext).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

    private static readonly string s_sdkAssemblyVersion =
        typeof(IActivityModel).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

    // ── JSON options (shared, thread-safe) ──────────────────────────────────
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    // ── Construction (runs exactly once via Lazy) ───────────────────────────

    private RunContext()
    {
        StartedAt = DateTime.UtcNow;
        RunId = GenerateRunId(StartedAt);

        RunsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WatchfulAnvil", "runs");

        RunDirectory = Path.Combine(RunsRoot, RunId);
        Directory.CreateDirectory(RunDirectory);
        Directory.CreateDirectory(Path.Combine(RunDirectory, "by-package"));

        // Write initial run-meta.json — project fields will be empty until
        // TapProjectRule fires and calls SetProjectContext.
        WriteRunMeta(completedAt: null);

        // ProcessExit fires after all rules complete (child process teardown).
        AppDomain.CurrentDomain.ProcessExit += (_, _) => OnProcessExit();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Supplies project-level fields that are only available from <see cref="IProjectModel"/>.
    /// Safe to call multiple times; last call wins. Rewrites run-meta.json.
    /// </summary>
    public void SetProjectContext(IProjectModel project)
    {
        lock (_metaLock)
        {
            _projectFilePath = project?.ProjectFilePath ?? string.Empty;
            _projectOutputType = project?.ProjectOutputType?.ToString();
            _projectProfileType = project?.ProjectProfileType?.ToString();
            _expressionLanguage = project?.ExpressionLanguage;
            _entryPointName = project?.EntryPointName;
            // hasModernBehavior: not exposed on IProjectModel in SDK ≤24.10.
            _hasModernBehavior = null;
        }
        WriteRunMeta(completedAt: null);
    }

    /// <summary>Absolute path to project.json. Empty string until <see cref="SetProjectContext"/> is called.</summary>
    public string ProjectFilePath
    {
        get { lock (_metaLock) { return _projectFilePath; } }
    }

    /// <summary>Atomically increments the write-failure counter recorded in run-meta.json.</summary>
    public void IncrementWriteFailures() => Interlocked.Increment(ref _writeFailures);

    // ── Workflow root registry (root instance ID → relative path) ──────────
    private readonly Dictionary<string, string> _workflowRootRegistry = new();

    /// <summary>
    /// Registers the mapping from a workflow root activity instance ID to
    /// the workflow file's relative path. Called from <c>TapWorkflowRule</c>.
    /// </summary>
    public void RegisterWorkflowRoot(string? rootId, string? relativePath)
    {
        if (string.IsNullOrEmpty(rootId) || string.IsNullOrEmpty(relativePath))
            return;
        lock (_metaLock)
        {
            _workflowRootRegistry[rootId] = relativePath;
        }
    }

    /// <summary>
    /// Returns the relative path for the workflow whose root has the given instance ID,
    /// or <c>null</c> if not registered (workflow rule has not yet fired).
    /// </summary>
    public string? GetWorkflowRelativePath(string? rootId)
    {
        if (string.IsNullOrEmpty(rootId)) return null;
        lock (_metaLock)
        {
            return _workflowRootRegistry.TryGetValue(rootId, out var path) ? path : null;
        }
    }

    /// <summary>
    /// Returns the absolute path to the by-package activity.jsonl directory for the given package.
    /// Hashes the <paramref name="sourceId"/> when the full path would exceed 250 characters,
    /// and records the mapping in run-meta.json under <c>packagePathAliases</c>.
    /// </summary>
    public string GetByPackagePath(string? sourceId, string? sourceVersion)
    {
        var safeId = sourceId ?? "unknown";
        var safeVersion = sourceVersion ?? "0.0.0";

        var candidate = Path.Combine(RunDirectory, "by-package", safeId, safeVersion);
        if (candidate.Length <= 250)
            return candidate;

        var hash = Sha8(safeId);
        lock (_metaLock)
        {
            _packagePathAliases.TryAdd(hash, safeId);
        }
        return Path.Combine(RunDirectory, "by-package", $"{hash}-{safeVersion}");
    }

    // ── ProcessExit handler ─────────────────────────────────────────────────

    private void OnProcessExit()
    {
        try
        {
            WriteRunMeta(completedAt: DateTime.UtcNow);
            File.WriteAllText(Path.Combine(RunsRoot, "latest.txt"), RunId);
            PruneOldRuns(keepCount: 5);
        }
        catch { /* best-effort; never throw from ProcessExit */ }
    }

    private void PruneOldRuns(int keepCount)
    {
        try
        {
            // ISO-8601 directory names are lexicographically chronological.
            var toDelete = Directory.GetDirectories(RunsRoot)
                .OrderByDescending(d => d)
                .Skip(keepCount)
                .ToArray();

            foreach (var dir in toDelete)
            {
                try { Directory.Delete(dir, recursive: true); }
                catch { /* skip directories that cannot be deleted */ }
            }
        }
        catch { }
    }

    // ── run-meta.json serialisation ─────────────────────────────────────────

    private void WriteRunMeta(DateTime? completedAt)
    {
        try
        {
            RunMetaRecord meta;
            lock (_metaLock)
            {
                meta = new RunMetaRecord
                {
                    SchemaVersion = "0.1.0",
                    RunId = RunId,
                    StartedAt = StartedAt,
                    CompletedAt = completedAt,
                    ProjectFilePath = _projectFilePath,
                    ProjectOutputType = _projectOutputType,
                    ProjectProfileType = _projectProfileType,
                    ExpressionLanguage = _expressionLanguage,
                    HasModernBehavior = _hasModernBehavior,
                    EntryPointName = _entryPointName,
                    TapRuleVersion = s_tapRuleVersion,
                    SdkAssemblyVersion = s_sdkAssemblyVersion,
                    WriteFailures = _writeFailures,
                    PackagePathAliases = new Dictionary<string, string>(_packagePathAliases),
                };
            }

            var json = JsonSerializer.Serialize(meta, s_jsonOptions);

            lock (_metaLock)
            {
                File.WriteAllText(Path.Combine(RunDirectory, "run-meta.json"), json);
            }
        }
        catch { /* never throw from metadata writes */ }
    }

    // ── Static helpers ───────────────────────────────────────────────────────

    private static string GenerateRunId(DateTime utcNow)
    {
        // e.g. 2026-04-11T10-42-01.347Z+a3f2c8
        var ts = utcNow.ToString("yyyy-MM-ddTHH-mm-ss.fffZ");
        var bytes = new byte[3];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var hex = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        return $"{ts}+{hex}";
    }

    private static string Sha8(string input)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash, 0, 4).Replace("-", string.Empty).ToLowerInvariant();
    }
}

// ── Data transfer object for run-meta.json ──────────────────────────────────

internal sealed class RunMetaRecord
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = "0.1.0";

    [JsonPropertyName("runId")]
    public string RunId { get; init; } = string.Empty;

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; init; }

    [JsonPropertyName("projectFilePath")]
    public string ProjectFilePath { get; init; } = string.Empty;

    [JsonPropertyName("projectOutputType")]
    public string? ProjectOutputType { get; init; }

    [JsonPropertyName("projectProfileType")]
    public string? ProjectProfileType { get; init; }

    [JsonPropertyName("expressionLanguage")]
    public string? ExpressionLanguage { get; init; }

    [JsonPropertyName("hasModernBehavior")]
    public bool? HasModernBehavior { get; init; }

    [JsonPropertyName("entryPointName")]
    public string? EntryPointName { get; init; }

    [JsonPropertyName("tapRuleVersion")]
    public string TapRuleVersion { get; init; } = string.Empty;

    [JsonPropertyName("sdkAssemblyVersion")]
    public string SdkAssemblyVersion { get; init; } = string.Empty;

    [JsonPropertyName("writeFailures")]
    public int WriteFailures { get; init; }

    [JsonPropertyName("packagePathAliases")]
    public Dictionary<string, string> PackagePathAliases { get; init; } = new();
}
