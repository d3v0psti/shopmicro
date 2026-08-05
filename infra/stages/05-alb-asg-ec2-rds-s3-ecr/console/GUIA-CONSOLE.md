# Stage 05 — AWS pelo Console

Este roteiro mantém a arquitetura validada no Stage 04 e substitui a construção
das aplicações nas EC2 pelo download de três imagens privadas do Amazon ECR.

## 1. Criar os repositórios ECR persistentes

Em **Elastic Container Registry → Private registry → Repositories**, crie:

```text
shopmicro-backend
shopmicro-frontend
shopmicro-frontend-admin
```

Use em cada repositório:

- tag mutability: `Mutable`, necessária para sobrescrever `latest`;
- criptografia `AES-256` gerenciada pela AWS;
- scan on push habilitado quando a opção estiver disponível;
- repositório privado.

Crie também uma lifecycle policy para remover imagens **untagged** antigas após
sete dias. Ao publicar um novo `latest`, a imagem anterior perde a tag; essa
limpeza evita crescimento contínuo do armazenamento.

Esses três repositórios são recursos compartilhados e permanentes. Não os
recrie no Terraform e não os remova ao finalizar o stage.

## 2. Construir e publicar as imagens

Na sua máquina, configure AWS CLI para a conta do treinamento e mantenha Docker
em execução. A identidade usada para publicar precisa de permissão de push
somente nos três repositórios e de `ecr:GetAuthorizationToken`. Na raiz do
projeto:

```bash
infra/stages/05-alb-asg-ec2-rds-s3-ecr/console/publicar-imagens.sh all
```

O script descobre a conta autenticada, faz login no ECR e publica imagens
`linux/amd64` com tag `latest`. Para publicar somente uma aplicação:

```bash
infra/stages/05-alb-asg-ec2-rds-s3-ecr/console/publicar-imagens.sh backend
infra/stages/05-alb-asg-ec2-rds-s3-ecr/console/publicar-imagens.sh frontend
infra/stages/05-alb-asg-ec2-rds-s3-ecr/console/publicar-imagens.sh frontend-admin
```

No Console ECR, confirme `latest` nos três repositórios e anote os digests. Não
grave token do Docker, Access Key ou Secret Key no projeto.

## 3. Rede, S3, segredos e RDS

Repita a base do Stage 04 com nomes `shopmicro-stage-05-*`:

1. VPC default e duas sub-redes públicas em zonas diferentes.
2. Bucket privado, versionado e criptografado.
3. S3 Gateway Endpoint associado às tabelas de rotas das duas sub-redes, com
   policy Full access; a IAM Role restringirá o projeto a `uploads/*`.
4. Parâmetros SecureString:
   - `/shopmicro/stage-05/rds/password`;
   - `/shopmicro/stage-05/jwt/secret`.
5. RDS PostgreSQL 18 Single-AZ `db.t4g.micro`, gp3 de 20 GiB, criptografado,
   banco `shopdb`, usuário `shopadmin` e `Public access: No`.

## 4. Security Groups

1. `shopmicro-stage-05-alb-sg`:
   - TCP 80 de `0.0.0.0/0`;
   - TCP 81 somente de `SEU_IP/32`.
2. `shopmicro-stage-05-ec2-sg`:
   - TCP 80 e 81 somente de `shopmicro-stage-05-alb-sg`;
   - nenhuma entrada 22, 5432 ou 8080.
3. `shopmicro-stage-05-rds-sg`:
   - TCP 5432 somente de `shopmicro-stage-05-ec2-sg`.

## 5. IAM Role das EC2

Crie `shopmicro-stage-05-ec2-role` com:

- policy gerenciada `AmazonSSMManagedInstanceCore`;
- `s3:GetObject` e `s3:PutObject` somente no prefixo `uploads/*` do bucket;
- `ssm:GetParameter` somente nos dois parâmetros do Stage 05;
- `ecr:GetAuthorizationToken` com recurso `*`;
- `ecr:BatchCheckLayerAvailability`, `ecr:BatchGetImage` e
  `ecr:GetDownloadUrlForLayer` somente nos três repositórios ShopMicro.

Essa role permite pull, não permite push nem exclusão de repositórios.

## 6. Target groups e ALB

1. Target group `shopmicro-stage-05-market`: Instances, HTTP 80, health check
   `/health/ready`.
2. Target group `shopmicro-stage-05-admin`: Instances, HTTP 81, health check
   `/`, sucesso `200-399`.
3. ALB público `shopmicro-stage-05-alb`, nas duas sub-redes e com o SG do ALB.
4. Listener `HTTP:80` encaminhando para o target group do frontend.
5. Em **Listeners and rules → Add listener**, crie `HTTP:81` encaminhando para
   o target group do frontend-admin.

## 7. Launch Template e ASG

1. Ajuste `S3_BUCKET_NAME` e `RDS_HOST` em [user-data.sh](user-data.sh).
2. Crie o Launch Template com:
   - Amazon Linux 2023 x86_64, `t3.micro` e IP público;
   - sem Key Pair e com IMDSv2 obrigatório;
   - EBS gp3 criptografado de 8 GiB;
   - IAM Role e Security Group das EC2;
   - o user data ajustado.
3. Crie o ASG nas duas sub-redes, associe ambos os target groups e configure:
   - `min=2`, `desired=2`, `max=2`;
   - health check `ELB`;
   - grace period `900` segundos.
4. Aguarde duas instâncias saudáveis em cada target group.

Acompanhe uma instância pelo Session Manager:

```bash
sudo tail -f /var/log/shopmicro-aws-stage-05.log
```

## 8. Atualizar uma imagem `latest`

Publique a aplicação desejada novamente. Como a tag continua igual, instâncias
em execução não mudam sozinhas. Neste treinamento, abra o ASG e execute
**Instance refresh** para substituir as EC2 gradualmente. Cada nova instância
baixa os três `latest` atuais.

É possível testar apenas um container via SSM, mas isso deve ser feito nas duas
EC2 para evitar versões diferentes:

```bash
cd /opt/shopmicro/infra
ECR_REGISTRY="$(sudo awk -F= '$1 == "ECR_REGISTRY" { print $2 }' .env.aws-stage-05)"
aws ecr get-login-password --region us-east-1 | \
  sudo docker login --username AWS --password-stdin "$ECR_REGISTRY"
sudo docker compose --env-file .env.aws-stage-05 \
  -f compose.aws-stage-05.yaml pull frontend
sudo docker compose --env-file .env.aws-stage-05 \
  -f compose.aws-stage-05.yaml up --no-build --no-deps -d frontend
unset ECR_REGISTRY
```

Para backend, prefira Instance refresh: os frontends mantêm conexão com o nome
do container e também devem reiniciar de forma coordenada.

## 9. Validar e remover

Execute [VALIDACOES.md](../validacoes/VALIDACOES.md). Ao terminar, remova ASG,
Launch Template, ALB, target groups, RDS, bucket e demais recursos do stage.

**Não remova os três repositórios ECR.** Eles serão reutilizados.
