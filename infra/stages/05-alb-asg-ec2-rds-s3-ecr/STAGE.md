# Stage 05 — imagens independentes no Amazon ECR

- Status: `VALIDADO — tag pendente de autorização`
- Tag planejada: `shopmicro-aws-stage-05`
- Local: imagens construídas pelo Docker Compose
- AWS: três imagens privadas no ECR com tag `latest`
- Aplicação: ALB e ASG com duas EC2 em zonas diferentes
- Dados: RDS PostgreSQL 18 privado e bucket S3 privado

## Objetivo

Reduzir o tempo de inicialização das EC2. As instâncias deixam de clonar o Git e
construir a aplicação; passam a baixar imagens prontas do ECR.

```text
ECR criado manualmente e mantido entre os stages
├── shopmicro-backend:latest
├── shopmicro-frontend:latest
└── shopmicro-frontend-admin:latest
                  │
                  ▼
        ASG — duas EC2 atrás do ALB
                  │
                  ├── RDS PostgreSQL 18
                  └── S3 privado
```

## Decisões

- Os três repositórios ECR são criados manualmente e não pertencem ao Terraform.
- `terraform destroy` não remove imagens nem repositórios ECR.
- A tag `latest` é mutável neste stage; versionamento chega futuramente com CI/CD.
- Cada aplicação pode ser construída, publicada e atualizada individualmente.
- As EC2 recebem somente permissões de pull nos três repositórios.
- O ambiente local continua independente da AWS e constrói pelo código-fonte.
- O S3 Gateway Endpoint permanece; endpoints privados do ECR não entram agora
  para evitar custo fixo de AWS PrivateLink.

Usar `latest` simplifica o treinamento, mas não oferece rollback determinístico.
O próximo ciclo de CI/CD deverá introduzir tags imutáveis ou implantação por
digest antes de tratar esse fluxo como produção.

## Ordem de execução

1. [Local](local/GUIA-LOCAL.md)
2. [AWS pelo Console](console/GUIA-CONSOLE.md)
3. [AWS pelo Terraform](terraform/GUIA-TERRAFORM.md)
4. [Validações](validacoes/VALIDACOES.md)

Validação manual de Local, Console e Terraform confirmada pelo responsável pelo
projeto. Criar e publicar a tag continuam exigindo autorizações separadas.
