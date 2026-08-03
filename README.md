# ShopMicro

Marketplace local de referência para catálogo, carrinho, pedidos e gestão
administrativa. O projeto foi preparado para execução exclusivamente local com
Docker Compose.

## Funcionalidades

### Marketplace

- Catálogo de produtos com busca e categorias
- Carrinho lateral, cupom e cálculo de frete demonstrativo
- Cadastro, login e edição do perfil do cliente
- Checkout e criação de pedidos
- Consulta e cancelamento de pedidos elegíveis
- Exclusão da conta do cliente

### Painel administrativo

- Cadastro, edição e exclusão de produtos
- Upload local de imagens
- Consulta de pedidos
- Gestão separada de clientes e administradores
- Redefinição de senhas e controle de acesso por perfil

## Tecnologias

- Frontend e frontend-admin: HTML, CSS e JavaScript servidos por Nginx
- Backend: ASP.NET Core 8 Web API
- Persistência: Entity Framework Core e PostgreSQL 16
- Autenticação: JWT com refresh token
- Ambiente local: Docker Compose

## Requisitos

- Docker Engine
- Docker Compose v2

Não é necessário instalar .NET, PostgreSQL, Nginx ou Node.js para executar o
ambiente completo.

## Executando localmente

Na raiz do repositório:

```bash
cd infra
docker compose up --build
```

Na primeira execução, aguarde o PostgreSQL ficar saudável e o backend concluir
a criação e a carga inicial do banco.

### Endereços

| Serviço | Endereço |
|---|---|
| Marketplace | http://localhost |
| Painel administrativo | http://localhost:81 |
| Swagger da API | http://localhost:8080/swagger |
| Backend | http://localhost:8080 |
| PostgreSQL | localhost:5432 |

### Administrador inicial

```text
E-mail: admin@admin.com
Senha: 123456
```

Essa credencial é destinada somente ao ambiente local. Depois de entrar no
painel, a senha pode ser alterada em **Minha senha**.

## Comandos úteis

Executar em segundo plano:

```bash
cd infra
docker compose up --build -d
```

Ver o estado dos serviços:

```bash
docker compose ps
```

Ver os logs:

```bash
docker compose logs -f
```

Parar o ambiente preservando os dados:

```bash
docker compose down
```

Recriar completamente o ambiente e apagar banco e uploads locais:

```bash
docker compose down -v
docker compose up --build
```

> O comando `down -v` remove permanentemente os volumes locais do projeto.

## Persistência local

O Compose cria dois volumes:

- `postgres_data`: banco de dados
- `backend_uploads`: imagens enviadas pelo painel administrativo

Os dados sobrevivem a `docker compose down`. Para removê-los, é necessário usar
explicitamente `docker compose down -v`.

## Estrutura

```text
shopmicro/
├── backend/
│   ├── Dockerfile
│   └── src/Api/
│       ├── Controllers/
│       ├── Data/
│       ├── Models/
│       ├── Services/
│       └── Program.cs
├── frontend/
│   ├── Dockerfile
│   ├── src/public/
│   └── templates/
├── frontend-admin/
│   ├── Dockerfile
│   ├── src/public/
│   └── templates/
└── infra/
    ├── compose.yaml
    └── aws/
        └── *.tf
```

## Configuração local

As variáveis ficam em [infra/compose.yaml](infra/compose.yaml):

| Variável | Uso |
|---|---|
| `DB_CONNECTION_STRING` | Conexão do backend com o PostgreSQL local |
| `JWT_SECRET` | Assinatura dos tokens locais |
| `CORS_ALLOWED_ORIGINS` | Origens aceitas pela API |
| `ENABLE_SWAGGER` | Habilita o Swagger |
| `BACKEND_UPSTREAM` | Endereço interno do backend usado pelos Nginx |

As imagens de produtos são armazenadas exclusivamente no volume local do
backend. O projeto não possui integração com storage externo.

## Principais endpoints

### Produtos

| Método | Rota | Acesso |
|---|---|---|
| GET | `/api/v1/products` | Público |
| GET | `/api/v1/products/{id}` | Público |
| GET | `/api/v1/products/categories` | Público |
| POST | `/api/v1/products` | Administrador |
| PUT | `/api/v1/products/{id}` | Administrador |
| DELETE | `/api/v1/products/{id}` | Administrador |

### Pedidos

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/v1/orders?email={email}` | Consulta pedidos |
| POST | `/api/v1/orders` | Cria pedido |
| POST | `/api/v1/orders/{id}/cancel` | Cancela pedido elegível |
| PUT | `/api/v1/orders/{id}/status` | Atualiza status |

### Usuários

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/users/register` | Cadastra cliente |
| POST | `/api/users/login` | Autentica usuário |
| POST | `/api/users/refresh-token` | Renova token |
| POST | `/api/users/logout` | Encerra sessão |
| GET | `/api/users?type=Client\|Admin` | Lista por perfil administrativo |
| POST | `/api/users/admins` | Cadastra administrador |
| PUT | `/api/users/{email}` | Atualiza perfil |
| PUT | `/api/users/{email}/password` | Atualiza senha |
| DELETE | `/api/users/{email}` | Exclui conta |

