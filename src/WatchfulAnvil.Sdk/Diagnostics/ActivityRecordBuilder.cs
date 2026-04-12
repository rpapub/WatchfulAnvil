// <copyright file="ActivityRecordBuilder.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

#pragma warning disable SA1402 // File may only contain a single type — intentional: internal record types co-located with their builder

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using UiPath.Studio.Activities.Api.PackageBindings;
using UiPath.Studio.Analyzer.Models;

namespace WatchfulAnvil.Sdk.Diagnostics;

/// <summary>
/// Builds the JSON string for one activity record conforming to
/// <c>tap-activity-record.schema.json v0.1.0</c>.
/// </summary>
public static class ActivityRecordBuilder
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = false, // JSONL: one line per record
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>
    /// Builds and serialises the activity record to a single-line JSON string.
    /// Never throws; field-level failures are recorded in the <c>_errors</c> array.
    /// </summary>
    public static string Build(IActivityModel activity, RunContext run)
    {
        var errors = new List<TapError>();

        // ── Identity ─────────────────────────────────────────────────────────
        var id = SafeGet(() => activity.UiPathActivityTypeId, "id", errors);
        var fullName = SafeGet(() => activity.Type, "fullName", errors);

        // Resolve the actual CLR Type for attribute reflection.
        Type? actType = TryResolveType(fullName);

        var displayName = SafeGet(() => activity.DisplayName ?? string.Empty, "displayName", errors) ?? string.Empty;
        var description = actType != null
            ? ReflectionHelper.GetDescription(actType)
            : null;
        var category = SafeGet(() => activity.ToolboxName, "category", errors);

        // visibility: check BrowsableAttribute + known UiPath hidden/legacy markers.
        var visibility = DeriveVisibility(actType, errors);

        var hasGenericParameters = SafeGet<bool?>(
            () => actType?.IsGenericTypeDefinition, "hasGenericParameters", errors);
        var genericParameterNames = SafeGet<string[]?>(
            () => actType?.IsGenericTypeDefinition == true
                ? actType.GetGenericArguments().Select(t => t.Name).ToArray()
                : Array.Empty<string>(),
            "genericParameterNames", errors);

        var (isObsolete, obsoleteMessage) = actType != null
            ? ReflectionHelper.GetObsolete(actType)
            : (null, null);

        // ── Package binding ──────────────────────────────────────────────────
        var primaryBinding = SafeGet(
            () => activity.PackageBindings?.FirstOrDefault(), "packageBinding", errors);

        var sourceId = SafeGet(
            () => primaryBinding?.Key.ToString()?.ToLowerInvariant(), "sourceId", errors);
        var sourceVersion = SafeGet(
            () => ProjectJsonReader.ResolveVersion(run.ProjectFilePath, sourceId),
            "sourceVersion", errors);

        // ── XML namespace ────────────────────────────────────────────────────
        var xmlNamespace = SafeGet(
            () => ResolveXmlNamespace(actType, fullName), "xmlNamespace", errors);

        // ── Instance/position ────────────────────────────────────────────────
        var instanceId = SafeGet(() => activity.Id, "instanceId", errors);
        var annotationText = SafeGet(() => activity.AnnotationText, "annotationText", errors);
        var isRootOrContainer = SafeGet(() => activity.IsRootOrActivityContainer, "isRootOrActivityContainer", errors);
        var parentDisplayName = SafeGet(() => activity.Parent?.DisplayName, "parentDisplayName", errors);
        var depthFromRoot = SafeGet(() => ComputeDepth(activity), "depthFromRoot", errors);

        // ── Structural kind ──────────────────────────────────────────────────
        var structuralKind = SafeGet(() => DeriveStructuralKind(activity), "structuralKind", errors);

        // ── Concrete type ────────────────────────────────────────────────────
        ConcreteTypeRecord? concreteType = null;
        SafeGet<object?>(() =>
        {
            var t = activity.GetType();
            concreteType = new ConcreteTypeRecord
            {
                FullName = t.FullName ?? t.Name,
                AssemblyName = t.Assembly.GetName().Name ?? string.Empty,
                AssemblyVersion = t.Assembly.GetName().Version?.ToString() ?? string.Empty,
            };
            return null;
        }, "concreteType", errors);

        // ── Workflow context ─────────────────────────────────────────────────
        var projectFilePath = run.ProjectFilePath;
        var rootId = SafeGet(() => FindRoot(activity)?.Id, "workflowRelativePath", errors);
        var workflowRelativePath = run.GetWorkflowRelativePath(rootId);

        // ── Package binding record ───────────────────────────────────────────
        PackageBindingRecord? packageBindingRecord = null;
        if (primaryBinding != null)
        {
            SafeGet<object?>(() =>
            {
                packageBindingRecord = new PackageBindingRecord
                {
                    Key = primaryBinding.Key.ToString() ?? string.Empty,
                    BindingType = primaryBinding.BindingType?.ToString() ?? "NuGet",
                    Description = null, // not available on IPackageBindingModel
                };
                return null;
            }, "packageBinding", errors);
        }

        // ── Variables (union of enclosing scopes via parent walk) ────────────
        var scopedVariables = SafeGet(
            () => CollectScopedVariables(activity), "scopedVariables", errors)
            ?? new List<VariableRecord>();

        // ── Workflow arguments (from root activity) ──────────────────────────
        var workflowArguments = SafeGet(
            () => CollectWorkflowArguments(activity), "workflowArguments", errors)
            ?? new List<ArgumentRecord>();

        // ── Members ──────────────────────────────────────────────────────────
        var members = SafeGet(
            () => BuildMembers(activity, actType, errors), "members", errors)
            ?? new List<MemberRecord>();

        // ── Assemble record ──────────────────────────────────────────────────
        var record = new ActivityRecord
        {
            SchemaVersion = "0.1.0",
            RunId = run.RunId,
            CapturedAt = DateTime.UtcNow,
            ThreadId = Environment.CurrentManagedThreadId,
            Id = id,
            FullName = fullName,
            DisplayName = displayName,
            Description = description,
            Category = category,
            Visibility = visibility,
            HasGenericParameters = hasGenericParameters,
            GenericParameterNames = genericParameterNames,
            IsObsolete = isObsolete,
            ObsoleteMessage = obsoleteMessage,
            SourceId = sourceId,
            SourceVersion = sourceVersion,
            XmlNamespace = xmlNamespace,
            InstanceId = instanceId,
            AnnotationText = annotationText,
            IsRootOrActivityContainer = isRootOrContainer,
            ParentDisplayName = parentDisplayName,
            DepthFromRoot = depthFromRoot,
            CallChainDepth = null,
            StructuralKind = structuralKind,
            ConcreteType = concreteType,
            ProjectFilePath = projectFilePath,
            WorkflowRelativePath = workflowRelativePath,
            PackageBinding = packageBindingRecord,
            ScopedVariables = scopedVariables,
            WorkflowArguments = workflowArguments,
            Members = members,
            Errors = errors,
        };

        return JsonSerializer.Serialize(record, s_jsonOptions);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static T SafeGet<T>(Func<T> getter, string field, List<TapError> errors, T fallback = default!)
        => ReflectionHelper.SafeReflect(getter, field, errors, fallback);

    private static Type? TryResolveType(string? fullName)
    {
        if (string.IsNullOrEmpty(fullName))
        {
            return null;
        }

        try
        {
            // Try direct lookup first (works when the name is assembly-qualified).
            var t = Type.GetType(fullName, throwOnError: false);
            if (t != null)
            {
                return t;
            }

            // Search loaded assemblies for the type by full name.
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType(fullName, throwOnError: false);
                    if (t != null)
                    {
                        return t;
                    }
                }
                catch
                {
                    // skip inaccessible assemblies
                }
            }
        }
        catch
        {
            // return null below
        }

        return null;
    }

    private static string? DeriveVisibility(Type? actType, List<TapError> errors)
    {
        if (actType == null)
        {
            return null;
        }

        try
        {
            // Check BrowsableAttribute(false) → hidden
            var browsable = ReflectionHelper.GetBrowsable(actType);
            if (browsable == false)
            {
                return "hidden";
            }

            // Check for UiPath-specific legacy markers by attribute simple name.
            var attrs = actType.GetCustomAttributes(inherit: true);
            foreach (var attr in attrs)
            {
                var name = attr.GetType().Name;
                if (name == "ObsoleteAttribute")
                {
                    return "legacy";
                }

                // UiPath uses "ObsoleteActivityAttribute" in some versions.
                if (name == "ObsoleteActivityAttribute")
                {
                    return "legacy";
                }
            }

            return "browsable";
        }
        catch (Exception ex)
        {
            errors.Add(new TapError
            {
                Field = "visibility",
                ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                Message = ex.Message,
            });
            return null;
        }
    }

    private static int ComputeDepth(IActivityModel activity)
    {
        int depth = 0;
        var current = activity;
        while (current != null)
        {
            if (current.IsRootOrActivityContainer)
            {
                depth++;
            }

            current = current.Parent;
        }

        // Subtract 1 because the root itself counts as depth 0.
        return Math.Max(0, depth - 1);
    }

    private static string? DeriveStructuralKind(IActivityModel activity)
    {
        if (!activity.IsRootOrActivityContainer)
        {
            return null;
        }

        // Cast-based classification using SDK structural interfaces.
        if (activity is IFlowchartModel)
        {
            return "flowchart";
        }

        if (activity is IStateMachineModel)
        {
            return "stateMachine";
        }

        if (activity is ITryCatchModel)
        {
            return "tryCatch";
        }

        if (activity is ISwitchModel)
        {
            return "switch";
        }

        if (activity is IPickModel)
        {
            return "pick";
        }

        if (activity is IIfElseModel)
        {
            return "ifElse";
        }

        return "sequence"; // default for all non-specialised containers
    }

    private static string? ResolveXmlNamespace(Type? actType, string? fullName)
    {
        if (actType == null && string.IsNullOrEmpty(fullName))
        {
            return null;
        }

        var targetAssembly = actType?.Assembly;
        if (targetAssembly == null)
        {
            return null;
        }

        string? bestNamespace = null;
        int bestLength = -1;

        // XmlnsDefinitionAttribute maps CLR namespaces to XML namespaces.
        // Resolved via reflection to avoid a hard WindowsBase/WPF reference.
        var xmlnsDefAttrType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a =>
            {
                try
                {
                    return a.GetType("System.Windows.Markup.XmlnsDefinitionAttribute");
                }
                catch
                {
                    return null;
                }
            })
            .FirstOrDefault(t => t != null);

        if (xmlnsDefAttrType == null)
        {
            return null;
        }

        var xmlnsDefAttrs = targetAssembly.GetCustomAttributes(xmlnsDefAttrType, inherit: false);
        var clrNsProp = xmlnsDefAttrType.GetProperty("ClrNamespace");
        var xmlNsProp = xmlnsDefAttrType.GetProperty("XmlNamespace");

        foreach (var attr in xmlnsDefAttrs)
        {
            var clrNs = clrNsProp?.GetValue(attr) as string ?? string.Empty;
            var typeNs = actType?.Namespace ?? string.Empty;

            if (typeNs.StartsWith(clrNs, StringComparison.Ordinal) && clrNs.Length > bestLength)
            {
                bestNamespace = xmlNsProp?.GetValue(attr) as string;
                bestLength = clrNs.Length;
            }
        }

        return bestNamespace;
    }

    private static IActivityModel? FindRoot(IActivityModel activity)
    {
        var current = activity;
        while (current?.Parent != null)
        {
            current = current.Parent;
        }

        return current;
    }

    private static List<VariableRecord> CollectScopedVariables(IActivityModel activity)
    {
        // Walk up the parent chain and collect variables from each container scope.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<VariableRecord>();
        var current = activity;

        while (current != null)
        {
            foreach (var v in current.Variables ?? Enumerable.Empty<IVariableModel>())
            {
                if (seen.Add(v.DisplayName ?? string.Empty))
                {
                    result.Add(new VariableRecord
                    {
                        Name = v.DisplayName ?? string.Empty,
                        Type = v.Type?.ToString(),
                        DefaultExpression = v.DefinedExpression,
                    });
                }
            }

            current = current.Parent;
        }

        return result;
    }

    private static List<ArgumentRecord> CollectWorkflowArguments(IActivityModel activity)
    {
        var root = FindRoot(activity);
        if (root == null)
        {
            return new List<ArgumentRecord>();
        }

        return (root.Arguments ?? Enumerable.Empty<IArgumentModel>())
            .Select(a => new ArgumentRecord
            {
                Direction = MapDirection(a.Direction),
                Name = a.DisplayName ?? string.Empty,
                Type = a.Type?.ToString(),
            })
            .ToList();
    }

    private static List<MemberRecord> BuildMembers(
        IActivityModel activity,
        Type? actType,
        List<TapError> errors)
    {
        var members = new List<MemberRecord>();

        // ── Arguments (argument memberKind) ──────────────────────────────────
        foreach (var arg in activity.Arguments ?? Enumerable.Empty<IArgumentModel>())
        {
            var memberErrors = new List<TapError>();
            PropertyInfo? prop = actType?.GetProperty(arg.DisplayName ?? string.Empty);

            // Three-step enum resolution. IArgumentModel.Type is a string in the SDK.
            var enumValues = ReflectionHelper.ResolveEnumValues(null, arg.Type, memberErrors);
            errors.AddRange(memberErrors.Select(e => new TapError
            {
                Field = $"members[{arg.DisplayName}].{e.Field}",
                ExceptionType = e.ExceptionType,
                Message = e.Message,
            }));

            members.Add(new MemberRecord
            {
                Name = arg.DisplayName ?? string.Empty,
                MemberKind = "argument",
                DisplayName = prop != null ? ReflectionHelper.GetDisplayName(prop) : arg.DisplayName,
                Description = prop != null ? ReflectionHelper.GetDescription(prop) : null,
                Category = prop != null ? ReflectionHelper.GetCategory(prop) : null,
                DataType = prop?.PropertyType.ToString(),
                ArgumentDirection = MapDirection(arg.Direction),
                IsRequired = prop != null ? ReflectionHelper.GetIsRequired(prop) : null,
                IsBrowsable = prop != null ? ReflectionHelper.GetBrowsable(prop) : null,
                DefaultValue = prop != null ? ReflectionHelper.GetDefaultValue(prop) : null,
                TypeConverter = prop != null ? ReflectionHelper.GetTypeConverter(prop) : null,
                IsObsolete = prop != null ? ReflectionHelper.GetObsolete(prop).IsObsolete : null,
                ObsoleteMessage = prop != null ? ReflectionHelper.GetObsolete(prop).Message : null,
                DelegateArgumentName = null,
                EnumValues = enumValues,
                DefinedExpression = arg.DefinedExpression,
                HasLiteralExpression = arg.HasLiteralExpression,
                ChildActivityDisplayName = null,
            });
        }

        // ── Properties (property memberKind) ─────────────────────────────────
        foreach (var prop in activity.Properties ?? Enumerable.Empty<IPropertyModel>())
        {
            PropertyInfo? propInfo = actType?.GetProperty(prop.DisplayName ?? string.Empty);

            members.Add(new MemberRecord
            {
                Name = prop.DisplayName ?? string.Empty,
                MemberKind = "property",
                DisplayName = propInfo != null ? ReflectionHelper.GetDisplayName(propInfo) : prop.DisplayName,
                Description = propInfo != null ? ReflectionHelper.GetDescription(propInfo) : null,
                Category = propInfo != null ? ReflectionHelper.GetCategory(propInfo) : null,
                DataType = propInfo?.PropertyType.ToString(),
                ArgumentDirection = null,
                IsRequired = null,
                IsBrowsable = propInfo != null ? ReflectionHelper.GetBrowsable(propInfo) : null,
                DefaultValue = propInfo != null ? ReflectionHelper.GetDefaultValue(propInfo) : null,
                TypeConverter = propInfo != null ? ReflectionHelper.GetTypeConverter(propInfo) : null,
                IsObsolete = propInfo != null ? ReflectionHelper.GetObsolete(propInfo).IsObsolete : null,
                ObsoleteMessage = propInfo != null ? ReflectionHelper.GetObsolete(propInfo).Message : null,
                DelegateArgumentName = null,
                EnumValues = null,
                DefinedExpression = prop.DefinedExpression,
                HasLiteralExpression = null,
                ChildActivityDisplayName = null,
            });
        }

        // ── Child slots (child memberKind) via reflection ─────────────────────
        // System.Activities types are not in the compile-time references; use name matching.
        if (actType != null)
        {
            foreach (var propInfo in actType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var pt = propInfo.PropertyType;
                var ptName = pt.FullName ?? pt.Name;
                bool isChildSlot = ptName.StartsWith("System.Activities.Activity", StringComparison.Ordinal)
                                || ptName.StartsWith("System.Activities.ActivityDelegate", StringComparison.Ordinal)
                                || (ptName.StartsWith("System.Activities.InArgument", StringComparison.Ordinal) == false
                                    && (pt.Name == "Activity" || pt.Name == "ActivityDelegate"));
                if (!isChildSlot)
                {
                    continue;
                }

                // Look for the placed child activity display name from the activity's children.
                string? childDisplayName = null;
                try
                {
                    // Children of this activity whose parent is this activity.
                    // The slot name matches if the child.Id starts with the property name (heuristic).
                    childDisplayName = activity.Children?
                        .FirstOrDefault(c => c.Id?.StartsWith(propInfo.Name,
                            StringComparison.OrdinalIgnoreCase) == true)
                        ?.DisplayName;
                }
                catch
                {
                    // best-effort; childDisplayName stays null
                }

                var (isObs, obsMsg) = ReflectionHelper.GetObsolete(propInfo);

                members.Add(new MemberRecord
                {
                    Name = propInfo.Name,
                    MemberKind = "child",
                    DisplayName = ReflectionHelper.GetDisplayName(propInfo),
                    Description = ReflectionHelper.GetDescription(propInfo),
                    Category = ReflectionHelper.GetCategory(propInfo),
                    DataType = pt.ToString(),
                    ArgumentDirection = null,
                    IsRequired = null,
                    IsBrowsable = ReflectionHelper.GetBrowsable(propInfo),
                    DefaultValue = null,
                    TypeConverter = ReflectionHelper.GetTypeConverter(propInfo),
                    IsObsolete = isObs,
                    ObsoleteMessage = obsMsg,
                    DelegateArgumentName = DeriveActivityDelegateName(activity, propInfo.Name),
                    EnumValues = null,
                    DefinedExpression = null,
                    HasLiteralExpression = null,
                    ChildActivityDisplayName = childDisplayName,
                });
            }
        }

        return members;
    }

    private static string? DeriveActivityDelegateName(IActivityModel activity, string slotName)
    {
        // ForEach and similar activities expose delegate arguments on the activity model.
        // Match by the slot property name (heuristic: delegate args live in DelegateArguments).
        try
        {
            return activity.DelegateArguments?
                .FirstOrDefault()
                ?.DisplayName;
        }
        catch
        {
            return null;
        }
    }

    private static string? MapDirection(object? direction)
    {
        if (direction == null)
        {
            return null;
        }

        var s = direction.ToString()?.ToLowerInvariant() ?? string.Empty;
        return s switch
        {
            "in" => "in",
            "out" => "out",
            "inout" => "inout",
            _ => s.Contains("in") && s.Contains("out") ? "inout"
                 : s.Contains("out") ? "out"
                 : s.Contains("in") ? "in"
                 : null,
        };
    }
}

