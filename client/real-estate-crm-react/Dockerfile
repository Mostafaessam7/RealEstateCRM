# Build context: client/real-estate-crm-react (docker build -f Dockerfile .)

FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

# Baked into the static build at build time — Vite env vars are compile-time, not runtime.
ARG VITE_API_BASE_URL=http://localhost:5063/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
