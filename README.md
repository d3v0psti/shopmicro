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

## Ambientes

| Ambiente | Banco | Imagens |
|---|---|---|
| Local | PostgreSQL 18 em container | Volume Docker |
| AWS Stage 01 | PostgreSQL 18 na EC2 | Volume Docker |
| AWS Stage 02 | PostgreSQL 18 na EC2 | Bucket S3 privado |

O ambiente local não exige credenciais AWS. No Stage 01, banco e imagens ficam
em volumes Docker na EC2. No Stage 02, o backend passa a armazenar imagens no
S3 por uma IAM Role, sem Access Key ou Secret Key no projeto. O user data gera
automaticamente senhas aleatórias para o PostgreSQL e para o JWT.
A administração da instância usa somente AWS Systems Manager Session Manager;
o Security Group não possui entrada na porta 22.

## AWS

```text
Stage 01: EC2 + Docker Compose + armazenamento local
Stage 02: EC2 + Docker Compose + Amazon S3
```

Os arquivos para criação pelo Console e pelo Terraform estão em
[infra/aws](infra/aws/).

### Debug do PostgreSQL na EC2

A senha gerada fica em `/opt/shopmicro/infra/.env.aws-stage-01`. Para consultar
somente esse valor:

```bash
sudo grep '^POSTGRES_PASSWORD=' /opt/shopmicro/infra/.env.aws-stage-01
```

Para acessar o banco sem exibir a senha:

```bash
cd /opt/shopmicro/infra
sudo docker compose --env-file .env.aws-stage-01 -f compose.aws-stage-01.yaml \
  exec postgres psql -U postgres -d shopdb
```

O arquivo também contém o segredo JWT. Não compartilhe seu conteúdo nem o envie
para logs.

## Estrutura

```text
shopmicro/
├── backend/
├── frontend/
├── frontend-admin/
└── infra/
    ├── compose.yaml
    └── aws/
```

## Evolução

O projeto será ampliado com identidade administrativa, cache, filas, workers,
eventos, observabilidade, domínio e HTTPS, preservando a execução local e na AWS.
