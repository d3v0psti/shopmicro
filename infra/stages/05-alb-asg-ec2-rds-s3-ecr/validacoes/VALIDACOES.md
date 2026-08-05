# Stage 05 — validações

Somente o responsável pelo projeto confirma estas evidências. A tag só pode ser
criada após validar Local, Console e Terraform.

## Local

Dentro de `infra/stages/05-alb-asg-ec2-rds-s3-ecr/local`:

```bash
docker compose up --build -d
docker compose ps
curl --fail http://localhost:8080/health/live
curl --fail http://localhost:8080/health/ready
docker compose exec -T postgres psql -U postgres -d shopdb \
  < ../validacoes/consultar-produtos.sql
docker compose exec backend find /app/uploads -maxdepth 1 -type f -printf '%f\n'
docker compose restart
```

Confirme marketplace, admin, produto, imagem e persistência após o reinício.

## AWS — Console ou Terraform

### 1. ECR

No Console, confirme nos três repositórios:

- tag `latest` disponível;
- digest registrado;
- repositórios privados e tags mutáveis;
- resultado do scan sem vulnerabilidade crítica aceita sem análise.

### 2. EC2, ALB e imagens em execução

Confirme duas EC2 em zonas diferentes e dois targets saudáveis em cada target
group. Acesse marketplace na porta 80 e admin na porta 81 pelo DNS do ALB.

Em uma EC2 via Session Manager:

```bash
cd /opt/shopmicro/infra
sudo docker compose --env-file .env.aws-stage-05 \
  -f compose.aws-stage-05.yaml ps
sudo docker compose --env-file .env.aws-stage-05 \
  -f compose.aws-stage-05.yaml images
sudo docker image inspect "$(sudo docker compose --env-file .env.aws-stage-05 \
  -f compose.aws-stage-05.yaml images -q backend)" \
  --format '{{index .RepoDigests 0}}'
curl --fail http://127.0.0.1/health/live
curl --fail http://127.0.0.1/health/ready
```

Confirme no log que não existem `git clone` nem `docker build`:

```bash
sudo grep -E 'git clone|docker build' /var/log/shopmicro-aws-stage-05.log
```

O resultado esperado é vazio.

### 3. Persistência

Cadastre `VALIDACAO-STAGE-05-AAAA-MM-DD` com uma imagem. Consulte o RDS sem
imprimir a senha:

```bash
RDS_PASSWORD="$(aws ssm get-parameter \
  --name /shopmicro/stage-05/rds/password \
  --with-decryption --query 'Parameter.Value' --output text)"

sudo docker run --rm --network host \
  -e PGPASSWORD="$RDS_PASSWORD" postgres:18-alpine \
  psql -h "$(sudo grep '^RDS_HOST=' .env.aws-stage-05 | cut -d= -f2-)" \
  -U shopadmin -d shopdb -c \
  'SELECT "Id", "Name", "ImageUrl" FROM products ORDER BY "Id" DESC LIMIT 10;'

unset RDS_PASSWORD
```

Confirme `uploads/ARQUIVO` no S3 privado.

### 4. Atualização e ASG

1. Publique novamente somente `frontend:latest`.
2. Anote os digests atuais de backend e frontend.
3. Execute Instance refresh no ASG.
4. Aguarde duas EC2 e ambos os target groups saudáveis.
5. Confirme o novo digest do frontend e o mesmo digest do backend.
6. Confirme produto e imagem ainda disponíveis.

## Evidências

| Verificação | Local | Console | Terraform |
|---|---|---|---|
| Aplicação funcional | [x] | [x] | [x] |
| PostgreSQL 18 e uploads persistentes | [x] | [x] | [x] |
| Três repositórios ECR com `latest` | N/A | [x] | [x] |
| EC2 realiza pull sem clone ou build | N/A | [x] | [x] |
| IAM das EC2 permite pull, mas não push | N/A | [x] | [x] |
| Duas EC2 e targets saudáveis | N/A | [x] | [x] |
| Atualização independente do frontend | N/A | [x] | [x] |
| ASG substitui as instâncias gradualmente | N/A | [x] | [x] |
| RDS privado e objeto S3 privado | N/A | [x] | [x] |
| Nenhum segredo em código ou logs | [x] | [x] | [x] |
| Recursos removidos e ECR preservado | N/A | [x] | [x] |
