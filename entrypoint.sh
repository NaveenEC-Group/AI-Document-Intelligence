#!/bin/sh
set -e

PORT_VALUE="${PORT:-8080}"
export ASPNETCORE_URLS="http://0.0.0.0:${PORT_VALUE}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"
export Database__Provider="${Database__Provider:-Sqlite}"
export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Data Source=/app/data/aidocs.db}"

mkdir -p /app/data

echo "Starting API on ${ASPNETCORE_URLS}"
exec dotnet "AI Document Intelligence.dll"
