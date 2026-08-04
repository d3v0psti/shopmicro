# Stage 02 — execução local

Usa PostgreSQL 18 e uploads em volumes Docker. O S3 será usado somente na AWS.

## 1. Iniciar

Antes de iniciar, encerre qualquer outro stage que esteja usando as portas 80,
81, 5432 e 8080. Use `docker compose down` no diretório desse stage.

```bash
cd infra/stages/02-ec2-postgresql-container-s3/local
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
