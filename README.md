# WatchfulAnvil – UiPath Studio Workflow Analyzer Rules

A minimal, working example of custom **UiPath Workflow Analyzer rules**, built in **.NET**, intended for integration with **UiPath Studio's Workflow Analyzer**.

<p align="center">
  <img src="https://rpapub.github.io/WatchfulAnvil/assets/img/logo/TheWatchfulAnvil.png" alt="Watchful Anvil Logo" height="200">
</p>

This repository provides the technical foundation for custom rule development, multi-targeted builds, and packaging. While internal tooling and installers are under development, this release focuses on structural readiness and early reference value.

---

## 📌 Project Goals

- Provide a reliable starting point for authoring custom rules  
- Support **multi-framework builds** (`net461`, `net6.0`, `net8.0`)  
- Enable Git-based collaboration and iterative experimentation  
- Lay the foundation for future educational content and tooling

---

## ✅ Current Status (v0.1.0-alpha)

- ✔️ Project structure and solution layout finalized  
- ✔️ Custom rules (e.g., "NullOperation") recognized by UiPath Studio  
- ✔️ Builds across all supported target frameworks  
- 🚧 Internal work on CI, installer, and SDK reverse-engineering in progress  
- 📚 Documentation and learning artifacts planned for future milestones

> ℹ️ This is an internal-facing alpha release. It is not yet intended for general consumption or end-to-end rule authoring guidance.

---

## 📂 Project Structure

```
WatchfulAnvil
│── AUTHORS.md
│── CHANGELOG.md
│── LICENCE.md
│── README.md
│── WatchfulAnvil.WorkflowAnalyzerRules.sln
│
├── src
│   ├── CPM.WorkflowAnalyzerRules
│   │   ├── CPM.WorkflowAnalyzerRules.csproj
│   │   └── RegisterAnalyzerConfiguration.cs
│   └── YOU.WorkflowAnalyzerRules
│       ├── YOU.WorkflowAnalyzerRules.csproj
│       └── RegisterAnalyzerConfiguration.cs
│
├── templates
│   └── workflow-analyzer-rule
│       ├── Project.csproj
│       └── lib-deps
│           └── UiPath.Activities.Api
│               ├── net461\UiPath.Studio.Activities.Api.dll
│               ├── net6.0\UiPath.Studio.Activities.Api.dll
│               └── net8.0\UiPath.Studio.Activities.Api.dll
```

---


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
