# Stage 02 — AWS pelo Terraform

Execute depois de validar o modo local e o Console.

## 1. Configurar

```bash
cd infra/stages/02-ec2-postgresql-container-s3/terraform
cp terraform.tfvars.example terraform.tfvars
```

Abra https://checkip.amazonaws.com/ e substitua `seu_ip/32` no
`terraform.tfvars`, mantendo `/32`.

## 2. Criar

```bash
terraform init
terraform fmt -check
terraform validate
terraform plan -out=stage-02.tfplan
terraform apply stage-02.tfplan
```

## 3. Validar

Use `terraform output` e execute `AWS — Console ou Terraform` em
[VALIDACOES.md](../validacoes/VALIDACOES.md).

O exemplo usa `force_destroy_bucket = true` para permitir a remoção dos objetos
durante este treino. Não replique essa escolha automaticamente em produção.

## 4. Remover

```bash
terraform destroy
```

Confirme no Console que nenhum recurso do stage permaneceu.
