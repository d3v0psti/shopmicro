# Stage 01 — AWS pelo Terraform

Execute somente depois de validar o modo local e a criação pelo Console.

```bash
cd infra/aws/stages/01-ec2-docker-compose/terraform
cp terraform.tfvars.example terraform.tfvars
```

No `terraform.tfvars`, substitua `seu_ip/32` pelo IPv4 obtido em
https://checkip.amazonaws.com/, mantendo o `/32`.

```bash
terraform init
terraform fmt -check
terraform validate
terraform plan -out=stage-01.tfplan
terraform apply stage-01.tfplan
```

Use os outputs para abrir a aplicação e localizar a EC2. Depois execute a seção
`AWS — Console ou Terraform` de [VALIDACOES.md](../validacoes/VALIDACOES.md).

Ao terminar:

```bash
terraform destroy
```

Confirme no Console que EC2, EBS, Security Group, instance profile e IAM Role
foram removidos.
