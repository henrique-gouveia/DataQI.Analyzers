# DataQI Analyzers

DataQI.Analyzers suggests `FindBy...` query-method names while you edit DataQI repository interfaces. It completes properties, operators, logical junctions, and ordering based on the entity type.

[![Build](https://github.com/henrique-gouveia/DataQI.Analyzers/actions/workflows/dotnet.yml/badge.svg)](https://github.com/henrique-gouveia/DataQI.Analyzers/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/henrique-gouveia/DataQI.Analyzers/branch/main/graph/badge.svg)](https://codecov.io/gh/henrique-gouveia/DataQI.Analyzers)
[![NuGet](https://img.shields.io/nuget/v/DataQI.Analyzers.svg)](https://www.nuget.org/packages/DataQI.Analyzers/)

## Getting Started

Add the package to the project that contains your repository interfaces:

```bash
dotnet add package DataQI.Analyzers
```

It is an analyzer package: it runs inside the compiler and the IDE, and adds no runtime assembly to your output. To keep it from flowing to projects that reference yours, mark it as private:

```xml
<PackageReference Include="DataQI.Analyzers" Version="..." PrivateAssets="all" />
```

Use the version that `dotnet add package` writes. DataQI.Analyzers does not know which other packages in your solution may already reference it. If one does, restoring the project already brings the analyzer and this step is not needed; confirm by checking that the suggestions appear (see Troubleshooting).

Then configure a compatible IDE as described below. The package requires an IDE host with Roslyn 4.8 or newer. Check NuGet for available versions; development packages may require a configured local feed.

Completion follows the DataQI query-method grammar, defined by [DataQI.Commons](https://github.com/henrique-gouveia/DataQI.Commons/blob/main/README.md#query-methods). It was tested against the Commons 4.0 grammar and does not check which version a project references.

### Editor Setup

- **VS Code:** completion has been validated.
- **Visual Studio:** completion has not been tested yet.
- **Rider:** completion has been validated. Select **Enable Roslyn analyzers** and **Always Prefer Roslyn Completions** in the Roslyn settings. See [RIDER-74403](https://youtrack.jetbrains.com/issue/RIDER-74403) for third-party completion support.

## Usage

Inside an interface that inherits directly or indirectly from `ICrudRepository<TEntity, TId>`, type the first letter of a member name after its return type. Completion offers `FindBy`, then public scalar properties, operators compatible with each property, `And`/`Or`, and `OrderBy` with `Asc`/`Desc`. Ctrl+Space after the return type also offers `FindBy`.

For example:

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public interface IProductRepository : ICrudRepository<Product, int>
{
    IEnumerable<Product> FindByNameEqual(string name);

    IEnumerable<Product> FindByNameStartingWithAndPriceGreaterThan(string name, decimal price);

    Task<IEnumerable<Product>> FindByNameOrderByPriceDescAsync(
        string name,
        CancellationToken cancellationToken = default);
}
```

For async repository methods, return `Task<TEntity>` or `Task<IEnumerable<TEntity>>`. The runtime strips an optional `Async` suffix and dispatches by return type. A trailing `CancellationToken` parameter is part of the grammar. Completion suggests names; it does not generate or validate the method signature.

### Suggested Operators

Every supported scalar type offers `Equal` and `Not`. The following table describes current suggestions. A suggestion does not guarantee that every provider can execute it; check the provider's documentation:

| Property type | Additional operators |
|---|---|
| `string` | `StartingWith`, `EndingWith`, `Containing`, `Like`, `In`, `NotIn` |
| Numeric types, `DateTime`, `DateTimeOffset`, `TimeSpan`, enums | `GreaterThan`, `GreaterThanEqual`, `LessThan`, `LessThanEqual`, `Between`, `NotBetween`, `In`, `NotIn` |
| `Guid` | `In`, `NotIn` |
| `bool`, `char`, `byte[]` | None beyond equality and negation |

Reference types and nullable value types also offer `Null` and `NotNull`.

Equality can be implicit (`FindByName`) or explicit (`FindByNameEqual`). The aliases `Equals`, `IsEqual`, and `IsEquals` are also recognized when typed; suggestions use the canonical form `Equal`. The same convention applies to aliases such as `Contains` and `IsContaining`: completion suggests `Containing`.

### Ordering and Continuing a Query

After a property or operator, completion offers `And`, `Or`, and `OrderBy`. Ordering requires a predicate first. After an ordered property, completion offers `Asc` and `Desc`; after a direction, it offers the next property. For example: `FindByNameOrderByPriceDescIdAsc`. The final direction may be omitted and defaults to ascending; use an explicit direction between successive ordered properties.

The editor may close the list when you accept an item. Continue typing the next part or press Ctrl+Space to request the next suggestions. Reopening the list immediately after acceptance depends on the editor.

## Known Limitations

- Only public instance scalar properties with public getters are offered, including inherited properties: numeric types, `bool`, `char`, `string`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, `byte[]`, enums, and nullable forms. Navigation properties, other collections, indexers, `[NotMapped]` properties, `DateOnly`, and `TimeOnly` are excluded.
- Getter-only properties, including calculated properties, are offered. Completion does not inspect ORM or provider mappings. Use mapped properties in query methods; mark calculated properties `[NotMapped]` to exclude them from suggestions.
- Property names must start with an ASCII uppercase letter. Names that overlap query keywords are filtered conservatively, including `InStock`, `LinkedIn`, `LikeCount`, `TermsAndConditions`, and names ending in `Async`, `Is`, `Not`, `And`, or `Or`.
- That filter does not catch every property/operator overlap: string properties such as `PeriodEnd` and `LineStart` are offered, but `FindByPeriodEndEndingWith` and `FindByLineStartStartingWith` are misparsed by the runtime. Use criteria with an explicit property name for these cases. Keyword filtering differs for predicates and ordering, so a property excluded from predicates may still appear after `OrderBy`.
- After `Not`, completion repeats the operator list, including already negated operators. Avoid names such as `FindByNameNotNotIn`; type `FindByNameNotIn` or `FindByNameNotEqual` with a single `Not`.
- Inline argument counts for `And`, `Or`, and `Not` currently show `1 arg(s)`. Junctions consume no arguments; negation uses the argument count of its operator. Follow the query-method grammar when declaring parameters.
- Only `FindBy` is completed. Diagnostics, code fixes, other prefixes, and return-type and parameter completion are outside 0.1.

## Troubleshooting

If the suggestions do not appear, confirm that the package has been restored in the project containing the repository interface and that the IDE has finished loading it. In Rider, check **Always Prefer Roslyn Completions** first.

When testing a repacked local version, remove only that exact version from NuGet's global cache, restore again, and reload the project or restart the IDE. The package must contain `analyzers/dotnet/cs/DataQI.Analyzers.dll`. Check build output for `CS8032` or `AD0001` if the provider fails to load.

## License

DataQI Analyzers is released under the [MIT License](https://opensource.org/licenses/MIT).
