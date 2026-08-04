# Stage 01 — execução local

Esta execução valida o mesmo conjunto de containers antes de criar recursos AWS.
Não requer AWS CLI nem credenciais AWS.

```bash
cd infra/aws/stages/01-ec2-docker-compose/local
docker compose up --build -d
docker compose ps
```

Acesse:

- Marketplace: http://localhost
- Admin: http://localhost:81
- Swagger: http://localhost:8080/swagger

Execute a seção `Local` de [VALIDACOES.md](../validacoes/VALIDACOES.md). Para
parar preservando os dados:

```bash
docker compose down
```

Use `docker compose down -v` somente quando quiser apagar banco e imagens desse
stage. Se outro ambiente local estiver usando as portas 80, 81, 5432 ou 8080,
pare-o antes de iniciar este Compose.
