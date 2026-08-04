# Stage 03 — execução local

Usa PostgreSQL 18 e uploads em volumes Docker. RDS e S3 serão usados somente na
AWS.

## 1. Iniciar

Antes de iniciar, encerre qualquer outro stage que esteja usando as portas 80,
81, 5432 e 8080 com `docker compose down`.

```bash
cd infra/stages/03-ec2-rds-postgresql-s3/local
docker compose up --build -d
docker compose ps
```

## 2. Acessar

- Marketplace: http://localhost
- Admin: http://localhost:81
- Swagger: http://localhost:8080/swagger

## 3. Validar

Execute a seção `Local` de [VALIDACOES.md](../validacoes/VALIDACOES.md).

## 4. Parar

```bash
docker compose down
```

O comando preserva os dados. Use `docker compose down -v` somente para apagá-los.
