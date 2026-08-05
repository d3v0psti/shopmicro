#!/usr/bin/env bash
set -e

exec > >(tee -a /var/log/shopmicro-aws-stage-05.log | logger -t shopmicro-aws-stage-05 -s 2>/dev/console) 2>&1

# Preencha antes de usar no Launch Template.
S3_BUCKET_NAME='NOME_UNICO_DO_BUCKET'
RDS_HOST='ENDPOINT_PRIVADO_DO_RDS'
RDS_PASSWORD_PARAMETER='/shopmicro/stage-05/rds/password'
JWT_SECRET_PARAMETER='/shopmicro/stage-05/jwt/secret'
AWS_REGION='us-east-1'

if [ "$S3_BUCKET_NAME" = 'NOME_UNICO_DO_BUCKET' ] || \
   [ "$RDS_HOST" = 'ENDPOINT_PRIVADO_DO_RDS' ]; then
  echo 'ERRO: preencha S3_BUCKET_NAME e RDS_HOST no user-data.'
  exit 1
fi

dnf install -y docker
systemctl enable --now docker
systemctl enable --now amazon-ssm-agent

DOCKER_COMPOSE_VERSION='v2.32.4'
install -m 0755 -d /usr/local/lib/docker/cli-plugins
curl -fsSL \
  "https://github.com/docker/compose/releases/download/${DOCKER_COMPOSE_VERSION}/docker-compose-linux-x86_64" \
  -o /usr/local/lib/docker/cli-plugins/docker-compose
chmod 0755 /usr/local/lib/docker/cli-plugins/docker-compose
docker compose version
aws --version

AWS_ACCOUNT_ID="$(aws sts get-caller-identity \
  --region "$AWS_REGION" --query Account --output text)"
ECR_REGISTRY="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"
BACKEND_IMAGE="${ECR_REGISTRY}/shopmicro-backend:latest"
FRONTEND_IMAGE="${ECR_REGISTRY}/shopmicro-frontend:latest"
FRONTEND_ADMIN_IMAGE="${ECR_REGISTRY}/shopmicro-frontend-admin:latest"

install -m 0755 -d /opt/shopmicro/infra
install -m 0644 /dev/stdin /opt/shopmicro/infra/compose.aws-stage-05.yaml <<'COMPOSE'
name: shopmicro-aws-stage-05

services:
  backend:
    image: "${BACKEND_IMAGE:?BACKEND_IMAGE não configurado}"
    pull_policy: always
    restart: unless-stopped
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      DB_CONNECTION_STRING: "Host=${RDS_HOST:?RDS_HOST não configurado};Port=5432;Database=shopdb;Username=shopadmin;Password=${RDS_PASSWORD:?RDS_PASSWORD não configurado};SSL Mode=Require;Trust Server Certificate=true"
      JWT_SECRET: "${JWT_SECRET:?JWT_SECRET não configurado}"
      CORS_ALLOWED_ORIGINS: "*"
      ENABLE_SWAGGER: "true"
      STORAGE_PROVIDER: "S3"
      S3_BUCKET_NAME: "${S3_BUCKET_NAME:?S3_BUCKET_NAME não configurado}"
      AWS_REGION: "${AWS_REGION:-us-east-1}"
    ports:
      - "127.0.0.1:8080:8080"

  frontend:
    image: "${FRONTEND_IMAGE:?FRONTEND_IMAGE não configurado}"
    pull_policy: always
    restart: unless-stopped
    environment:
      BACKEND_UPSTREAM: "http://backend:8080"
    ports:
      - "80:80"
    depends_on:
      backend:
        condition: service_started

  frontend-admin:
    image: "${FRONTEND_ADMIN_IMAGE:?FRONTEND_ADMIN_IMAGE não configurado}"
    pull_policy: always
    restart: unless-stopped
    environment:
      BACKEND_UPSTREAM: "http://backend:8080"
    ports:
      - "81:80"
    depends_on:
      backend:
        condition: service_started
COMPOSE

RDS_PASSWORD="$(aws ssm get-parameter \
  --region "$AWS_REGION" --name "$RDS_PASSWORD_PARAMETER" \
  --with-decryption --query 'Parameter.Value' --output text)"
JWT_SECRET="$(aws ssm get-parameter \
  --region "$AWS_REGION" --name "$JWT_SECRET_PARAMETER" \
  --with-decryption --query 'Parameter.Value' --output text)"

aws ecr get-login-password --region "$AWS_REGION" | \
  docker login --username AWS --password-stdin "$ECR_REGISTRY"

umask 077
printf 'JWT_SECRET=%s\nRDS_HOST=%s\nRDS_PASSWORD=%s\nS3_BUCKET_NAME=%s\nAWS_REGION=%s\nECR_REGISTRY=%s\nBACKEND_IMAGE=%s\nFRONTEND_IMAGE=%s\nFRONTEND_ADMIN_IMAGE=%s\n' \
  "$JWT_SECRET" "$RDS_HOST" "$RDS_PASSWORD" "$S3_BUCKET_NAME" "$AWS_REGION" \
  "$ECR_REGISTRY" "$BACKEND_IMAGE" "$FRONTEND_IMAGE" "$FRONTEND_ADMIN_IMAGE" \
  > /opt/shopmicro/infra/.env.aws-stage-05
unset RDS_PASSWORD JWT_SECRET

cd /opt/shopmicro/infra
docker compose --env-file .env.aws-stage-05 -f compose.aws-stage-05.yaml pull
docker compose --env-file .env.aws-stage-05 -f compose.aws-stage-05.yaml up --no-build -d

for attempt in $(seq 1 90); do
  if curl -fsS http://127.0.0.1/health/live >/dev/null; then
    echo 'ShopMicro AWS Stage 05 iniciado com imagens do ECR.'
    docker compose --env-file .env.aws-stage-05 -f compose.aws-stage-05.yaml ps
    exit 0
  fi
  sleep 5
done

echo 'ERRO: aplicação não respondeu no prazo.'
docker compose --env-file .env.aws-stage-05 -f compose.aws-stage-05.yaml ps
docker compose --env-file .env.aws-stage-05 -f compose.aws-stage-05.yaml logs --tail 200
exit 1
