// <copyright file="ReflectionHelperTests.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;

using WatchfulAnvil.Sdk.Diagnostics;

using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Diagnostics
{
    /// <summary>
    /// Tests for <see cref="ReflectionHelper"/> — CLR attribute extraction and SafeReflect guard.
    /// Uses purpose-built test types decorated with known attributes for deterministic assertion.
    /// </summary>
    public sealed class ReflectionHelperTests
    {
        // ── Test fixture types ───────────────────────────────────────────────

        [Description("My description")]
        [Category("My category")]
        [DisplayName("My display name")]
        [Obsolete("Use something else")]
        [Browsable(false)]
        [DefaultValue("default-val")]
        private sealed class AnnotatedType { }

        private sealed class PlainType { }

        private enum SampleEnum { Alpha, Beta, Gamma }

        // ── SafeReflect ──────────────────────────────────────────────────────

        [Fact]
        public void SafeReflect_ReturnsValue_WhenGetterSucceeds()
        {
            var errors = new List<TapError>();
            var result = ReflectionHelper.SafeReflect(() => 42, "field", errors);
            Assert.Equal(42, result);
            Assert.Empty(errors);
        }

        [Fact]
        public void SafeReflect_ReturnsFallback_AndRecordsError_WhenGetterThrows()
        {
            var errors = new List<TapError>();
            var result = ReflectionHelper.SafeReflect<string>(
                () => throw new InvalidOperationException("boom"), "myField", errors, fallback: "FB");
            Assert.Equal("FB", result);
            Assert.Single(errors);
            Assert.Equal("myField", errors[0].Field);
            Assert.Contains("boom", errors[0].Message);
        }

        [Fact]
        public void SafeReflect_ErrorEntry_ContainsExceptionTypeName()
        {
            var errors = new List<TapError>();
            ReflectionHelper.SafeReflect<int>(
                () => throw new ArgumentNullException("p"), "f", errors);
            Assert.Equal("System.ArgumentNullException", errors[0].ExceptionType);
        }

        // ── Attribute extraction ─────────────────────────────────────────────

        [Fact]
        public void GetDescription_ReturnsAttributeValue()
            => Assert.Equal("My description", ReflectionHelper.GetDescription(typeof(AnnotatedType)));

        [Fact]
        public void GetDescription_ReturnsNull_WhenAbsent()
            => Assert.Null(ReflectionHelper.GetDescription(typeof(PlainType)));

        [Fact]
        public void GetCategory_ReturnsAttributeValue()
            => Assert.Equal("My category", ReflectionHelper.GetCategory(typeof(AnnotatedType)));

        [Fact]
        public void GetDisplayName_ReturnsAttributeValue()
            => Assert.Equal("My display name", ReflectionHelper.GetDisplayName(typeof(AnnotatedType)));

        [Fact]
        public void GetObsolete_ReturnsTrue_WhenPresent()
        {
            var (isObs, msg) = ReflectionHelper.GetObsolete(typeof(AnnotatedType));
            Assert.True(isObs);
            Assert.Equal("Use something else", msg);
        }

        [Fact]
        public void GetObsolete_ReturnsFalse_WhenAbsent()
        {
            var (isObs, msg) = ReflectionHelper.GetObsolete(typeof(PlainType));
            Assert.False(isObs);
            Assert.Null(msg);
        }

        [Fact]
        public void GetBrowsable_ReturnsFalse_WhenAttributeIsFalse()
            => Assert.False(ReflectionHelper.GetBrowsable(typeof(AnnotatedType)));

        [Fact]
        public void GetBrowsable_ReturnsNull_WhenAttributeAbsent()
            => Assert.Null(ReflectionHelper.GetBrowsable(typeof(PlainType)));

        [Fact]
        public void GetDefaultValue_ReturnsAttributeValue()
            => Assert.Equal("default-val", ReflectionHelper.GetDefaultValue(typeof(AnnotatedType)));

        // ── ResolveEnumValues ────────────────────────────────────────────────

        [Fact]
        public void ResolveEnumValues_Step1_ReturnsNames_WhenTypeIsEnum()
        {
            var errors = new List<TapError>();
            var result = ReflectionHelper.ResolveEnumValues(typeof(SampleEnum), null, errors);
            Assert.NotNull(result);
            Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, result);
            Assert.Empty(errors);
        }

        [Fact]
        public void ResolveEnumValues_Step1_ReturnsEmpty_WhenTypeIsNotEnum()
        {
            var errors = new List<TapError>();
            var result = ReflectionHelper.ResolveEnumValues(typeof(string), null, errors);
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void ResolveEnumValues_Step2_ResolvesViaTypeName()
        {
            // System.DayOfWeek is a well-known enum accessible via Type.GetType.
            var errors = new List<TapError>();
            var result = ReflectionHelper.ResolveEnumValues(null, "System.DayOfWeek", errors);
            Assert.NotNull(result);
            Assert.Contains("Monday", result);
        }

        [Fact]
        public void ResolveEnumValues_Step2_AddsErrorEntry_ForFallback()
        {
            var errors = new List<TapError>();
            ReflectionHelper.ResolveEnumValues(null, "System.DayOfWeek", errors);
            Assert.Single(errors);
            Assert.Equal("TypeResolutionFallback", errors[0].ExceptionType);
        }

        [Fact]
        public void ResolveEnumValues_Step3_ReturnsNull_WhenUnresolvable()
        {
            var errors = new List<TapError>();
            var result = ReflectionHelper.ResolveEnumValues(null, "No.Such.Type.Ever.Exists", errors);
            Assert.Null(result);
        }

        [Fact]
        public void ResolveEnumValues_Step3_AddsErrorEntry_WhenUnresolvable()
        {
            var errors = new List<TapError>();
            ReflectionHelper.ResolveEnumValues(null, "No.Such.Type.Ever.Exists", errors);
            Assert.Contains(errors, e => e.ExceptionType == "TypeResolutionFailed");
        }

        [Fact]
        public void ResolveEnumValues_BothNull_ReturnsNull()
        {
            var errors = new List<TapError>();
            var result = ReflectionHelper.ResolveEnumValues(null, null, errors);
            Assert.Null(result);
        }
    }
}
