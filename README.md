# Backend Ingeniería Purrujas

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
