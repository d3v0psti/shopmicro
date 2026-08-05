#!/usr/bin/env bash
set -euo pipefail

SERVICE="${1:-all}"
AWS_REGION="${AWS_REGION:-us-east-1}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIR/../../../.." && pwd)"
AWS_ACCOUNT_ID="$(aws sts get-caller-identity \
  --region "$AWS_REGION" --query Account --output text)"
ECR_REGISTRY="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"

publish_image() {
  local service_name="$1"
  local context_path="$2"
  local image_uri="${ECR_REGISTRY}/shopmicro-${service_name}:latest"

  docker build --platform linux/amd64 -t "$image_uri" "$context_path"
  docker push "$image_uri"
  echo "Publicada: $image_uri"
}

case "$SERVICE" in
  backend|frontend|frontend-admin|all) ;;
  *)
    echo 'Uso: ./publicar-imagens.sh [backend|frontend|frontend-admin|all]'
    exit 1
    ;;
esac

aws ecr get-login-password --region "$AWS_REGION" | \
  docker login --username AWS --password-stdin "$ECR_REGISTRY"

if [ "$SERVICE" = 'backend' ] || [ "$SERVICE" = 'all' ]; then
  publish_image 'backend' "$REPOSITORY_ROOT/backend"
fi
if [ "$SERVICE" = 'frontend' ] || [ "$SERVICE" = 'all' ]; then
  publish_image 'frontend' "$REPOSITORY_ROOT/frontend"
fi
if [ "$SERVICE" = 'frontend-admin' ] || [ "$SERVICE" = 'all' ]; then
  publish_image 'frontend-admin' "$REPOSITORY_ROOT/frontend-admin"
fi
