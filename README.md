# ShopMicro

Marketplace criado com IA generativa para apoiar estudos de AWS, containers e
arquitetura em nuvem.

O projeto usa uma aplicação funcional para praticar serviços AWS pelo Console e
com Terraform, sempre avaliando custo, segurança e desempenho. Requisitos e
decisões são orientados pelo autor; a IA gera e refina código e documentação.

## Funcionalidades

- Catálogo, busca, categorias e carrinho
- Cadastro e autenticação de clientes
- Checkout, consulta e cancelamento de pedidos
- Painel administrativo
- Gestão de produtos, pedidos, clientes e administradores
- Upload de imagens localmente ou no S3

## Tecnologias

- HTML, CSS, JavaScript e Nginx
- ASP.NET Core 8 e Entity Framework Core
- PostgreSQL 18
- JWT com refresh token
- Docker Compose
- EC2, IAM, S3 e Terraform

## Execução local

Requisitos: Docker Engine e Docker Compose v2.

```bash
cd infra
docker compose up --build
```

| Serviço | Endereço |
|---|---|
| Marketplace | http://localhost |
| Painel administrativo | http://localhost:81 |
| Swagger | http://localhost:8080/swagger |
| PostgreSQL | localhost:5432 |

Acesso administrativo inicial:

```text
E-mail: admin@admin.com
Senha: 123456
```

Altere essa senha depois do primeiro acesso e não utilize dados sensíveis.

Para parar preservando os dados:

```bash
docker compose down
```

Para remover também banco e uploads locais:

```bash
docker compose down -v
```

> PostgreSQL 18 utiliza o volume em `/var/lib/postgresql`. Volumes de versões
> anteriores devem ser migrados ou recriados.

## Evolução na AWS

| Ambiente | Banco | Imagens |
|---|---|---|
| Local | PostgreSQL 18 em container | Volume Docker |
| AWS Stage 01 | PostgreSQL 18 na EC2 | Volume Docker |
| AWS Stage 02 | PostgreSQL 18 na EC2 | Bucket S3 privado |
| AWS Stage 03 | RDS PostgreSQL 18 privado | Bucket S3 privado |

Cada stage adiciona um conceito sem quebrar a execução local:

1. Stage 01: aplicação, PostgreSQL e uploads na EC2.
2. Stage 02: uploads transferidos para o S3.
3. Stage 03: PostgreSQL transferido para o RDS.
4. Stage 04: imagens Docker no ECR, planejado.

EC2 é administrada somente pelo Session Manager, sem SSH. A aplicação usa IAM
Roles, sem Access Key ou Secret Key no código. Consulte os roteiros em
[infra/stages](infra/stages/) e a [ordem de execução](infra/stages/GUIA-STAGES.md).

## Estrutura

```text
shopmicro/
├── backend/
├── frontend/
├── frontend-admin/
└── infra/
    ├── compose.yaml
    └── stages/
```

## Próximos passos

O projeto será ampliado com identidade administrativa, cache, filas, workers,
eventos, observabilidade, domínio e HTTPS, preservando a execução local e na AWS.
