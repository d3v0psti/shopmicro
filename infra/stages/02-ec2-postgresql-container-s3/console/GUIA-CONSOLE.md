# Stage 02 — AWS pelo Console

Objetivo: manter aplicação e PostgreSQL na EC2 e transferir os uploads para um
bucket S3 privado.

## Recursos

- Recursos do Stage 01
- Bucket S3 privado, criptografado e versionado
- IAM Role com acesso mínimo a `uploads/*`
- S3 Gateway VPC Endpoint

## 1. Criar

1. Selecione `us-east-1` e confirme a VPC default, a sub-rede pública e sua
   tabela de rotas.
2. Crie um bucket globalmente único, como `shopmicro-stage-02-SEU-ID`:
   - ACLs desabilitadas;
   - Block Public Access integralmente habilitado;
   - versionamento habilitado;
   - criptografia SSE-S3.
3. Crie `shopmicro-stage-02-ec2-role` para EC2 com:
   - `AmazonSSMManagedInstanceCore`;
   - policy inline permitindo somente `s3:GetObject` e `s3:PutObject` em
     `arn:aws:s3:::NOME_DO_BUCKET/uploads/*`.
4. Crie o endpoint privado do S3:
   1. Abra **VPC → Endpoints → Create endpoint**.
   2. Use o nome `shopmicro-stage-02-s3-endpoint`.
   3. Em **Service category**, selecione **AWS services**.
   4. Procure por `com.amazonaws.us-east-1.s3` e selecione o serviço cujo tipo
      seja **Gateway**. Não selecione o tipo Interface.
   5. Selecione a mesma VPC usada pela EC2.
   6. Em **Route tables**, marque a tabela associada à sub-rede pública onde a
      EC2 será criada. O endpoint adicionará automaticamente uma rota do S3
      com destino iniciado por `pl-` nessa tabela.
   7. Em **Policy**, mantenha **Full access**. O endpoint também transportará
      downloads dos repositórios oficiais do Amazon Linux e das imagens do
      Docker Hub, que utilizam buckets S3 gerenciados por terceiros. A restrição
      efetiva do ShopMicro permanece na IAM Role do passo 3, limitada ao bucket
      do stage e ao prefixo `uploads/*`.
   8. Crie o endpoint e aguarde o status **Available**. Endpoint Gateway não
      utiliza Security Group.

   A policy do endpoint permite o tráfego, mas a IAM Role continua limitando o
   acesso da EC2 ao bucket do stage.
5. Crie `shopmicro-stage-02-sg` com TCP `80` público e TCP `81` limitado a
   `SEU_IP/32`. Não abra `22`, `5432` ou `8080`.
6. Abra [user-data.sh](user-data.sh), substitua `NOME_UNICO_DO_BUCKET` pelo nome
   real e copie todo o conteúdo.
7. Inicie uma EC2 com a AMI **Amazon Linux 2023 AMI** para arquitetura x86_64,
   tipo `t3.micro`, sem Key Pair, com IP público, EBS gp3 criptografado de
   25 GiB e IMDSv2 obrigatório.
8. Associe a role, o Security Group e o user data preparados.
9. Aguarde `2/2 checks passed` e conecte pelo Session Manager.

## 2. Acompanhar

```bash
sudo tail -f /var/log/shopmicro-aws-stage-02.log
```

## 3. Validar

Execute `AWS — Console ou Terraform` em
[VALIDACOES.md](../validacoes/VALIDACOES.md).

## 4. Remover

Exclua EC2 e EBS, esvazie e exclua o bucket, remova o VPC Endpoint, Security
Group, instance profile e IAM Role. Confira se nenhum recurso do stage permaneceu.