## Desenvolvimento do backend sem Docker

Com .NET 8 instalado e um PostgreSQL disponível:

```bash
export DB_CONNECTION_STRING='Host=localhost;Port=5432;Database=shopdb;Username=postgres;Password=postgres'
export JWT_SECRET='shopmicro_local_development_secret'
dotnet run --project backend/src/Api/Api.csproj
```

Validação de compilação:

```bash
dotnet build backend/src/Api/Api.csproj
```

## Health checks

- Backend: `/health/live` e `/health/ready`
- Marketplace: `/health/live` e `/health/ready`

## Deploy gerenciado na AWS

O mesmo repositório contém a infraestrutura AWS. O desenvolvimento continua
local com Docker Compose; somente imagens validadas são publicadas no ECR e
executadas no ECS sobre uma instância EC2 compartilhada.

### Arquitetura

```text
Internet
   │
   ▼
Application Load Balancer
   ├── :80 ───────────────► ECS EC2 / frontend
   ├── :81 ───────────────► ECS EC2 / frontend-admin
   ├── /api/* ────────────► ECS EC2 / backend
   └── /uploads/* ────────► ECS EC2 / backend
                                  │
                    ┌─────────────┴─────────────┐
                    ▼                           ▼
              RDS PostgreSQL                  EFS
              sub-redes privadas       imagens persistentes
```

Também são criados três repositórios ECR, Secrets Manager para a conexão do
banco e o JWT, CloudWatch Logs, VPC, Security Groups e um Auto Scaling Group
associado ao ECS por capacity provider. O padrão usa uma única instância
`t3.small` com a AMI ECS-optimized Amazon Linux 2023. O RDS não possui acesso
público.

Para reduzir o custo de estudo, a instância ECS fica em sub-rede pública e não
aceita conexões de entrada. As interfaces de rede das tarefas aceitam tráfego
somente do ALB. Essa configuração evita Fargate e NAT Gateway.

### Custos

ALB, RDS, EC2 e EFS geram cobrança. Mesmo com zero tarefas ECS, a instância EC2,
o ALB e o RDS permanecem cobrados. Ao terminar, use `terraform destroy`.

### Pré-requisitos

- AWS CLI v2 autenticada
- Docker
- Terraform 1.7 ou superior
- `jq`

```bash
aws sts get-caller-identity
```

### Provisionamento inicial

```bash
cd infra/aws
cp terraform.tfvars.example terraform.tfvars
terraform init
terraform fmt -check
terraform validate
terraform plan
terraform apply
```

O arquivo de exemplo inicia com `service_desired_count = 0`, pois ainda não há
imagens nos repositórios ECR. A capacidade padrão é uma única `t3.small`:

```hcl
ecs_instance_type    = "t3.small"
ecs_min_size         = 1
ecs_max_size         = 1
ecs_desired_capacity = 1
```

Depois do primeiro apply, volte à raiz e publique as imagens:

```bash
./scripts/deploy-aws.sh
```

O script autentica no ECR, constrói as três imagens `linux/amd64`, publica-as,
altera os serviços para uma tarefa e força o deployment na capacidade EC2.

URLs resultantes:

```bash
terraform -chdir=infra/aws output marketplace_url
terraform -chdir=infra/aws output admin_url
```

Para novos deployments:

```bash
IMAGE_TAG=v1.1.0 ./scripts/deploy-aws.sh
```

Logs do backend:

```bash
aws logs tail /ecs/shopmicro-dev/backend --region us-east-1 --follow
```

### Alta disponibilidade e produção

O padrão de estudo usa RDS Single-AZ, uma instância ECS e uma tarefa por
serviço. Para aumentar a disponibilidade, ajuste no `terraform.tfvars`:

```hcl
db_multi_az           = true
service_desired_count = 2
ecs_min_size          = 2
ecs_max_size          = 2
ecs_desired_capacity  = 2
protect_database      = true
```

Para produção, adicione certificado ACM, listener HTTPS e domínio no Route 53.

### Remoção da infraestrutura

Este comando apaga a infraestrutura e, com `protect_database = false`, também
os dados do RDS:

```bash
terraform -chdir=infra/aws destroy
```

Referências oficiais: [ECS com ALB](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/alb.html),
[capacity providers EC2](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/asg-capacity-providers.html),
[role da instância ECS](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/instance_IAM_role.html) e
[RDS em VPC](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_VPC.WorkingWithRDSInstanceinaVPC.html).
