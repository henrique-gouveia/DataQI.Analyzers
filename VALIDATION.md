# Manual Validation — VS Code

## Prerequisites

- Visual Studio Code
- C# extension (`ms-dotnettools.csharp`)
- .NET 8 SDK
- C# Dev Kit (optional — run the scenarios once with it disabled, once with it enabled)

## Steps

1. `./pack.sh 0.1.0-preview.1` (if repacking this version, clear only its exact entry in NuGet's global cache).
2. `dotnet restore samples/CompletionSample/CompletionSample.csproj`
3. Open `samples/CompletionSample` as its own folder in VS Code.
4. Clear the sample's `obj/` and `bin/` folders, run **C#: Restart Language Server**, and wait for the project to finish loading (status bar).

## Scenarios

| # | Type this in `IProductRepository` | Expected |
|---|---|---|
| 1 | `IEnumerable<Product> F` without Ctrl+Space | `FindBy` appears automatically |
| 2 | `IEnumerable<Product> ` then Ctrl+Space | `FindBy` appears |
| 3 | `IEnumerable<Product> FindBy` | Public scalar properties of `Product` appear |
| 4 | `IEnumerable<Product> FindByNa` | `FindByName` appears and the list is not wrongly broad |
| 5 | `IEnumerable<Product> FindByName` | String operators, `Null`/`NotNull`, `And`, `Or`, `OrderBy` |
| 6 | `IEnumerable<Product> FindByNameOrderBy` | Properties only; after a property, `Asc`/`Desc` only |
| 7 | Add `public bool InStock { get; set; }` to `Product`, then type `FindBy` | `InStock` is absent; remove the temporary property afterward |
| 8 | A repository that inherits `ICrudRepository` through another interface | The same suggestions appear |
| 9 | `FindBy` inside `INotARepository` | Nothing from DataQI |
| 10 | `dotnet build` from the command line | No new warnings |

Mark ✅/❌ per scenario, per run (extension-only, then with Dev Kit). Leave a scenario pending when it was not observed in the editor.

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
