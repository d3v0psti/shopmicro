# Stage 03 — EC2, RDS PostgreSQL e S3

- Status: `VALIDADO em 2026-08-04 — tag pendente de aprovação`
- Validações concluídas: local e Console AWS
- Tag planejada: `shopmicro-aws-stage-03`
- Aplicação: containers construídos na EC2
- Banco local: PostgreSQL 18 em container
- Banco na AWS: RDS PostgreSQL 18 privado
- Imagens locais: volume Docker
- Imagens na AWS: bucket S3 privado via Gateway VPC Endpoint
- Administração AWS: Systems Manager Session Manager

## Como executar

1. [Executar localmente](local/GUIA-LOCAL.md)
2. [Criar pelo Console AWS](console/GUIA-CONSOLE.md)
3. [Registrar as validações](validacoes/VALIDACOES.md)

O modo local não exige credenciais AWS. Na AWS, o PostgreSQL deixa a EC2 e
passa a ser gerenciado pelo RDS. A senha do banco fica em um parâmetro
`SecureString` do Parameter Store e não é gravada diretamente no user data.

A tag planejada ainda exige aprovação explícita do autor.
