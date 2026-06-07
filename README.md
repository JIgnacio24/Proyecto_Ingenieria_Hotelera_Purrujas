# Backend Ingeniería Purrujas

---

## Cómo correr el proyecto con Docker

Docker permite levantar toda la aplicación (base de datos, backend y ambos frontends) con un solo comando, sin necesidad de instalar .NET, Node.js ni SQL Server en tu máquina.

### 1. Instalar Docker Desktop

1. Descarga Docker Desktop desde **[https://www.docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop)**
2. Selecciona la versión para tu sistema operativo:
   - **Windows con Intel o AMD** → elige **AMD64**
   - **Windows con ARM / Mac Apple Silicon** → elige **ARM64**
3. Ejecuta el instalador. Si te pregunta si usar **WSL 2**, acepta (recomendado en Windows).
4. **Reinicia la computadora** cuando el instalador lo pida.
5. Abre Docker Desktop y espera a que el ícono de la ballena en la barra de tareas deje de animarse. Eso indica que Docker está listo.

Verifica que quedó instalado correctamente abriendo una terminal y ejecutando:

```bash
docker --version
docker-compose --version
```

Ambos comandos deben responder con un número de versión.

> **Si Docker Desktop no levanta o da error de WSL**, abre PowerShell como administrador y ejecuta:
> ```powershell
> wsl --update
> wsl --install
> ```
> Luego reinicia la computadora y vuelve a abrir Docker Desktop.

---

### 2. Configurar el archivo `.env` para Docker

El proyecto necesita un archivo `.env` en la carpeta `Backend/` con las credenciales para la base de datos y otros servicios. Este archivo **no está en el repositorio** por seguridad.

Crea el archivo `Backend/.env` con el contenido enviado por whatsapp

> También puedes copiar el archivo `Backend/.env.example` que ya viene en el repositorio como punto de partida y agregar las variables de JWT y SMTP.

---

### 3. Levantar el proyecto

Abre una terminal en la **raíz del proyecto** (donde está el archivo `docker-compose.yml`) y ejecuta:

```bash
docker-compose up --build
```

Este comando:
1. Construye las imágenes de todos los servicios
2. Levanta la base de datos SQL Server y espera a que esté lista
3. Levanta el backend .NET una vez que la base de datos está sana
4. Levanta ambos frontends Angular una vez que el backend está corriendo

La **primera vez tarda varios minutos** porque descarga las imágenes base (.NET, Node.js, SQL Server). Las siguientes veces es mucho más rápido.

Cuando veas los logs de los cuatro servicios corriendo sin errores, abre el navegador en:

| Servicio | URL |
|---|---|
| Frontend Cliente | http://localhost:4204 |
| Frontend Admin | http://localhost:4203 |
| Backend API | http://localhost:5234 |

> Usa siempre `localhost` con esos puertos. Las IPs internas que muestra Docker Desktop (`172.18.x.x`) son de la red interna de Docker y no son accesibles desde el navegador.

---

### 4. Detener el proyecto

Para detener todos los contenedores:

```bash
docker-compose down
```

Para detenerlos y **eliminar también los datos de la base de datos** (útil para empezar desde cero):

```bash
docker-compose down -v
```

---

### 5. Comandos útiles

```bash
# Ver el estado de los contenedores
docker-compose ps

# Ver los logs de un servicio específico
docker-compose logs backend
docker-compose logs db
docker-compose logs frontend-cliente
docker-compose logs frontend-admin

# Reconstruir solo un servicio
docker-compose up --build backend
```

---

### Nota sobre desarrollo local vs Docker

Docker y el entorno local **no se usan al mismo tiempo** porque ambos ocupan los mismos puertos.

| Situación | Usar |
|---|---|
| Desarrollo día a día | `dotnet run` + `ng serve` (más rápido, hot reload) |
| Probar que todo funciona junto | `docker-compose up --build` |

---

Backend del sistema hotelero **Las Purrujas Hotel & Resort**, diseñado con una arquitectura en capas inspirada en principios de **Clean Architecture / Hexagonal Architecture**, orientado a mantenibilidad, separación de responsabilidades y escalabilidad.

## Objetivo del proyecto

Este proyecto busca gestionar los procesos principales de un sistema hotelero, incluyendo:

- Gestión de clientes
- Gestión de habitaciones y tipos de habitación
- Reservaciones
- Facturación y pagos
- Promociones y temporadas
- Gestión de contenido del sitio
- Analítica y predicción

## Tecnologías utilizadas

| Tecnología | Uso |
|-----------|-----|
| ASP.NET Core | Backend / API |
| Angular v.21 | Frontend |
| SQL Server | Base de datos |
| Entity Framework / Repositories | Persistencia |
| Clean / Hexagonal Architecture | Organización del proyecto |

## Arquitectura del backend

El backend está organizado en cuatro capas principales:

- **API**: expone endpoints HTTP y configura la aplicación.
- **Application**: contiene casos de uso, comandos, queries y DTOs.
- **Domain**: contiene entidades, reglas de negocio, value objects y contratos.
- **Infrastructure**: contiene persistencia, servicios externos e implementaciones técnicas.

### Reglas de dependencia

- **API** → depende de `Application` e `Infrastructure`
- **Infrastructure** → depende de `Application` y `Domain`
- **Application** → depende de `Domain`
- **Domain** → no depende de ninguna otra capa

## Estructura del backend

```text
Backend/
└── Backend-Ingenieria-Purrujas/
    ├── src/
    │   ├── Backend-Ingenieria-Purrujas.Api/
    │   │   ├── Controllers/
    │   │   ├── Extensions/
    │   │   ├── Middleware/
    │   │   └── Program.cs
    │   │
    │   ├── Backend-Ingenieria-Purrujas.Application/
    │   │   ├── Abstractions/
    │   │   ├── Auth/
    │   │   ├── Customers/
    │   │   ├── Reservations/
    │   │   └── Rooms/
    │   │
    │   ├── Backend-Ingenieria-Purrujas.Domain/
    │   │   ├── Common/
    │   │   ├── Customers/
    │   │   ├── Reservations/
    │   │   └── Rooms/
    │   │
    │   └── Backend-Ingenieria-Purrujas.Infrastructure/
    │       ├── Files/
    │       ├── Identity/
    │       ├── Persistence/
    │       └── Services/
    │
    ├── tests/
    └── Backend-Ingenieria-Purrujas.sln
