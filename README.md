# DataQI Analyzers

DataQI.Analyzers 0.1 suggests `FindBy...` query-method names while repository interfaces are edited. It is a Roslyn completion provider for repositories built on DataQI.Commons 4.0; it does not provide diagnostics or code fixes.

[![Build](https://github.com/henrique-gouveia/DataQI.Analyzers/actions/workflows/dotnet.yml/badge.svg)](https://github.com/henrique-gouveia/DataQI.Analyzers/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/henrique-gouveia/DataQI.Analyzers/branch/main/graph/badge.svg)](https://codecov.io/gh/henrique-gouveia/DataQI.Analyzers)
[![NuGet](https://img.shields.io/nuget/v/DataQI.Analyzers.svg)](https://www.nuget.org/packages/DataQI.Analyzers/)

## Install

Use `DataQI.Analyzers` 0.1.0-preview.1 with DataQI.Commons 4.0.0-preview.1 (or a provider that depends on that Commons version). The host needs Roslyn 4.8 or newer: .NET SDK 8 or Visual Studio 17.8+.

```xml
<ItemGroup>
  <PackageReference Include="DataQI.Commons" Version="4.0.0-preview.1" />
  <PackageReference Include="DataQI.Analyzers" Version="0.1.0-preview.1" PrivateAssets="all" />
</ItemGroup>
```

## Example

Inside an interface that inherits directly or indirectly from `ICrudRepository<TEntity, TId>`, type the first letter of a member name after its return type. Completion offers `FindBy`, then public scalar properties, operators compatible with each property, `And`/`Or`, and `OrderBy` with `Asc`/`Desc`. Ctrl+Space after the return type also offers `FindBy`.

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataQI.Commons.Repository;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public interface IProductRepository : ICrudRepository<Product, int>
{
    IEnumerable<Product> FindByNameStartingWithAndPriceGreaterThan(string name, decimal price);

    Task<IEnumerable<Product>> FindByNameOrderByPriceDescAsync(
        string name,
        CancellationToken cancellationToken = default);
}
```

For async repository methods, use the `Async` suffix and a trailing `CancellationToken` when the repository API supports it.

## Build and Pack

```bash
dotnet build DataQI.Analyzers.sln
dotnet test DataQI.Analyzers.sln
./pack.sh 0.1.0-preview.1
```

The package is written to `feed/`; the completion provider assembly is placed under `analyzers/dotnet/cs` and Roslyn dependencies are supplied by the IDE host. NuGet caches restored versions, so clear only the exact cached version if repacking the same local version.

## Validate in VS Code

Follow [VALIDATION.md](VALIDATION.md) to restore the sample from the local feed and run the scenarios with the C# extension. The earlier PoC was checked in VS Code and Rider. Manual popup validation of this 0.1 preview is recorded as pending in [validation-results.md](validation-results.md); Visual Studio remains unverified. Rider 2025.2+ requires **Always Prefer Roslyn Completions** under `Editor → Inspection Settings → Roslyn`.

## Known Limitations

- Only public scalar properties are offered: primitives, `string`, `decimal`, `char`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, `byte[]`, enums, and nullable forms. Navigation properties, collections, indexers, and `[NotMapped]` properties are excluded.
- Names the Commons runtime would misread are not suggested. Examples include `InStock`, `LinkedIn`, `LikeCount`, `TermsAndConditions`, and names ending in `Async`, `Is`, `Not`, `And`, or `Or`. Such names can still be typed by hand, but the runtime will not read them as intended. A future diagnostic is planned for this case.
- Only `FindBy` is completed. Diagnostics, code fixes, other prefixes, and return-type and parameter completion are outside 0.1.

## License

DataQI Analyzers is released under the [MIT License](https://opensource.org/licenses/MIT).
