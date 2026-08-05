# Guia dos stages AWS

Os stages do ShopMicro preservam o aprendizado da aplicação local e da criação
manual pelo Console AWS:

```text
local → console → validações
```

## Estrutura

```text
NN-nome-do-stage/
├── STAGE.md
├── local/
├── console/
└── validacoes/
```

O Terraform não é mais repetido em cada stage. A infraestrutura evolutiva está
no repositório independente e genérico `d3v0psti/terraform-aws-platform`,
dividido nas
camadas fixas `01-network`, `02-data`, `03-storage` e `04-compute`.

## Ordem

1. Validar a aplicação local.
2. Criar e validar a arquitetura pelo Console AWS.
3. Remover os recursos manuais.
4. Registrar as evidências.
5. Solicitar autorização para criar a tag.
6. Solicitar outra autorização antes de publicar a tag.

Tags publicadas são imutáveis. As tags antigas preservam o Terraform que fazia
parte dos stages naquele momento. Correções históricas usam um sufixo, como
`shopmicro-aws-stage-01.1`.
