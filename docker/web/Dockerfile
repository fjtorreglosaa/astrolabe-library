# syntax=docker/dockerfile:1

# ---------- build ----------
FROM node:24-alpine AS build
WORKDIR /app

# Lockfile first so dependency installation caches independently of source changes.
COPY src/frontend/astrolabe-web/package.json src/frontend/astrolabe-web/package-lock.json ./
RUN npm ci

COPY src/frontend/astrolabe-web/ ./

# The API base URL is baked in at build time because Vite inlines env vars.
ARG VITE_API_BASE_URL=http://localhost:5080
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

# ---------- runtime ----------
FROM nginx:1.29-alpine AS runtime

COPY docker/web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
    CMD wget --quiet --tries=1 --spider http://127.0.0.1/ || exit 1
