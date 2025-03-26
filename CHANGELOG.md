# Changelog

All notable user-facing changes are documented here.

---

## [Unreleased]

### 🚀 Features
- Placeholder for enhancements beyond v0.9.0

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
