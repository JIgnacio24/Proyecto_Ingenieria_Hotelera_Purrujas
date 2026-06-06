#!/usr/bin/env bash
set -euo pipefail

SQLSERVER_PORT="${MSSQL_TCP_PORT:-1433}"

if [ -x "/opt/mssql-tools18/bin/sqlcmd" ]; then
    /opt/mssql-tools18/bin/sqlcmd -C -S "localhost,${SQLSERVER_PORT}" -U sa -P "${MSSQL_SA_PASSWORD}" -Q "SELECT 1" >/dev/null
else
    /opt/mssql-tools/bin/sqlcmd -S "localhost,${SQLSERVER_PORT}" -U sa -P "${MSSQL_SA_PASSWORD}" -Q "SELECT 1" >/dev/null
fi
