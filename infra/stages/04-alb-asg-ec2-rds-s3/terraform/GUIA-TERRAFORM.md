# Stage 04 — AWS pelo Terraform

Cria ALB, Auto Scaling Group com duas EC2 em zonas diferentes, RDS PostgreSQL
18 privado, bucket S3 privado e os controles de acesso necessários.

Execute somente depois de validar os modos local e Console.

## 1. Configurar

```bash
cd infra/stages/04-alb-asg-ec2-rds-s3/terraform
cp terraform.tfvars.example terraform.tfvars
```

Abra https://checkip.amazonaws.com/ e substitua `seu_ip/32` no
`terraform.tfvars`, mantendo `/32`. Revise também repositório e branch.

## 2. Criar

```bash
terraform init
terraform fmt -check
terraform validate
terraform plan -out=stage-04.tfplan
terraform apply stage-04.tfplan
```

O RDS e a construção das imagens nas duas EC2 podem levar vários minutos. O
Terraform aguarda as duas instâncias ficarem saudáveis no ALB.

## 3. Validar

```bash
terraform output
```

Acesse as URLs exibidas e execute `AWS — Console ou Terraform` em
[VALIDACOES.md](../validacoes/VALIDACOES.md).

O `terraform.tfstate` contém valores e metadados sensíveis. Ele é ignorado pelo
Git e não deve ser compartilhado. Uma evolução futura moverá o state para um
backend remoto criptografado e protegido.

## 4. Remover

```bash
terraform destroy
```

Confirme no Console que ASG, EC2, ALB, target groups, RDS, bucket e demais
recursos foram removidos, sem snapshots ou backups cobrados.
