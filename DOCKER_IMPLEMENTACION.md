# Docker en el Proyecto Ingenieria Hotelera Purrujas

Este documento describe como esta implementado Docker en el proyecto, que hace cada Dockerfile, como funciona `docker-compose.yml`, como se inicializa la base de datos, que comandos se deben usar y que se verifico para confirmar que el proyecto queda consistente y listo para ejecutarse.

## Estado Verificado

Validacion realizada localmente el 2026-06-19:

- `docker compose config --quiet`: correcto.
- `dotnet build Backend-Ingenieria-Purrujas.sln`: correcto, con advertencia de seguridad moderada en `MailKit 4.13.0`.
- `bun run build` en `Frontend-Ingenieria-Purrujas-Admin`: correcto, con advertencia de presupuesto de bundle.
- `bun run build` en `Frontend-Ingenieria-Purrujas-Cliente`: correcto, con advertencias de presupuesto de bundle y fuentes CSS.
- `docker compose build`: correcto para los cuatro servicios.
- Inicializacion real de BD en un proyecto temporal de Compose con volumen limpio: `healthy`, `RestartCount=0`, 29 tablas, 67 procedimientos y 3 patches registrados en `dbo.__SchemaMigrations`.

Tambien se corrigio una condicion necesaria para SQL Server: el script de base de datos ahora activa las opciones `SET` requeridas antes de crear el indice filtrado `UX_Room_RoomNumber_Active`.

## Arquitectura Docker

El proyecto se ejecuta con cuatro servicios principales:

| Servicio | Carpeta | Tecnologia | Puerto host | Puerto contenedor | Funcion |
|---|---|---:|---:|---:|---|
| `db` | `DB/` | SQL Server 2022 | `1435` | `1435` | Base de datos `Ingenieria_Purrujas_BD` |
| `backend` | `Backend/` | ASP.NET Core .NET 9 | `5234` | `5234` | API REST |
| `frontend-admin` | `Frontend/Frontend-Ingenieria-Purrujas-Admin/` | Angular + Bun | `4203` | `4203` | Panel administrativo |
| `frontend-cliente` | `Frontend/Frontend-Ingenieria-Purrujas-Cliente/` | Angular + Bun | `4204` | `4204` | Sitio del cliente |

URLs principales al levantar todo:

```powershell
http://localhost:4203   # Frontend administrativo
http://localhost:4204   # Frontend cliente
http://localhost:5234   # Backend API
localhost,1435          # SQL Server desde herramientas externas
```

## Archivos Docker del Proyecto

```text
Proyecto_Ingenieria_Hotelera_Purrujas/
├── docker-compose.yml
├── DOCKER_IMPLEMENTACION.md
├── Backend/
│   ├── Dockerfile
│   ├── .dockerignore
│   └── .env
├── DB/
│   ├── Dockerfile
│   ├── Ingenieria_Purrujas_BD.sql
│   ├── docker-entrypoint.sh
│   ├── sqlserver-healthcheck.sh
│   ├── .dockerignore
│   └── Patches/
│       ├── 2026-05-26_add_description_capacity_to_room_type.sql
│       ├── 2026-06-11_fix_room_types_invalid_data.sql
│       └── 2026-06-12_advertising_crud.sql
└── Frontend/
    ├── Frontend-Ingenieria-Purrujas-Admin/
    │   ├── Dockerfile
    │   ├── .dockerignore
    │   ├── package.json
    │   └── bun.lock
    └── Frontend-Ingenieria-Purrujas-Cliente/
        ├── Dockerfile
        ├── .dockerignore
        ├── package.json
        ├── bun.lock
        ├── proxy.conf.json
        └── proxy.docker.conf.json
```

## docker-compose.yml

`docker-compose.yml` es el orquestador principal. Define como se construyen las imagenes, como se conectan los contenedores, que puertos se publican y en que orden deben arrancar los servicios.

### Servicio db

```yaml
db:
  build:
    context: ./DB
  container_name: ingenieria-purrujas-db
  restart: unless-stopped
  env_file:
    - ./Backend/.env
  environment:
    ACCEPT_EULA: "Y"
    MSSQL_PID: "Developer"
    MSSQL_TCP_PORT: "1435"
    MSSQL_SA_PASSWORD: "${MSSQL_SA_PASSWORD:-Purrujas_2026!}"
    RESET_DATABASE_ON_START: "${RESET_DATABASE_ON_START:-false}"
  ports:
    - "1435:1435"
  volumes:
    - purrujas_sqlserver_data:/var/opt/mssql
  healthcheck:
    test: ["CMD", "/usr/local/bin/sqlserver-healthcheck.sh"]
    interval: 10s
    timeout: 5s
    retries: 20
    start_period: 30s
```

