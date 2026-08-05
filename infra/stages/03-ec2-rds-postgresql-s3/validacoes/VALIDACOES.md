# Stage 03 — validações

Cadastre um produto chamado `VALIDACAO-STAGE-03-AAAA-MM-DD` com uma imagem
pequena e registre apenas IDs, nomes e horários, nunca segredos.

## Local

Execute dentro de `infra/stages/03-ec2-rds-postgresql-s3/local`:

```bash
docker compose ps
curl --fail http://localhost:8080/health/live
curl --fail http://localhost:8080/health/ready
docker compose exec -T postgres psql -U postgres -d shopdb \
  < ../validacoes/consultar-produtos.sql
docker compose exec backend find /app/uploads -maxdepth 1 -type f -printf '%f\n'
```

Reinicie os containers e confirme que banco e imagem continuam disponíveis:

```bash
docker compose restart
```

## AWS — Console

Conecte à EC2 pelo Session Manager:

```bash
cd /opt/shopmicro/infra
sudo docker compose --env-file .env.aws-stage-03 \
  -f compose.aws-stage-03.yaml ps
curl --fail http://127.0.0.1/health/live
curl --fail http://127.0.0.1/health/ready
```

Após cadastrar o produto, consulte o RDS a partir do container do backend. A
senha é lida do Parameter Store sem ser impressa:

```bash
RDS_PASSWORD="$(aws ssm get-parameter \
  --name /shopmicro/stage-03/rds/password \
  --with-decryption --query 'Parameter.Value' --output text)"

sudo docker run --rm --network host \
  -e PGPASSWORD="$RDS_PASSWORD" postgres:18-alpine \
  psql -h "$(sudo grep '^RDS_HOST=' .env.aws-stage-03 | cut -d= -f2-)" \
  -U shopadmin -d shopdb -c \
  'SELECT "Id", "Name", "ImageUrl" FROM products ORDER BY "Id" DESC LIMIT 10;'

unset RDS_PASSWORD
```

No Console S3, confirme que existe `uploads/ARQUIVO` e que o objeto não é
público. Reinicie os containers e a EC2; repita a consulta, abra a imagem e
confirme que os dados persistem no RDS e no S3.

No Console RDS, confirme:

- `Publicly accessible: No`;
- PostgreSQL 18;
- conexão na porta 5432 permitida apenas pelo Security Group da EC2;
- criptografia habilitada.

## Evidências

| Verificação | Local | Console |
|---|---|---|
| Quatro containers locais / três na AWS | [x] | [x] |
| Health checks aprovados | [x] | [x] |
| Produto persistido no PostgreSQL | [x] | [x] |
| Imagem local no volume | [x] | N/A |
| Objeto privado no S3 | N/A | [x] |
| RDS privado e criptografado | N/A | [x] |
| Persistência após reinício da EC2 | [x] | [x] |
| Nenhum segredo nos logs | [x] | [x] |
| Recursos AWS removidos | N/A | [x] |
