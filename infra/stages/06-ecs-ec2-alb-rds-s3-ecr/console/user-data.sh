#!/usr/bin/env bash
set -euo pipefail

# Substitua pelo nome exato do cluster criado no Console.
ECS_CLUSTER_NAME='shopmicro-stage-06'

printf 'ECS_CLUSTER=%s\n' "$ECS_CLUSTER_NAME" >> /etc/ecs/ecs.config
printf 'ECS_ENABLE_AWSLOGS_EXECUTIONROLE_OVERRIDE=true\n' >> /etc/ecs/ecs.config

systemctl enable --now amazon-ssm-agent
systemctl enable --now ecs
