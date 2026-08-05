# Stage 04 — validações

Cadastre um produto chamado `VALIDACAO-STAGE-04-AAAA-MM-DD` com uma imagem
pequena. Registre apenas IDs, nomes e horários; nunca registre segredos.

Somente o responsável pelo projeto confirma estas evidências. A tag do stage só
pode ser criada depois da validação manual de Local, Console e Terraform.

## Local

Execute dentro de `infra/stages/04-alb-asg-ec2-rds-s3/local`:

```bash
docker compose ps
curl --fail http://localhost:8080/health/live
curl --fail http://localhost:8080/health/ready
docker compose exec -T postgres psql -U postgres -d shopdb \
  < ../validacoes/consultar-produtos.sql
docker compose exec backend find /app/uploads -maxdepth 1 -type f -printf '%f\n'
docker compose restart
```

Após o reinício, confirme novamente o produto e a imagem.

## AWS — Console ou Terraform

### 1. ALB e duas instâncias

No Console, confirme:

- ASG com `min=2`, `desired=2` e `max=2`;
- duas EC2 em zonas de disponibilidade diferentes;
- listener `HTTP:80` encaminhando somente para o target group do frontend;
- listener `HTTP:81` encaminhando somente para o target group do frontend-admin;
- dois targets **Healthy** no target group da porta 80;
- dois targets **Healthy** no target group da porta 81;
- nenhuma regra de entrada direta da internet no Security Group das EC2.

Acesse somente pelo DNS do ALB:

```text
http://DNS_DO_ALB
http://DNS_DO_ALB:81
```

### 2. Containers e RDS

Conecte a uma das EC2 pelo Session Manager:

```bash
cd /opt/shopmicro/infra
sudo docker compose --env-file .env.aws-stage-04 \
  -f compose.aws-stage-04.yaml ps
curl --fail http://127.0.0.1/health/live
curl --fail http://127.0.0.1/health/ready
```

Depois de cadastrar o produto, consulte o RDS sem imprimir a senha:

```bash
RDS_PASSWORD="$(aws ssm get-parameter \
  --name /shopmicro/stage-04/rds/password \
  --with-decryption --query 'Parameter.Value' --output text)"

sudo docker run --rm --network host \
  -e PGPASSWORD="$RDS_PASSWORD" postgres:18-alpine \
  psql -h "$(sudo grep '^RDS_HOST=' .env.aws-stage-04 | cut -d= -f2-)" \
  -U shopadmin -d shopdb -c \
  'SELECT "Id", "Name", "ImageUrl" FROM products ORDER BY "Id" DESC LIMIT 10;'

unset RDS_PASSWORD
```

No RDS, confirme PostgreSQL 18, criptografia, `Publicly accessible: No` e porta
5432 permitida somente pelo Security Group das EC2.

### 3. S3 e substituição automática

No S3, confirme `uploads/ARQUIVO`, objeto privado e imagem acessível pela
aplicação. Em seguida:

1. anote os IDs das duas EC2;
2. encerre uma delas pelo Console EC2;
3. continue acessando marketplace e admin pelo ALB;
4. aguarde o ASG criar a substituta;
5. confirme novamente dois targets saudáveis;
6. confirme que produto e imagem continuam disponíveis.

Esse teste remove uma instância intencionalmente; o ASG deve substituí-la. Ele
também comprova que banco, imagens e `JWT_SECRET` não dependem de uma EC2.

## Evidências

Preencha somente após executar cada teste:

| Verificação | Local | Console | Terraform |
|---|---|---|---|
| Quatro containers locais / três por EC2 | [x] | [x] | [x] |
| Duas EC2 em zonas diferentes | N/A | [x] | [x] |
| Listeners 80 e 81 encaminham para os target groups corretos | N/A | [x] | [x] |
| Dois targets saudáveis em cada target group | N/A | [x] | [x] |
| Marketplace e admin acessíveis pelo ALB | N/A | [x] | [x] |
| Produto persistido no PostgreSQL | [x] | [x] | [x] |
| Imagem local no volume | [x] | N/A | N/A |
| Objeto privado persistido no S3 | N/A | [x] | [x] |
| RDS privado e criptografado | N/A | [x] | [x] |
| ASG substitui uma EC2 encerrada | N/A | [x] | [x] |
| Aplicação e dados disponíveis após substituição | N/A | [x] | [x] |
| Nenhum segredo nos logs | [x] | [x] | [x] |
| Recursos AWS removidos | N/A | [x] | [x] |
