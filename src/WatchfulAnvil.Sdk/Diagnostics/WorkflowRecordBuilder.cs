// <copyright file="WorkflowRecordBuilder.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

#pragma warning disable SA1402 // File may only contain a single type — intentional: internal record types co-located with their builder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Diagnostics;

/// <summary>
/// Builds the JSON string for one workflow record conforming to
/// <c>tap-workflow-record.schema.json v0.1.0</c>.
/// </summary>
public static class WorkflowRecordBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>
    /// Builds and serialises the workflow record to a single-line JSON string.
    /// Also registers the workflow root with <paramref name="run"/> so that
    /// activity records can resolve <c>workflowRelativePath</c>.
    /// Never throws.
    /// </summary>
    public static string Build(IWorkflowModel workflow, RunContext run)
    {
        var errors = new List<TapError>();

        var relativePath = SafeGet(() => workflow.RelativePath, "workflowRelativePath", errors);

        // Register root ID so TapActivityRule can resolve workflowRelativePath.
        var rootId = SafeGet(() => workflow.Root?.Id, "rootDisplayName", errors);
        run.RegisterWorkflowRoot(rootId, relativePath);

        // ── Coded workflow detection ─────────────────────────────────────────
        // ICodeSourceFileModel is the SDK interface for .cs coded workflows.
        // Check via reflection to avoid hard dependency on the interface existing in older SDKs.
        bool isCodedWorkflow = false;
        string? sourceCode = null;
        string? sourceFilePath = null;
        SafeGet<object?>(() =>
        {
            // Try cast: coded workflows implement ICodeSourceFileModel.
            var codeSource = workflow as ICodeSourceFileModel;
            if (codeSource != null)
            {
                isCodedWorkflow = true;
                sourceCode = codeSource.SourceCode;
                sourceFilePath = codeSource.FilePath;
            }
            return null;
        }, "isCodedWorkflow", errors);

        // ── Root activity ────────────────────────────────────────────────────
        var root = workflow.Root as IActivityModel;
        var rootDisplayName = SafeGet(() => root?.DisplayName, "rootDisplayName", errors);
        var rootAnnotationText = SafeGet(() => root?.AnnotationText, "rootAnnotationText", errors);
        var rootStructuralKind = SafeGet(() => DeriveStructuralKind(root), "rootStructuralKind", errors);

        // ── Arguments ────────────────────────────────────────────────────────
        var arguments = SafeGet(
            () => (workflow.Arguments ?? Enumerable.Empty<IArgumentModel>())
                .Select(a => new WfArgumentRecord
                {
                    Direction = MapDirection(a.Direction),
                    Name = a.DisplayName ?? string.Empty,
                    Type = a.Type?.ToString(),
                })
                .ToList(),
            "arguments", errors) ?? new List<WfArgumentRecord>();

        // ── Imported namespaces ──────────────────────────────────────────────
        var importedNamespaces = SafeGet(
            () => (workflow.ImportedNamespaces ?? Enumerable.Empty<string>()).ToList(),
            "importedNamespaces", errors) ?? new List<string>();

        // ── Assemblies ───────────────────────────────────────────────────────
        var assemblies = SafeGet(
            () => BuildAssemblies(workflow), "assemblies", errors)
            ?? new List<AssemblyRecord>();

        // ── XML prefixes ─────────────────────────────────────────────────────
        // XML prefix declarations are in the raw XAML, not exposed on IWorkflowModel.
        // Read from the XAML file if it exists.
        var xmlPrefixes = SafeGet(
            () => ReadXmlPrefixes(relativePath, isCodedWorkflow),
            "xmlPrefixes", errors) ?? new List<XmlPrefixRecord>();

        // ── Trigger model ────────────────────────────────────────────────────
        TriggerModelRecord? triggerModel = null;
        SafeGet<object?>(() =>
        {
            var trigger = workflow.TriggerModel;
            if (trigger != null)
            {
                // ITriggerModel exposes Local/Start/Integration sub-interface properties.
                // HasLocal/HasStart/HasIntegration are derived as non-null checks.
                triggerModel = new TriggerModelRecord
                {
                    HasAny = trigger.HasAny(),
                    HasLocal = trigger.Local != null,
                    HasStart = trigger.Start != null,
                    HasIntegration = trigger.Integration != null,
                };
            }
            return null;
        }, "triggerModel", errors);

        // ── Assemble record ──────────────────────────────────────────────────
        var record = new WorkflowRecord
        {
            SchemaVersion = "0.1.0",
            RunId = run.RunId,
            CapturedAt = DateTime.UtcNow,
            ThreadId = Environment.CurrentManagedThreadId,
            ProjectFilePath = run.ProjectFilePath,
            WorkflowRelativePath = relativePath ?? string.Empty,
            IsCodedWorkflow = isCodedWorkflow,
            RootDisplayName = rootDisplayName,
            RootAnnotationText = rootAnnotationText,
            RootStructuralKind = rootStructuralKind,
            Arguments = arguments,
            ImportedNamespaces = importedNamespaces,
            Assemblies = assemblies,
            XmlPrefixes = xmlPrefixes,
            TriggerModel = triggerModel,
            SourceCode = sourceCode,
            SourceFilePath = sourceFilePath,
            Errors = errors,
        };

        return JsonSerializer.Serialize(record, s_jsonOptions);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static T SafeGet<T>(Func<T> getter, string field, List<TapError> errors, T fallback = default!)
        => ReflectionHelper.SafeReflect(getter, field, errors, fallback);

    private static string? DeriveStructuralKind(IActivityModel? root)
    {
        if (root == null) return null;
        if (root is IFlowchartModel) return "flowchart";
        if (root is IStateMachineModel) return "stateMachine";
        if (root is ITryCatchModel) return "tryCatch";
        if (root is ISwitchModel) return "switch";
        if (root is IPickModel) return "pick";
        if (root is IIfElseModel) return "ifElse";
        return "sequence";
    }

    private static List<AssemblyRecord> BuildAssemblies(IWorkflowModel workflow)
    {
        // workflow.Assemblies is IReadOnlyCollection<string> — just assembly simple names.
        // File paths (IDependency.Assemblies) are only available on project-level dependencies.
        return (workflow.Assemblies ?? Enumerable.Empty<string>())
            .Select(name => new AssemblyRecord
            {
                Name = name ?? string.Empty,
                Files = null,
            })
            .ToList();
    }

    private static List<XmlPrefixRecord> ReadXmlPrefixes(string? relativePath, bool isCodedWorkflow)
    {
        if (isCodedWorkflow || string.IsNullOrEmpty(relativePath))
            return new List<XmlPrefixRecord>();

        // Parse xmlns attributes from the XAML root element.
        // Use XmlReader to avoid loading the full document.
        try
        {
            if (!System.IO.File.Exists(relativePath))
                return new List<XmlPrefixRecord>();

            var prefixes = new List<XmlPrefixRecord>();
            using var reader = System.Xml.XmlReader.Create(relativePath,
                new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Ignore });

            // Advance to the root element.
            while (reader.Read())
            {
                if (reader.NodeType != System.Xml.XmlNodeType.Element) continue;

                // Collect all namespace declarations from the root element.
                if (reader.HasAttributes)
                {
                    reader.MoveToFirstAttribute();
                    do
                    {
                        if (reader.Prefix == "xmlns")
                        {
                            prefixes.Add(new XmlPrefixRecord
                            {
                                Prefix = reader.LocalName,
                                Namespace = reader.Value,
                            });
                        }
                        else if (reader.Name == "xmlns")
                        {
                            prefixes.Add(new XmlPrefixRecord
                            {
                                Prefix = null, // default namespace
                                Namespace = reader.Value,
                            });
                        }
                    }
                    while (reader.MoveToNextAttribute());
                }
                break; // only process the root element
            }

            return prefixes;
        }
        catch
        {
            return new List<XmlPrefixRecord>();
        }
    }

    private static string? MapDirection(object? direction)
    {
        if (direction == null) return null;
        var s = direction.ToString()?.ToLowerInvariant() ?? string.Empty;
        return s switch
        {
            "in" => "in",
            "out" => "out",
            "inout" => "inout",
            _ => null,
        };
    }
}

