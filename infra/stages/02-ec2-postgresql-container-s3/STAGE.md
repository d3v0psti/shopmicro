# Stage 02 — EC2 com PostgreSQL em container e S3

- Status: `VALIDADO em 2026-08-04`
- Validações concluídas: local, Console AWS e Terraform
- Tag: `shopmicro-aws-stage-02`
- Banco: PostgreSQL 18 em container
- Imagens locais: volume Docker
- Imagens na AWS: bucket S3 privado via Gateway VPC Endpoint
- Administração AWS: Systems Manager Session Manager

## Ordem do laboratório

1. [Executar localmente](local/GUIA-LOCAL.md)
2. [Criar pelo Console AWS](console/GUIA-CONSOLE.md)
3. [Criar pelo Terraform](terraform/GUIA-TERRAFORM.md)
4. [Registrar as validações](validacoes/VALIDACOES.md)

O modo local permanece independente da AWS. O provider S3 é ativado somente nas
execuções AWS.