// ── Record POCOs ─────────────────────────────────────────────────────────────

internal sealed class ActivityRecord
{
    [JsonPropertyName("schemaVersion")] public string SchemaVersion { get; init; } = "0.1.0";
    [JsonPropertyName("runId")] public string RunId { get; init; } = string.Empty;
    [JsonPropertyName("capturedAt")] public DateTime CapturedAt { get; init; }
    [JsonPropertyName("threadId")] public int ThreadId { get; init; }
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("fullName")] public string? FullName { get; init; }
    [JsonPropertyName("displayName")] public string DisplayName { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("category")] public string? Category { get; init; }
    [JsonPropertyName("visibility")] public string? Visibility { get; init; }
    [JsonPropertyName("hasGenericParameters")] public bool? HasGenericParameters { get; init; }
    [JsonPropertyName("genericParameterNames")] public string[]? GenericParameterNames { get; init; }
    [JsonPropertyName("isObsolete")] public bool? IsObsolete { get; init; }
    [JsonPropertyName("obsoleteMessage")] public string? ObsoleteMessage { get; init; }
    [JsonPropertyName("sourceId")] public string? SourceId { get; init; }
    [JsonPropertyName("sourceVersion")] public string? SourceVersion { get; init; }
    [JsonPropertyName("xmlNamespace")] public string? XmlNamespace { get; init; }
    [JsonPropertyName("instanceId")] public string? InstanceId { get; init; }
    [JsonPropertyName("annotationText")] public string? AnnotationText { get; init; }
    [JsonPropertyName("isRootOrActivityContainer")] public bool IsRootOrActivityContainer { get; init; }
    [JsonPropertyName("parentDisplayName")] public string? ParentDisplayName { get; init; }
    [JsonPropertyName("depthFromRoot")] public int DepthFromRoot { get; init; }
    [JsonPropertyName("callChainDepth")] public object? CallChainDepth { get; init; }
    [JsonPropertyName("structuralKind")] public string? StructuralKind { get; init; }
    [JsonPropertyName("concreteType")] public ConcreteTypeRecord? ConcreteType { get; init; }
    [JsonPropertyName("projectFilePath")] public string ProjectFilePath { get; init; } = string.Empty;
    [JsonPropertyName("workflowRelativePath")] public string? WorkflowRelativePath { get; init; }
    [JsonPropertyName("packageBinding")] public PackageBindingRecord? PackageBinding { get; init; }
    [JsonPropertyName("scopedVariables")] public List<VariableRecord> ScopedVariables { get; init; } = new();
    [JsonPropertyName("workflowArguments")] public List<ArgumentRecord> WorkflowArguments { get; init; } = new();
    [JsonPropertyName("members")] public List<MemberRecord> Members { get; init; } = new();
    [JsonPropertyName("_errors")] public List<TapError> Errors { get; init; } = new();
}

