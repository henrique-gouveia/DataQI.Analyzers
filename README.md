# DataQI Analyzers

DataQI.Analyzers is a feasibility PoC for a Roslyn `CompletionProvider` that suggests DataQI `FindBy...` query-method names while repository interfaces are edited in VS Code. It is not a finished analyzer product and does not provide diagnostics or code fixes.

[![Build](https://github.com/henrique-gouveia/DataQI.Analyzers/actions/workflows/dotnet.yml/badge.svg)](https://github.com/henrique-gouveia/DataQI.Analyzers/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/henrique-gouveia/DataQI.Analyzers/branch/main/graph/badge.svg)](https://codecov.io/gh/henrique-gouveia/DataQI.Analyzers)
[![NuGet](https://img.shields.io/nuget/v/DataQI.Analyzers.svg)](https://www.nuget.org/packages/DataQI.Analyzers/)

## Example

The provider completes entity properties, compatible operators, and `And`/`Or` junctions in declarations such as:

```csharp
IEnumerable<Product> FindByNameStartingWithAndStockGreaterThan(string name, decimal stock);
Task<IEnumerable<Product>> FindByNameAsync(
    string name,
    CancellationToken cancellationToken = default);
```

Async method names may end in `Async`; when supported by the repository API, pass a trailing `CancellationToken` as shown above.

## Build and Pack

```bash
dotnet build DataQI.Analyzers.sln
dotnet test test/DataQI.Analyzers.Test/DataQI.Analyzers.Test.csproj
./pack.sh 0.1.0-poc.N
```

Each package version must be unique because NuGet caches restored packages. The package is written to `feed/`; the completion provider assembly is placed under `analyzers/dotnet/cs` and Roslyn dependencies are supplied by the IDE host.

## Validate in VS Code

Follow [VALIDATION.md](VALIDATION.md) to restore the sample from the local feed and run the scenarios with the C# extension, both without and with C# Dev Kit.

## Known Limitations

- Evaluated on VS Code and JetBrains Rider (2025.2+, build `252.21603+`); Rider requires enabling **Always Prefer Roslyn Completions** under `Editor → Inspection Settings → Roslyn` — see [VALIDATION.md](VALIDATION.md#validate-in-rider). Other Roslyn-hosted editors (e.g. Visual Studio) are untested.
- Only the `FindBy` prefix is recognized.
- Diagnostics and code fixes are out of scope.
- Bare `IncompleteMemberSyntax` forms are unsupported; use a typed field or method declaration while invoking completion.

## License

DataQI Analyzers is released under the [MIT License](https://opensource.org/licenses/MIT).
