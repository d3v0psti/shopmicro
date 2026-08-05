# Stage 06 — AWS pelo Terraform

O Terraform cria ECS, Capacity Provider, ASG, duas EC2 `t3.small`, ALB, RDS e
S3. Também habilita `awsvpcTrunking` na conta para comportar as seis tasks. Os
três repositórios ECR continuam manuais e são apenas consultados.

## 1. Pré-requisitos

Confirme na mesma conta e região:

- `shopmicro-backend:latest`;
- `shopmicro-frontend:latest`;
- `shopmicro-frontend-admin:latest`.

## 2. Configurar

```bash
cd infra/stages/06-ecs-ec2-alb-rds-s3-ecr/terraform
cp terraform.tfvars.example terraform.tfvars
```

Consulte https://checkip.amazonaws.com/ e configure `admin_cidr` como
`SEU_IP/32`. O marketplace permanece público.

## 3. Criar

```bash
terraform init
terraform fmt -check
terraform validate
terraform plan -out=stage-06.tfplan
terraform apply stage-06.tfplan
terraform output
```

O primeiro start pode demorar enquanto o RDS é criado e as imagens são
baixadas. Acompanhe **ECS → Clusters → shopmicro-stage-06 → Services**.

## 4. Atualizar uma imagem `latest`

Depois de publicar uma nova imagem, force uma nova implantação do serviço:

```bash
aws ecs update-service \
  --cluster shopmicro-stage-06 \
  --service shopmicro-stage-06-frontend \
  --force-new-deployment
```

Troque o nome do serviço para atualizar backend ou frontend-admin. O próximo
stage automatizará esse processo com CI/CD e versionamento imutável.

## 5. Validar e remover

Execute [VALIDACOES.md](../validacoes/VALIDACOES.md) e, somente depois da
validação manual:

```bash
terraform destroy
```

Os repositórios ECR não são removidos. O state pode conter dados sensíveis e
nunca deve ser versionado.
