# Stage 06 — AWS pelo Console

Neste stage, o ECS substitui o Docker Compose nas EC2. Execute as seções na
ordem apresentada.

## 1. Publicar as imagens no ECR

Mantenha os repositórios privados e persistentes:

```text
shopmicro-backend
shopmicro-frontend
shopmicro-frontend-admin
```

Na pasta `shopmicro/infra`:

```bash
export AWS_REGION='us-east-1'
export AWS_ACCOUNT_ID="$(aws sts get-caller-identity --query Account --output text)"
export ECR_REGISTRY="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com"

aws ecr get-login-password --region "$AWS_REGION" | \
  docker login --username AWS --password-stdin "$ECR_REGISTRY"

docker build --platform linux/amd64 -f ../backend/Dockerfile \
  -t shopmicro-backend:latest ../backend
docker tag shopmicro-backend:latest "$ECR_REGISTRY/shopmicro-backend:latest"
docker push "$ECR_REGISTRY/shopmicro-backend:latest"

docker build --platform linux/amd64 -f ../frontend/Dockerfile \
  -t shopmicro-frontend:latest ../frontend
docker tag shopmicro-frontend:latest "$ECR_REGISTRY/shopmicro-frontend:latest"
docker push "$ECR_REGISTRY/shopmicro-frontend:latest"

docker build --platform linux/amd64 -f ../frontend-admin/Dockerfile \
  -t shopmicro-frontend-admin:latest ../frontend-admin
docker tag shopmicro-frontend-admin:latest \
  "$ECR_REGISTRY/shopmicro-frontend-admin:latest"
docker push "$ECR_REGISTRY/shopmicro-frontend-admin:latest"
```

O atalho equivalente é:

```bash
./stages/06-ecs-ec2-alb-rds-s3-ecr/console/publicar-imagens.sh all
```

Confirme `latest` nos três repositórios e execute `docker logout
$ECR_REGISTRY`. Não salve credenciais no projeto.

## 2. Criar a base de dados e armazenamento

Use nomes `shopmicro-stage-06-*` e repita a base já praticada:

1. bucket S3 privado, criptografado e versionado;
2. S3 Gateway Endpoint nas tabelas de rotas usadas;
3. RDS PostgreSQL 18 Single-AZ `db.t4g.micro`, gp3 20 GiB, privado;
4. banco `shopdb` e usuário `shopadmin`;
5. parâmetros SecureString para a connection string e o JWT:
   - `/shopmicro/stage-06/database/connection-string`;
   - `/shopmicro/stage-06/jwt/secret`.

## 3. Criar os Security Groups

| Security Group | Entrada permitida |
|---|---|
| `shopmicro-stage-06-alb` | TCP 80 de `0.0.0.0/0`; TCP 81 de `SEU_IP/32` |
| `shopmicro-stage-06-ecs-instances` | nenhuma |
| `shopmicro-stage-06-ecs-tasks` | TCP 80 e 8080 somente do SG do ALB |
| `shopmicro-stage-06-rds` | TCP 5432 somente do SG das tasks |

Não abra SSH. A administração das instâncias ocorre somente pelo SSM.

## 4. Criar as IAM Roles

Crie três roles, sem misturar suas responsabilidades:

1. `shopmicro-stage-06-ecs-instance` para EC2:
   - `AmazonEC2ContainerServiceforEC2Role`;
   - `AmazonSSMManagedInstanceCore`.
2. `shopmicro-stage-06-task-execution` para o agente ECS:
   - `AmazonECSTaskExecutionRolePolicy`;
   - `ssm:GetParameters` somente nos dois parâmetros do Stage 06.
3. `shopmicro-stage-06-backend-task` para o código do backend:
   - `s3:GetObject` e `s3:PutObject` somente em `BUCKET/uploads/*`.

Frontend e frontend-admin não recebem permissão de acesso ao S3.

## 5. Criar o cluster ECS sobre EC2

1. Abra **ECS → Clusters → Create cluster**.
2. Nome: `shopmicro-stage-06`.
3. Infraestrutura: **Amazon EC2 instances**.
4. Crie um ASG novo usando duas sub-redes em zonas diferentes.
5. AMI: **ECS-optimized Amazon Linux 2023 x86_64**.
6. Tipo: `t3.small`; volume gp3 criptografado de 30 GiB. A AMI otimizada para
   ECS já possui volume raiz de 30 GiB e não permite reduzi-lo no lançamento.
7. Capacidade: mínimo 2, desejado 2, máximo 4.
8. Associe a role e o SG das instâncias.
9. Não use Key Pair e exija IMDSv2.
10. Use [user-data.sh](user-data.sh) no Launch Template.

Antes de iniciar as tasks, habilite **awsvpcTrunking** em **ECS → Account
settings**. Isso aumenta a densidade de ENIs e permite acomodar as seis tasks
nas duas `t3.small`.

Confirme duas container instances `ACTIVE`, uma em cada zona. O Capacity
Provider deve usar managed scaling e managed draining.

## 6. Criar ALB e target groups

Crie um ALB público em duas sub-redes e três target groups do tipo **IP**:

| Target group | Porta | Health check |
|---|---:|---|
| frontend | 80 | `/` (`200-399`) |
| frontend-admin | 80 | `/` (`200-399`) |
| backend | 8080 | `/health/ready` (`200`) |

Configure:

- listener 80: ação padrão frontend;
- listener 81: ação padrão frontend-admin;
- em ambos, regra de maior prioridade para `/api/*` e `/uploads/*` → backend.

## 7. Registrar três Task Definitions

Use launch type **EC2**, network mode `awsvpc` e logs `awslogs` com retenção de
sete dias.

### Backend

- imagem: `shopmicro-backend:latest` do ECR;
- porta 8080, CPU 256, memory reservation 384 MiB;
- execution role da etapa 4 e task role do backend;
- variáveis: `STORAGE_PROVIDER=S3`, bucket e região;
- secrets: connection string em `DB_CONNECTION_STRING` e JWT em `JWT_SECRET`.

### Frontend

- imagem: `shopmicro-frontend:latest`;
- porta 80, CPU 128, memory reservation 128 MiB;
- `BACKEND_UPSTREAM=http://DNS_DO_ALB`.

### Frontend-admin

- imagem: `shopmicro-frontend-admin:latest`;
- porta 80, CPU 128, memory reservation 128 MiB;
- `BACKEND_UPSTREAM=http://DNS_DO_ALB`.

## 8. Criar três ECS Services

Para cada Task Definition:

1. capacity provider do cluster;
2. desired tasks: `2`;
3. duas sub-redes e SG das tasks;
4. sem IP público direto nas tasks; o agente ECS da instância faz o pull das
   imagens e entrega os segredos;
5. target group correspondente;
6. placement strategy `spread` por zona de disponibilidade;
7. health-check grace period: 180 s para backend e 120 s para frontends.

O resultado esperado é três Services estáveis, seis tasks `RUNNING` e todos os
targets saudáveis.

## 9. Atualizar uma aplicação

Depois de publicar uma nova `latest`, abra o serviço correspondente e escolha
**Update → Force new deployment**. Não reinicie os outros serviços.

## 10. Validar e remover

Execute [VALIDACOES.md](../validacoes/VALIDACOES.md). Depois da sua validação,
remova services, cluster/ASG, ALB, RDS, bucket e parâmetros. Preserve os três
repositórios ECR para o próximo stage.
