#!/usr/bin/env bash
set -euo pipefail

SQLSERVER_PORT="${MSSQL_TCP_PORT:-1433}"
SQLSERVER_PASSWORD="${MSSQL_SA_PASSWORD:-${SA_PASSWORD:-}}"
SQL_SCRIPT="/docker-entrypoint-initdb.d/Ingenieria_Purrujas_BD.sql"

sqlcmd() {
    if [ -x "/opt/mssql-tools18/bin/sqlcmd" ]; then
        /opt/mssql-tools18/bin/sqlcmd -C "$@"
    elif [ -x "/opt/mssql-tools/bin/sqlcmd" ]; then
        /opt/mssql-tools/bin/sqlcmd "$@"
    else
        echo "No se encontro sqlcmd en la imagen de SQL Server." >&2
        return 1
    fi
}

if [ -z "$SQLSERVER_PASSWORD" ]; then
    echo "La variable MSSQL_SA_PASSWORD es obligatoria para SQL Server." >&2
    exit 1
fi

/opt/mssql/bin/sqlservr &
server_pid=$!

cleanup() {
    kill "$server_pid" 2>/dev/null || true
    wait "$server_pid" 2>/dev/null || true
}
trap cleanup EXIT SIGTERM SIGINT

echo "Esperando a que SQL Server acepte conexiones en el puerto ${SQLSERVER_PORT}..."
ready=0
for _ in {1..60}; do
    if sqlcmd -S "localhost,${SQLSERVER_PORT}" -U sa -P "$SQLSERVER_PASSWORD" -Q "SELECT 1" >/dev/null 2>&1; then
        ready=1
        break
    fi

    sleep 2
done

if [ "$ready" -ne 1 ]; then
    echo "SQL Server no estuvo listo a tiempo." >&2
    exit 1
fi

db_exists="$(sqlcmd -S "localhost,${SQLSERVER_PORT}" -U sa -P "$SQLSERVER_PASSWORD" -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'Ingenieria_Purrujas_BD') IS NULL THEN 0 ELSE 1 END" | tr -d '\r[:space:]')"

if [ "${RESET_DATABASE_ON_START:-false}" = "true" ] || [ "$db_exists" != "1" ]; then
    echo "Inicializando Ingenieria_Purrujas_BD desde ${SQL_SCRIPT}..."
    sqlcmd -S "localhost,${SQLSERVER_PORT}" -U sa -P "$SQLSERVER_PASSWORD" -b -i "$SQL_SCRIPT"
else
    echo "La base Ingenieria_Purrujas_BD ya existe. Se omite la inicializacion."
fi

wait "$server_pid"
