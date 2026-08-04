# Stage 02 — validações

Use o nome `VALIDACAO-STAGE-02-AAAA-MM-DD` ao cadastrar o produto. Selecione uma
imagem pequena e anote o nome usado.

## Local

Execute dentro de `infra/stages/02-ec2-postgresql-container-s3/local`.

```bash
docker compose ps
curl --fail http://localhost:8080/health/live
curl --fail http://localhost:8080/health/ready
```

Cadastre o produto pelo painel em http://localhost:81 e valide o banco:

```bash
docker compose exec -T postgres psql -U postgres -d shopdb \
  < ../validacoes/consultar-produtos.sql
docker compose exec backend find /app/uploads -maxdepth 1 -type f -printf '%f\n'
```

Localmente, `ImageUrl` deve começar por `/uploads/` e o arquivo deve estar no
volume `backend_uploads`. Isso comprova que o suporte ao S3 não tornou o ambiente
local dependente da AWS.

```bash
docker compose restart
```

Repita SQL e abertura da imagem para confirmar a persistência local.

## AWS — Console ou Terraform

Conecte à EC2 pelo Session Manager:

```bash
cd /opt/shopmicro/infra
sudo docker compose --env-file .env.aws-stage-02 \
  -f compose.aws-stage-02.yaml ps
curl --fail http://127.0.0.1/health/live
curl --fail http://127.0.0.1/health/ready
```

Cadastre `VALIDACAO-STAGE-02-AAAA-MM-DD` pelo painel e consulte o PostgreSQL:

```bash
sudo docker compose --env-file .env.aws-stage-02 \
  -f compose.aws-stage-02.yaml exec -T postgres \
  psql -U postgres -d shopdb \
  < stages/02-ec2-postgresql-container-s3/validacoes/consultar-produtos.sql
```

Copie apenas o valor de `ImageUrl`, por exemplo `/uploads/abc.jpg`. A chave
esperada no bucket será `uploads/abc.jpg`.

### Confirmar no Console S3

1. Abra **S3 → bucket do Stage 02 → uploads/**.
2. Localize a chave correspondente à `ImageUrl`.
3. Confira tamanho, horário e criptografia do objeto.
4. Confirme que **Block Public Access** continua habilitado.
5. Abra a imagem pelo marketplace; não torne o objeto público.

Reinicie o backend e depois a EC2:

```bash
sudo docker compose --env-file .env.aws-stage-02 \
  -f compose.aws-stage-02.yaml restart backend
```

Repita a consulta SQL, a conferência no S3 e a abertura da imagem.

## Evidências

| Verificação | Local | Console | Terraform |
|---|---|---|---|
| Quatro containers ativos | [x] | [x] | [x] |
| Health checks aprovados | [x] | [x] | [x] |
| Produto no PostgreSQL | [x] | [x] | [x] |
| Imagem no volume local | [x] | N/A | N/A |
| Objeto privado no S3 | N/A | [x] | [x] |
| Banco e imagem persistem | [x] | [x] | [x] |
| Nenhum segredo nos logs | [x] | [x] | [x] |
| Recursos AWS removidos | N/A | [x] | [x] |

Registre nome do produto, ID, `ImageUrl`, chave S3 e horários. Não registre
senhas, JWT, credenciais ou o conteúdo dos arquivos `.env`.
