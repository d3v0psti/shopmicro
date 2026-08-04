# Stage 02 — AWS pelo Terraform

Execute somente depois de validar o modo local e a criação pelo Console.

```bash
cd infra/aws/stages/02-ec2-postgresql-container-s3/terraform
cp terraform.tfvars.example terraform.tfvars
```

No `terraform.tfvars`, substitua `seu_ip/32` pelo IPv4 obtido em
https://checkip.amazonaws.com/, mantendo o `/32`.

```bash
terraform init
terraform fmt -check
terraform validate
terraform plan -out=stage-02.tfplan
terraform apply stage-02.tfplan
```

Use os outputs para localizar EC2, URLs e bucket. Execute a seção
`AWS — Console ou Terraform` de [VALIDACOES.md](../validacoes/VALIDACOES.md).

O exemplo usa `force_destroy_bucket = true` para permitir a remoção dos objetos
durante este treino. Não replique essa escolha automaticamente em produção.

Ao terminar:

```bash
terraform destroy
```

Confirme no Console que EC2, EBS, bucket, VPC Endpoint, Security Group, instance
profile e IAM Role foram removidos.
