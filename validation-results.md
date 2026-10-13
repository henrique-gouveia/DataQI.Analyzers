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
