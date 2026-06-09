# Implementación de Docker en el Sistema Hotelero Las Purrujas

## ¿Qué es Docker y por qué se usó?

Docker es una plataforma que permite empaquetar una aplicación junto con todas sus dependencias (librerías, configuraciones, runtime) dentro de una unidad llamada **contenedor**. A diferencia de instalar el software directamente en una máquina, un contenedor es aislado, reproducible y funciona igual en cualquier computadora, independientemente del sistema operativo o configuración local del desarrollador.

Para este proyecto, Docker resuelve un problema concreto: el sistema está compuesto por cuatro servicios distintos (base de datos SQL Server, backend .NET, frontend de administración Angular y frontend de clientes Angular). Sin Docker, cada integrante del equipo necesita instalar y configurar manualmente todos esos componentes, gestionar versiones y resolver conflictos entre herramientas. Con Docker, basta con tener Docker Desktop instalado y ejecutar un único comando para levantar toda la aplicación.

---

## Herramienta requerida: Docker Desktop

Para ejecutar Docker en una computadora local se necesita instalar **Docker Desktop**, que incluye el motor de Docker, Docker Compose y una interfaz visual para monitorear los contenedores.

**Descarga:** https://www.docker.com/products/docker-desktop

Al instalar en Windows, el instalador pide habilitar **WSL 2** (Windows Subsystem for Linux), que es el entorno que permite correr contenedores Linux dentro de Windows. Es necesario aceptarlo.

Se debe elegir la versión correcta según la arquitectura del procesador:
- Procesadores **Intel o AMD** → versión **AMD64**
- Procesadores **ARM** (algunos portátiles modernos) o **Apple Silicon** → versión **ARM64**

> AMD64 no significa que sea exclusivo para procesadores AMD. Es el nombre de la arquitectura de 64 bits que comparten tanto Intel como AMD.

---

## Estructura de archivos Docker del proyecto

El proyecto utiliza los siguientes archivos relacionados con Docker:

```
Proyecto_Ingenieria_Hotelera_Purrujas/
├── docker-compose.yml                          # Orquestador principal
├── Backend/
│   ├── Dockerfile                              # Imagen del backend .NET
│   ├── .env                                    # Variables de entorno para Docker (no está en git)
│   ├── .env.example                            # Plantilla del .env para Docker
│   └── .dockerignore                           # Archivos excluidos al construir la imagen
├── DB/
│   ├── Dockerfile                              # Imagen personalizada de SQL Server
│   ├── Ingenieria_Purrujas_BD.sql              # Script de creación de la base de datos
│   ├── docker-entrypoint.sh                    # Script de inicialización del contenedor de BD
│   ├── sqlserver-healthcheck.sh                # Script de verificación de salud de la BD
│   └── .dockerignore
├── Frontend/
│   ├── Frontend-Ingenieria-Purrujas-Admin/
│   │   ├── Dockerfile                          # Imagen del frontend admin
│   │   └── .dockerignore
│   └── Frontend-Ingenieria-Purrujas-Cliente/
│       ├── Dockerfile                          # Imagen del frontend cliente
│       ├── proxy.conf.json                     # Proxy para desarrollo local
│       ├── proxy.docker.conf.json              # Proxy para desarrollo dentro de Docker
│       └── .dockerignore
```

---

## Descripción detallada de cada archivo

### `docker-compose.yml` — El orquestador

Este archivo define los cuatro servicios que componen la aplicación y cómo se relacionan entre sí. Docker Compose lee este archivo y se encarga de construir las imágenes, crear los contenedores, conectarlos en una red interna y levantarlos en el orden correcto.

```yaml
services:
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
      RESET_DATABASE_ON_START: "false"
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
    networks:
      - Ingenieria-Purrujas-Sistema-Hotelero

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
      Cors__AllowedOrigins__0: "http://localhost:4203"
      Cors__AllowedOrigins__1: "http://127.0.0.1:4203"
      Cors__AllowedOrigins__2: "http://localhost:4204"
      Cors__AllowedOrigins__3: "http://127.0.0.1:4204"
    ports:
      - "5234:5234"
    depends_on:
      db:
        condition: service_healthy
    networks:
      - Ingenieria-Purrujas-Sistema-Hotelero

  frontend-admin:
    build:
      context: ./Frontend/Frontend-Ingenieria-Purrujas-Admin
    container_name: ingenieria-purrujas-frontend-admin
    restart: unless-stopped
    ports:
      - "4203:4203"
    depends_on:
      - backend
    networks:
      - Ingenieria-Purrujas-Sistema-Hotelero

  frontend-cliente:
    build:
      context: ./Frontend/Frontend-Ingenieria-Purrujas-Cliente
    container_name: ingenieria-purrujas-frontend-cliente
    restart: unless-stopped
    ports:
      - "4204:4204"
    depends_on:
      - backend
    networks:
      - Ingenieria-Purrujas-Sistema-Hotelero

volumes:
  purrujas_sqlserver_data:

networks:
  Ingenieria-Purrujas-Sistema-Hotelero:
    name: Ingenieria-Purrujas-Sistema-Hotelero
    driver: bridge
```

