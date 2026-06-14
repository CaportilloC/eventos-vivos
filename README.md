# Eventos Vivos

Sistema de gestión de eventos y reservas construido como prueba técnica con ASP.NET Core Web API, Angular, SQL Server y Docker.

## Stack

- Backend: ASP.NET Core Web API (.NET 10), EF Core, SQL Server.
- Frontend: Angular 22, Bootstrap 5, Angular Material y SweetAlert2.
- Arquitectura: monorepo con Onion/Clean Architecture ligera.
- Infraestructura local: Docker Compose.

## Ejecución local recomendada

Desde la raíz del repo:

```bash
cp .env.example .env
docker compose -f docker/docker-compose.yml up -d --build sqlserver api
```

Luego iniciar Angular en modo desarrollo:

```bash
cd src/frontend/eventos-vivos-web
npm install
npm start
```

URLs:

- Frontend: <http://localhost:4200>
- API Swagger: <http://localhost:5000/swagger>
- API base: <http://localhost:5000/api/v1>
- SQL Server: `localhost:1433`

La contraseña local de SQL Server se toma desde `.env`:

```text
MSSQL_SA_PASSWORD=...
```

El frontend local usa proxy de Angular:

```text
http://localhost:4200/api/* -> http://localhost:5000/api/*
```

## Demo data

El ambiente Docker local tiene habilitado el seeder demo:

```yaml
DemoData__SeedOnStartup: "true"
DemoData__ResetBeforeSeed: "true"
```

Al iniciar la API en Docker:

1. Se preservan los venues base.
2. Se eliminan eventos y reservas anteriores.
3. Se cargan 9 eventos demo y 27 reservas demo.

La demo cubre:

- 3 eventos por venue.
- 3 eventos por tipo: conferencia, taller, concierto.
- eventos pasados, actuales y futuros.
- reservas `pendiente_pago`, `confirmada`, `cancelada` y `perdida`.
- precios en pesos colombianos.

> Nota: las reservas `pendiente_pago` expiran en 15 minutos por regla de negocio. Para una demo interactiva, reiniciá la API justo antes de presentar.

## Comandos útiles

### Backend Docker

```bash
docker compose -f docker/docker-compose.yml build api
docker compose -f docker/docker-compose.yml up -d sqlserver api
docker compose -f docker/docker-compose.yml logs -f api
```

### Frontend

```bash
cd src/frontend/eventos-vivos-web
npm run build
npm test -- --watch=false
```

### Validación rápida de API

```bash
curl http://localhost:5000/api/v1/events?pageNumber=1\&pageSize=20
curl http://localhost:5000/api/v1/reservations?pageNumber=1\&pageSize=50
```

## Funcionalidad cubierta

- CRUD de eventos.
- CRUD de reservas pendientes.
- Confirmación de pago con código `EV-######`.
- Cancelación de reservas con regla de penalización de 48 horas.
- Reporte de ocupación por evento.
- Filtros y paginación en eventos, reservas y catálogos.
- Swagger/OpenAPI documentado.

## Reglas principales

- Un evento no puede exceder la capacidad del venue.
- No puede haber eventos activos solapados en el mismo venue.
- Eventos de fin de semana no inician después de las 22:00.
- No se permiten reservas para eventos que inician en menos de 1 hora.
- Eventos con precio mayor a COP 100.000 limitan máximo 10 entradas por transacción.
- Un evento se marca como completado cuando la fecha actual supera su hora de fin.
- Una reserva confirmada cancelada con menos de 48 horas queda como `perdida`.

## Deuda conocida

- La configuración local usa `.env`; no commitear secretos reales.
- Warnings frontend conocidos: Sass `@import` deprecado y SweetAlert2 CommonJS.
- Tests backend completos requieren SDK .NET disponible o ejecución en contenedor con red SQL Server configurada.
