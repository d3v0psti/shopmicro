# Stage 01 — execução local

Usa PostgreSQL 18 e uploads em volumes Docker. Não requer AWS.

## 1. Iniciar

Pare antes qualquer outro stage que use as portas 80, 81, 5432 ou 8080.

```bash
cd infra/stages/01-ec2-docker-compose/local
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

O comando preserva os dados. Use `docker compose down -v` somente para apagar
banco e imagens deste stage.
