# Stage 03 — AWS pelo Terraform

Execute depois de validar o modo local e o Console.

## 1. Configurar

```bash
cd infra/stages/03-ec2-rds-postgresql-s3/terraform
cp terraform.tfvars.example terraform.tfvars
```

Abra https://checkip.amazonaws.com/ e substitua `seu_ip/32` no
`terraform.tfvars`, mantendo `/32`.

## 2. Criar

```bash
terraform init
terraform fmt -check
terraform validate
terraform plan -out=stage-03.tfplan
terraform apply stage-03.tfplan
```

A criação do RDS costuma levar mais tempo que a EC2.

## 3. Validar

Use `terraform output` e execute `AWS — Console ou Terraform` em
[VALIDACOES.md](../validacoes/VALIDACOES.md).

O arquivo `terraform.tfstate` contém valores sensíveis gerados pelo Terraform.
Ele não deve ser enviado ao Git. Em uma evolução futura, o state será movido
para um backend remoto criptografado e protegido.

## 4. Remover

```bash
terraform destroy
```

Confirme no Console que nenhum recurso, snapshot ou backup do stage permaneceu.
