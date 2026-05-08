# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Hotel management system for "Purrujas" with a public-facing website and an admin dashboard. The system handles reservations, quotes, promotions, and editable page content.

## Repository Structure

```
Proyecto_Ingenieria_Hotelera_Purrujas/
├── Backend/Backend-Ingenieria-Purrujas/src/   ASP.NET Core 9 Web API
├── Frontend/Frontend-Ingenieria-Purrujas-Cliente/  Angular 21 public website (port 4200)
├── Frontend/Frontend-Ingenieria-Purrujas-Admin/    Angular 21 admin dashboard (port 4201)
└── DB/                                             SQL Server schema + patches
```

## Commands

### Backend
```bash
cd Backend/Backend-Ingenieria-Purrujas
dotnet build
dotnet run --project src/Api
```

### Frontend (both apps use Bun, not npm)
```bash
cd Frontend/Frontend-Ingenieria-Purrujas-Cliente
bun install && bun run start        # public site on port 4200

cd Frontend/Frontend-Ingenieria-Purrujas-Admin
bun install && bun run start        # admin panel on port 4201
bun run build                       # production build
```

### Testing (Frontend)
```bash
bun run test        # Vitest
```

### Code Formatting (Frontend)
```bash
bun run format      # Prettier
```

## Backend Architecture

Clean Architecture with four layers — dependency flow is strictly one-directional:

```
Api → Application → Domain
Api → Infrastructure → Application → Domain
Domain (no dependencies)
```

- **Domain** — Entities (`AdminUser`, `Customer`, `Room`, `RoomType`, `Reservation`, `Payment`, `Season`, `Promotion`, `Advertising`, `AboutUsPageContent`, `FacilitiesPageContent`) and repository interfaces.
- **Application** — Service interfaces and implementations (`AuthService`, `QuoteService`, `ReservationService`). Only depends on Domain.
- **Infrastructure** — Repository implementations using raw `SqlClient` against SQL Server. Depends on Application + Domain.
- **Api** — ASP.NET Core controllers, DTOs, JWT auth, DI wiring. Configures CORS for `localhost:4200` and `localhost:4201`.

Environment variables (via DotNetEnv) override `appsettings.json`. Copy `.env.example` to `.env` and populate secrets before running locally.

JWT tokens expire in 8 hours. The `AdminOnly` policy is applied to mutating endpoints. Auth endpoints include explicit no-cache headers.

## Frontend Architecture

Both Angular apps use **standalone components** (no NgModules), **Bun**, **Tailwind CSS 4**, and **Vitest**.

### Public Website (`Frontend-Ingenieria-Purrujas-Cliente`)
Routes: `/` (home), `/about-us`, `/facilities`, `/cotizar` (quote calculator), `/promociones`, `/publicidad`.

No auth interceptor — all calls are public. Services in `src/app/services/` handle HTTP and provide default values for missing content fields.

### Admin Dashboard (`Frontend-Ingenieria-Purrujas-Admin`)
Routes: `/ingreso` (login), `/panel` (dashboard), `/panel/sobre-nosotros` (About Us editor).

`core/` holds all auth logic:
- `auth.service.ts` — JWT session stored in `sessionStorage`; uses Angular signals for state.
- `auth.interceptor.ts` — Injects `Authorization: Bearer` header on every request.
- `auth.guard.ts` — Protects `/panel` routes; validates token expiry and `ADMINISTRATOR_ROLE`.

## Database

SQL Server database `Ingenieria_Purrujas_BD`. Schema lives in `DB/DB-Ingenieria-Purrujas.sql`. Incremental patches are in `DB/Patches/` and must be applied in date order after the base schema.

Default dev connection string (Windows auth):
```
Server=.;Database=Ingenieria_Purrujas_BD;Trusted_Connection=True;TrustServerCertificate=True
```

## Key Patterns

- Repository pattern: interfaces in `Domain/Repositories/`, implementations in `Infrastructure/Repositories/`.
- Services registered as `Scoped` in `Program.cs`.
- Frontend HTTP calls return typed RxJS `Observable`s; use `tap`/`map` for side-effects and transforms.
- Content pages (About Us, Facilities) store structured data as JSON columns in the database and expose dedicated PUT endpoints for admin editing.
