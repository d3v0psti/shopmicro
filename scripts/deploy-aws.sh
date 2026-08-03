#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TF_DIR="${PROJECT_ROOT}/infra/aws"
AWS_REGION="${AWS_REGION:-$(terraform -chdir="${TF_DIR}" output -raw aws_region 2>/dev/null || true)}"
AWS_REGION="${AWS_REGION:-us-east-1}"
IMAGE_TAG="${IMAGE_TAG:-latest}"

aws sts get-caller-identity >/dev/null
ACCOUNT_ID="$(aws sts get-caller-identity --query Account --output text)"
REGISTRY="${ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"

aws ecr get-login-password --region "${AWS_REGION}" \
  | docker login --username AWS --password-stdin "${REGISTRY}"

for service in backend frontend frontend-admin; do
  repository="$(terraform -chdir="${TF_DIR}" output -json ecr_repositories | jq -r --arg service "${service}" '.[$service]')"
  docker build --platform linux/amd64 -t "${repository}:${IMAGE_TAG}" "${PROJECT_ROOT}/${service}"
  docker push "${repository}:${IMAGE_TAG}"
done

terraform -chdir="${TF_DIR}" apply \
  -var="image_tag=${IMAGE_TAG}" \
  -var="service_desired_count=1"

cluster="$(terraform -chdir="${TF_DIR}" output -raw ecs_cluster_name)"
for service in backend frontend frontend-admin; do
  aws ecs update-service \
    --region "${AWS_REGION}" \
    --cluster "${cluster}" \
    --service "${service}" \
    --force-new-deployment >/dev/null
done

echo "Deploy iniciado. Acompanhe com:"
echo "aws ecs wait services-stable --region ${AWS_REGION} --cluster ${cluster} --services backend frontend frontend-admin"
terraform -chdir="${TF_DIR}" output marketplace_url admin_url
