#!/usr/bin/env bash
set -euo pipefail

version="${1:?Usage: ./pack.sh <version, e.g. 0.1.0-poc.2>}"

dotnet pack src/DataQI.Analyzers/DataQI.Analyzers.csproj \
    -c Release \
    -o ./feed \
    -p:Version="$version"

echo "Packed DataQI.Analyzers $version to ./feed"
