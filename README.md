# Mis Gastos

Aplicación para registrar ingresos y gastos personales y controlar el presupuesto mensual. Funciona en la web y en el celular (PWA instalable desde el navegador).

## Stack

| Capa | Tecnología |
| --- | --- |
| Frontend | Angular 22 (standalone, signals), Angular Material, PWA |
| Backend | .NET 10 Web API, EF Core 10, FluentValidation |
| Base de datos | PostgreSQL 16 |
| Infra | Docker Compose, GitHub Actions |

## Arquitectura

Monolito modular con Clean Architecture. Las dependencias apuntan hacia el dominio:

```
backend/
  src/
    Gastos.Domain          Entidades y reglas de negocio. No depende de nada.
    Gastos.Application     Casos de uso, validaciones e interfaces (IApplicationDbContext, ICurrentUser).
    Gastos.Infrastructure  EF Core + PostgreSQL, migraciones, auditoría.
    Gastos.Api             Controllers, autenticación, OpenAPI.
  tests/
    Gastos.UnitTests
frontend/
  src/app/
    core/        Servicios transversales (API, auth).
    layout/      Shell: menú lateral en escritorio, barra inferior en el celular.
    features/    Una carpeta por sección, cargada bajo demanda.
```

Cada módulo nuevo (recurrentes, cuotas, importación de resúmenes) se suma como una carpeta en `Domain`, `Application` y `features/` sin tocar los demás.

Decisiones de datos:

- Todas las tablas usan UUID v7 como clave, `created_at`/`updated_at` y borrado lógico (`deleted_at`).
- Cada consulta se filtra automáticamente por el usuario autenticado (filtro global de EF Core).
- Los montos son `numeric(18,2)` y siempre positivos; el tipo de movimiento define si suma o resta.
- El saldo de una cuenta se calcula a partir de sus movimientos, nunca se guarda.
- Cada alta, cambio o baja queda registrada en `audit_log`.

## Cómo levantarlo

### Con Docker (todo junto)

```bash
docker compose up --build
```

- Web: http://localhost:8080
- API y documentación: http://localhost:5080/docs
- Salud: http://localhost:5080/health

### Para desarrollar

Requisitos: .NET SDK 10, Node 24 y PostgreSQL 16 (o `docker compose up db`).

```bash
# Backend (aplica las migraciones al arrancar en Development)
cd backend
dotnet run --project src/Gastos.Api        # http://localhost:5080

# Frontend (redirige /api al backend)
cd frontend
npm install
npm start                                  # http://localhost:4200
```

### Tests

```bash
cd backend && dotnet test
cd frontend && npx ng test --watch=false
```

### Migraciones

```bash
cd backend
dotnet tool restore
dotnet ef migrations add <Nombre> -p src/Gastos.Infrastructure -s src/Gastos.Api -o Persistence/Migrations
```

## Estado

- [x] Paso 1: alcance del MVP, arquitectura y modelo de datos.
- [x] Paso 2: esqueleto del proyecto (este commit).
- [ ] Paso 3: MVP: login (email y Google), cuentas, categorías, movimientos, presupuesto y dashboard.
- [ ] Paso 4: publicación.
