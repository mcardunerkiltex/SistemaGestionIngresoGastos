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

### Login con Google (opcional)

Sin configurar, la app funciona solo con email y contraseña y el botón de Google no aparece.

1. En [Google Cloud Console](https://console.cloud.google.com/apis/credentials) creá un **ID de cliente OAuth** de tipo *Aplicación web*.
2. En **Orígenes autorizados de JavaScript** agregá `http://localhost:4200`, `http://localhost:8080` y el dominio donde se publique.
3. Pasá el Client ID a la API:
   - Desarrollo: `cd backend && dotnet user-secrets --project src/Gastos.Api set Google:ClientId <client-id>`
   - Docker: `GOOGLE_CLIENT_ID=<client-id> docker compose up --build`

### Sesión y seguridad

- Access token JWT de 15 minutos, guardado solo en memoria del navegador.
- Refresh token de 30 días en una cookie `httpOnly`, `Secure` y `SameSite=Strict`; se rota en cada uso y se guarda hasheado. Si alguien reusa uno viejo, se cierran todas las sesiones del usuario.
- Contraseñas con PBKDF2 (hasher de ASP.NET Core Identity).
- Límite de 10 intentos por minuto por IP en los endpoints de login y registro.
- En producción `Jwt:SigningKey` (32+ caracteres) va como variable de entorno `Jwt__SigningKey`, nunca en el repo.

### Tests

Los tests de integración levantan la API completa contra PostgreSQL y crean una base descartable por corrida (usa `TEST_DATABASE_URL` o la base local de docker compose).

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
- [x] Paso 2: esqueleto del proyecto.
- [ ] Paso 3: MVP
  - [x] Login con email y Google, sesión con refresh token
  - [x] Cuentas (con saldo) y categorías
  - [ ] Movimientos y carga rápida
  - [ ] Presupuesto y dashboard
- [ ] Paso 4: publicación.
