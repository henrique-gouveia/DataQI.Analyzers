# DataQI Analyzers

DataQI.Analyzers suggests `FindBy...` query-method names while you edit DataQI repository interfaces. It completes properties, operators, logical junctions, and ordering based on the entity type.

[![Build](https://github.com/henrique-gouveia/DataQI.Analyzers/actions/workflows/dotnet.yml/badge.svg)](https://github.com/henrique-gouveia/DataQI.Analyzers/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/henrique-gouveia/DataQI.Analyzers/branch/main/graph/badge.svg)](https://codecov.io/gh/henrique-gouveia/DataQI.Analyzers)
[![NuGet](https://img.shields.io/nuget/v/DataQI.Analyzers.svg)](https://www.nuget.org/packages/DataQI.Analyzers/)

## Install

Add the package directly to the project containing your repository interfaces:

```xml
<ItemGroup>
  <PackageReference Include="DataQI.Analyzers" Version="0.1.0-preview.2" PrivateAssets="all" />
</ItemGroup>
```

For a locally built preview, add the directory containing the `.nupkg` to your NuGet sources before restoring. The provider requires an IDE host with Roslyn 4.8 or newer.

## Editor Setup

- **Rider 2025.2+:** open `Settings → Editor → Inspection Settings → Roslyn` and enable **Enable Roslyn analyzers** and **Always Prefer Roslyn Completions**. The latter enables third-party completion providers ([RIDER-74403](https://youtrack.jetbrains.com/issue/RIDER-74403)).
- **VS Code:** install the C# extension (`ms-dotnettools.csharp`) and wait for the project to load. C# Dev Kit is optional.
- **Visual Studio 17.8+:** meets the Roslyn requirement; editor behavior has not been verified.

## Usage

Inside an interface that inherits directly or indirectly from `ICrudRepository<TEntity, TId>`, type the first letter of a member name after its return type. Completion offers `FindBy`, then public scalar properties, operators compatible with each property, `And`/`Or`, and `OrderBy` with `Asc`/`Desc`. Ctrl+Space after the return type also offers `FindBy`.

For example, in a project already using the Entity Framework Core provider:

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DataQI.EntityFrameworkCore.Repository;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public interface IProductRepository : IEntityRepository<Product, int>
{
    IEnumerable<Product> FindByNameEqual(string name);

    IEnumerable<Product> FindByNameStartingWithAndPriceGreaterThan(string name, decimal price);

    Task<IEnumerable<Product>> FindByNameOrderByPriceDescAsync(
        string name,
        CancellationToken cancellationToken = default);
}
```

For async repository methods, use the `Async` suffix and a trailing `CancellationToken` when the repository API supports it.

### Suggested Operators

Every supported scalar type offers `Equal` and `Not`. Additional suggestions depend on its type:

| Property type | Additional operators |
|---|---|
| `string` | `StartingWith`, `EndingWith`, `Containing`, `Like`, `In`, `NotIn` |
| Numeric types, `DateTime`, `DateTimeOffset`, `TimeSpan`, enums | `GreaterThan`, `GreaterThanEqual`, `LessThan`, `LessThanEqual`, `Between`, `NotBetween`, `In`, `NotIn` |
| `Guid` | `In`, `NotIn` |
| `bool`, `char`, `byte[]` | None beyond equality and negation |

Reference types and nullable value types also offer `Null` and `NotNull`.

Equality can be implicit (`FindByName`) or explicit (`FindByNameEqual`). The aliases `Equals`, `IsEqual`, and `IsEquals` are also recognized when typed; suggestions use the canonical form `Equal`. The same convention applies to aliases such as `Contains` and `IsContaining`: completion suggests `Containing`.

### Ordering and Continuing a Query

After a property or operator, completion offers `And`, `Or`, and `OrderBy`. After an ordered property, it offers `Asc` and `Desc`; after a direction, it offers the next property. For example: `FindByNameOrderByPriceDescIdAsc`.

The editor may close the list when you accept an item. Continue typing the next part or press Ctrl+Space to request the next suggestions. Reopening the list immediately after acceptance depends on the editor.

## Known Limitations

- Only public instance scalar properties with public getters are offered, including inherited properties: primitives, `string`, `decimal`, `char`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, `byte[]`, enums, and nullable forms. Navigation properties, collections, indexers, and `[NotMapped]` properties are excluded.
- Property names must start with an ASCII uppercase letter. Names that overlap query keywords are filtered conservatively, including `InStock`, `LinkedIn`, `LikeCount`, `TermsAndConditions`, and names ending in `Async`, `Is`, `Not`, `And`, or `Or`.
- Only `FindBy` is completed. Diagnostics, code fixes, other prefixes, and return-type and parameter completion are outside 0.1.

## Troubleshooting

If the suggestions do not appear, confirm that the package has been restored in the project containing the repository interface and that the IDE has finished loading it. In Rider, check **Always Prefer Roslyn Completions** first.

When testing a repacked local version, remove only that exact version from NuGet's global cache, restore again, and reload the project or restart the IDE. The package must contain `analyzers/dotnet/cs/DataQI.Analyzers.dll`. Check build output for `CS8032` or `AD0001` if the provider fails to load.

## License

DataQI Analyzers is released under the [MIT License](https://opensource.org/licenses/MIT).
