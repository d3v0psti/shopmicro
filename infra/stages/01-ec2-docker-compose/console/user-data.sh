#!/usr/bin/env bash
set -e

exec > >(tee -a /var/log/shopmicro-aws-stage-01.log | logger -t shopmicro-aws-stage-01 -s 2>/dev/console) 2>&1

dnf install -y docker git openssl
systemctl enable --now docker

DOCKER_COMPOSE_VERSION='v2.32.4'
install -m 0755 -d /usr/local/lib/docker/cli-plugins
curl -fsSL \
  "https://github.com/docker/compose/releases/download/${DOCKER_COMPOSE_VERSION}/docker-compose-linux-x86_64" \
  -o /usr/local/lib/docker/cli-plugins/docker-compose
chmod 0755 /usr/local/lib/docker/cli-plugins/docker-compose
docker compose version

systemctl enable --now amazon-ssm-agent

# Ajuda a t3.small durante o build das imagens sem aumentar a instância.
if ! swapon --show | grep -q /swapfile; then
  fallocate -l 2G /swapfile
  chmod 600 /swapfile
  mkswap /swapfile
  swapon /swapfile
  echo '/swapfile none swap sw 0 0' >> /etc/fstab
fi

git clone --depth 1 --branch develop --single-branch \
  https://github.com/d3v0psti/shopmicro.git \
  /opt/shopmicro

install -m 0644 /dev/stdin /opt/shopmicro/infra/compose.aws-stage-01.yaml <<'COMPOSE'
name: shopmicro-aws-stage-01

services:
  postgres:
    image: postgres:18-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: shopdb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD não configurado}"
    volumes:
      - postgres_data:/var/lib/postgresql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d shopdb"]
      interval: 5s
      timeout: 5s
      retries: 10
      start_period: 10s

  backend:
    build:
      context: /opt/shopmicro/backend
      dockerfile: Dockerfile
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      DB_CONNECTION_STRING: "Host=postgres;Port=5432;Database=shopdb;Username=postgres;Password=${POSTGRES_PASSWORD:?POSTGRES_PASSWORD não configurado}"
      JWT_SECRET: "${JWT_SECRET:?JWT_SECRET não configurado}"
      CORS_ALLOWED_ORIGINS: "*"
      ENABLE_SWAGGER: "true"
      STORAGE_PROVIDER: "Local"
    ports:
      - "127.0.0.1:8080:8080"
    depends_on:
      postgres:
        condition: service_healthy
    volumes:
      - backend_uploads:/app/uploads

  frontend:
    build:
      context: /opt/shopmicro/frontend
      dockerfile: Dockerfile
    restart: unless-stopped
    environment:
      BACKEND_UPSTREAM: "http://backend:8080"
    ports:
      - "80:80"
    depends_on:
      backend:
        condition: service_started

  frontend-admin:
    build:
      context: /opt/shopmicro/frontend-admin
      dockerfile: Dockerfile
    restart: unless-stopped
    environment:
      BACKEND_UPSTREAM: "http://backend:8080"
    ports:
      - "81:80"
    depends_on:
      backend:
        condition: service_started

volumes:
  postgres_data:
  backend_uploads:
COMPOSE

JWT_SECRET="$(openssl rand -hex 32)"
POSTGRES_PASSWORD="$(openssl rand -hex 24)"
umask 077
printf 'JWT_SECRET=%s\nPOSTGRES_PASSWORD=%s\n' \
  "$JWT_SECRET" "$POSTGRES_PASSWORD" \
  > /opt/shopmicro/infra/.env.aws-stage-01

cd /opt/shopmicro/infra
docker compose --env-file .env.aws-stage-01 -f compose.aws-stage-01.yaml up --build -d

for attempt in $(seq 1 60); do
  if curl -fsS http://127.0.0.1/health/live >/dev/null; then
    echo 'ShopMicro AWS Stage 01 iniciado com sucesso.'
    docker compose --env-file .env.aws-stage-01 -f compose.aws-stage-01.yaml ps
    exit 0
  fi
  sleep 5
done

echo 'ERRO: aplicação não respondeu no prazo.'
docker compose --env-file .env.aws-stage-01 -f compose.aws-stage-01.yaml ps
docker compose --env-file .env.aws-stage-01 -f compose.aws-stage-01.yaml logs --tail 200
exit 1
