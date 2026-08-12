# Execução local

O ambiente local reproduz a carga de trabalho sem exigir serviços ou
credenciais AWS.

## Requisitos

- Docker Engine ou Docker Desktop.
- Docker Compose v2.

## Configurar

```bash
cd infra
cp .env.example .env
```

Gere valores aleatórios usando Docker:

```bash
# JWT_SECRET
docker run --rm alpine/openssl rand -base64 48

# POSTGRES_PASSWORD e ADMIN_BOOTSTRAP_PASSWORD
docker run --rm alpine/openssl rand -base64 24
```

Copie os resultados para os campos correspondentes do `infra/.env`. O arquivo
é ignorado pelo Git e centraliza toda a configuração local.

O bootstrap administrativo é usado somente quando a tabela
`administrative_accounts` está vazia. Alterar essas variáveis depois não muda
uma conta existente.

## Iniciar

```bash
docker compose up --build
```

Em segundo plano:

```bash
docker compose up -d --build
docker compose ps
```

## Endereços padrão

| Serviço | Endereço |
|---|---|
| Marketplace | http://localhost |
| Painel administrativo | http://localhost:81 |
| Swagger | http://localhost:8080/swagger |
| PostgreSQL | localhost:5432 |

As portas podem ser alteradas no `infra/.env`.

## Diagnóstico

```bash
docker compose ps
docker compose logs --tail=100 shopmicro-backend
docker compose exec postgres sh -c 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
```

## Encerrar

```bash
# Preserva banco e uploads
docker compose down

# Apaga também os volumes locais
docker compose down -v
```

`docker compose down -v` é destrutivo: remove o banco e os uploads armazenados
nos volumes Docker.
