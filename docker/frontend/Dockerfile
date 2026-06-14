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

# Copy custom nginx config
COPY docker/frontend/nginx.conf /etc/nginx/conf.d/default.conf

# Copy built assets
COPY --from=build /build/dist/eventos-vivos-web/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