**Decisiones de diseño relevantes:**

- `depends_on` con `condition: service_healthy`: el backend no arranca hasta que la base de datos pase el healthcheck. Esto evita que el backend falle al intentar conectarse a una base de datos que todavía está iniciando.
- `restart: unless-stopped`: si un contenedor falla inesperadamente, Docker lo reinicia automáticamente.
- `volumes: purrujas_sqlserver_data`: los datos de la base de datos se guardan en un volumen persistente. Si el contenedor se detiene y se vuelve a levantar, los datos no se pierden.
- `RESET_DATABASE_ON_START: "false"`: la base de datos solo se inicializa con el script SQL si no existe previamente. Esto protege los datos en levantadas posteriores.
- Red `bridge` compartida: todos los servicios están en la misma red interna, lo que les permite comunicarse entre sí usando el nombre del servicio (por ejemplo, el backend accede a la BD como `Server=db,1435` en lugar de `localhost`).
- `ports ("host:contenedor")`: mapea los puertos del contenedor a la máquina anfitriona para que el navegador pueda acceder usando `localhost`.

---

### `DB/Dockerfile` — Imagen personalizada de SQL Server

```dockerfile
FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

COPY Ingenieria_Purrujas_BD.sql /docker-entrypoint-initdb.d/Ingenieria_Purrujas_BD.sql
COPY docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
COPY sqlserver-healthcheck.sh /usr/local/bin/sqlserver-healthcheck.sh

RUN chmod +x /usr/local/bin/docker-entrypoint.sh /usr/local/bin/sqlserver-healthcheck.sh \
    && chown -R mssql:root /docker-entrypoint-initdb.d \
    && chown mssql:root /usr/local/bin/docker-entrypoint.sh /usr/local/bin/sqlserver-healthcheck.sh

USER mssql

EXPOSE 1435

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]
```

Se parte de la imagen oficial de Microsoft SQL Server 2022. Sobre ella se copian el script de inicialización de la base de datos y los dos scripts de shell personalizados. Los permisos de ejecución se asignan con `chmod +x` y la propiedad de los archivos se transfiere al usuario `mssql` por razones de seguridad. El contenedor inicia ejecutando `docker-entrypoint.sh`.

---

### `DB/docker-entrypoint.sh` — Inicialización automática de la base de datos

Este script reemplaza el proceso de inicio predeterminado de SQL Server para agregar lógica de inicialización automática de la base de datos.

```bash
#!/usr/bin/env bash
set -euo pipefail

SQLSERVER_PORT="${MSSQL_TCP_PORT:-1433}"
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

if [ -z "${MSSQL_SA_PASSWORD:-}" ]; then
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
    if sqlcmd -S "localhost,${SQLSERVER_PORT}" -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" >/dev/null 2>&1; then
        ready=1
        break
    fi
    sleep 2
done

if [ "$ready" -ne 1 ]; then
    echo "SQL Server no estuvo listo a tiempo." >&2
    exit 1
fi

db_exists="$(sqlcmd -S "localhost,${SQLSERVER_PORT}" -U sa -P "$MSSQL_SA_PASSWORD" -h -1 -W -Q \
    "SET NOCOUNT ON; SELECT CASE WHEN DB_ID(N'Ingenieria_Purrujas_BD') IS NULL THEN 0 ELSE 1 END" \
    | tr -d '\r[:space:]')"

if [ "${RESET_DATABASE_ON_START:-false}" = "true" ] || [ "$db_exists" != "1" ]; then
    echo "Inicializando Ingenieria_Purrujas_BD desde ${SQL_SCRIPT}..."
    sqlcmd -S "localhost,${SQLSERVER_PORT}" -U sa -P "$MSSQL_SA_PASSWORD" -b -i "$SQL_SCRIPT"
else
    echo "La base Ingenieria_Purrujas_BD ya existe. Se omite la inicializacion."
fi

wait "$server_pid"
```

