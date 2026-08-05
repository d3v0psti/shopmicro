# Stage 06 — validações

Somente o responsável pelo projeto confirma as evidências. A tag Git depende da
validação de Local e Console e de autorização explícita.

## Local

```bash
cd infra/stages/06-ecs-ec2-alb-rds-s3-ecr/local
docker compose up --build -d
docker compose ps
curl --fail http://localhost:8080/health/live
curl --fail http://localhost:8080/health/ready
docker compose exec -T postgres psql -U postgres -d shopdb \
  < ../validacoes/consultar-produtos.sql
docker compose restart
```

Confirme marketplace, admin, cadastro, imagem e persistência após o reinício.

## AWS — Console

### 1. Cluster e alta disponibilidade

- duas EC2 `t3.small` registradas no cluster e em zonas diferentes;
- três ECS Services com `desired=2` e `running=2`;
- seis tasks em estado `RUNNING`;
- targets dos três target groups em estado `healthy`;
- marketplace na porta 80 e admin na porta 81 do ALB.

Pela AWS CLI:

```bash
aws ecs describe-services --cluster shopmicro-stage-06 \
  --services shopmicro-stage-06-backend \
  shopmicro-stage-06-frontend \
  shopmicro-stage-06-frontend-admin \
  --query 'services[].{service:serviceName,desired:desiredCount,running:runningCount}'

aws ecs list-container-instances --cluster shopmicro-stage-06
aws ecs list-tasks --cluster shopmicro-stage-06
```

### 2. Banco e S3

Cadastre `VALIDACAO-STAGE-06-AAAA-MM-DD` com uma imagem. Confirme:

- registro no RDS PostgreSQL 18;
- objeto em `uploads/` no bucket privado;
- imagem acessível pelo ALB;
- dados preservados após forçar nova implantação do backend.

Consulte o banco pelo ECS Exec ou por uma task temporária com cliente `psql`.
Não exponha o RDS nem imprima a senha em logs.

### 3. Implantação independente

1. Publique somente `frontend:latest`.
2. Anote os digests atuais das três imagens.
3. force nova implantação apenas do serviço frontend.
4. Confirme o novo digest do frontend e os mesmos digests dos demais serviços.
5. Confirme que marketplace, admin, produto e imagem continuam disponíveis.

## Checklist manual

| Verificação | Local | Console |
|---|---|---|
| Aplicação e persistência | [ ] | [ ] |
| Três Services com duas tasks | N/A | [ ] |
| Duas EC2 em zonas diferentes | N/A | [ ] |
| ALB e target groups saudáveis | N/A | [ ] |
| RDS e S3 privados | N/A | [ ] |
| Atualização independente | N/A | [ ] |
| Nenhum segredo em código ou logs | [ ] | [ ] |
| Recursos removidos e ECR preservado | N/A | [ ] |