Puntos importantes:

- Usa una imagen personalizada construida desde `DB/Dockerfile`.
- Expone SQL Server en `localhost:1435`.
- Guarda los datos reales en el volumen `purrujas_sqlserver_data`.
- Ejecuta `sqlserver-healthcheck.sh` hasta que SQL Server responda.
- `RESET_DATABASE_ON_START` queda en `false` por defecto para no borrar datos en cada arranque.
- El password `sa` debe coincidir con el password usado por el backend en su cadena de conexion.

Nota sobre variables: los valores `${...}` de Compose se resuelven desde variables del shell o desde un archivo `.env` en la raiz del proyecto, no desde `env_file`. En el estado actual, el valor por defecto coincide con `Backend/.env`, por eso backend y base de datos quedan alineados.

### Servicio backend

```yaml
backend:
  build:
    context: ./Backend
  container_name: ingenieria-purrujas-backend
  restart: unless-stopped
  env_file:
    - ./Backend/.env
  environment:
    ASPNETCORE_ENVIRONMENT: "Development"
    ASPNETCORE_URLS: "http://+:5234"
    ConnectionStrings__DefaultConnection: "Server=db,1435;Database=Ingenieria_Purrujas_BD;User Id=sa;Password=${MSSQL_SA_PASSWORD:-Purrujas_2026!};TrustServerCertificate=True;Encrypt=False"
    Cors__AllowedOrigins__0: "http://localhost:4203"
    Cors__AllowedOrigins__1: "http://127.0.0.1:4203"
    Cors__AllowedOrigins__2: "http://localhost:4204"
    Cors__AllowedOrigins__3: "http://127.0.0.1:4204"
  ports:
    - "5234:5234"
  depends_on:
    db:
      condition: service_healthy
```

Puntos importantes:

- El backend no arranca hasta que `db` este `healthy`.
- Dentro de Docker, la base de datos se alcanza como `Server=db,1435`, no como `localhost`.
- `ASPNETCORE_URLS=http://+:5234` permite que ASP.NET escuche conexiones desde fuera del contenedor.
- CORS permite los dos frontends locales: `4203` y `4204`.
- El archivo `Backend/.env` tambien se carga, pero la cadena `ConnectionStrings__DefaultConnection` del compose tiene prioridad.

### Servicio frontend-admin

```yaml
frontend-admin:
  build:
    context: ./Frontend/Frontend-Ingenieria-Purrujas-Admin
  container_name: ingenieria-purrujas-frontend-admin
  restart: unless-stopped
  ports:
    - "4203:4203"
  depends_on:
    - backend
```

El admin se sirve con Angular en modo desarrollo. Su Dockerfile usa `ng serve --host 0.0.0.0 --port 4203 --configuration development`.

En configuracion `development`, Angular reemplaza `environment.ts` por `environment.development.ts`, donde la API queda en:

```ts
apiBaseUrl: 'http://localhost:5234/api'
```

Esto funciona para uso local porque el navegador del usuario llama a `localhost:5234`, que esta publicado por Docker hacia el backend.

### Servicio frontend-cliente

```yaml
frontend-cliente:
  build:
    context: ./Frontend/Frontend-Ingenieria-Purrujas-Cliente
  container_name: ingenieria-purrujas-frontend-cliente
  restart: unless-stopped
  ports:
    - "4204:4204"
  depends_on:
    - backend
```

El frontend cliente tambien usa Angular en modo desarrollo, pero sus llamadas a API se hacen contra rutas relativas `/api`. Por eso su Dockerfile usa `proxy.docker.conf.json`, que redirige esas rutas al backend dentro de la red Docker.

## Red y Volumen

```yaml
volumes:
  purrujas_sqlserver_data:

networks:
  Ingenieria-Purrujas-Sistema-Hotelero:
    name: Ingenieria-Purrujas-Sistema-Hotelero
    driver: bridge
```

La red `Ingenieria-Purrujas-Sistema-Hotelero` permite que los contenedores se comuniquen por nombre de servicio:

- `backend` accede a SQL Server con hostname `db`.
- `frontend-cliente` redirige `/api` hacia hostname `backend`.

El volumen `purrujas_sqlserver_data` persiste `/var/opt/mssql`. Esto contiene los archivos de datos de SQL Server. Si se elimina el contenedor sin eliminar el volumen, la base sigue existiendo.

