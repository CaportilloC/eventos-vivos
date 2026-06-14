# Checklist final de entrega — Eventos Vivos

## Estado funcional

### Requerimientos funcionales

- [x] RF-01 Crear evento con validaciones de título, descripción, fechas, precio, capacidad y venue.
- [x] RF-02 Listar/filtrar eventos por tipo, fechas, venue, estado y título parcial.
- [x] RF-03 Reservar entradas con disponibilidad, email válido, cantidad mínima y reglas de 1h/24h/precio.
- [x] RF-04 Confirmar pago, pasar a `confirmada` y generar código único `EV-######`.
- [x] RF-05 Cancelar reservas con regla aprobada de 48h: `cancelada` o `perdida`.
- [x] RF-06 Reporte de ocupación por evento con confirmadas, disponibles, ocupación, ingresos y estado.

### Reglas de negocio

- [x] RN-01 Capacidad del venue.
- [x] RN-02 No solapamiento de eventos activos por venue.
- [x] RN-03 Weekend no inicia después de las 22:00.
- [x] RN-04 No reservar si el evento inicia en menos de 1 hora.
- [x] RN-05 Precio mayor a COP 100.000 limita a 10 entradas por transacción.
- [x] RN-06 Estado `completado` cuando la fecha actual supera la hora de fin.
- [x] RN-07 Cancelación confirmada con menos de 48h queda `perdida`.

## Datos demo

- [x] Seeder demo controlado por configuración.
- [x] Docker local habilita demo seed en Development.
- [x] Se preservan venues.
- [x] Se limpian eventos/reservas de tests.
- [x] 9 eventos demo: 3 por venue, 3 por tipo, pasado/presente/futuro.
- [x] 27 reservas demo con todos los estados.
- [x] Precios en COP.
- [x] Usuarios ficticios realistas con `example.com`.

## Validación ejecutada

### Frontend

```bash
cd src/frontend/eventos-vivos-web
npm run build
npm test -- --watch=false
```

Resultado: build OK, 2/2 tests OK.

### Backend

```bash
docker compose -f docker/docker-compose.yml build api
docker run --rm -v "$PWD":/work -w /work mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test tests/backend/EventosVivos.Tests/EventosVivos.Tests.csproj \
  --nologo --filter "FullyQualifiedName!~IntegrationTests"
```

Resultado: Docker API build OK, 95/95 tests no-integración OK.

### Docker frontend

```bash
docker compose -f docker/docker-compose.yml build frontend
```

Resultado: Docker frontend build OK.

### Endpoints demo

```bash
curl http://localhost:5000/api/v1/events?pageNumber=1\&pageSize=20
curl http://localhost:5000/api/v1/reservations?pageNumber=1\&pageSize=50
curl http://localhost:5000/swagger/v1/swagger.json
```

Resultado:

- 9 eventos.
- 27 reservas.
- Swagger disponible.

### Smoke UI

- [x] `/events` carga sin errores de consola.
- [x] `/reservations` carga sin errores de consola.
- [x] `/reports` carga sin errores de consola.

## Deuda conocida aceptada

- Docker local usa `.env`; no commitear secretos reales.
- Sass `@import` deprecado en `styles.scss`.
- SweetAlert2 CommonJS optimization warning.
- `npm audit` reporta 3 vulnerabilidades high que requieren revisión separada.
- Tests de integración con SQL Server requieren ejecutar dentro de la red Compose o configurar connection string adecuado.
- Reservas `pendiente_pago` del seed expiran en 15 minutos por regla real de negocio.

## Comandos recomendados antes del primer commit

```bash
git status --short
git diff --stat
git diff --check
```

No commitear archivos ignorados/locales como `.playwright-mcp/`, `node_modules/`, `dist/`, `bin/`, `obj/` o screenshots temporales.
