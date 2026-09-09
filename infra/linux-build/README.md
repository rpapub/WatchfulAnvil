# Linux build probe

Answers one question: **can the rule packs build and pack on a Linux runner?**

It matters because the ILRepack MSBuild task ships a single assembly marked
`.NETFramework,Version=v4.7.2`. That attribute does *not* by itself prevent
.NET Core MSBuild from loading it — verified on Windows, where `dotnet pack`
(Core MSBuild) loads the task and merges correctly. Whether it also works on
Linux depends on what the task's implementation touches, which only a Linux
run can settle.

## Run

Pull first, as a separate step. A pull that dies midway can leave a partial
layer and take Docker Desktop down with `layer not mounted` — which is exactly
what happened on the first attempt here, with the ~1 GB `sdk:10.0` image.

```bash
docker pull mcr.microsoft.com/dotnet/sdk:9.0-alpine
docker compose -f infra/linux-build/compose.yaml run --rm build
```

Results land in `infra/linux-build/out/` — `build.log` plus any produced
`nupkg/`. Both are gitignored.

The image is Alpine (~250 MB) rather than the full SDK image (~1 GB). Alpine is
musl rather than glibc, so it is not identical to a typical `ubuntu-latest`
runner: treat a **PASS** as conclusive, and re-test any **FAIL** on
`mcr.microsoft.com/dotnet/sdk:9.0` before drawing a conclusion.

## How it works

The repo is mounted **read-only** at `/repo` and copied to `/work` inside the
container before building, so a Linux build never writes `bin/`/`obj/` into the
Windows working tree. NuGet's package cache is a named volume, so a second run
is fast.

Each target is attempted independently and a failure is recorded rather than
fatal, so one run reports the status of all of them.

| Target | Frameworks | Why it is in the probe |
|---|---|---|
| `Cpmf.WorkflowAnalyzerRules` | `net6.0;net8.0` + ILRepack | the actual question |
| `Cpmf.Rules.Libs` | `net6.0;net8.0` + ILRepack | second ILRepack pack |
| `WatchfulAnvil.Sdk` | `net461;net6.0;net8.0` | expected to fail — see below |

## Expected results

`WatchfulAnvil.Sdk` targets `net461`, which needs .NET Framework reference
assemblies that are not part of the .NET SDK. On Linux this fails at compile
time unless the project adds:

```xml
<PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies"
                  Version="1.0.3" PrivateAssets="all" />
```

That is a Microsoft-published package and it makes `net461` compile cleanly on
Linux. It does not make the output *runnable* there, which is irrelevant — the
DLL is for Studio.

So a `FAIL` on the SDK is informative, not a blocker: it prices the cost of
dropping `net461`, or of adding one package reference.

## Reading the output

A green build is not sufficient. **ILRepack can no-op silently**, producing a
package that looks correct but does not contain the merged SDK — this was
observed on Windows when the SDK arrived via `PackageReference` without
`CopyLocalLockFileAssemblies=true`.

The script therefore prints the `lib/**.dll` sizes from each merged package:

- a merged pack DLL is **~70-80 KB** (the SDK alone is ~72 KB)
- a pack DLL under **~15 KB** means the merge did not happen

Check the sizes, not just the exit code.

## What this probe does not cover

Building packs is not the same as exercising them. `uipcli` and Studio are
Windows-only, so a Linux runner can compile and pack rule packs but cannot run
the analyzer against a project to confirm the rules load and fire.

The practical split is a Linux job for build + pack + unit tests (which mock
`IWorkflowModel` / `IProjectModel` and never touch Studio), and a Windows job
for the analyzer integration check.
