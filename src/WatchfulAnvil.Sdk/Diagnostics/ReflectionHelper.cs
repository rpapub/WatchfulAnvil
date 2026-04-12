// <copyright file="ReflectionHelper.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

#pragma warning disable SA1402 // File may only contain a single type — intentional: internal record types co-located with their builder

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace WatchfulAnvil.Sdk.Diagnostics;

/// <summary>
/// Static helpers for safe CLR reflection over activity types and their members.
/// All public methods are exception-safe; callers receive a <see cref="ReflectResult{T}"/>
/// that carries either the value or the captured error.
/// </summary>
public static class ReflectionHelper
{
    // ── SafeReflect ─────────────────────────────────────────────────────────

    /// <summary>
    /// Executes <paramref name="getter"/> and returns its result.
    /// On any exception, records the failure in <paramref name="errors"/> under
    /// <paramref name="fieldName"/> and returns <paramref name="fallback"/>.
    /// </summary>
    public static T SafeReflect<T>(
        Func<T> getter,
        string fieldName,
        List<TapError> errors,
        T fallback = default!)
    {
        try
        {
            return getter();
        }
        catch (Exception ex)
        {
            errors.Add(new TapError
            {
                Field = fieldName,
                ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                Message = ex.Message,
            });
            return fallback;
        }
    }

    // ── Enum value resolution (three-step strategy) ─────────────────────────

    /// <summary>
    /// Resolves the valid enum member names for a member whose CLR type is known
    /// or can be derived from its type string.
    /// </summary>
    /// <param name="resolvedType">
    ///   The CLR <see cref="Type"/> already resolved by the SDK (e.g. from
    ///   <c>IArgumentModel.Type</c>). <c>null</c> triggers step 2.
    /// </param>
    /// <param name="dataTypeString">
    ///   The raw <see cref="PropertyInfo.PropertyType"/> string, used as a fallback
    ///   when <paramref name="resolvedType"/> is null.
    /// </param>
    /// <param name="errors">Collector for diagnostic entries when resolution partially fails.</param>
    /// <returns>
    ///   <c>string[]</c> of enum names when the type is a known enum;
    ///   empty array (<c>[]</c>) when the type is known and is NOT an enum;
    ///   <c>null</c> when type resolution fails entirely.
    /// </returns>
    public static string[]? ResolveEnumValues(
        Type? resolvedType,
        string? dataTypeString,
        List<TapError> errors)
    {
        // Step 1 — SDK-resolved type is directly available.
        if (resolvedType != null)
        {
            return resolvedType.IsEnum
                ? Enum.GetNames(resolvedType)
                : Array.Empty<string>();
        }

        // Step 2 — Attempt to resolve via the dataType string.
        if (!string.IsNullOrEmpty(dataTypeString))
        {
            try
            {
                // Strip generic parameter wrappers such as
                // "System.Activities.InArgument`1[System.SomeEnum]" → "System.SomeEnum"
                var innerType = ExtractInnerTypeName(dataTypeString);
                var t = Type.GetType(innerType, throwOnError: false);
                if (t != null)
                {
                    if (t.IsEnum)
                    {
                        errors.Add(new TapError
                        {
                            Field = "enumValues",
                            ExceptionType = "TypeResolutionFallback",
                            Message = $"Resolved via dataType string fallback: '{innerType}'.",
                        });
                        return Enum.GetNames(t);
                    }
                    return Array.Empty<string>();
                }
            }
            catch (Exception ex)
            {
                errors.Add(new TapError
                {
                    Field = "enumValues",
                    ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                    Message = $"Step 2 type-string resolution threw: {ex.Message}",
                });
            }
        }

        // Step 3 — Unresolvable.
        errors.Add(new TapError
        {
            Field = "enumValues",
            ExceptionType = "TypeResolutionFailed",
            Message = $"arg.Type null; dataType string resolution failed for '{dataTypeString}'.",
        });
        return null;
    }

    // ── Attribute extraction helpers ─────────────────────────────────────────

