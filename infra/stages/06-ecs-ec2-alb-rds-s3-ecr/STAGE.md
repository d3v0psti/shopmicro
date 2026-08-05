# Stage 06 — ECS sobre EC2

- Status: `EM PREPARAÇÃO`
- Tag planejada: `shopmicro-aws-stage-06`
- Computação: cluster ECS com duas EC2 `t3.small` em zonas diferentes
- Aplicação: três ECS Services independentes
- Dados: RDS PostgreSQL 18 privado e bucket S3 privado
- Entrada: ALB público nas portas 80 e 81

## Objetivo

Substituir o Docker Compose nas EC2 pelo Amazon ECS, mantendo as imagens
privadas do ECR e preparando a aplicação para implantação automatizada no
próximo stage.

```text
ALB :80/:81
     │
     ├── frontend Service (2 tasks)
     ├── frontend-admin Service (2 tasks)
     └── backend Service (2 tasks) ── RDS PostgreSQL 18
                                      └── S3 privado
                 │
        ECS Capacity Provider
                 │
        ASG: 2 × EC2 t3.small
```

## Decisões

- ECS usa EC2, não Fargate, para controlar o custo do treinamento.
- Cada EC2 usa 30 GiB gp3, mínimo da AMI Amazon Linux 2023 otimizada para ECS.
- O Capacity Provider gerencia o ASG com mínimo 2 e máximo 4 instâncias.
- Cada serviço mantém duas tasks distribuídas entre zonas de disponibilidade.
- O ALB envia `/api/*` e `/uploads/*` diretamente ao serviço backend.
- ECR permanece manual e fora do lifecycle do Terraform.
- A tag `latest` permanece até a introdução de CI/CD e tags imutáveis.
- O backend ainda é um monólito de negócio. Neste ponto, a solução é uma
  arquitetura orientada a serviços, não uma arquitetura de microserviços.
- O ambiente local continua funcionando somente com Docker Compose.

## Ordem

1. [Local](local/GUIA-LOCAL.md)
2. [AWS pelo Console](console/GUIA-CONSOLE.md)
3. [AWS pelo Terraform](terraform/GUIA-TERRAFORM.md)
4. [Validações](validacoes/VALIDACOES.md)

Somente o responsável pelo projeto valida o stage e autoriza separadamente a
criação e a publicação da tag Git.
