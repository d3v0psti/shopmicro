# Stage 05 — AWS pelo Terraform

O Terraform cria a arquitetura do Stage 05, mas não cria nem remove o ECR.
Ele consulta os três repositórios como fontes de dados e falha cedo se algum
nome estiver incorreto ou ausente.

## 1. Pré-requisitos

Antes de executar, confirme na mesma conta e em `us-east-1`:

- `shopmicro-backend:latest`;
- `shopmicro-frontend:latest`;
- `shopmicro-frontend-admin:latest`.

Crie e publique essas imagens seguindo o [guia do Console](../console/GUIA-CONSOLE.md).

## 2. Configurar

```bash
cd infra/stages/05-alb-asg-ec2-rds-s3-ecr/terraform
cp terraform.tfvars.example terraform.tfvars
```

Abra https://checkip.amazonaws.com/, substitua `seu_ip/32` e mantenha `/32`.
Os nomes dos repositórios no `tfvars` devem corresponder exatamente aos criados
manualmente.

## 3. Criar

```bash
terraform init
terraform fmt -check
terraform validate
terraform plan -out=stage-05.tfplan
terraform apply stage-05.tfplan
```

O user data autentica no ECR usando a IAM Role e apenas baixa as imagens. Não há
clone do Git ou build nas EC2. Use `terraform output` para acessar o ALB.

## 4. Atualizar `latest`

Um novo push não altera o Terraform nem reinicia as EC2. Após publicar, inicie
um **Instance refresh** no ASG pelo Console para que novas instâncias baixem os
digests atuais. O versionamento e rollout automatizado serão tratados em CI/CD.

## 5. Validar e remover

Execute [VALIDACOES.md](../validacoes/VALIDACOES.md) e depois:

```bash
terraform destroy
```

Confirme a remoção dos recursos do stage. Os três repositórios e suas imagens
devem continuar no ECR, pois não fazem parte do state do Terraform.

O `terraform.tfstate` contém dados sensíveis, é ignorado pelo Git e não deve ser
compartilhado.
