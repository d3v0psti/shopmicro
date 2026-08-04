# Stage 04 — EC2, RDS, S3 e ECR

- Status: `PLANEJADO — preparar após a validação do Stage 03`
- Tag planejada: `shopmicro-aws-stage-04`
- Banco local: PostgreSQL 18 em container
- Banco na AWS: RDS PostgreSQL 18 privado
- Imagens de produto: S3 privado
- Imagens de containers: três repositórios privados no ECR

## Objetivo

Eliminar a compilação da aplicação no user data. A EC2 deverá autenticar no ECR,
baixar imagens já construídas e iniciar o Docker Compose.

Repositórios planejados:

```text
shopmicro/stage-04/backend
shopmicro/stage-04/frontend
shopmicro/stage-04/frontend-admin
```

Cada componente terá uma imagem e ciclo de publicação próprios. Isso cria
unidades de implantação separadas, mas não transforma o backend atual em vários
microserviços de negócio.

## Fluxo planejado

```text
Código → build local/CI → ECR → docker compose pull → EC2
                         ├── backend
                         ├── frontend
                         └── frontend-admin
```

O Terraform será executado em duas fases para evitar criar uma EC2 antes das
imagens existirem:

1. criar ECR e infraestrutura base;
2. construir e enviar as três imagens;
3. habilitar a EC2 e aplicar novamente;
4. medir e comparar o tempo de bootstrap com o Stage 03.

O detalhamento de `local/`, `console/`, `terraform/` e `validacoes/` será criado
somente depois que o Stage 03 for validado, preservando a progressão por versões
funcionais.