## DB/Dockerfile

```dockerfile
FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

COPY Ingenieria_Purrujas_BD.sql /docker-entrypoint-initdb.d/Ingenieria_Purrujas_BD.sql
COPY Patches/ /docker-entrypoint-initdb.d/Patches/
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
COPY sqlserver-healthcheck.sh /usr/local/bin/sqlserver-healthcheck.sh

RUN sed -i 's/\r$//' /usr/local/bin/docker-entrypoint.sh /usr/local/bin/sqlserver-healthcheck.sh \
    && chmod +x /usr/local/bin/docker-entrypoint.sh /usr/local/bin/sqlserver-healthcheck.sh \
    && chown -R mssql:root /docker-entrypoint-initdb.d \
    && chown mssql:root /usr/local/bin/docker-entrypoint.sh /usr/local/bin/sqlserver-healthcheck.sh

USER mssql

EXPOSE 1435

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]
```

Este Dockerfile crea una imagen SQL Server preparada para inicializar automaticamente el proyecto.

Que hace cada parte:

- `FROM mcr.microsoft.com/mssql/server:2022-latest`: usa la imagen oficial de SQL Server 2022.
- `USER root`: cambia temporalmente a root para copiar archivos y ajustar permisos.
- `COPY Ingenieria_Purrujas_BD.sql`: copia el script base de la base de datos.
- `COPY Patches/`: copia migraciones incrementales.
- `COPY docker-entrypoint.sh`: copia el script que arranca SQL Server e inicializa la BD.
- `COPY sqlserver-healthcheck.sh`: copia el script usado por Docker Compose para marcar salud.
- `sed -i 's/\r$//'`: elimina retornos de carro de Windows en scripts `.sh`.
- `chmod +x`: permite ejecutar los scripts.
- `chown`: entrega ownership al usuario `mssql`.
- `USER mssql`: vuelve a usuario no root para ejecutar SQL Server.
- `EXPOSE 1435`: documenta el puerto SQL configurado.
- `ENTRYPOINT`: usa el entrypoint propio del proyecto.

## DB/docker-entrypoint.sh

El entrypoint personaliza el arranque normal de SQL Server. Su flujo es:

1. Lee `MSSQL_TCP_PORT`, `MSSQL_SA_PASSWORD` y la ruta del SQL base.
2. Define una funcion `sqlcmd()` compatible con `mssql-tools18` y `mssql-tools`.
3. Falla rapido si no existe password de SQL Server.
4. Ejecuta `/opt/mssql/bin/sqlservr` en segundo plano.
5. Espera hasta que SQL Server acepte conexiones.
6. Comprueba si existe `Ingenieria_Purrujas_BD`.
7. Si no existe, o si `RESET_DATABASE_ON_START=true`, ejecuta `Ingenieria_Purrujas_BD.sql`.
8. Espera a que la base ya creada sea accesible.
9. Crea `dbo.__SchemaMigrations` si no existe.
10. Aplica los `.sql` de `DB/Patches` en orden alfabetico.
11. Registra cada patch aplicado en `dbo.__SchemaMigrations`.
12. Mantiene el proceso de SQL Server en primer plano con `wait "$server_pid"`.

La tabla de control queda asi:

```sql
CREATE TABLE dbo.__SchemaMigrations (
    PatchName NVARCHAR(260) NOT NULL PRIMARY KEY,
    AppliedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

Esto evita aplicar dos veces el mismo patch sobre una base persistente.

## DB/sqlserver-healthcheck.sh

```bash
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
```

El healthcheck no valida todo el modelo de datos. Su responsabilidad es confirmar que SQL Server responde a una consulta simple. El backend depende de este estado con:

```yaml
depends_on:
  db:
    condition: service_healthy
