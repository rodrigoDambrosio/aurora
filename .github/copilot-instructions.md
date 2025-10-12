---
applyTo: "**"
---

# Aurora - Mobile-First Personal Planner

.NET 9 backend (Clean Architecture) + React 19 frontend. SQLite persistence.

## Architecture & Structure

**Backend Layers** (`Aurora.*` projects):

- `Aurora.Api` → Controllers, CORS, OpenAPI (refs: Application + Infrastructure)
- `Aurora.Application` → Services, DTOs, FluentValidation (refs: Domain only)
- `Aurora.Domain` → Entities, interfaces (no dependencies)
- `Aurora.Infrastructure` → EF Core, repositories (refs: Domain + Application)

**Frontend**: `frontend/` - React 19 + TypeScript + Vite, mobile-first design

## Quick Start

```bash
# Backend: http://localhost:5291
dotnet run --project backend/Aurora.Api/Aurora.Api.csproj

# Frontend: http://localhost:5173 (proxies /api to backend)
cd frontend && npm run dev
```

**VS Code Tasks**: `build-backend`, `start-frontend`, `start-fullstack`

## Core Conventions

**Language**: Code in English, UI text in Spanish
**Backend**: PascalCase classes/methods, camelCase variables, `_camelCase` privates
**Frontend**: camelCase vars/functions, PascalCase components
**Validation**: FluentValidation ONLY (no DataAnnotations/manual checks)
**API**: JSON, centralized in `src/services/apiService.ts`

## Data Model

**Storage**: SQLite with EF Core

## Key Rules

1. Mobile-first responsive design
2. Clean Architecture layer boundaries
3. FluentValidation for all backend validation
4. Spanish error messages to frontend
5. 80% test coverage (xUnit backend, Jest frontend)

No hagas resumen de lo que acabás de hacer, solo ejecutá la tarea pedida.
No agregues comentarios a Jira si no te lo pido.
