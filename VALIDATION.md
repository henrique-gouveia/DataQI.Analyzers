# Manual Validation — VS Code

## Prerequisites

- Visual Studio Code
- C# extension (`ms-dotnettools.csharp`)
- .NET 8 SDK
- C# Dev Kit (optional — run the scenarios once with it disabled, once with it enabled)

## Steps

1. `./pack.sh 0.1.0-poc.N` (bump `N` each time — NuGet's global cache serves the old binary silently if you repack the same version).
2. `dotnet restore samples/CompletionSample/CompletionSample.csproj`
3. Open `samples/CompletionSample` as its own folder in VS Code.
4. Run the command **C#: Restart Language Server** and wait for the project to finish loading (status bar).

## Scenarios

| # | Type this in `IProductRepository` | Expected |
|---|---|---|
| 1 | `IEnumerable<Product> FindBy` | All public properties of `Product` suggested |
| 2 | `IEnumerable<Product> FindByNa` | Filtered to `FindByName` |
| 3 | `IEnumerable<Product> FindByName` | String operators, `Null`/`NotNull`, `And`, `Or` |
| 4 | `IEnumerable<Product> FindByStock` | Comparison operators, **no** `StartingWith` |
| 5 | `IEnumerable<Product> FindByNameStartingWithAnd` | Properties suggested again |
| 6 | `FindBy` inside `INotARepository` | Nothing from DataQI |
| 7 | `dotnet build` from the command line | No new warnings |

Mark ✅/❌ per scenario, per run (extension-only, then with Dev Kit).

## Validate in Rider

Rider added support for third-party Roslyn `CompletionProvider`s in build `252.21603+` (Rider 2025.2 EAP 5 / 2025.3 EAP 1 — [RIDER-74403](https://youtrack.jetbrains.com/issue/RIDER-74403)). On an eligible build:

1. Open `Settings/Preferences → Editor → Inspection Settings → Roslyn` (Search Everywhere → "Roslyn" also gets you there).
2. Under **General settings**, all three checkboxes must be checked: **Enable Roslyn analyzers**, **Include Roslyn analyzers in Solution-Wide Analysis**, and **Always Prefer Roslyn Completions**. The last one specifically is what enables third-party completion providers to run — it is not guaranteed to be checked by default, and leaving it off produces a silent, empty completion list with no error or warning anywhere.
3. Open `samples/CompletionSample` (or the whole `DataQI.Analyzers.sln`) in Rider and wait for restore/indexing to finish.
4. Run through the same [Scenarios](#scenarios) table above.

If completions don't appear in Rider, check this settings page before anything else.

## If nothing appears

1. Enable the C# language server trace (`csharp.server.trace: "Verbose"` or the Dev Kit equivalent) and check the Output panel's C# channel for the provider assembly's name, a `ReflectionTypeLoadException`, or any load error.
2. Confirm the packed `.nupkg` actually contains the DLL at `analyzers/dotnet/cs/DataQI.Analyzers.dll` (`unzip -l feed/DataQI.Analyzers.<version>.nupkg`).
3. Confirm `dotnet build` produced no new `CS8032` (analyzer failed to load) or `AD0001` (analyzer threw) warnings.

## Environment record

| | Version |
|---|---|
| VS Code | |
| C# extension | |
| C# Dev Kit | |
| .NET SDK | |
