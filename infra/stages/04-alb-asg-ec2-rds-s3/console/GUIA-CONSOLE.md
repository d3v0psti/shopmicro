# Stage 04 — AWS pelo Console

Objetivo: distribuir o ShopMicro em duas EC2, uma por zona de disponibilidade,
atrás de um Application Load Balancer. O banco fica no RDS PostgreSQL 18 e os
uploads em um bucket S3 privado.

## 1. Rede e S3

1. Selecione `us-east-1` e use a VPC default neste stage.
2. Escolha duas sub-redes públicas em zonas de disponibilidade diferentes.
3. Crie um bucket globalmente único, como `shopmicro-stage-04-SEU-ID`, com:
   - ACLs desabilitadas e Block Public Access integral;
   - versionamento habilitado;
   - criptografia SSE-S3.
4. Crie o S3 Gateway Endpoint `shopmicro-stage-04-s3-endpoint` para
   `com.amazonaws.us-east-1.s3`. Associe-o à VPC e às tabelas de rotas das duas
   sub-redes. Mantenha a policy **Full access**: a IAM Role limitará o projeto a
   `uploads/*` e isso evita bloquear repositórios do Amazon Linux e camadas do
   Docker Hub que também usam S3.

## 2. Security Groups

Crie nesta ordem:

1. `shopmicro-stage-04-alb-sg`:
   - TCP 80 a partir de `0.0.0.0/0`;
   - TCP 81 somente a partir de `SEU_IP/32`;
   - saída liberada.
2. `shopmicro-stage-04-ec2-sg`:
   - TCP 80 com origem em `shopmicro-stage-04-alb-sg`;
   - TCP 81 com origem em `shopmicro-stage-04-alb-sg`;
   - nenhuma entrada para 22, 5432 ou 8080;
   - saída liberada.
3. `shopmicro-stage-04-rds-sg`:
   - TCP 5432 com origem em `shopmicro-stage-04-ec2-sg`;
   - sem acesso público.

## 3. Segredos e RDS

1. No CloudShell, gere dois valores diferentes:

   ```bash
   openssl rand -hex 24
   openssl rand -hex 32
   ```

2. No Parameter Store, crie dois parâmetros `Standard` do tipo `SecureString`:
   - `/shopmicro/stage-04/rds/password`: primeiro valor;
   - `/shopmicro/stage-04/jwt/secret`: segundo valor.
3. Crie `shopmicro-stage-04-db-subnet-group` com as duas sub-redes escolhidas.
4. Crie o RDS:
   - PostgreSQL 18, Single-AZ e `db.t4g.micro`;
   - identificador `shopmicro-stage-04-postgres`;
   - usuário `shopadmin`, banco inicial `shopdb` e senha do Parameter Store;
   - gp3 de 20 GiB, criptografado;
   - `Public access: No`;
   - DB subnet group e `shopmicro-stage-04-rds-sg`;
   - backups e deletion protection desabilitados somente para o treinamento.
5. Aguarde **Available** e copie o endpoint sem `:5432`.

O mesmo `JWT_SECRET` será lido pelas duas EC2. Isso mantém as sessões válidas
quando o ALB alternar as requisições entre instâncias.

## 4. IAM Role

Crie `shopmicro-stage-04-ec2-role` para EC2 com:

- policy gerenciada `AmazonSSMManagedInstanceCore`;
- `s3:GetObject` e `s3:PutObject` somente em
  `arn:aws:s3:::NOME_DO_BUCKET/uploads/*`;
- `ssm:GetParameter` somente nos dois parâmetros criados.

Não crie Key Pair nem abra a porta 22. O acesso administrativo será feito pelo
Session Manager.

## 5. Target groups e ALB

1. Crie dois target groups do tipo **Instances**, sem registrar instâncias:
   - `shopmicro-stage-04-market`: HTTP 80, health check `/health/ready`;
   - `shopmicro-stage-04-admin`: HTTP 81, health check `/`, sucesso `200-399`.
2. Em **EC2 → Load Balancers → Create load balancer**, crie um Application
   Load Balancer público chamado `shopmicro-stage-04-alb`:
   - esquema `Internet-facing` e endereço `IPv4`;
   - nas duas sub-redes escolhidas;
   - com `shopmicro-stage-04-alb-sg`;
   - em **Listeners and routing**, configure `HTTP : 80` encaminhando para
     `shopmicro-stage-04-market`.
3. Finalize a criação e aguarde o ALB ficar `Active`.
4. Abra o ALB criado e acesse **Listeners and rules → Add listener**:
   - protocolo `HTTP`;
   - porta `81`;
   - ação padrão `Forward to target groups`;
   - target group `shopmicro-stage-04-admin`;
   - salve o listener.
5. Confirme que o ALB exibe exatamente estes dois listeners:

   | Listener do ALB | Destino | Serviço nas EC2 |
   |---|---|---|
   | `HTTP:80` | `shopmicro-stage-04-market` | `frontend` na porta 80 |
   | `HTTP:81` | `shopmicro-stage-04-admin` | `frontend-admin` na porta 81 |

Não crie uma regra de caminho para unir os frontends no listener 80. Neste
stage, cada interface possui sua própria porta e seu próprio listener.

## 6. Launch Template e Auto Scaling Group

1. Abra [user-data.sh](user-data.sh) e substitua:
   - `NOME_UNICO_DO_BUCKET`;
   - `ENDPOINT_PRIVADO_DO_RDS`.
2. Crie `shopmicro-stage-04-launch-template`:
   - Amazon Linux 2023 x86_64 e `t3.micro`;
   - sem Key Pair;
   - IAM Role e `shopmicro-stage-04-ec2-sg`;
   - IP público habilitado;
   - EBS gp3 criptografado de 8 GiB;
   - IMDSv2 obrigatório;
   - conteúdo ajustado de `user-data.sh` no campo User data.
3. Crie `shopmicro-stage-04-asg`:
   - selecione as duas sub-redes escolhidas;
   - associe os dois target groups;
   - health check `ELB` e grace period de `900` segundos;
   - capacidade mínima `2`, desejada `2` e máxima `2`.
4. Aguarde duas instâncias **Healthy** em ambos os target groups.

## 7. Acessar e validar

- Marketplace: `http://DNS_DO_ALB`
- Admin: `http://DNS_DO_ALB:81`

Execute a seção `AWS — Console ou Terraform` em
[VALIDACOES.md](../validacoes/VALIDACOES.md).

## 8. Remover

Remova nesta ordem: ASG, Launch Template, ALB, target groups, RDS, DB subnet
group, parâmetros, objetos e bucket, endpoint, Security Groups e IAM Role.
Confirme que não restaram EC2, volumes, snapshots ou backups cobrados.