internal sealed class ConcreteTypeRecord
{
    [JsonPropertyName("fullName")] public string FullName { get; init; } = string.Empty;
    [JsonPropertyName("assemblyName")] public string AssemblyName { get; init; } = string.Empty;
    [JsonPropertyName("assemblyVersion")] public string AssemblyVersion { get; init; } = string.Empty;
}

internal sealed class PackageBindingRecord
{
    [JsonPropertyName("key")] public string Key { get; init; } = string.Empty;
    [JsonPropertyName("bindingType")] public string BindingType { get; init; } = "NuGet";
    [JsonPropertyName("description")] public string? Description { get; init; }
}

internal sealed class VariableRecord
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("type")] public string? Type { get; init; }
    [JsonPropertyName("defaultExpression")] public string? DefaultExpression { get; init; }
}

internal sealed class ArgumentRecord
{
    [JsonPropertyName("direction")] public string? Direction { get; init; }
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("type")] public string? Type { get; init; }
}

internal sealed class MemberRecord
{
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("memberKind")] public string MemberKind { get; init; } = "argument";
    [JsonPropertyName("displayName")] public string? DisplayName { get; init; }
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("category")] public string? Category { get; init; }
    [JsonPropertyName("dataType")] public string? DataType { get; init; }
    [JsonPropertyName("argumentDirection")] public string? ArgumentDirection { get; init; }
    [JsonPropertyName("isRequired")] public bool? IsRequired { get; init; }
    [JsonPropertyName("isBrowsable")] public bool? IsBrowsable { get; init; }
    [JsonPropertyName("defaultValue")] public string? DefaultValue { get; init; }
    [JsonPropertyName("typeConverter")] public string? TypeConverter { get; init; }
    [JsonPropertyName("isObsolete")] public bool? IsObsolete { get; init; }
    [JsonPropertyName("obsoleteMessage")] public string? ObsoleteMessage { get; init; }
    [JsonPropertyName("delegateArgumentName")] public string? DelegateArgumentName { get; init; }
    [JsonPropertyName("enumValues")] public string[]? EnumValues { get; init; }
    [JsonPropertyName("definedExpression")] public string? DefinedExpression { get; init; }
    [JsonPropertyName("hasLiteralExpression")] public bool? HasLiteralExpression { get; init; }
    [JsonPropertyName("childActivityDisplayName")] public string? ChildActivityDisplayName { get; init; }
}
