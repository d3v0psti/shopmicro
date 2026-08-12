# ShopMicro — portfólio de arquitetura AWS

ShopMicro é uma carga de trabalho de marketplace usada para estudar a evolução
de um ambiente local em Docker Compose até uma arquitetura distribuída na AWS.

O foco é arquitetura de soluções e engenharia de cloud. A aplicação continua
funcionando localmente enquanto novos serviços são incorporados e validados na
AWS, sempre considerando custo, segurança e desempenho.

O projeto foi construído com apoio de IA generativa. Requisitos, testes e
decisões arquiteturais foram conduzidos pelo autor.

## Arquitetura atual

Na AWS, marketplace, painel administrativo e backend executam como serviços
independentes no Amazon ECS sobre EC2. O ALB distribui o tráfego HTTPS, o RDS
mantém os dados e o S3 armazena as imagens dos produtos.

[![Ambiente local e arquitetura AWS](docs/architecture/deployment-profiles.svg)](docs/architecture/deployment-profiles.drawio)

| Área | Estrutura validada |
|---|---|
| Rede | VPC, sub-redes em zonas distintas e Security Groups |
| Computação | EC2, Auto Scaling Group e ECS Capacity Provider |
| Containers | Três ECS Services e imagens privadas no ECR |
| Entrada | Application Load Balancer e Target Groups |
| Dados | RDS PostgreSQL 18 privado |
| Arquivos | Bucket S3 privado |
| DNS e HTTPS | Route 53 e certificado ACM |
| Segurança | IAM Roles e Parameter Store |
| Administração | SSM Session Manager, sem SSH público |

## Evolução validada

O ambiente local permanece como base para desenvolvimento e testes. A tabela
registra somente capacidades que já funcionaram localmente e foram validadas na
AWS.

[![Evolução da arquitetura](docs/architecture/validated-profiles.svg)](docs/architecture/validated-profiles.drawio)

| Evolução | Ambiente local | AWS |
|---|---|---|
| Base | Aplicações e PostgreSQL 18 no Docker Compose | Docker Compose em uma EC2 |
| Arquivos | Volume Docker | Amazon S3 privado |
| Banco | PostgreSQL 18 em container | Amazon RDS PostgreSQL 18 |
| Disponibilidade | Uma execução local | Duas EC2, ALB e Auto Scaling Group |
| Imagens Docker | Build local | Três repositórios no Amazon ECR |
| Orquestração | Docker Compose | Três serviços no Amazon ECS sobre EC2 |
| Acesso | `localhost` com HTTP | Route 53, ACM e HTTPS no ALB |

## Conhecimentos praticados

- Distribuição da carga entre zonas de disponibilidade.
- Persistência externa à camada computacional com RDS e S3.
- Balanceamento e roteamento com ALB e Target Groups.
- Orquestração de containers com ECS sobre EC2.
- Controle de acesso com IAM Roles e Security Groups.
- Segredos no Parameter Store.
- Administração das EC2 pelo SSM Session Manager.
- DNS e HTTPS com Route 53 e ACM.
- Diagnóstico por logs, eventos e health checks.

## Carga de trabalho

O ShopMicro possui marketplace, painel administrativo e backend. A carga inclui
catálogo, autenticação, pedidos, PostgreSQL 18 e upload de imagens, permitindo
validar tráfego, persistência, armazenamento e segurança.

No ambiente local, tudo é executado pelo Docker Compose. Na AWS, banco e
armazenamento são direcionados para RDS e S3 por configuração.

## Executar localmente

Requisitos: Docker Engine e Docker Compose v2.

```bash
cd infra
cp .env.example .env
# Preencha os campos obrigatórios do .env
docker compose up --build
```

| Serviço | Endereço padrão |
|---|---|
| Marketplace | http://localhost |
| Painel administrativo | http://localhost:81 |
| API e Swagger | http://localhost:8080/swagger |
| PostgreSQL | localhost:5432 |

Consulte o [guia de execução local](docs/local-execution.md).

## Documentação

- [Diagramas da arquitetura](docs/architecture/)
- [Modelo de dados](docs/database/README.md)
- [Execução local](docs/local-execution.md)

## Escopo atual

Esta é uma arquitetura de estudo e demonstração, não uma configuração pronta
para produção:

- RDS em Single-AZ.
- Cluster ECS com instâncias de pequeno porte.
- Imagens publicadas com a tag `latest`.
- Deployments executados manualmente.
- Sem autoscaling baseado em métricas da carga de trabalho.
- Observabilidade baseada em logs e health checks.
