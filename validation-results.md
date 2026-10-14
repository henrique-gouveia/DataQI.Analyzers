# DataQI.Analyzers — Manual Validation Results

## Session

- Date: 2026-09-26
- Conducted via: manual IDE execution (test case)
- Branch: `feature/completion-poc`
- Package validated: `DataQI.Analyzers.0.1.0-poc.3.nupkg`

## Preparation (terminal)

| Item | Result |
|---|---|
| `git log` | 7 commits on `feature/completion-poc`; full implementation of the plan's 5 tasks + validation guide |
| `git status` | 2 uncommitted cosmetic diffs (PT→EN comment translation in `IProductRepository.cs` and `QueryTokenExtensions.cs`); `docs/` and `.idea/` untracked — no functional impact |
| `dotnet build DataQI.Analyzers.sln` | Success — 0 warnings, 0 errors (3 projects: DataQI.Analyzers, DataQI.Analyzers.Test, CompletionSample) |
| `dotnet test` | **75/75 passed**, 0 failed, 0 skipped |
| `./pack.sh 0.1.0-poc.3` | Success |
| `.nupkg` contents | DLL present at `analyzers/dotnet/cs/DataQI.Analyzers.dll`, no extra dependencies bundled |
| `CompletionSample.csproj` | Updated to `DataQI.Analyzers 0.1.0-poc.3` |
| `dotnet restore` (sample) | Success |
| `dotnet build` (sample, post-restore) | Success — 0 warnings, 0 errors |

## Environment

| | Version |
|---|---|
| VS Code CLI | 1.106(?) — confirm in a follow-up run |
| C# extension | pending |
| C# Dev Kit | pending — enabled/disabled during the run below? |
| .NET SDK | 10.0.103 |

## Scenarios (extension only)

| # | Scenario | Result | Notes |
|---|---|---|---|
| 1 | `IEnumerable<Product> FindBy` → all properties | ✅ | Suggested Active, DateRegister, Department, Ean, Id, Keywords, ListPrice, Name, Price, Reference, Stock — all correctly typed. Need to confirm whether Dev Kit was enabled/disabled for this run. |
| 2 | `IEnumerable<Product> FindByNa` → filters to `FindByName` | ⚠️ | Divergence: `FindByName`, `FindByEan`, and `FindByDepartment` all appeared (expected only `FindByName`). Likely cause (from code): when the last token is `Incomplete`, the provider falls back to `builderToken = tokens[0]` (Prefix) and rebuilds the list with ALL properties again (`QueryMethodCompletionProvider.cs:70-94`), without pre-filtering server-side by prefix. `FilterText` isn't set explicitly (defaults to `DisplayText`, e.g. `"FindByEan"`). Final filtering depends entirely on the host's matcher (VS Code/Roslyn), which here proved more permissive than assumed when `VALIDATION.md` was written — not a strict left-to-right subsequence match. Not blocking for the PoC, but a candidate for a follow-up fix (e.g. pre-filter by `StartsWith` on the incomplete token, or set a more precise `FilterText`). |
| 3 | `IEnumerable<Product> FindByName` → string operators, Null/NotNull, And/Or | ✅ | Suggested exactly: And, Containing, EndingWith, In, Like, Not, NotIn, NotNull, Null, Or, StartingWith — each with correct arity (0 or 1 arg). |
| 4 | `IEnumerable<Product> FindByStock` → comparison operators, no StartingWith | ✅ | Suggested exactly: And, Between, GreaterThan, GreaterThanEqual, In, LessThan, LessThanEqual, Not, NotBetween, NotIn, Or — correctly excludes StartingWith; Between/NotBetween correctly show 2 args. |
| 5 | `IEnumerable<Product> FindByNameStartingWithAnd` → properties suggested again | ✅ | Verbally confirmed for this test case (no screenshot). |
| 6 | `FindBy` inside `INotARepository` → nothing from DataQI | ✅ | Verbally confirmed for this test case (no screenshot). |
| 7 | `dotnet build` from the command line → no new warnings | ✅ | Confirmed in the terminal (see Preparation table above). |

## Scenarios (with C# Dev Kit)

| # | Scenario | Result | Notes |
|---|---|---|---|
| 1-6 | (same as above) | | |

## Divergences between VALIDATION.md and the real code

None found so far — `VALIDATION.md` accurately reflects what's implemented.

## Issues found

- Scenario 2 (see above): host-side completion filtering is looser than `VALIDATION.md` assumed; not blocking, flagged as a follow-up candidate.

## Rider — additional step (outside the PoC's original scope)

Rider historically didn't consume Roslyn's `CompletionProvider` extension point (it uses ReSharper/PSI's own completion engine instead), but this was fixed in **RIDER-74403**, available starting with build `252.21603+` (Rider 2025.2 EAP5 / 2025.3 EAP1).

This test case ran on Rider `2026.1.2` (build `261.24374.190`) — above the threshold, so the same package (`analyzers/dotnet/cs/DataQI.Analyzers.dll`) should work without code changes.

Decision: finish the 7 scenarios in VS Code first, then run an equivalent pass in Rider.

**Rider version:** 2026.1.2, build `261.24374.190` (well above the `252.21603` threshold).

**Initial result:** completions did **not** appear out of the box — Rider's real completion popup showed "No suggestions" (a separate "Tab to complete" ghost-text overlay from Rider's AI Assistant full-line completion was visible above it, but that's unrelated — it's ML-based inline suggestion, not our provider, and it hallucinated a guess).

