# AGENTS.md

## Projeto

ShopMicro é um marketplace com:

- `shopmicro-frontend` e `shopmicro-frontend-admin` estáticos servidos por Nginx.
- `shopmicro-backend` em ASP.NET Core 8 com Entity Framework Core.
- PostgreSQL 18.
- Docker Compose local.
- Uploads configuráveis para volume local ou bucket S3.

Leia [README.md](README.md) antes de alterar o projeto.

## Comandos principais

```bash
cd infra
docker compose up --build
```

```bash
dotnet build shopmicro-backend/src/Api/Api.csproj
```

Endpoints locais: marketplace `:80`, admin `:81`, backend `:8080` e PostgreSQL
`:5432`. Health checks: `/health/live` e `/health/ready`.

Identidades são separadas entre os prefixos `/api/marketplace` e
`/api/administration`. Não volte a compartilhar tabelas, tokens ou endpoints
de login entre esses contextos.

## Convenções

- Mantenha o ambiente local independente da AWS.
- Use configuração por ambiente e nunca versione segredos.
- Preserve PostgreSQL 18 em todos os ambientes.
- Administre EC2 somente por SSM Session Manager; não abra a porta 22.
- Atualize Compose e documentação ao adicionar configuração na aplicação.
- Mantenha rotas relativas `/api/...` entre frontend e backend.
- Mudanças de API devem atualizar a documentação correspondente.
- Considere o bootstrap e a carga inicial ao alterar o banco.
- Altere o esquema somente por migrations do Entity Framework Core.
- Ao usar mais de uma EC2, compartilhe segredos de autenticação e mantenha dados
  persistentes fora das instâncias.

## Validação mínima

- Execute `dotnet build` após mudanças no backend.
- Valide os arquivos Compose após mudanças de infraestrutura.
- Não exponha credenciais, tokens ou dados sensíveis em código e logs.
