# Stage 03 — AWS pelo Console

Objetivo: executar os três containers da aplicação na EC2, PostgreSQL 18 no RDS
privado e uploads no S3 privado.

## 1. Rede e armazenamento

1. Selecione `us-east-1` e use a VPC default neste stage.
2. Identifique pelo menos duas sub-redes em zonas de disponibilidade diferentes.
3. Crie um bucket globalmente único, como `shopmicro-stage-03-SEU-ID`, com:
   - ACLs desabilitadas e Block Public Access integral;
   - versionamento habilitado;
   - criptografia SSE-S3.
4. Crie um S3 Gateway Endpoint chamado `shopmicro-stage-03-s3-endpoint` para
   `com.amazonaws.us-east-1.s3`, associado à VPC e à tabela de rotas da sub-rede
   da EC2. Mantenha a policy **Full access** para não bloquear os repositórios do
   Amazon Linux e as imagens do Docker Hub. O acesso do projeto será limitado
   pela IAM Role.

## 2. Security Groups

1. Crie `shopmicro-stage-03-ec2-sg`:
   - TCP 80 a partir de `0.0.0.0/0`;
   - TCP 81 somente a partir de `SEU_IP/32`;
   - nenhuma entrada para 22, 5432 ou 8080;
   - saída liberada.
2. Crie `shopmicro-stage-03-rds-sg` sem acesso público:
   - entrada TCP 5432 cuja **origem seja o Security Group da EC2**;
   - saída padrão.

Não use seu IP nem `0.0.0.0/0` como origem da porta 5432.

## 3. Senha e RDS

1. Gere uma senha forte no AWS CloudShell sem publicá-la em logs ou evidências:

   ```bash
   openssl rand -hex 24
   ```

2. Em **Systems Manager → Parameter Store**, crie:
   - nome: `/shopmicro/stage-03/rds/password`;
   - tier: `Standard`;
   - tipo: `SecureString`;
   - valor: a senha gerada.
3. Em **RDS → Subnet groups**, crie `shopmicro-stage-03-db-subnet-group` usando
   a VPC e pelo menos duas sub-redes em zonas diferentes.
4. Crie o banco em **RDS → Databases → Create database**:
   - Standard create; PostgreSQL 18;
   - Single-AZ e classe `db.t4g.micro`;
   - identificador `shopmicro-stage-03-postgres`;
   - usuário mestre `shopadmin` e a mesma senha do Parameter Store;
   - banco inicial `shopdb`;
   - armazenamento gp3 de 20 GiB, criptografado;
   - `Public access: No`;
   - DB subnet group e `shopmicro-stage-03-rds-sg`;
   - backups e deletion protection desabilitados somente para este treinamento.
5. Aguarde o status **Available** e copie apenas o endpoint, sem `:5432`.

## 4. IAM e EC2

1. Crie `shopmicro-stage-03-ec2-role` para EC2 com:
   - `AmazonSSMManagedInstanceCore`;
   - `s3:GetObject` e `s3:PutObject` somente em
     `arn:aws:s3:::NOME_DO_BUCKET/uploads/*`;
   - `ssm:GetParameter` somente no parâmetro
     `/shopmicro/stage-03/rds/password`.
2. Abra [user-data.sh](user-data.sh) e substitua:
   - `NOME_UNICO_DO_BUCKET` pelo bucket;
   - `ENDPOINT_PRIVADO_DO_RDS` pelo endpoint copiado.
3. Inicie uma EC2 Amazon Linux 2023 x86_64 `t3.micro`, sem Key Pair, com IP
   público, EBS gp3 criptografado de 25 GiB e IMDSv2 obrigatório.
4. Associe a IAM Role, `shopmicro-stage-03-ec2-sg` e o user data.
5. Conecte somente pelo Session Manager e acompanhe:

   ```bash
   sudo tail -f /var/log/shopmicro-aws-stage-03.log
   ```

## 5. Validar

Execute `AWS — Console` em
[VALIDACOES.md](../validacoes/VALIDACOES.md).

## 6. Remover

Remova EC2/EBS, RDS, DB subnet group, parâmetro SecureString, bucket, endpoint,
Security Groups, instance profile e IAM Role. Confirme que não restaram snapshots
nem backups cobrados.
