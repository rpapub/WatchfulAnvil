## Cpmf.WorkflowAnalyzerRules - Rule Reference (v0.1.2)

| ID | Name | Scope | Cat | Sev | On | Params |
|---|---|---|---|---|---|---|
| CPMF-L001 | Log Message Bookends | Workflow | L | warn | yes | 2 |
| CPMF-U001 | Unit Out Status Argument | Workflow | U | error | yes | - |
| CPMF-U002 | CodedConfig Argument | Workflow | U | error | yes | - |
| CPMF-U003 | No Flowchart or State Machine | Activity | U | error | yes | - |
| CPMF-N001 | Variable Naming Convention | Activity | N | warn | yes | - |
| CPMF-F001 | SpecificContent Assignment Must Use MultipleAssign | Activity | F | error | yes | - |
| CPMF-N002 | Workflow Filename Is Valid .NET Identifier | Workflow | N | error | yes | - |
| CPMF-U004 | InvokeWorkflowFile Argument Count Mismatch | Workflow | U | error | yes | - |
| CPMF-F002 | Pipeline Structure | Workflow | F | error | yes | 1 |
| CPMF-F003 | Pipeline Domain Model | Workflow | F | error | yes | - |
| CPMF-N003 | Project Name Is Valid .NET Identifier | Project | N | error | yes | - |
| CPMF-FC001 | Pipeline Presence | Project | F | error | yes | - |
| CPMF-FC002 | Workflow Type Ratio | Project | F | warn | opt-in | 4 |

### Parameters

| Rule | Key | Display Name | Default |
|---|---|---|---|
| CPMF-L001 | `StartPrefix` | Start log message prefix | `Going to` |
| CPMF-L001 | `EndPrefix` | End log message prefix | `Finished` |
| CPMF-F002 | `Stages` | Pipeline Stages (ordered, comma-separated) | `Initialize,Ingest,Enrich,Decide,Execute,Complete,Finalize` |
| CPMF-FC002 | `MinModulesPerPipeline` | Min modules per pipeline | `5` |
| CPMF-FC002 | `MaxModulesPerPipeline` | Max modules per pipeline | `10` |
| CPMF-FC002 | `MinUnitsPerModule` | Min units per module | `1` |
| CPMF-FC002 | `MaxUnitsPerModule` | Max units per module | `5` |
