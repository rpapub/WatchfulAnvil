# CPRIMA-UiPath-Studio-WorkflowAnalyzerRules

A custom **UiPath Workflow Analyzer Rules Library**, built in **.NET**, designed to integrate with **UiPath Studio's Workflow Analyzer**. 

This repository is in **early development**, focusing on:
- 📂 Setting up the initial structure
- 🚀 Ensuring compatibility with multiple .NET versions
- 🧪 Implementing basic test automation

---

## 📌 Project Status

- ✅ **Multi-framework support** (`net461`, `net6.0`, `net8.0`)
- ✅ **Basic "NullOperation" rules registered and visible in UiPath Studio**
- ✅ **Unit tests set up using xUnit and Moq**
- 🛠️ **Next Steps:** Expand rule implementations, enhance test coverage, validate production deployment.

---

## 📂 Project Structure

```
CPRIMA-UiPath-Studio-WorkflowAnalyzerRules
│── .gitignore
│── LICENSE
│── README.md
│── CPRIMA.WorkflowAnalyzerRules.sln
│
├── src
│   ├── CPRIMA.WorkflowAnalyzerRules
│   │   ├── CPRIMA.WorkflowAnalyzerRules.csproj
│   │   ├── RegisterAnalyzerConfiguration.cs
│   │   ├── Rules
│   │   │   ├── Noop
│   │   │   │   ├── NullOperationActivityRule.cs
│   │   │   │   ├── NullOperationWorkflowRule.cs
│   │   │   │   ├── NullOperationProjectRule.cs
│
├── tests
│   ├── CPRIMA.WorkflowAnalyzerRules.Tests
│   │   ├── CPRIMA.WorkflowAnalyzerRules.Tests.csproj
│   │   ├── NullOperationRuleTests.cs
│
├── docs
│── .vs
│── bin
│── obj
```

---

## 🚀 Getting Started

### 🔹 Prerequisites
- **.NET SDK** (`6.0` and `8.0` required)
- **UiPath Studio** (supports `net461`, `net6.0`, and `net8.0`)
- **Visual Studio** (recommended for development)

### 🔹 Build & Test

To **restore dependencies**, **build the solution**, and **run tests**:

```sh
dotnet restore
dotnet build
dotnet test
```

To **run tests** with detailed output:

```sh
dotnet test --verbosity detailed
```

---

## 📦 Deployment: Making Rules Available in UiPath Studio

Once built, the **compiled DLLs** must be placed in specific **UiPath Studio Rules folders** for **global** availability in all projects.

### 🔹 Deployment Paths (Per-Machine Installations)

| Studio Version        | Target Framework | Deployment Path |
|----------------------|----------------|----------------|
| **2021.10.6+**      | .NET 6 & 8      | `%ProgramFiles%\UiPath\Studio\Rules\net6.0\`  |
|                     | Legacy (.NET 4.6.1) | `%ProgramFiles%\UiPath\Studio\net461\Rules\` |
| **Pre-2021.10**      | All            | `%ProgramFiles%\UiPath\Studio\Rules\` |

### 🔹 Deployment Paths (Per-User Installations)

| Studio Version        | Target Framework | Deployment Path |
|----------------------|----------------|----------------|
| **2021.10.6+**      | .NET 6 & 8      | `%LocalAppData%\Programs\UiPath\Studio\Rules\net6.0\`  |
|                     | Legacy (.NET 4.6.1) | `%LocalAppData%\Programs\UiPath\Studio\net461\Rules\` |
| **Pre-2021.10**      | All            | `%LocalAppData%\Programs\UiPath\Studio\Rules\` |

After placing the **DLL files**, **restart UiPath Studio** to apply the changes.

---

## 📜 Roadmap
- ✅ Initial rule registration & visibility in UiPath Studio
- 🏗️ Implement meaningful Workflow Analyzer rules
- 🧪 Expand unit test coverage with mocked UiPath activities
- 📦 Package rules as a **NuGet package** for easier distribution
- 📖 Improve documentation & add real-world use cases

## 🛠️ Contributing
This is a personal project, but contributions or feedback are welcome! 

## 📄 License
MIT License - see [LICENSE.md](LICENSE.md) for details.