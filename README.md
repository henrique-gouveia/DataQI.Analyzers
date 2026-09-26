# DataQI Analyzers

Data Query Interface Analyzers is written in C# and provides Roslyn analyzers and code fixes that enforce conventions shared across every DataQI Provider.

[![Build](https://github.com/henrique-gouveia/DataQI.Analyzers/actions/workflows/dotnet.yml/badge.svg)](https://github.com/henrique-gouveia/DataQI.Analyzers/actions/workflows/dotnet.yml)
[![codecov](https://codecov.io/gh/henrique-gouveia/DataQI.Analyzers/branch/main/graph/badge.svg)](https://codecov.io/gh/henrique-gouveia/DataQI.Analyzers)
[![NuGet](https://img.shields.io/nuget/v/DataQI.Analyzers.svg)](https://www.nuget.org/packages/DataQI.Analyzers/)

## Features

* Roslyn analyzers enforcing DataQI conventions
* Code fixes for reported diagnostics

## Getting Started

### Installing

This library can add to the project by the way:

    dotnet add package DataQI.Analyzers

See [Nuget](https://www.nuget.org/packages/DataQI.Analyzers) for other options.

## News

**v1.0.0**

* Provided initial core base

## Providers

* [DataQI.Commons](https://github.com/henrique-gouveia/DataQI.Commons)
* [DataQI.Dapper.FastCrud](https://github.com/henrique-gouveia/DataQI.Dapper.FastCrud)
* [DataQI.EntityFrameworkCore](https://github.com/henrique-gouveia/DataQI.EntityFrameworkCore)

## License

DataQI Analyzers is released under the [MIT License](https://opensource.org/licenses/MIT).
