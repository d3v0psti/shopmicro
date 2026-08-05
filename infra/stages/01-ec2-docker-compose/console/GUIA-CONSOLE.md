# Stage 01 — AWS pelo Console

Objetivo: executar aplicação, PostgreSQL e uploads na mesma EC2.

## Recursos

- VPC default e uma sub-rede pública
- IAM Role com `AmazonSSMManagedInstanceCore`
- Security Group
- EC2 Amazon Linux 2023 x86_64, `t3.micro`, EBS gp3 de 25 GiB

## 1. Criar

1. Selecione `us-east-1` e confirme a VPC default.
2. Crie a role `shopmicro-stage-01-ec2-role` para EC2 com a policy
   `AmazonSSMManagedInstanceCore`.
3. Crie `shopmicro-stage-01-sg` com:
   - TCP `80` a partir de `0.0.0.0/0`;
   - TCP `81` somente a partir de `SEU_IP/32`;
   - nenhuma entrada para `22`, `5432` ou `8080`.
4. Inicie uma EC2 com a AMI **Amazon Linux 2023 AMI** x86_64, tipo `t3.micro`,
   sem Key Pair, com IP público, volume raiz gp3 criptografado
   de 25 GiB e IMDSv2 obrigatório.
5. Associe a IAM Role e o Security Group criados.
6. Cole todo o conteúdo de [user-data.sh](user-data.sh) em **Advanced details →
   User data** e inicie a instância.
7. Aguarde `2/2 checks passed` e abra **Connect → Session Manager**.

## 2. Acompanhar

```bash
sudo tail -f /var/log/shopmicro-aws-stage-01.log
```

## 3. Validar

Execute `AWS — Console` em
[VALIDACOES.md](../validacoes/VALIDACOES.md).

## 4. Remover

Exclua a EC2 e confirme a remoção do volume raiz. Depois exclua o Security Group,
o instance profile e a IAM Role exclusivos do Stage 01.
