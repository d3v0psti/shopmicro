# Stage 01 — AWS pelo Terraform

Execute depois de validar o modo local e o Console.

## 1. Configurar

```bash
cd infra/stages/01-ec2-docker-compose/terraform
cp terraform.tfvars.example terraform.tfvars
```

Abra https://checkip.amazonaws.com/ e substitua `seu_ip/32` no
`terraform.tfvars`, mantendo `/32`.

## 2. Criar

```bash
terraform init
terraform fmt -check
terraform validate
terraform plan -out=stage-01.tfplan
terraform apply stage-01.tfplan
```

## 3. Validar

Use `terraform output` para localizar a EC2 e execute `AWS — Console ou
Terraform` em [VALIDACOES.md](../validacoes/VALIDACOES.md).

## 4. Remover

```bash
terraform destroy
```

Confirme no Console que nenhum recurso do stage permaneceu.
