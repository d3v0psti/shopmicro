# Guia dos stages AWS

Cada stage preserva uma versão funcional do ShopMicro. Siga sempre esta ordem:

```text
local → console → terraform → validações
```

## O que existe em cada stage

```text
NN-nome-do-stage/
├── STAGE.md
├── local/
├── console/
├── terraform/
└── validacoes/
```

- `STAGE.md`: objetivo, arquitetura e status.
- `local/`: execução sem credenciais AWS.
- `console/`: criação manual na AWS.
- `terraform/`: criação automatizada da mesma arquitetura.
- `validacoes/`: testes e evidências dos três modos.

## Ordem obrigatória

1. Validar localmente.
2. Criar e validar a AWS pelo Console.
3. Remover os recursos manuais.
4. Criar e validar a AWS pelo Terraform.
5. Executar `terraform destroy`.
6. Atualizar documentação e evidências.
7. Registrar as evidências.
8. Solicitar autorização para criar a tag.
9. Solicitar outra autorização antes de publicar a tag.

Validação e autorização de tag são decisões separadas. Tags publicadas são
imutáveis. Correções posteriores usam um sufixo, como
`shopmicro-aws-stage-01.1`.

## Executar um stage antigo

Depois que a tag estiver publicada:

```bash
git clone https://github.com/d3v0psti/shopmicro.git
cd shopmicro
git switch --detach shopmicro-aws-stage-01
```

Depois, abra o `STAGE.md` e siga os links na ordem indicada.

Stages antigos são material de estudo. Dependências externas podem deixar de
receber correções de segurança; não trate uma tag histórica como implantação de
produção sem atualizar e revalidar seus componentes.
