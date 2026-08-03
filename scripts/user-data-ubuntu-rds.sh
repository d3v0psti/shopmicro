#!/usr/bin/env bash
set -e

# Preencha antes de colar este script no campo User data da EC2.
DB_CONNECTION_STRING='Host=ENDPOINT_DO_RDS;Port=5432;Database=shopdb;Username=postgres;Password=SENHA_DO_RDS;SSL Mode=Require;Trust Server Certificate=true'
S3_BUCKET_NAME='NOME_DO_BUCKET_S3'
AWS_REGION='us-east-1'

apt-get update
apt-get upgrade -y
apt-get install -y ca-certificates curl git openssl

# Gera automaticamente um segredo JWT aleatório de 256 bits para esta EC2.
JWT_SECRET="$(openssl rand -hex 32)"

install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
  -o /etc/apt/keyrings/docker.asc
chmod a+r /etc/apt/keyrings/docker.asc

. /etc/os-release
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu ${UBUNTU_CODENAME:-$VERSION_CODENAME} stable" \
  > /etc/apt/sources.list.d/docker.list

apt-get update
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl enable --now docker

git clone --branch develop --single-branch \
  https://github.com/d3v0psti/shopmicro.git \
  /opt/shopmicro

cd /opt/shopmicro/infra

export DB_CONNECTION_STRING
export JWT_SECRET
export S3_BUCKET_NAME
export AWS_REGION

docker compose -f compose.aws.yaml up --build -d
