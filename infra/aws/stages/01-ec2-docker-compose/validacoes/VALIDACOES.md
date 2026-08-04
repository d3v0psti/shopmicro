# Stage 01 — validações

Use o nome `VALIDACAO-STAGE-01-AAAA-MM-DD` ao cadastrar o produto. Selecione uma
imagem pequena do computador e anote o nome usado.

## Local

Execute dentro de `infra/aws/stages/01-ec2-docker-compose/local`.

### 1. Serviços e saúde

```bash
docker compose ps
curl --fail http://localhost:8080/health/live
curl --fail http://localhost:8080/health/ready
```

Os quatro serviços devem estar ativos e os dois health checks devem responder
com sucesso.

### 2. Cadastro e PostgreSQL

Cadastre o produto pelo painel em http://localhost:81 e consulte o banco:

```bash
docker compose exec -T postgres psql -U postgres -d shopdb \
  < ../validacoes/consultar-produtos.sql
```

O produto deve aparecer com `ImageUrl` iniciando por `/uploads/`.

### 3. Upload local

```bash
docker compose exec backend find /app/uploads -maxdepth 1 -type f -printf '%f\n'
docker compose config --volumes
```

O arquivo correspondente à `ImageUrl` deve existir e o Compose deve listar
`backend_uploads` e `postgres_data`.

### 4. Persistência

```bash
docker compose restart
```

Repita a consulta SQL e abra o marketplace. O produto e a imagem devem continuar
disponíveis. Não use `down -v` antes desta verificação.

## AWS — Console ou Terraform

Conecte à EC2 pelo Session Manager e execute:

```bash
cd /opt/shopmicro/infra
sudo docker compose --env-file .env.aws-stage-01 \
  -f compose.aws-stage-01.yaml ps
curl --fail http://127.0.0.1/health/live
curl --fail http://127.0.0.1/health/ready
```

Cadastre `VALIDACAO-STAGE-01-AAAA-MM-DD` pelo painel e valide o PostgreSQL:

```bash
sudo docker compose --env-file .env.aws-stage-01 \
  -f compose.aws-stage-01.yaml exec -T postgres \
  psql -U postgres -d shopdb \
  < aws/stages/01-ec2-docker-compose/validacoes/consultar-produtos.sql
```

Valide o arquivo:

```bash
sudo docker compose --env-file .env.aws-stage-01 \
  -f compose.aws-stage-01.yaml exec backend \
  find /app/uploads -maxdepth 1 -type f -printf '%f\n'
```

Reinicie primeiro os containers e depois a EC2. Repita SQL, listagem do arquivo
e abertura da imagem no marketplace.

## Evidências

| Verificação | Local | Console | Terraform |
|---|---|---|---|
| Quatro containers ativos | [ ] | [ ] | [ ] |
| Health checks aprovados | [ ] | [ ] | [ ] |
| Produto no PostgreSQL | [ ] | [ ] | [ ] |
| Imagem no volume local | [ ] | [ ] | [ ] |
| Persistência após reinício | [ ] | [ ] | [ ] |
| Nenhum segredo nos logs | [ ] | [ ] | [ ] |
| Recursos AWS removidos | N/A | [ ] | [ ] |

Registre IDs, nomes e horários; não copie senha PostgreSQL, JWT ou conteúdo de
arquivos `.env` para as evidências.
