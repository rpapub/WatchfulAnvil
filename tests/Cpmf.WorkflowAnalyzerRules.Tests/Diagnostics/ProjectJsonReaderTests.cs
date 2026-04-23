// <copyright file="ProjectJsonReaderTests.cs" company="Christian Prior-Mamulyan">
// Copyright (c) 2026 Christian Prior-Mamulyan. Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.
// </copyright>

using System;
using System.IO;

using WatchfulAnvil.Sdk.Common;

using Xunit;

namespace Cpmf.WorkflowAnalyzerRules.Tests.Diagnostics
{
    /// <summary>
    /// Tests for <see cref="ProjectJsonReader"/> — resolves package version from project.json.
    /// </summary>
    public sealed class ProjectJsonReaderTests : IDisposable
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        public ProjectJsonReaderTests() => Directory.CreateDirectory(_tempDir);

        public void Dispose() => Directory.Delete(_tempDir, recursive: true);

        private string WriteProjectJson(string content)
        {
            var path = Path.Combine(_tempDir, "project.json");
            File.WriteAllText(path, content);
            return path;
        }

        // ── Null / missing input ─────────────────────────────────────────────

        [Fact]
        public void ReturnsNull_WhenProjectFilePathIsNull()
            => Assert.Null(ProjectJsonReader.ResolveVersion(null, "some.package"));

        [Fact]
        public void ReturnsNull_WhenPackageIdIsNull()
        {
            var path = WriteProjectJson("{}");
            Assert.Null(ProjectJsonReader.ResolveVersion(path, null));
        }

        [Fact]
        public void ReturnsNull_WhenFileDoesNotExist()
            => Assert.Null(ProjectJsonReader.ResolveVersion(Path.Combine(_tempDir, "missing.json"), "pkg"));

        [Fact]
        public void ReturnsNull_WhenJsonIsInvalid()
        {
            var path = WriteProjectJson("{ not valid json }}}");
            Assert.Null(ProjectJsonReader.ResolveVersion(path, "pkg"));
        }

        // ── Standard object-version format ───────────────────────────────────

        [Fact]
        public void ResolvesVersion_FromObjectFormat()
        {
            var path = WriteProjectJson(@"{
  ""dependencies"": {
    ""UiPath.System.Activities"": { ""version"": ""26.2.4"" }
  }
}");
            Assert.Equal("26.2.4", ProjectJsonReader.ResolveVersion(path, "UiPath.System.Activities"));
        }

        [Fact]
        public void ResolvesVersion_CaseInsensitiveKey()
        {
            var path = WriteProjectJson(@"{
  ""dependencies"": {
    ""UiPath.System.Activities"": { ""version"": ""26.2.4"" }
  }
}");
            Assert.Equal("26.2.4", ProjectJsonReader.ResolveVersion(path, "uipath.system.activities"));
        }

        [Fact]
        public void ResolvesVersion_FromStringValueFormat()
        {
            var path = WriteProjectJson(@"{
  ""dependencies"": {
    ""MyPackage"": ""1.0.0""
  }
}");
            Assert.Equal("1.0.0", ProjectJsonReader.ResolveVersion(path, "MyPackage"));
        }

        [Fact]
        public void ReturnsNull_WhenPackageKeyAbsent()
        {
            var path = WriteProjectJson(@"{
  ""dependencies"": {
    ""UiPath.System.Activities"": { ""version"": ""26.2.4"" }
  }
}");
            Assert.Null(ProjectJsonReader.ResolveVersion(path, "uipath.uiautomation.activities"));
        }

        [Fact]
        public void ReturnsNull_WhenDependenciesSectionAbsent()
        {
            var path = WriteProjectJson(@"{ ""name"": ""MyProject"" }");
            Assert.Null(ProjectJsonReader.ResolveVersion(path, "anything"));
        }

        [Fact]
        public void ReturnsNull_WhenVersionFieldAbsent()
        {
            var path = WriteProjectJson(@"{
  ""dependencies"": {
    ""MyPackage"": { ""someOtherField"": ""value"" }
  }
}");
            Assert.Null(ProjectJsonReader.ResolveVersion(path, "MyPackage"));
        }

        [Fact]
        public void ResolvesVersion_WithMultipleDependencies()
        {
            var path = WriteProjectJson(@"{
  ""dependencies"": {
    ""PackageA"": { ""version"": ""1.0.0"" },
    ""PackageB"": { ""version"": ""2.3.4"" },
    ""PackageC"": { ""version"": ""0.9.0-alpha"" }
  }
}");
            Assert.Equal("1.0.0", ProjectJsonReader.ResolveVersion(path, "PackageA"));
            Assert.Equal("2.3.4", ProjectJsonReader.ResolveVersion(path, "PackageB"));
            Assert.Equal("0.9.0-alpha", ProjectJsonReader.ResolveVersion(path, "PackageC"));
        }
    }
}
