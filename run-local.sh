#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
dotnet restore
dotnet build --no-restore
dotnet run --project src/DocumentIntake.Api/DocumentIntake.Api.csproj
