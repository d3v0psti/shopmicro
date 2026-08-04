# Stage 04 — ALB, duas EC2, RDS e S3

- Status: `PLANEJADO — preparação pendente`
- Tag planejada: `shopmicro-aws-stage-04`
- Banco local: PostgreSQL 18 em container
- Banco na AWS: RDS PostgreSQL 18 privado
- Uploads na AWS: bucket S3 privado
- Aplicação na AWS: ALB e duas EC2 em zonas diferentes

## Objetivo

Adicionar alta disponibilidade à camada da aplicação sem alterar RDS e S3.

## Arquitetura planejada

```text
Internet
   │
   ▼
Application Load Balancer
   ├── EC2 A — Zona A
   └── EC2 B — Zona B
          │
          ├── RDS PostgreSQL 18 privado
          └── Bucket S3 privado
```

## Decisões

- Auto Scaling Group com `min=2`, `desired=2` e `max=2`.
- Duas sub-redes em zonas de disponibilidade diferentes.
- ALB público; EC2 aceita tráfego web somente do Security Group do ALB.
- Listener 80 para o marketplace e listener 81 restrito ao IP administrativo.
- Um target group para cada frontend, com health checks.
- RDS Single-AZ e S3 permanecem como no Stage 03.
- Senha do RDS e `JWT_SECRET` compartilhados via Parameter Store.
- Cada EC2 ainda constrói os containers no user data.

O JWT precisa ser igual nas duas EC2. Se cada instância gerar seu próprio
segredo, uma sessão autenticada poderá falhar quando o ALB encaminhar a próxima
requisição para o outro servidor.

Para evitar o custo de NAT Gateway neste momento, as EC2 poderão permanecer em
sub-redes públicas, sem portas de entrada diretas e administradas somente por
Session Manager. Uma evolução posterior poderá movê-las para sub-redes privadas.

## Validação planejada

1. Confirmar duas EC2 saudáveis em zonas diferentes.
2. Acessar marketplace e painel somente pelo DNS do ALB.
3. Encerrar uma EC2 e confirmar substituição pelo ASG.
4. Confirmar aplicação disponível durante a substituição.
5. Validar persistência compartilhada no RDS e no S3.

Os arquivos executáveis serão preparados a partir da tag validada do Stage 03.
