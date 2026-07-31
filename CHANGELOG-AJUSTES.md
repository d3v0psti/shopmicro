# Ajustes aplicados — ShopMicro (Docker local + Kubernetes local + qualquer nuvem)

## 🔴 Crítico — corrigido

**Build quebrado por migration órfã**
`Migrations/20260724000000_AddUsersTable.Designer.cs` existia sem o
arquivo principal da migration correspondente. Essa classe parcial ficava
sem herdar de `Migration`, e o projeto não compilava. Como o `Program.cs`
usa `db.Database.EnsureCreated()` (não usa `Migrate()`), a pasta
`Migrations/` inteira estava morta — **removi a pasta inteira**. Isso por
si só provavelmente já resolve o erro de build que você teria no primeiro
`docker build`.

## 🟠 Quebrava especificamente em Kubernetes

**Nginx com hostname do backend fixo no código**
`proxy_pass http://backend:8080` funcionava só em Docker Compose (onde o
serviço se chama `backend`). Troquei para um **template** processado pelo
próprio entrypoint oficial do Nginx (`envsubst`, nativo, sem script
extra):
- `frontend/default.conf` → `frontend/templates/default.conf.template`,
  usando `proxy_pass ${BACKEND_UPSTREAM};`
- `BACKEND_UPSTREAM` tem um valor padrão no Dockerfile
  (`http://backend:8080`, que já cobre o Docker Compose local sem precisar
  de nada extra) e é sobrescrito via variável de ambiente no Deployment do
  Kubernetes (`http://backend-service:8080`).

**Deployment do frontend desatualizado**
`infra/k8s/04-frontend.yaml` ainda descrevia a versão antiga do frontend
(Node.js, porta 3000, variáveis `PORT`/`BACKEND_URL`). Reescrevi para
bater com o Nginx real: porta 80, variável `BACKEND_UPSTREAM`, e os
probes agora batem com endpoints que realmente existem (veja abaixo).

**Nginx sem endpoints de health check**
Os probes do Kubernetes apontavam para `/health/live` e `/health/ready`,
que não existiam na configuração do Nginx — os pods nunca ficariam
"Ready". Adicionei os dois endpoints no template do Nginx (retornam `200
ok` diretamente, sem tocar no backend).

## 🟡 Limpeza / consistência

**Frontend: código morto removido**
O `Dockerfile` tinha um estágio Node.js (`FROM node:20-alpine AS
builder`) que **nunca era usado** — o estágio final do Nginx copiava os
arquivos direto do contexto de build, não do builder. Removi:
- `frontend/src/server.js` (Express, não rodava mais)
- `frontend/package.json`
- `frontend/.env.example`
- O estágio "builder" do Dockerfile (agora é single-stage, só Nginx)

Se você quiser voltar a ter um BFF em Node.js de verdade no meio do
caminho (proxy com lógica própria, sessão, etc.), me avisa que eu
reintroduzo — mas hoje ele não fazia nada, então mantive só o que
realmente roda.

**Dois sistemas de login coexistindo**
Havia `AuthController` (JWT, rota `/api/v1/auth/*`, com uma chave secreta
fixa no código) e `UsersController` (rota `/api/users/*`). Conferi o
`app.js` — o frontend só chama `/api/users/*`. **Removi o
`AuthController`** por estar morto (e, de brinde, isso elimina o JWT
secret hardcoded, já que o JWT nem estava em uso de verdade). Removi
também os pacotes `System.IdentityModel.Tokens.Jwt` e
`Microsoft.AspNetCore.Authentication.JwtBearer` do `.csproj`, já sem uso.

> Vale lembrar (já tínhamos comentado isso antes): o `UsersController`
> ainda compara senha em texto puro (`PasswordHash != dto.Password`). Não
> mexi nisso agora porque não foi o que você pediu, mas se for expor essa
> aplicação publicamente por mais tempo, vale trocar por hash (BCrypt, por
> exemplo) — é rápido de ajustar, me chama quando quiser.

**`docker-compose-aws.yml` específico demais**
Fixava `SSL Mode=VerifyFull` e um caminho de certificado da AWS RDS
(`global-bundle.pem`) direto no compose — não servia pra Azure Database
ou Cloud SQL, que usam certificados diferentes. Substituí por
`infra/docker-compose-managed-db.yml`, genérico:
- A string de conexão inteira vem de fora via `.env` (veja
  `infra/.env.managed-db.example`)
- Uma pasta `infra/certs/` é montada no container — solta ali o
  certificado raiz do provedor que você escolher (AWS, Azure ou GCP) e
  referencie o caminho dentro do `DB_CONNECTION_STRING`
- Comentários no próprio arquivo mostram o formato da connection string
  para os três provedores

**Namespace do Kubernetes**
Renomeei `tasks-app` → `shopmicro` em todos os manifests (resquício do
nome antigo do projeto, antes de virar e-commerce).

**`.gitignore` criado**
A pasta `backend/src/Api/obj/` (artefato de build do .NET) estava
commitada no repositório. Adicionei um `.gitignore` cobrindo
`bin/`/`obj/`, `node_modules/`, arquivos `.env` e certificados.

> **Ação que você precisa tomar manualmente**: como o `obj/` já foi
> commitado antes do `.gitignore` existir, rodar `git add .gitignore` não
> remove ele do histórico atual. Rode isto uma vez, depois de aplicar
> estas mudanças:
> ```bash
> git rm -r --cached backend/src/Api/obj
> git add .
> git commit -m "Remove artefatos de build e aplica .gitignore"
> ```

## Como testar

**Docker Compose local:**
```bash
cd infra
docker compose -f docker-compose-local.yaml up --build
```
Acesse `http://localhost` (porta 80 agora, não mais 3000).

**Docker Compose com banco gerenciado (RDS/Azure/Cloud SQL):**
```bash
cd infra
cp .env.managed-db.example .env
# edite o .env com sua connection string real
# coloque o certificado do seu provedor em ./certs/
docker compose -f docker-compose-managed-db.yml up --build
```

**Kubernetes (local — Kind/Minikube/k3d — ou qualquer cloud):**
```bash
# builde e publique as imagens antes (troque pelo seu registry)
docker build -t SEU_REGISTRY/shopmicro-backend:latest ./backend
docker build -t SEU_REGISTRY/shopmicro-frontend:latest ./frontend
docker push SEU_REGISTRY/shopmicro-backend:latest
docker push SEU_REGISTRY/shopmicro-frontend:latest

# ajuste "image:" em infra/k8s/03-backend.yaml e 04-frontend.yaml

kubectl apply -f infra/k8s/00-namespace.yaml
kubectl apply -f infra/k8s/01-config-secret.yaml
kubectl apply -f infra/k8s/02-postgres.yaml
kubectl apply -f infra/k8s/03-backend.yaml
kubectl apply -f infra/k8s/04-frontend.yaml
kubectl apply -f infra/k8s/05-ingress.yaml

kubectl get pods -n shopmicro
```
Os pods do frontend e do backend devem ficar `Running` com `READY 1/1` —
se o frontend não ficar Ready, confira `kubectl logs -n shopmicro
<pod-frontend>` e `kubectl describe pod` pra ver o resultado das probes.

Isso funciona sem alteração em EKS, AKS ou GKE — a única diferença entre
eles é de onde vem o `StorageClass` (para o volume do Postgres, se você
não estiver usando um banco gerenciado) e o Ingress Controller que você
instalar no cluster.