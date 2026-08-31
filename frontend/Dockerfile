# ---- Build stage ----
FROM node:22-alpine AS build
WORKDIR /app

# Copy only the manifest files first so `npm ci` is cached unless
# dependencies actually changed.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---- Runtime stage ----
FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
