#!/usr/bin/env bash
set -euo pipefail

SQLSERVER_PORT="${MSSQL_TCP_PORT:-1433}"
SQLSERVER_PASSWORD="${MSSQL_SA_PASSWORD:-${SA_PASSWORD:-}}"

if [ -z "$SQLSERVER_PASSWORD" ]; then
    exit 1
fi

if [ -x "/opt/mssql-tools18/bin/sqlcmd" ]; then
    /opt/mssql-tools18/bin/sqlcmd -C -S "localhost,${SQLSERVER_PORT}" -U sa -P "$SQLSERVER_PASSWORD" -Q "SELECT 1" >/dev/null
else
    /opt/mssql-tools/bin/sqlcmd -S "localhost,${SQLSERVER_PORT}" -U sa -P "$SQLSERVER_PASSWORD" -Q "SELECT 1" >/dev/null
fi