**Root cause found for this test case:** `Editor → Inspection Settings → Roslyn → General settings` has three checkboxes — **Enable Roslyn analyzers**, **Include Roslyn analyzers in Solution-Wide Analysis**, **Always Prefer Roslyn Completions**. The last one specifically gates third-party `CompletionProvider`s and was off by default; enabling it fixed the issue immediately, no restart needed. This is not documented anywhere as a prerequisite for RIDER-74403 — a real, non-obvious gotcha now captured in `VALIDATION.md` and `README.md`.

| # | Scenario | Result | Notes |
|---|---|---|---|
| 1-6 | (same scenarios as the VS Code table) | ✅ | "Everything working" reported for this test case after enabling the setting above (self-reported, no individual screenshots beyond Scenario 1). |

## 2026-10-13: 0.1.0-poc.4 member-name trigger check

- Automated Roslyn `CompletionService` insertion and manual-invoke checks: 11/11 passed.
- VS Code popup check: not observed; macOS UI automation did not return control of the editor.

## 2026-10-13: 0.1.0-preview.1 validation

- Branch: `feature/complete-first-version`
- Package: `feed/DataQI.Analyzers.0.1.0-preview.1.nupkg` (repacked after the final review corrections)
- Environment: VS Code CLI 1.134.0; .NET SDK 10.0.103. The VS Code C# extension and C# Dev Kit are not installed in this profile.
- Analyzer unit tests: 191 passed, 0 failed. Runtime parity tests: 10 passed, 0 failed. Release build: 0 warnings, 0 errors.
- `FindByNa` filter: a `CompletionService` regression test first returned `FindById` and failed; after filtering, the matching, missing, multiple-property, and partial-operator tests passed.
- Sample: forced restore from the local feed and `dotnet build --no-restore` succeeded with 0 warnings and 0 errors.
- Clean solution restore against nuget.org only succeeded after keeping the unpublished sample package out of the solution. The sample remains buildable separately from the local feed.
- The package DLL and the freshly restored analyzer DLL have the same SHA-256 as the Release build: `89c0292f36c533073d7f0fdd7feae7779f0e306c6afbd5fcaa2fc951b08a7981`.

| Manual VS Code scenario from `VALIDATION.md` | Result |
|---|---|
| Type `F` without Ctrl+Space | Pending: popup not observed |
| Ctrl+Space after the return type | Pending: popup not observed |
| `FindBy` and `FindByNa` property lists | Pending: popup not observed |
| Operators, `And`/`Or`, and `OrderBy` directions | Pending: popup not observed |
| Unsafe `InStock` name omitted | Pending: popup not observed |
| Indirect repository inheritance | Pending: popup not observed |

The macOS UI automation attempt did not return control of the editor. The C# extensions must be installed and the cases rerun in an interactive VS Code session before reporting this preview as editor-validated.

## 2026-10-14: Explicit equality suggestions

- Added `Equal` to the suggested operators for every supported scalar type. Equality remains available without a suffix, and typed aliases `Equals`, `IsEqual`, and `IsEquals` remain supported.
- Red: 19 regression cases failed because the explicit equality suggestion was missing (15 scalar types and 4 Roslyn completion scenarios, including partial `...E` identifiers).
- Green: 210 Analyzer unit tests and 10 runtime parity tests passed. Release build of the full solution, including the sample: 0 warnings, 0 errors.
- Repacked `0.1.0-preview.1` to the workspace `packages/` directory and refreshed `feed/`. Moved only the exact cached Analyzer version, then force-restored the solution with the workspace `NuGet.Config`.
- Release, package, and restored analyzer DLLs share SHA-256 `65cd443a5d63783f2022d182a2baabdc721f0e7bd68b1f3ec54976cc5cdc13c8`.
- Rider popup observation of this correction remains pending.

## 2026-10-14: Commons-only preview.2 reference

- Generated local `DataQI.Commons 4.0.0-preview.2` and `DataQI.Analyzers 0.1.0-preview.2` packages in workspace `packages/`, with copies in the sample's `feed/`.
- Commons references the Analyzer package with `IncludeAssets="analyzers"` and `PrivateAssets="none"` so consumers receive the completion provider transitively.
- Red: a temporary consumer referencing only Commons preview.1 failed an MSBuild assertion because `DataQI.Analyzers.dll` was absent from `@(Analyzer)`.
- Green: the same consumer referencing only Commons preview.2 passed that assertion and built with 0 warnings and 0 errors. The resolved analyzer was `dataqi.analyzers/0.1.0-preview.2/analyzers/dotnet/cs/DataQI.Analyzers.dll`.
- `CompletionSample` now has only one PackageReference, to Commons preview.2, and inherits directly from `ICrudRepository<Product, int>`. Its resolved `@(Analyzer)` also contains the Analyzer preview.2 DLL.
- Release build of the Analyzer solution, including the sample: 0 warnings, 0 errors. Full suites: Commons 294/294, Analyzer 210/210, runtime parity 10/10.
- Rider popup validation of the transitive package remains for the interactive test.
