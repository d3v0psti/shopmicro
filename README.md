# ShopMicro

Projeto de marketplace usado para estudar AWS, containers e arquitetura em
nuvem por meio de uma aplicação funcional.

Cada evolução é praticada primeiro no ambiente local e depois no Console AWS,
sempre considerando custo, segurança e desempenho. O projeto foi construído
com apoio de IA generativa, com requisitos, testes e decisões técnicas
conduzidos pelo autor.

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
- ECS sobre EC2, ECR, ALB, RDS, S3 e IAM
- Route 53, ACM, Parameter Store e SSM Session Manager

## Arquitetura local e AWS

O ShopMicro utiliza o mesmo código e as mesmas imagens Docker nos dois
ambientes. Banco, armazenamento e serviços de infraestrutura mudam por
configuração, sem alterar as regras de negócio.

[![Perfis de execução do ShopMicro](docs/architecture/deployment-profiles.svg)](docs/architecture/deployment-profiles.drawio)

| Componente | Ambiente local | Ambiente AWS validado |
|---|---|---|
| Execução | Docker Compose | Amazon ECS sobre EC2 |
| Backend | Container ASP.NET Core | ECS Service independente |
| Interfaces | Dois containers Nginx | Dois ECS Services independentes |
| Banco | PostgreSQL 18 em container | RDS PostgreSQL 18 privado |
| Imagens de produtos | Volume Docker | Bucket S3 privado |
| Imagens dos containers | Build local | Repositórios privados no ECR |
| Entrada | `localhost:80` e `localhost:81` | Application Load Balancer |
| DNS e HTTPS | Não necessários | Route 53 e certificado ACM |
| Segredos | Variáveis locais | Parameter Store |
| Permissões | Sem dependência de IAM | IAM Roles |
| Administração | Docker local | SSM Session Manager, sem SSH público |

## Cenários de implantação validados

Além do ambiente local e da arquitetura AWS atual, a aplicação foi executada em
configurações intermediárias. Cada cenário introduziu uma capacidade sem exigir
uma versão diferente do código.

[![Cenários validados na AWS](docs/architecture/validated-profiles.svg)](docs/architecture/validated-profiles.drawio)

| Perfil | Aplicação | Banco | Arquivos | Capacidade validada |
|---|---|---|---|---|
| Local | Docker Compose | PostgreSQL em container | Volume Docker | desenvolvimento sem AWS |
| EC2 básica | Docker Compose em uma EC2 | PostgreSQL na EC2 | Volume Docker | execução em máquina virtual |
| Armazenamento externo | Docker Compose em uma EC2 | PostgreSQL na EC2 | Amazon S3 | arquivos fora da instância |
| Dados gerenciados | Docker Compose em uma EC2 | Amazon RDS | Amazon S3 | banco e arquivos persistentes |
| Múltiplas instâncias | ALB e ASG com duas EC2 | Amazon RDS | Amazon S3 | distribuição entre zonas |
| Imagens centralizadas | ALB e ASG consumindo ECR | Amazon RDS | Amazon S3 | entrega por imagens independentes |
| Orquestração | Amazon ECS sobre EC2 | Amazon RDS | Amazon S3 | Services e tasks independentes |
| Acesso seguro atual | ECS, ALB e Route 53 | Amazon RDS | Amazon S3 | DNS próprio e HTTPS com ACM |

Esses perfis representam cenários efetivamente testados. A última linha é a
arquitetura AWS mais recente; as anteriores registram a portabilidade da
aplicação e as decisões que levaram ao desenho atual.

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

## Configuração por ambiente

O backend seleciona o armazenamento por variável de ambiente.

Local:

```env
STORAGE_PROVIDER=Local
```

AWS:

```env
STORAGE_PROVIDER=S3
S3_BUCKET_NAME=nome-do-bucket
AWS_REGION=região-escolhida-para-o-ambiente
```

PostgreSQL é utilizado nos dois ambientes. Apenas o endereço da conexão muda:

```env
# Local
DB_CONNECTION_STRING=Host=postgres;Port=5432;Database=shopdb;...

# AWS
DB_CONNECTION_STRING=Host=endpoint-do-rds;Port=5432;Database=shopdb;...
```

Outras configurações importantes:

- `JWT_SECRET` deve ser diferente e protegido em cada ambiente.
- `CORS_ALLOWED_ORIGINS` restringe as origens aceitas pelo backend.
- `BACKEND_UPSTREAM` informa aos containers Nginx como alcançar o backend.

Credenciais AWS não são armazenadas no código. Na AWS, o backend utiliza IAM
Role para acessar o S3, e os valores sensíveis são entregues pelo Parameter
Store.

Essa organização permite desenvolver sem uma conta AWS e promover os mesmos
containers para os serviços gerenciados quando necessário.

## Estrutura

```text
shopmicro/
├── backend/
├── docs/
│   └── architecture/
├── frontend/
├── frontend-admin/
└── infra/
    └── compose.yaml
```

## Próximos passos

O projeto será ampliado com identidade administrativa, cache, filas, workers,
eventos, observabilidade e CI/CD, preservando a execução local e na AWS.