// ── Record POCOs ─────────────────────────────────────────────────────────────

internal sealed class WorkflowRecord
{
    [JsonPropertyName("schemaVersion")] public string SchemaVersion { get; init; } = "0.1.0";
    [JsonPropertyName("runId")] public string RunId { get; init; } = string.Empty;
    [JsonPropertyName("capturedAt")] public DateTime CapturedAt { get; init; }
    [JsonPropertyName("threadId")] public int ThreadId { get; init; }
    [JsonPropertyName("projectFilePath")] public string ProjectFilePath { get; init; } = string.Empty;
    [JsonPropertyName("workflowRelativePath")] public string WorkflowRelativePath { get; init; } = string.Empty;
    [JsonPropertyName("isCodedWorkflow")] public bool IsCodedWorkflow { get; init; }
    [JsonPropertyName("rootDisplayName")] public string? RootDisplayName { get; init; }
    [JsonPropertyName("rootAnnotationText")] public string? RootAnnotationText { get; init; }
    [JsonPropertyName("rootStructuralKind")] public string? RootStructuralKind { get; init; }
    [JsonPropertyName("arguments")] public List<WfArgumentRecord> Arguments { get; init; } = new();
    [JsonPropertyName("importedNamespaces")] public List<string> ImportedNamespaces { get; init; } = new();
    [JsonPropertyName("assemblies")] public List<AssemblyRecord> Assemblies { get; init; } = new();
    [JsonPropertyName("xmlPrefixes")] public List<XmlPrefixRecord> XmlPrefixes { get; init; } = new();
    [JsonPropertyName("triggerModel")] public TriggerModelRecord? TriggerModel { get; init; }
    [JsonPropertyName("sourceCode")] public string? SourceCode { get; init; }
    [JsonPropertyName("sourceFilePath")] public string? SourceFilePath { get; init; }
    [JsonPropertyName("_errors")] public List<TapError> Errors { get; init; } = new();
}

internal sealed class WfArgumentRecord
{
    [JsonPropertyName("direction")] public string? Direction { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("type")] public string? Type { get; init; }
}

internal sealed class AssemblyRecord
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("files")] public List<string>? Files { get; init; }
}

internal sealed class XmlPrefixRecord
{
    [JsonPropertyName("prefix")] public string? Prefix { get; init; }
    [JsonPropertyName("namespace")] public string Namespace { get; init; } = string.Empty;
}

internal sealed class TriggerModelRecord
{
    [JsonPropertyName("hasAny")] public bool HasAny { get; init; }
    [JsonPropertyName("hasLocal")] public bool HasLocal { get; init; }
    [JsonPropertyName("hasStart")] public bool HasStart { get; init; }
    [JsonPropertyName("hasIntegration")] public bool HasIntegration { get; init; }
}