```

## Scripts de Base de Datos

### Script base

`DB/Ingenieria_Purrujas_BD.sql` crea y prepara `Ingenieria_Purrujas_BD`. Incluye tablas, datos iniciales, procedimientos almacenados y bloques idempotentes para estructuras agregadas durante la evolucion del proyecto.

Objetos validados en inicializacion limpia:

- 29 tablas.
- 67 procedimientos almacenados.
- Patches registrados en `dbo.__SchemaMigrations`: 3.

### Patches

Los patches actuales son:

| Patch | Proposito |
|---|---|
| `2026-05-26_add_description_capacity_to_room_type.sql` | Agrega `Description` y `Capacity` a `RoomType` si no existen. |
| `2026-06-11_fix_room_types_invalid_data.sql` | Corrige tipos de habitacion invalidos y agrega constraint contra nombres vacios. |
| `2026-06-12_advertising_crud.sql` | Migra `Advertising` y crea procedimientos CRUD para publicidad. |

Los patches son idempotentes a nivel de esquema: revisan existencia de columnas, constraints o procedimientos antes de modificar. Ademas, el entrypoint registra cada archivo aplicado para no repetirlo en siguientes arranques.

### Alineacion con backend

Se verifico que los procedimientos `usp_*` usados por los repositorios C# existen en `DB/Ingenieria_Purrujas_BD.sql`. Tambien se valido que las tablas usadas por consultas directas del backend existen en el script base.

Ejemplos de procedimientos usados y presentes:

- `usp_AdminUser_Register`
- `usp_AdminUser_Login`
- `usp_AdminAuditLog_Create`
- `usp_Advertising_GetAll`
- `usp_Advertising_Create`
- `usp_Reservation_Create`
- `usp_Reservation_Update`
- `usp_Room_GetAll`
- `usp_Room_GetFirstAvailableByTypeKey`
- `usp_Room_CountAvailableByTypeKey`
- `usp_Season_Create`
- `usp_Promotion_Update`
- `usp_FacilitiesPageContent_Get`
- `usp_AboutUsPageContent_Upsert`
- `usp_GettingTherePageContent_Upsert`

## Backend/Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Backend-Ingenieria-Purrujas.sln ./
COPY Backend-Ingenieria-Purrujas/src/Directory.Build.props Backend-Ingenieria-Purrujas/src/
COPY Backend-Ingenieria-Purrujas/src/Api/Backend-Ingenieria-Purrujas.Api.csproj Backend-Ingenieria-Purrujas/src/Api/
COPY Backend-Ingenieria-Purrujas/src/Application/Backend-Ingenieria-Purrujas.Application.csproj Backend-Ingenieria-Purrujas/src/Application/
COPY Backend-Ingenieria-Purrujas/src/Domain/Backend-Ingenieria-Purrujas.Domain.csproj Backend-Ingenieria-Purrujas/src/Domain/
COPY Backend-Ingenieria-Purrujas/src/Infrastructure/Backend-Ingenieria-Purrujas.Infrastructure.csproj Backend-Ingenieria-Purrujas/src/Infrastructure/

RUN dotnet restore Backend-Ingenieria-Purrujas.sln

COPY Backend-Ingenieria-Purrujas/ Backend-Ingenieria-Purrujas/
RUN dotnet publish Backend-Ingenieria-Purrujas/src/Api/Backend-Ingenieria-Purrujas.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5234
EXPOSE 5234

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Backend-Ingenieria-Purrujas.Api.dll"]
```

Este Dockerfile usa build multi-etapa:

- Etapa `build`: usa SDK .NET 9 para restaurar dependencias y publicar la API.
- Etapa `runtime`: usa ASP.NET Runtime .NET 9, mas liviano que el SDK.

La copia de `.csproj` antes del resto del codigo permite aprovechar cache de Docker. Si cambia un controlador, pero no cambian dependencias, `dotnet restore` puede reutilizarse.

`Backend/.dockerignore` excluye:

```text
.env
.vscode/
**/bin/
**/obj/
**/*.user
**/*.suo
**/*.cache
**/*.lscache
**/*.log
```

Esto evita copiar secretos, binarios locales y basura de build al contexto de Docker.

## Frontend Admin Dockerfile

```dockerfile
FROM oven/bun:1.2.15-slim AS bun

FROM node:22.12-bookworm-slim
COPY --from=bun /usr/local/bin/bun /usr/local/bin/bun

WORKDIR /app

COPY package.json bun.lock ./
RUN bun install --frozen-lockfile

COPY . .

ENV CHOKIDAR_USEPOLLING=true
EXPOSE 4203

CMD ["bun", "run", "ng", "serve", "--host", "0.0.0.0", "--port", "4203", "--configuration", "development"]
```

Puntos importantes:

- Usa Node 22.12 como runtime del dev server.
- Copia el binario de Bun desde `oven/bun:1.2.15-slim`.
- Instala dependencias con `bun install --frozen-lockfile`.
- Usa `ng serve` para servir Angular en desarrollo.
- Expone `4203`.
- `CHOKIDAR_USEPOLLING=true` mejora deteccion de cambios en entornos Docker sobre Windows.