El script realiza los siguientes pasos en orden:
1. Inicia el proceso de SQL Server en segundo plano.
2. Espera activamente hasta 120 segundos (60 intentos × 2 segundos) a que SQL Server acepte conexiones.
3. Verifica si la base de datos `Ingenieria_Purrujas_BD` ya existe.
4. Si no existe (o si `RESET_DATABASE_ON_START` es `true`), ejecuta el script SQL de creación de la base de datos.
5. Si ya existe, omite la inicialización para preservar los datos.
6. Mantiene el proceso en primer plano para que el contenedor siga corriendo.

---

### `DB/sqlserver-healthcheck.sh` — Verificación de salud

```bash
#!/usr/bin/env bash
set -euo pipefail

SQLSERVER_PORT="${MSSQL_TCP_PORT:-1433}"

if [ -x "/opt/mssql-tools18/bin/sqlcmd" ]; then
    /opt/mssql-tools18/bin/sqlcmd -C -S "localhost,${SQLSERVER_PORT}" -U sa -P "${MSSQL_SA_PASSWORD}" -Q "SELECT 1" >/dev/null
else
    /opt/mssql-tools/bin/sqlcmd -S "localhost,${SQLSERVER_PORT}" -U sa -P "${MSSQL_SA_PASSWORD}" -Q "SELECT 1" >/dev/null
fi
```

Docker Compose ejecuta este script periódicamente para determinar si el contenedor de base de datos está listo. Si el comando `SELECT 1` se ejecuta sin error, el contenedor se marca como `healthy` y el backend recibe autorización para arrancar. Esto implementa el patrón de dependencia con condición de salud definido en el `docker-compose.yml`.

---

### `Backend/Dockerfile` — Imagen del backend .NET

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

Este Dockerfile usa la técnica de **build multi-etapa**:

- **Etapa `build`**: usa la imagen del SDK completo de .NET 9 para compilar y publicar la aplicación. Se copian primero solo los archivos `.csproj` para que Docker pueda aprovechar su caché de capas al restaurar paquetes NuGet. Si el código fuente cambia pero los proyectos no cambian, Docker reutiliza la capa de `dotnet restore`.
- **Etapa `runtime`**: usa únicamente la imagen de runtime de ASP.NET Core, que es mucho más liviana que el SDK. Solo se copia el resultado compilado de la etapa anterior. Esto produce una imagen final pequeña y sin herramientas de compilación innecesarias.

---

### `Frontend/Dockerfile` — Imágenes de los frontends Angular

Ambos frontends siguen la misma estructura:

```dockerfile
FROM node:22.12-bookworm-slim

WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

ENV CHOKIDAR_USEPOLLING=true
EXPOSE 4203  # (o 4204 para el cliente)

CMD ["npm", "run", "ng", "--", "serve", "--host", "0.0.0.0", "--port", "4203", "--configuration", "development"]
```

Se copian primero los archivos de dependencias (`package.json` y `package-lock.json`) y se instalan con `npm ci` antes de copiar el resto del código. Esto permite que Docker reutilice la capa de instalación de paquetes si las dependencias no cambiaron. `npm ci` se prefiere sobre `npm install` en entornos reproducibles porque instala exactamente las versiones del `package-lock.json`.

`CHOKIDAR_USEPOLLING=true` activa el polling para la detección de cambios de archivos, necesario porque los sistemas de archivos virtualizados dentro de Docker en Windows no emiten eventos de cambio nativos de manera confiable.

`--host 0.0.0.0` hace que el servidor de Angular escuche en todas las interfaces de red del contenedor, lo que permite que el mapeo de puertos de Docker funcione. Sin esto, Angular solo escucharía en `localhost` dentro del contenedor y sería inaccesible desde el exterior.

El frontend cliente agrega `--proxy-config proxy.docker.conf.json`, que redirige las llamadas a `/api` hacia el contenedor del backend usando su nombre de servicio interno (`http://backend:5234`), en lugar de `localhost`.

---

### `Frontend/Frontend-Ingenieria-Purrujas-Cliente/proxy.docker.conf.json` — Proxy de red interna

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

Este archivo es específico para el entorno Docker. Dentro de la red de Docker, los contenedores no se pueden comunicar usando `localhost` porque cada uno tiene su propia interfaz de red aislada. En su lugar, se usa el nombre del servicio definido en `docker-compose.yml` (`backend`) como hostname. El servidor de desarrollo de Angular actúa como intermediario: cuando el navegador hace una petición a `/api/...`, el servidor Angular dentro del contenedor la redirige a `http://backend:5234/api/...`.

