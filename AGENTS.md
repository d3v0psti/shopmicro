# AGENTS.md

## Visão geral
Este repositório é uma aplicação de referência em arquitetura de microsserviços para catálogo e pedidos, com:
- Frontend e frontend-admin em HTML/CSS/JS estáticos servidos por Nginx.
- Backend em ASP.NET Core 8 Web API com Entity Framework Core + PostgreSQL.
- Infra local via Docker Compose e deploy em Kubernetes com manifests puros.

Leia primeiro [README.md](README.md) para contexto de produto e execução.

## Como trabalhar neste projeto

### 1. Fluxo de execução local
O caminho principal de validação local é:
```bash
cd infra
docker compose up --build
```

Isso sobe:
- Frontend em http://localhost:3000
- Frontend-admin em http://localhost:81
- Backend Swagger em http://localhost:8080/swagger
- PostgreSQL em localhost:5432

### 2. Backend
Para trabalhar diretamente no backend, use:
```bash
dotnet build backend/src/Api/Api.csproj
dotnet run --project backend/src/Api/Api.csproj
```

Pontos importantes:
- A configuração é orientada por variáveis de ambiente, principalmente `DB_CONNECTION_STRING`.
- O banco é criado automaticamente com `EnsureCreated()` em [backend/src/Api/Program.cs](backend/src/Api/Program.cs).
- O startup também popula produtos iniciais e cria um usuário admin padrão se necessário.
- Health checks estão disponíveis em `/health/live` e `/health/ready`.

### 3. Frontend
O frontend não depende de um servidor Node em desenvolvimento; ele é servido como conteúdo estático via Nginx.
- Arquivos principais: [frontend/src/public/app.js](frontend/src/public/app.js) e [frontend-admin/src/public/app.js](frontend-admin/src/public/app.js)
- Se alterar a interface, verifique também o template do Nginx em [frontend/templates/default.conf.template](frontend/templates/default.conf.template) e [frontend-admin/templates/default.conf.template](frontend-admin/templates/default.conf.template)

## Convenções importantes
- Prefira configuração por ambiente em vez de valores hardcoded.
- Se adicionar uma nova variável de ambiente, atualize os arquivos de infraestrutura e a documentação relevante.
- Mantenha backend e frontend desacoplados; a comunicação ocorre por HTTP e o frontend usa rotas relativas como `/api/v1/...`.
- Não introduza dependências cloud-specific no código de aplicação; o projeto é pensado para ser agnóstico de provedor.
- Alterações que mudam contratos da API devem refletir em [README.md](README.md) e, quando pertinente, nos manifests em [infra/k8s](infra/k8s).

## Estrutura relevante
- Backend: [backend/src/Api](backend/src/Api)
- Controllers principais: [backend/src/Api/Controllers](backend/src/Api/Controllers)
- Modelos e DTOs: [backend/src/Api/Models](backend/src/Api/Models)
- Infra local: [infra/docker-compose-local.yaml](infra/docker-compose-local.yaml)
- Kubernetes: [infra/k8s](infra/k8s)

## Boas práticas para agentes
- Faça mudanças pequenas e focalizadas.
- Valide com `dotnet build` para backend e, se possível, com `docker compose up --build` para verificar o fluxo end-to-end.
- Se o problema envolver banco, considere o impacto do bootstrap automático e da seed inicial.
- Evite duplicar lógica entre frontend e backend; mantenha os contratos claros e simples.