El lock `bun.lock` del admin fue sincronizado para incluir `chart.js`, dependencia necesaria para `occupancy-forecast.component.ts`.

## Frontend Cliente Dockerfile

```dockerfile
FROM oven/bun:1.2.15-slim AS bun

FROM node:22.12-bookworm-slim
COPY --from=bun /usr/local/bin/bun /usr/local/bin/bun

WORKDIR /app

COPY package.json bun.lock ./
RUN bun install --frozen-lockfile

COPY . .

ENV CHOKIDAR_USEPOLLING=true
EXPOSE 4204

CMD ["bun", "run", "ng", "serve", "--host", "0.0.0.0", "--port", "4204", "--configuration", "development", "--proxy-config", "proxy.docker.conf.json"]
```

La diferencia principal con el admin es el proxy:

```json
{
  "/api": {
    "target": "http://backend:5234",
    "secure": false,
    "changeOrigin": true,
    "logLevel": "info"
  }
}
```

Dentro de la red Docker, `backend` es el nombre DNS del contenedor de API. Esto permite que el cliente haga peticiones a `/api/...` desde el navegador, mientras el dev server de Angular las reenvia al backend.

## Variables de Entorno

`Backend/.env` existe en el proyecto local y contiene variables para el backend y valores compatibles con Docker.

No debe copiarse a la imagen del backend, y de hecho `Backend/.dockerignore` lo excluye. Docker Compose lo inyecta como variables de entorno en tiempo de ejecucion.

Variables relevantes:

```text
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:5234
API_BASE_URL=http://localhost:5234/api

MSSQL_SA_PASSWORD=<password>
MSSQL_TCP_PORT=1435
SQLSERVER_DATABASE=Ingenieria_Purrujas_BD
ConnectionStrings__DefaultConnection=Server=db,1435;Database=Ingenieria_Purrujas_BD;User Id=sa;Password=<password>;TrustServerCertificate=True;Encrypt=True;Connect Timeout=30
DATABASE_CONNECTION_STRING=Server=db,1435;Database=Ingenieria_Purrujas_BD;User Id=sa;Password=<password>;TrustServerCertificate=True;Encrypt=True;Connect Timeout=30
```

La variable que realmente usa el backend para los repositorios es `ConnectionStrings__DefaultConnection`, porque en .NET equivale a:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  }
}
```

En Docker Compose se sobreescribe explicitamente con `Encrypt=False` para la conexion interna al contenedor SQL Server.

## Comandos Principales

Usar los comandos desde la raiz del proyecto.

### Construir imagenes

```powershell
docker compose build
```

### Levantar todo en primer plano

```powershell
docker compose up --build
```

### Levantar todo en segundo plano

```powershell
docker compose up -d --build
```

### Ver estado

```powershell
docker compose ps
```

### Ver logs

```powershell
docker compose logs -f db
docker compose logs -f backend
docker compose logs -f frontend-admin
docker compose logs -f frontend-cliente
```

### Detener sin borrar datos

```powershell
docker compose down
```

Esto conserva `purrujas_sqlserver_data`.

### Detener y borrar la base de datos

```powershell
docker compose down -v
```

Esto elimina el volumen de SQL Server. En el siguiente `up`, el entrypoint ejecutara de nuevo `Ingenieria_Purrujas_BD.sql` y los patches.

### Forzar reinicializacion desde variable

En PowerShell:

```powershell
$env:RESET_DATABASE_ON_START = "true"
docker compose up -d --build db
Remove-Item Env:\RESET_DATABASE_ON_START
```

Esta opcion hace que el entrypoint vuelva a ejecutar el script base aunque la base exista. Usarla con cuidado porque el script base puede recrear la base y perder datos.

## Comandos de Verificacion

### Validar compose

```powershell
docker compose config --quiet
```

### Compilar backend localmente

```powershell
cd Backend
dotnet build Backend-Ingenieria-Purrujas.sln
```

### Compilar frontends localmente

```powershell
cd Frontend/Frontend-Ingenieria-Purrujas-Admin
bun run build

