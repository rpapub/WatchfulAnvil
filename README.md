[![📘 Wiki](https://img.shields.io/badge/docs-Watchful%20Anvil%20Wiki-blue?style=flat-square&logo=readthedocs)](https://github.com/rpapub/WatchfulAnvil/wiki/Getting-Started-Hello-World)
[![🚀 Roadmap](https://img.shields.io/badge/roadmap-Milestones-orange?style=flat-square&logo=target)](./ROADMAP.md)


# WatchfulAnvil – UiPath Studio Workflow Analyzer Rules

As of 2025-Q2, Watchful Anvil is an early-phase initiative designed to evolve into a community-driven platform. Aimed at making custom Workflow Analyzer rules for UiPath Studio more accessible, maintainable, and practical, it serves as the bridge between UiPath’s theoretical extensibility and the real-world usability needed by teams enforcing code quality at scale. <!-- factual summary -->

<p align="center">
  <img src="https://rpapub.github.io/WatchfulAnvil/assets/img/logo/TheWatchfulAnvil.png" alt="Watchful Anvil Logo" height="200">
</p>

**Watchful Anvil exists to** make static code analysis in UiPath Studio practical and accessible for developers, contributors, and governance teams. Through verified examples, reusable templates, and educational resources, it empowers automation professionals to build, enforce, and maintain high-quality workflows. This is especially critical in a future shaped by LLM-generated “vibe code,” where validation must be deterministic, explainable, and enforceable.<!-- value-driven, forward-looking -->

> [!NOTE]
> [Why Vibe-Coded Code Still Needs Static Code Analysis →](https://github.com/rpapub/WatchfulAnvil/wiki/Project-WatchfulAnvil-Why-Vibe-Coded-Code-Still-needs-Deterministic-Validation)

## 📌 Project Goals

- Provide a reliable starting point for authoring custom rules  
- Support **multi-framework builds** (`net461`, `net6.0`, `net8.0`)  
- Enable Git-based collaboration and iterative experimentation  
- Lay the foundation for future educational content and tooling

## 🛣️ Project Roadmap

See [ROADMAP.md](https://github.com/rpapub/WatchfulAnvil/blob/main/ROADMAP.md) for current milestones, upcoming releases, and long-term vision.


## 🎯 Target Personas

> Watchful Anvil supports teams navigating the rise of **vibe coding**, where LLMs generate the code, and humans guide, refine, and validate it.

### 🧑‍💻 RPA Developer  
**Hands-on UiPath developers building and maintaining workflows.**

> **Goals:**  
> - Ship high-quality automations faster  
> - Catch issues early without waiting for manual review  
> - Learn how to build and test custom rules

> **Pain Points:**  
> - Custom rules are fragile, undocumented, and hard to debug  
> - Hard to get rules to show up in Studio  
> - No reliable examples or templates to start from

> **Watchful Anvil Helps:**  
> ✅ Provides working rule examples that actually load  
> ✅ Includes CI/CD and versioning practices to stabilize rule development
> ✅ Pre-built and customizable rules help enforce coding standards even as LLM-generated workflows (vibe coding) become more common—catching mistakes early when human review is minimal or bypassed.


### 🛠️ Rule Contributor  
**Developers or architects contributing rules to the Watchful Anvil project.**

> **Goals:**  
> - Create reusable, high-value rules that can benefit the broader community  
> - Follow project structure, testing, and documentation standards  
> - See their work adopted by enterprise teams or UiPath itself

> **Pain Points:**  
> - No established patterns or testing harness  
> - Limited visibility into rule behavior across Studio versions  
> - Hard to package or distribute rules consistently

> **Watchful Anvil Helps:**  
> ✅ Contributing rules to Watchful Anvil supports a future where vibe-coded automations are the norm, ensuring critical patterns and anti-patterns are detected regardless of who—or what—wrote the code.
> ✅ Benefits from a shared library of rules and templates, automated builds, and an installer for easy deployment 
> ✅ Aims for visibility and inclusion in broader rule ecosystems


### 🏛️ Automation Lead & RPA Program Owner  
**Leaders responsible for governance, quality, and automation strategy at scale.**

> **Goals:**  
> - Enforce consistent standards across automation teams  
> - Reduce review effort and tech debt with automated analysis  
> - Align code quality practices with enterprise expectations

> **Pain Points:**  
> - Manual reviews don’t scale  
> - Governance policies are not enforceable in tooling  
> - Lack of rule reuse, visibility, or lifecycle management

> **Watchful Anvil Helps:**  
> ✅ As vibe coding reshapes automation development, Watchful Anvil provides a scalable, enforceable layer of trust—enabling governance teams to apply consistent quality controls to human- and AI-authored workflows alike.


### 🧩 **Vendor (UiPath Product Team)**  
**Product managers, developer advocates, or engineers responsible for the Workflow Analyzer and extensibility features.**

> **Goals:**  
> - Deliver a robust, developer-friendly static analysis feature  
> - Encourage adoption of custom rules to support enterprise governance  
> - Improve product quality based on real-world needs

> **Pain Points:**  
> - Limited resources to maintain SDK documentation and tooling  
> - Difficult to prioritize extensibility without clear, validated demand  
> - Low visibility into how customers or partners use custom rules

> **Value Proposition:**  
> ✅ Watchful Anvil serves as a **community-validated enhancement layer**, surfacing real use cases, patterns, and gaps—offloading prototyping and documentation burden while aligning product decisions with organic developer needs.


## 📂 Project Structure

This project consists of two key components:

### 1. 🧾 Source Tree (this repository)

```
WatchfulAnvil
│── AUTHORS.md
│── CHANGELOG.md
│── LICENCE.md
│── README.md
│── ROADMAP.md
│── WatchfulAnvil.WorkflowAnalyzerRules.sln
│
├── src
│   ├── HelloWorld.WorkflowAnalyzerRules
│   │   ├── HelloWorld.WorkflowAnalyzerRules.csproj
│   │   └── RegisterAnalyzerConfiguration.cs
│   └── CPRIMA.WorkflowAnalyzerRules
│       ├── CPRIMA.WorkflowAnalyzerRules.csproj
│       ├── RegisterAnalyzerConfiguration.cs
│       └── Rules
│       │   └── Usage
│       │   │   └── ShouldStopActivityRule.cs
│       │   └── Noop
│       │       ├── NullOperationActivityRule.cs
│       │       ├── NullOperationProjectRule.cs
│       │       ├── NullOperationWorkflowRule.cs
│       │       └── WaitBeforeExecutionRule.cs
│       └── LocalizationResources
│           └── Strings.resx
├── templates
│   └── workflow-analyzer-rule
│       ├── Project.csproj
│       └── Rules
│           └── SampleRule.cs
│
├── tools
│   └── New-RulesProject.ps1
```


### 2. 📘 Wiki (Documentation Hub)

The [Watchful Anvil Wiki](https://github.com/rpapub/WatchfulAnvil/wiki) provides:

- **Getting Started**: Step-by-step guide to setup and first rule
- **System Requirements**: Verified tooling for Studio integration
- **Development Docs**: Custom rule authoring, testing, packaging
- **Deployment Guides**: Manual and automated installer instructions
- **Vision & Roadmap**: Context for contributors and stakeholders

> [!TIP]
> Use the wiki for deep dives. This README stays high-level.

## 📦 Deployment Notes

The analyzer rules are compiled into DLLs and must be placed into folders that UiPath Studio scans for Workflow Analyzer rules. This can be done manually, or automatically via the installer (in development).

> ⚠️ **Note:** Deployment paths are partly based on reverse-engineering and observed behavior across multiple Studio versions. They are not comprehensively documented by UiPath and may change in future releases.

### 🔹 Manual Deployment (Current)

You can manually copy the appropriate DLL (e.g., `net6.0` or `net461`) into the matching UiPath Studio folder. These locations differ by Studio version and installation type.

#### Per-Machine Installations

| Studio Version     | Target Framework | Folder Path                                  |
| ------------------ | ---------------- | -------------------------------------------- |
| < 2021.10          | `net461`         | `%ProgramFiles%\UiPath\Studio\Rules\`        |
| 2021.10 – <2024.10 | `net461`         | `%ProgramFiles%\UiPath\Studio\net461\Rules\` |
| 2021.10 – <2024.10 | `net6.0`         | `%ProgramFiles%\UiPath\Studio\Rules\net6.0\` |
| 2024.10+           | `net8.0`         | `%ProgramFiles%\UiPath\Studio\Rules\net8.0\` |
| 2024.10+           | `net461`         | `%ProgramFiles%\UiPath\Studio\net472\Rules\` |

#### Per-User Installations

| Studio Version     | Target Framework | Folder Path                                           |
| ------------------ | ---------------- | ----------------------------------------------------- |
| < 2021.10          | `net461`         | `%LocalAppData%\Programs\UiPath\Studio\Rules\`        |
| 2021.10 – <2024.10 | `net461`         | `%LocalAppData%\Programs\UiPath\Studio\net461\Rules\` |
| 2021.10 – <2024.10 | `net6.0`         | `%LocalAppData%\Programs\UiPath\Studio\Rules\net6.0\` |
| 2024.10+           | `net8.0`         | `%LocalAppData%\Programs\UiPath\Studio\Rules\net8.0\` |
| 2024.10+           | `net461`         | `%LocalAppData%\Programs\UiPath\Studio\net472\Rules\` |

If no matching folders exist, you may use this fallback path:

- `%Public%\Documents\UiPath\Rules` (recognized by Studio in some cases)

After copying, restart UiPath Studio.

---

### 🔧 Installer (Preview)

An installer is in development. It will:

- Detect which Studio versions and paths exist
- Install appropriate rule DLLs for each detected environment
- Support both admin (system) and user (per-user) installations
- Allow custom folder override
- Work in silent mode for unattended deployment

> 🧪 Current status: internal testing only. Not recommended for public use in v0.1.0.

---

## 🛠️ Contributing

This is a personal project. Contributions are welcome, but coordinated participation is currently invite-only. Feel free to open issues for questions or feedback.

---

## 📄 License

Licensed under the [Creative Commons Attribution 4.0 International (CC BY 4.0)](LICENCE.md)
See [AUTHORS.md](AUTHORS.md) for contributors.
