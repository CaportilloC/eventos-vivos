# ──────────────────────────────────────────────────────────────
# Stage 1: Build Angular application
# ──────────────────────────────────────────────────────────────
FROM node:22-alpine AS build
WORKDIR /build

# Install dependencies first (layer caching)
COPY src/frontend/eventos-vivos-web/package*.json ./
RUN npm ci

# Copy source and build
COPY src/frontend/eventos-vivos-web/ .
RUN npm run build

# ──────────────────────────────────────────────────────────────
# Stage 2: Serve with nginx
# ──────────────────────────────────────────────────────────────
FROM nginx:alpine AS runtime

# Runtime API proxy target. Docker Compose keeps the internal service name;
# Azure Container Apps overrides this with the deployed API FQDN.
ENV API_PROXY_BASE_URL=http://api:8080

# Copy custom nginx config as a template so runtime env vars can be resolved.
COPY docker/frontend/nginx.conf /etc/nginx/templates/default.conf.template

# Copy built assets
COPY --from=build /build/dist/eventos-vivos-web/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