    /// <summary>Returns the <see cref="DescriptionAttribute"/> text for <paramref name="member"/>, or null.</summary>
    public static string? GetDescription(MemberInfo member)
    {
        try
        {
            return member
                .GetCustomAttribute<DescriptionAttribute>(inherit: true)
                ?.Description;
        }
        catch { return null; }
    }

    /// <summary>Returns the <see cref="CategoryAttribute"/> value for <paramref name="member"/>, or null.</summary>
    public static string? GetCategory(MemberInfo member)
    {
        try
        {
            return member
                .GetCustomAttribute<CategoryAttribute>(inherit: true)
                ?.Category;
        }
        catch { return null; }
    }

    /// <summary>Returns the <see cref="DisplayNameAttribute"/> value for <paramref name="member"/>, or null.</summary>
    public static string? GetDisplayName(MemberInfo member)
    {
        try
        {
            return member
                .GetCustomAttribute<DisplayNameAttribute>(inherit: true)
                ?.DisplayName;
        }
        catch { return null; }
    }

    /// <summary>
    /// Returns true when <see cref="ObsoleteAttribute"/> is present on <paramref name="member"/>,
    /// along with its message. Returns (null, null) on reflection failure.
    /// </summary>
    public static (bool? IsObsolete, string? Message) GetObsolete(MemberInfo member)
    {
        try
        {
            var attr = member.GetCustomAttribute<ObsoleteAttribute>(inherit: true);
            return attr == null ? (false, null) : (true, attr.Message);
        }
        catch { return (null, null); }
    }

    /// <summary>
    /// Returns the <see cref="BrowsableAttribute"/> value for <paramref name="member"/>:
    /// true/false when present, null when absent or on reflection failure.
    /// </summary>
    public static bool? GetBrowsable(MemberInfo member)
    {
        try
        {
            var attr = member.GetCustomAttribute<BrowsableAttribute>(inherit: true);
            return attr?.Browsable;
        }
        catch { return null; }
    }

    /// <summary>
    /// Returns the assembly-qualified type name of the <see cref="TypeConverterAttribute"/>
    /// declared on <paramref name="member"/>, or null when absent.
    /// </summary>
    public static string? GetTypeConverter(MemberInfo member)
    {
        try
        {
            return member
                .GetCustomAttribute<TypeConverterAttribute>(inherit: true)
                ?.ConverterTypeName;
        }
        catch { return null; }
    }

    /// <summary>
    /// Returns the string value of the <see cref="DefaultValueAttribute"/> declared
    /// on <paramref name="member"/>, or null when absent.
    /// </summary>
    public static string? GetDefaultValue(MemberInfo member)
    {
        try
        {
            return member
                .GetCustomAttribute<DefaultValueAttribute>(inherit: true)
                ?.Value?.ToString();
        }
        catch { return null; }
    }

    /// <summary>
    /// Checks for UiPath RequiredArgumentAttribute (matched by simple name to avoid
    /// a hard assembly dependency). Returns null on reflection failure.
    /// </summary>
    public static bool? GetIsRequired(MemberInfo member)
    {
        try
        {
            return member
                .GetCustomAttributes(inherit: true)
                .Any(a => a.GetType().Name == "RequiredArgumentAttribute");
        }
        catch { return null; }
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Extracts the innermost CLR type name from a generic wrapper string such as
    /// <c>System.Activities.InArgument`1[System.SomeEnum]</c>.
    /// Returns the input unchanged when no generic bracket is found.
    /// </summary>
    private static string ExtractInnerTypeName(string dataTypeString)
    {
        var start = dataTypeString.LastIndexOf('[');
        var end = dataTypeString.LastIndexOf(']');
        if (start >= 0 && end > start)
            return dataTypeString.Substring(start + 1, end - start - 1);
        return dataTypeString;
    }
}

// ── Error record ─────────────────────────────────────────────────────────────

/// <summary>
/// One structured entry in the <c>_errors</c> array of a TAP record.
/// Records a field-level capture failure without suppressing sibling fields.
/// </summary>
public sealed class TapError
{
    public string Field { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
