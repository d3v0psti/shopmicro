# ShopMicro — Catálogo de Produtos + Pedidos (Cloud-Agnostic Microservices)

Aplicação de referência para portfólio: mini e-commerce (catálogo de
produtos, carrinho e criação de pedidos), estruturada como microserviços
prontos para deploy em qualquer nuvem pública (AWS, Azure, GCP) ou
on-premises. Nenhuma parte do código depende de um provedor específico.

## Stack

- **Frontend**: Node.js + Express (atua como BFF — Backend For Frontend),
  servindo uma vitrine de produtos com carrinho e checkout.
- **Backend**: .NET Core 8 Web API + Entity Framework Core (Npgsql).
- **Banco de dados**: PostgreSQL.
- **Infra**: Docker (multi-stage builds) + Kubernetes manifests puros +
  docker-compose para desenvolvimento local.

## Funcionalidades

- Vitrine de produtos com filtro por categoria
- Carrinho de compras (adicionar, ajustar quantidade, remover)
- Checkout com nome/e-mail do cliente
- Criação de pedido no backend, com:
  - validação de estoque
  - "congelamento" de preço/nome do produto no momento da compra
  - baixa automática de estoque
  - cálculo de total em transação
- Endpoints para consultar pedidos e atualizar status (Pending → Confirmed → Shipped → Cancelled)
- Seed automático de 6 produtos de demonstração na primeira subida

## Estrutura do projeto

```
cloud-project/
├── frontend/                  # Node.js (Express) — BFF + vitrine
│   ├── src/
│   │   ├── server.js          # Servidor + proxy reverso para o backend
│   │   └── public/            # Vitrine, carrinho e checkout (HTML/CSS/JS)
│   ├── package.json
│   ├── Dockerfile
│   └── .env.example
│
├── backend/                   # .NET Core 8 Web API
│   ├── src/Api/
│   │   ├── Controllers/
│   │   │   ├── ProductsController.cs   # CRUD de produtos + categorias
│   │   │   └── OrdersController.cs     # Criação/consulta/status de pedidos
│   │   ├── Models/             # Product, Order, OrderItem + DTOs
│   │   ├── Data/                # AppDbContext (EF Core + Npgsql)
│   │   ├── Migrations/          # Migration inicial (products, orders, order_items)
│   │   ├── Program.cs           # Config agnóstica de ambiente + seed de demo
│   │   └── appsettings.json
│   └── Dockerfile
│
├── infra/
│   ├── docker-compose.yml       # Ambiente completo para dev local
│   ├── k8s/                     # Manifests Kubernetes (qualquer cluster)
│   │   ├── 00-namespace.yaml
│   │   ├── 01-config-secret.yaml
│   │   ├── 02-postgres.yaml
│   │   ├── 03-backend.yaml
│   │   ├── 04-frontend.yaml
│   │   └── 05-ingress.yaml
│   └── postgres/init.sql
│
└── README.md
```

## Por que é agnóstico de nuvem

- Backend e frontend só se comunicam via HTTP, com nomes de serviço
  resolvidos por variável de ambiente (`BACKEND_URL`, `DB_CONNECTION_STRING`).
- Sem SDKs proprietários de nuvem no código de aplicação. Trocar para um
  Postgres gerenciado (RDS, Azure Database, Cloud SQL) é só trocar a
  connection string.
- Imagens base padrão OCI (`node:20-alpine`,
  `mcr.microsoft.com/dotnet/aspnet:8.0`, `postgres:16-alpine`).
- Manifests Kubernetes puros (`apps/v1`, `networking.k8s.io/v1`), sem CRDs
  específicos — rodam em EKS, AKS, GKE, k3s, RKE, OpenShift ou cluster
  on-premises.
- Health checks padronizados (`/health/live`, `/health/ready`).

## Rodando localmente (Docker Compose)

```bash
cd infra
docker compose up --build
```

- Vitrine: http://localhost:3000
- Backend (Swagger): http://localhost:8080/swagger
- Postgres: localhost:5432 (user/senha: postgres/postgres)

Os 6 produtos de demonstração são criados automaticamente na primeira
subida do backend.

## Rodando em Kubernetes (qualquer cluster)

```bash
docker build -t your-registry/backend:latest ./backend
docker build -t your-registry/frontend:latest ./frontend
docker push your-registry/backend:latest
docker push your-registry/frontend:latest
```

Ajuste `image:` em `infra/k8s/03-backend.yaml` e `04-frontend.yaml`, depois:

```bash
kubectl apply -f infra/k8s/00-namespace.yaml
kubectl apply -f infra/k8s/01-config-secret.yaml
kubectl apply -f infra/k8s/02-postgres.yaml
kubectl apply -f infra/k8s/03-backend.yaml
kubectl apply -f infra/k8s/04-frontend.yaml
kubectl apply -f infra/k8s/05-ingress.yaml
```

Ajuste o `host` em `05-ingress.yaml` para seu domínio real e confirme que
há um IngressController instalado (nginx, traefik etc).

> **Importante:** `01-config-secret.yaml` traz um Secret em texto puro
> apenas como exemplo local. Em produção, use um cofre de segredos
> (Vault, AWS Secrets Manager, Azure Key Vault, GCP Secret Manager, ou
> Sealed Secrets/SOPS para clusters on-premises).

## Endpoints da API

### Produtos
| Método | Rota                              | Descrição                  |
|--------|------------------------------------|------------------------------|
| GET    | /api/v1/products                  | Lista produtos (filtro `?category=`) |
| GET    | /api/v1/products/{id}              | Busca produto por id         |
| GET    | /api/v1/products/categories        | Lista categorias disponíveis |
| POST   | /api/v1/products                  | Cria produto                 |
| PUT    | /api/v1/products/{id}              | Atualiza produto              |
| DELETE | /api/v1/products/{id}              | Remove produto                |

### Pedidos
| Método | Rota                              | Descrição                  |
|--------|------------------------------------|------------------------------|
| GET    | /api/v1/orders                    | Lista todos os pedidos       |
| GET    | /api/v1/orders/{id}                | Busca pedido por id           |
| POST   | /api/v1/orders                    | Cria pedido (valida estoque)  |
| PUT    | /api/v1/orders/{id}/status         | Atualiza status do pedido      |

## Variáveis de ambiente

### Backend
| Variável               | Descrição                                      |
|-------------------------|-------------------------------------------------|
| `DB_CONNECTION_STRING`  | String de conexão Npgsql para o PostgreSQL       |
| `CORS_ALLOWED_ORIGINS`  | Origens permitidas (`*` ou lista separada por vírgula) |
| `ENABLE_SWAGGER`        | `true`/`false` — habilita Swagger fora de Development |

### Frontend
| Variável       | Descrição                                    |
|----------------|-----------------------------------------------|
| `PORT`         | Porta do servidor Express (padrão 3000)       |
| `BACKEND_URL`  | URL base do backend para o proxy reverso      |

## Ideias para evoluir (bom material para o post no LinkedIn)

- Autenticação (JWT/OIDC) para diferenciar cliente e administrador
- Painel administrativo para gerenciar produtos e status de pedidos
- Pipeline de CI/CD (GitHub Actions) fazendo build, testes e push das imagens
- HorizontalPodAutoscaler nos Deployments do Kubernetes
- Observabilidade: métricas (Prometheus) e tracing distribuído (OpenTelemetry)
- Cache de catálogo (Redis) para reduzir carga no Postgres
