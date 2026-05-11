# Guia de instalacion y ejecucion del proyecto

Esta guia explica como levantar el proyecto despues de bajarlo desde Git. El sistema tiene tres partes:

- Backend ASP.NET Core: `http://localhost:5232`
- Frontend cliente Angular: `http://localhost:4200`
- Frontend admin Angular: `http://localhost:4201`
- Base de datos SQL Server: `Ingenieria_Purrujas_BD`

## 1. Requisitos

Instala antes de comenzar:

- Git
- SQL Server o SQL Server Express
- SQL Server Management Studio, Azure Data Studio o `sqlcmd`
- .NET SDK compatible con `net9.0`
- Node.js
- npm o Bun

Para verificar versiones:

```cmd
git --version
dotnet --version
node --version
npm --version
```

## 2. Clonar el repositorio

```cmd
git clone URL_DEL_REPOSITORIO
cd Proyecto_Ingenieria_Hotelera_Purrujas
```
O en el github desktop clonar y pegar la url del proyecto en el github en línea

## 3. Crear la base de datos

Abre SQL Server Management Studio y ejecuta el script:

```text
DB\Ingenieria_Purrujas_BD.sql
```

Ese script crea la base de datos:

```text
Ingenieria_Purrujas_BD
```

Importante: el script elimina y vuelve a crear la base si ya existe. No lo ejecutes sobre una base con datos que quieras conservar.

Puedes confirmar que estas usando la base correcta con:

```sql
SELECT DB_NAME() AS BaseDeDatosActual;
```

## 4. Revisar la conexion del backend

El backend lee la cadena de conexion desde el .env que debe crearse "ver guia en el .env.production"

```

## 5. Levantar el backend

Desde la raiz del proyecto:

```cmd
cd Backend\Backend-Ingenieria-Purrujas\src\Api
dotnet restore
dotnet build
dotnet run
```

Debe quedar escuchando en:

```text
http://localhost:5232
```


Deja esta terminal abierta mientras uses los frontends.

## 6. Levantar el frontend cliente

Abre otra terminal desde la raiz del proyecto:

```cmd
cd Frontend\Frontend-Ingenieria-Purrujas-Cliente
npm install
npm run
```

Abre:

```text
http://localhost:4200
```

El cliente usa proxy para `/api` hacia:

```text
http://localhost:5232
```

## 7. Levantar el frontend admin

Abre otra terminal desde la raiz del proyecto:

```cmd
cd Frontend\Frontend-Ingenieria-Purrujas-Admin
npm install
npm run
```

Abre:

```text
http://localhost:4201
```

El admin en desarrollo apunta directamente a:

```text
http://localhost:5232/api
```

## 8. Usuarios admin de prueba

El script de base de datos crea estos usuarios:

```text
Usuario: admin.purrujas
Password: Purrujas2026!
Rol: Administrador
```

```text
Usuario: recepcion.purrujas
Password: Recepcion2026!
Rol: Supervisor
```

## 9. Orden recomendado para ejecutar todo

1. Ejecutar `DB\Ingenieria_Purrujas_BD.sql`.
2. Levantar backend en `http://localhost:5232`.
3. Levantar frontend cliente en `http://localhost:4200`.
4. Levantar frontend admin en `http://localhost:4201`.

## 10. Problemas comunes

### PowerShell bloquea npm

Si PowerShell muestra un error parecido a `npm.ps1 no se puede cargar`, usa:

```cmd
npm.cmd install
npm.cmd run start
```

O ejecuta los comandos desde CMD.

### No conecta a SQL Server

Revisa la cadena de conexion en `.env`. Si usas SQL Server Express, probablemente necesites:

```text
Server=.\SQLEXPRESS
```

Tambien confirma que el servicio de SQL Server este iniciado.

### El frontend carga pero no trae datos

Verifica que el backend este activo en:

```text
http://localhost:5232
```

Y revisa la consola del navegador por errores de API o CORS.

### Imagenes no cargan

Las imagenes de galeria se sirven desde el backend en:

```text
Backend\Backend-Ingenieria-Purrujas\src\Api\wwwroot\uploads\gallery
```

Por eso el backend debe estar corriendo para verlas correctamente.

## 11. Comandos de build

Backend:

```cmd
cd Backend\Backend-Ingenieria-Purrujas\src\Api
dotnet build
```

Frontend cliente:

```cmd
cd Frontend\Frontend-Ingenieria-Purrujas-Cliente
npm run build
```

Frontend admin:

```cmd
cd Frontend\Frontend-Ingenieria-Purrujas-Admin
npm run build
```