Para desarrollo local sin Docker existe el archivo `proxy.conf.json` que apunta a `http://localhost:5234`, y el Dockerfile usa explícitamente la versión Docker (`proxy.docker.conf.json`).

---

### `Backend/.env` — Variables de entorno para Docker

Este archivo es **excluido del repositorio** por `.gitignore` porque contiene credenciales. Cada integrante del equipo debe crearlo manualmente. Es cargado por Docker Compose a través de la directiva `env_file: - ./Backend/.env` en el `docker-compose.yml`.

La diferencia fundamental con el `.env` de desarrollo local (`src/Api/.env`) está en la cadena de conexión a la base de datos:

| Aspecto | Desarrollo local | Docker |
|---|---|---|
| Servidor | `Server=.` (SQL Server local) | `Server=db,1435` (contenedor en la red Docker) |
| Autenticación | `Integrated Security=True` (Windows) | `User Id=sa;Password=...` (SQL auth) |
| Puerto | Estándar 1433 | Personalizado 1435 |

En Docker no existe "autenticación integrada de Windows" porque el contenedor corre Linux. Por eso se usa el usuario administrador `sa` con una contraseña.

---

### `.gitattributes` — Control de saltos de línea

```
# Auto detect text files and perform LF normalization
* text=auto

# Shell scripts siempre deben usar LF (Unix) o fallan en contenedores Linux
*.sh text eol=lf
```

Esta configuración fue necesaria para resolver un problema de compatibilidad entre Windows y Linux. Git en Windows, por defecto, convierte los saltos de línea de los archivos de texto de LF (Unix) a CRLF (Windows) al hacer checkout. Los scripts `.sh` que se ejecutan dentro de los contenedores Linux fallaban con el error:

```
/usr/bin/env: 'bash\r': No such file or directory
```

El `\r` visible en el error es el carácter de retorno de carro (`CR`) de Windows que quedaba al final de la primera línea del script. La regla `*.sh text eol=lf` fuerza que Git siempre mantenga los scripts con saltos de línea Unix, independientemente del sistema operativo donde se haga el checkout.

Los scripts afectados (`DB/docker-entrypoint.sh` y `DB/sqlserver-healthcheck.sh`) también fueron convertidos directamente en disco para corregir el problema en las copias ya descargadas.

---

## Flujo de arranque de los contenedores

Cuando se ejecuta `docker-compose up --build`, los servicios arrancan en este orden debido a las dependencias configuradas:

```
1. db (SQL Server)
      │
      │ Docker construye la imagen personalizada de SQL Server
      │ Corre docker-entrypoint.sh: inicia sqlservr, espera conexión, inicializa BD si no existe
      │ Docker ejecuta sqlserver-healthcheck.sh cada 10 segundos
      │
      ▼ (cuando healthcheck pasa: "healthy")
2. backend (.NET API)
      │
      │ Docker construye la imagen multi-etapa: restore → publish → runtime
      │ El backend arranca y se conecta a la BD usando "Server=db,1435"
      │
      ▼ (cuando el contenedor está corriendo)
3. frontend-admin y frontend-cliente (Angular)
      │
      │ Docker instala dependencias npm y sirve la app con ng serve
      │
      ▼
Todo el sistema está disponible en:
  - http://localhost:4203  (admin)
  - http://localhost:4204  (cliente)
  - http://localhost:5234  (API)
```

---

## Comandos de operación

```bash
# Levantar todo (construye imágenes si no existen o si hubo cambios)
docker-compose up --build

# Levantar en segundo plano
docker-compose up --build -d

# Ver el estado de los contenedores
docker-compose ps

# Ver los logs de un servicio en tiempo real
docker-compose logs -f backend
docker-compose logs -f db

# Detener todos los contenedores (conserva los datos)
docker-compose down

# Detener y eliminar volúmenes (borra la base de datos)
docker-compose down -v
```

---

## Coexistencia con el entorno de desarrollo local

Docker y el entorno de desarrollo local son independientes y **no deben usarse al mismo tiempo** porque ambos ocupan los mismos puertos del sistema.

| Modo | Cuándo usarlo | Cómo |
|---|---|---|
| **Local** | Desarrollo activo del día a día | `dotnet run` + `ng serve` (hot reload inmediato) |
| **Docker** | Prueba integrada de todos los servicios | `docker-compose up --build` |
