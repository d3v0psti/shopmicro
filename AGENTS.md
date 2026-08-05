# AGENTS.md

## Projeto

ShopMicro é um marketplace com:

- Frontend e frontend-admin estáticos servidos por Nginx.
- Backend ASP.NET Core 8 com Entity Framework Core.
- PostgreSQL 18.
- Docker Compose local.
- Execução AWS incremental: armazenamento local no Stage 01, S3 no Stage 02,
  RDS PostgreSQL no Stage 03, ALB com duas EC2 no Stage 04, imagens privadas
  independentes no ECR manual no Stage 05 e ECS sobre EC2 no Stage 06.

Leia [README.md](README.md) antes de alterar o projeto.

## Comandos principais

```bash
cd infra
docker compose up --build
```

```bash
dotnet build backend/src/Api/Api.csproj
```

Endpoints locais: marketplace `:80`, admin `:81`, backend `:8080` e PostgreSQL
`:5432`. Health checks: `/health/live` e `/health/ready`.

## Convenções

- Mantenha o ambiente local independente da AWS.
- Use configuração por ambiente e nunca versione segredos.
- Preserve PostgreSQL 18 em todos os ambientes.
- Administre EC2 somente por SSM Session Manager; não abra a porta 22.
- Atualize Compose e documentação ao adicionar configuração na aplicação.
- Mantenha rotas relativas `/api/...` entre frontend e backend.
- Recursos AWS ficam em [infra/stages](infra/stages/).
- Siga a estrutura e o fluxo definidos em
  [GUIA-STAGES.md](infra/stages/GUIA-STAGES.md).
- Não crie, publique, mova ou exclua tags sem aprovação explícita do autor.
- Tags publicadas são imutáveis; correções usam sufixos como
  `shopmicro-aws-stage-01.1`.
- Mudanças de API devem atualizar a documentação correspondente.
- Considere o bootstrap e a carga inicial ao alterar o banco.
- Ao usar mais de uma EC2, compartilhe segredos de autenticação e mantenha dados
  persistentes fora das instâncias.
- No Stage 05, preserve os três repositórios ECR manuais fora do Terraform e use
  `latest`; versionamento imutável será introduzido futuramente com CI/CD.
- No Stage 06, use duas EC2 `t3.small`, três ECS Services e `awsvpcTrunking`.
  Descreva a solução como arquitetura orientada a serviços enquanto o backend
  continuar sendo um monólito de negócio.

## Validação mínima

- Execute `dotnet build` após mudanças no backend.
- Valide os arquivos Compose após mudanças de infraestrutura.
- O Terraform pertence ao repositório independente
  `d3v0psti/terraform-aws-platform`.
- Execute `validacoes/` antes de concluir um stage.
- Não exponha credenciais, tokens ou dados sensíveis em código e logs.
