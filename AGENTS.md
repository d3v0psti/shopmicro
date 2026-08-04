# AGENTS.md

## Projeto

ShopMicro é um marketplace com:

- Frontend e frontend-admin estáticos servidos por Nginx.
- Backend ASP.NET Core 8 com Entity Framework Core.
- PostgreSQL 18.
- Docker Compose local.
- Execução AWS incremental: armazenamento local na EC2 no Stage 01 e uploads no S3 no Stage 02.

Leia [README.md](README.md) antes de alterar o projeto.

## Execução e validação

```bash
cd infra
docker compose up --build
```

Serviços:

- Marketplace: http://localhost
- Painel administrativo: http://localhost:81
- Swagger: http://localhost:8080/swagger
- PostgreSQL: localhost:5432

Backend:

```bash
dotnet build backend/src/Api/Api.csproj
dotnet run --project backend/src/Api/Api.csproj
```

Health checks: `/health/live` e `/health/ready`.

## Convenções

- Mantenha o ambiente local independente da AWS.
- Use configuração por ambiente e nunca versione segredos.
- Preserve PostgreSQL 18 em todos os ambientes.
- Administre EC2 somente por SSM Session Manager; não abra a porta 22.
- Atualize Compose, Terraform e documentação ao adicionar configurações.
- Mantenha rotas relativas `/api/...` entre frontend e backend.
- Recursos AWS ficam em [infra/aws](infra/aws/).
- Mudanças de API devem atualizar a documentação correspondente.
- Considere o bootstrap e a carga inicial ao alterar o banco.

## Validação mínima

- Execute `dotnet build` após mudanças no backend.
- Valide os arquivos Compose após mudanças de infraestrutura.
- Execute `terraform fmt -check` e `terraform validate` após mudanças no Terraform.
- Não exponha credenciais, tokens ou dados sensíveis em código e logs.
