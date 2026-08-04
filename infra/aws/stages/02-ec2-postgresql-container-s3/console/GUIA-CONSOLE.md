# Stage 02 — AWS pelo Console

Objetivo: executar a aplicação e PostgreSQL na EC2, armazenando imagens em um
bucket S3 privado.

## Recursos

- Recursos do Stage 01
- Bucket S3 privado, criptografado e versionado
- IAM Role com acesso mínimo a `uploads/*`
- S3 Gateway VPC Endpoint

## Passos

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
4. Crie um endpoint do tipo `Gateway` para
   `com.amazonaws.us-east-1.s3`, associado à VPC e à tabela de rotas escolhidas.
   Restrinja sua policy ao mesmo prefixo `uploads/*` do bucket.
5. Crie `shopmicro-stage-02-sg` com TCP `80` público e TCP `81` limitado a
   `SEU_IP/32`. Não abra `22`, `5432` ou `8080`.
6. Abra [user-data.sh](user-data.sh), substitua `NOME_UNICO_DO_BUCKET` pelo nome
   real e copie todo o conteúdo.
7. Inicie uma EC2 Ubuntu 24.04 `t3.small`, sem Key Pair, com IP público, EBS gp3
   criptografado de 25 GiB e IMDSv2 obrigatório.
8. Associe a role, o Security Group e o user data preparados.
9. Aguarde `2/2 checks passed` e conecte pelo Session Manager.

## Acompanhar o bootstrap

```bash
sudo tail -f /var/log/shopmicro-aws-stage-02.log
```

Execute a seção `AWS — Console ou Terraform` de
[VALIDACOES.md](../validacoes/VALIDACOES.md), incluindo a evidência no S3.

## Remoção

Exclua EC2 e EBS, esvazie e exclua o bucket, remova o VPC Endpoint, Security
Group, instance profile e IAM Role. Confira se nenhum recurso do stage permaneceu.
