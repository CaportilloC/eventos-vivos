# Eventos Vivos Web

Frontend Angular 22 para Eventos Vivos.

## Desarrollo local

Instalá dependencias e iniciá el servidor local:

```bash
npm install
npm start
```

Abrí <http://localhost:4200>.

El modo development usa `environment.development.ts` y proxy local:

```text
/api -> http://localhost:5000/api
/swagger -> http://localhost:5000/swagger
```

Por eso la API debe estar levantada en `http://localhost:5000`.

## Build

```bash
npm run build
```

## Tests

```bash
npm test -- --watch=false
```

## Configuración de ambientes

- Development: `apiBaseUrl = '/api/v1'` usando proxy Angular.
- Production: `apiBaseUrl = '/api/v1'` para servir frontend/API detrás del mismo host o proxy reverse.

## Notas

- No se usa NgRx: se descartó por compatibilidad no estable con Angular 22.
- El estado compartido simple se maneja con servicios/facades ligeras, RxJS y signals.