cd ../Frontend-Ingenieria-Purrujas-Cliente
bun run build
```

### Validar inicializacion limpia de BD sin tocar el volumen principal

```powershell
docker compose -p purrujas_verify up -d --build db
docker inspect -f "{{.State.Health.Status}} RestartCount={{.RestartCount}}" ingenieria-purrujas-db
docker compose -p purrujas_verify down -v
```

Advertencia: el `container_name` del servicio `db` es fijo (`ingenieria-purrujas-db`). Para esta prueba temporal, el compose principal no debe estar corriendo al mismo tiempo.

### Consultar objetos creados en SQL Server

```powershell
docker exec ingenieria-purrujas-db /opt/mssql-tools18/bin/sqlcmd -C `
  -S localhost,1435 `
  -U sa `
  -P "<password>" `
  -d Ingenieria_Purrujas_BD `
  -Q "SET NOCOUNT ON; SELECT 'migrations' AS Item, COUNT(*) AS Total FROM dbo.__SchemaMigrations UNION ALL SELECT 'tables', COUNT(*) FROM sys.tables UNION ALL SELECT 'procedures', COUNT(*) FROM sys.procedures;"
```

Resultado esperado en la validacion actual:

```text
migrations  3
tables      29
procedures  67
```

## Flujo de Arranque

```text
docker compose up
        |
        v
Construye imagen db
        |
        v
Arranca SQL Server con docker-entrypoint.sh
        |
        v
Espera conexion en localhost,1435 dentro del contenedor
        |
        v
Crea Ingenieria_Purrujas_BD si no existe
        |
        v
Crea dbo.__SchemaMigrations y aplica DB/Patches/*.sql
        |
        v
Healthcheck pasa a healthy
        |
        v
Arranca backend
        |
        v
Arrancan frontend-admin y frontend-cliente
```

## Consideraciones de Desarrollo y Produccion

La configuracion actual esta orientada a desarrollo e integracion local:

- Los frontends usan `ng serve`, no una build estatica servida por Nginx.
- `ASPNETCORE_ENVIRONMENT` queda en `Development`.
- SQL Server usa `MSSQL_PID=Developer`.
- Hay un password por defecto en `docker-compose.yml` para facilitar ejecucion local.
- Los puertos se publican directamente en `localhost`.

Para produccion convendria cambiar:

- Servir Angular con build estatica y Nginx.
- Usar secretos fuera del repositorio.
- Quitar passwords por defecto.
- Definir `ASPNETCORE_ENVIRONMENT=Production`.
- Revisar CORS con dominios reales.
- Usar imagenes versionadas en vez de `latest` para SQL Server.

## Problemas Comunes

### El backend no conecta a la base

Revisar:

```powershell
docker compose ps
docker compose logs db
docker compose logs backend
```

Confirmar que `db` este `healthy` y que la cadena del backend use:

```text
Server=db,1435
```

Dentro de Docker no se debe usar `localhost` para conectar backend con base de datos.

### La BD no se reinicializa aunque cambie el SQL

Esto es esperado si el volumen ya existe. El entrypoint omite el script base cuando encuentra `Ingenieria_Purrujas_BD`, salvo que `RESET_DATABASE_ON_START=true`.

Para recrear desde cero:

```powershell
docker compose down -v
docker compose up -d --build
```

### Aparecen errores de login mientras SQL arranca

Durante los primeros segundos pueden aparecer mensajes como:

```text
SQL Server is not ready to accept new client connections
Login failed for user 'sa'
```

Si luego el contenedor queda `healthy`, esos mensajes son intentos tempranos del healthcheck/entrypoint y no representan un fallo final.

### Cambie `Backend/.env` y Compose no cambio el password interpolado

`env_file` inyecta variables al contenedor, pero no alimenta las expresiones `${...}` del YAML. Para cambiar valores interpolados como `MSSQL_SA_PASSWORD`, usar una variable del shell o un `.env` en la raiz del proyecto.

PowerShell:

```powershell
$env:MSSQL_SA_PASSWORD = "<password>"
docker compose up -d --build
```

### El puerto ya esta ocupado

Revisar procesos o contenedores usando puertos:

```powershell
docker compose ps
docker ps
netstat -ano | findstr ":5234"
netstat -ano | findstr ":4203"
netstat -ano | findstr ":4204"
netstat -ano | findstr ":1435"
```

## Resumen de Consistencia Actual

El proyecto queda alineado asi:

- `docker-compose.yml` construye los cuatro servicios desde sus carpetas correctas.
- SQL Server escucha en `1435`, y el backend usa `Server=db,1435`.
- El backend expone `5234`, y CORS permite `4203` y `4204`.
- El frontend admin usa `http://localhost:5234/api` en configuracion Docker de desarrollo.
- El frontend cliente usa `/api` con proxy Docker hacia `http://backend:5234`.
- El script SQL base y los patches se ejecutan correctamente en una base limpia.
- Los procedimientos almacenados usados por C# estan presentes en la BD.
- Las imagenes Docker construyen correctamente.
