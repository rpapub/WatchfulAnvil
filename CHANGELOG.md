# Changelog

All notable user-facing changes are documented here.

---

## [Unreleased]

### ⚠️ Breaking — `WatchfulAnvil.Sdk` 0.1.2-alpha → 0.2.0-alpha

Source-breaking for anyone deriving from the SDK base classes directly. No rule class
that derives through `ScopedRule` / `ActivityRule` / `WorkflowRule` / `ProjectRule` is
affected, and no in-repo caller had to change.

- `AnalyzerBase.Info(string)` → `Info(Rule, string)`. It now sets `RecommendationMessage`,
  as `Fail` already did; an Info result previously reached Studio with an empty
  recommendation column.
- `IRegisterAnalyzerConfiguration` and `Initialize` moved off `AnalyzerBase` down to
  `RuleBase<T>` and `CounterBase<T>`. `AnalyzerBase` is now shared helpers only — the
  `InspectionResult` factories and the feature gate — not a registration contract.

### 🐛 Behaviour changes in annotation handling

These change which violations are reported. They are fixes, but they are not silent.

- **`@suppress` and `@violates` now honour every directive, not just the first.** Both
  resolved through `GetTagValue`, which returns the first match and stops, so given
  `@suppress:RULE-A @suppress:RULE-B` only `RULE-A` was ever detected — `RULE-B` was
  unreachable and the suppression was silently ignored.
- **`@tag:VALUE` now counts as `@tag`.** `HasTag` compared whole tokens, so `@unit:Login`
  was reported as untagged. Every rule gated via `RequiresAnyTag` / `RequiresAllTags`
  (`LogMessageBookendsRule`, `ModuleCodedConfigRule`, `UnitOutStatusRule`,
  `PipelineDomainModelRule`, `PipelineSequenceOrderRule`) previously skipped such
  workflows entirely, and `PipelinePresenceCounter` / `WorkflowTypeRatioRule` were
  undercounting them.
- **Project-scoped rules can now be suppressed.** A project carries no annotation, so
  `@nocheck` and `@suppress:RULE-ID` were inert on `IProjectModel`. `ScopedRule` now joins
  every workflow's root annotation. Note the reach: one `@suppress:RULE-ID` on one
  workflow suppresses that rule for the *whole* project, and one `@nocheck` anywhere
  silences every project rule.

Verified against the corpus harness: identical results before and after
(19 passed / 6 failed / 3 skipped, per-test identical). The corpus uses bare tags
throughout, so it confirms no regression rather than exercising the fixes; unit tests
cover those.

### 🚀 Features
- `tools/feed-versions/` reports what the repo pins against what the remote feeds offer
- `WatchfulAnvil.Build` shares the `UiPath.Activities.Api` version matrix and the net461
  language block across all 12 rule pack projects

### 🧹 Housekeeping
- Licensing corrected: Apache-2.0 for code, CC-BY-4.0 for documentation. Source headers
  claimed Apache-2.0 and pointed at a `LICENSE` file that did not exist, while every
  package shipped declaring CC-BY-4.0.
- Repaired the `CPRIMA` test project, which had not compiled since suppression moved into
  the SDK

---

## [0.9.0] – YYYY-MM-DD

Public Beta – *HelloWorld Rule Authoring*

This release provides advanced UiPath developers with a complete, hands-on guide to author, deploy, and test a basic custom Workflow Analyzer rule inside UiPath Studio.

### 🚀 Features
- Customizable HelloWorld rule template detecting `Log Message` activities with "HelloWorld!" content
- Visual Studio and CLI build instructions for analyzer rules
- Manual packaging into `.nupkg` via `dotnet pack` and project settings
- Deployment guide using local NuGet folder source for Studio integration
- Rule verification checklist and troubleshooting guidance in UiPath Studio
- Sample test workflows to validate rule triggering and behavior

### 📚 Documentation
- Prerequisite checklist for rule authoring (OS, Studio, SDKs, tools)
- Setup and verification of development environment
- Forking, cloning, and adapting the HelloWorld rule template
- Full walkthroughs for build, pack, and deployment steps
- Annotated examples and rule behavior explanation

---

## [0.1.0] – YYYY-MM-DD

Internal Foundation Release

Sets up the infrastructure, build system, and packaging mechanics required to support future rule authoring and distribution. This release is not intended for end users.

### 🧰 Internal & Infrastructure

- **CI/CD Setup**
  - [84ec7fb] First CI with GitHub Actions and GitVersion  
  - [e82fd2d] GitHub Action build now triggered by tag push  
  - [aa56c41] Added manual workflow dispatch  
  - [5be759a] Prepare UiPath API DLLs before build  
  - [143071d] Add UiPath NuGet source to workflow  
  - [60deddd] Fix: use GitVersion with dotnet build/pack  
  - [a0800de] Restructure workflows; add publish + installer scaffolding

- **Installer & Packaging**
  - [3d745d4] Initial EXE installer (Inno Setup)  
  - [819215f] Fix: prevent user-mode fallback duplication  
  - [f6473a7] Add template directory for future rules  
  - [0452638] Add metadata to `csproj`

- **Analyzer Rule Projects**
  - [dffcb99] Parallel project: `CPM.WorkflowAnalyzerRules`  
  - [081e941] Rename solution to `WatchfulAnvil`  
  - [b775f27] Implement `ShouldStopActivityRule` (initial logic)  
  - [73a594d] Add `CPM-NOOP-004` test/debug rule  
  - [625b6e8] Add mocked unit test for rule  
  - [0682661] Remove unsupported `net9.0` target  
  - [b25d0c6] Restructure project for local UiPath API references  
  - [3d745d4] Create installer for WatchfulAnvil rule distribution

> 🔒 *Internal release used to prepare the tooling and rule infrastructure for future user-facing educational content.*
