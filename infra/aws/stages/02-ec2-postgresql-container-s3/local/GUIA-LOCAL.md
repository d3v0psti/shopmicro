# Stage 02 — execução local

O código com suporte ao S3 deve continuar funcionando localmente sem AWS. Por
isso, esta execução usa `STORAGE_PROVIDER=Local` e volumes Docker.

```bash
cd infra/aws/stages/02-ec2-postgresql-container-s3/local
docker compose up --build -d
docker compose ps
```

Acesse:

- Marketplace: http://localhost
- Admin: http://localhost:81
- Swagger: http://localhost:8080/swagger

Execute a seção `Local` de [VALIDACOES.md](../validacoes/VALIDACOES.md). O S3
será validado somente no Console e no Terraform, sem MinIO ou credenciais AWS no
computador.

```bash
docker compose down
```

Use `docker compose down -v` somente para apagar os dados locais deste stage.
