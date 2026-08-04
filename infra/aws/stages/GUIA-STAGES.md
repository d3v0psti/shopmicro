# Guia dos stages AWS

Cada stage é uma etapa reproduzível do ShopMicro e possui quatro formas de uso:

```text
local → console → terraform → validações
```

## Estrutura obrigatória

```text
NN-nome-do-stage/
├── STAGE.md
├── local/
├── console/
├── terraform/
└── validacoes/
```

- `local/`: comprova que a aplicação funciona sem credenciais AWS.
- `console/`: criação manual dos recursos para aprendizado.
- `terraform/`: reprodução automatizada da arquitetura validada.
- `validacoes/`: mesmas evidências funcionais para os três modos.

## Ciclo de conclusão

1. Validar localmente.
2. Criar e validar a AWS pelo Console.
3. Remover os recursos manuais.
4. Criar e validar a AWS pelo Terraform.
5. Executar `terraform destroy`.
6. Atualizar documentação e evidências.
7. Apresentar as evidências e solicitar aprovação explícita do autor.
8. Somente após a aprovação, criar e publicar a tag indicada em `STAGE.md`.

Concluir as validações não autoriza automaticamente a criação da tag. Criar,
publicar, mover ou excluir uma tag exige aprovação explícita do autor. Tags
publicadas são imutáveis. Uma correção posterior recebe um sufixo, como
`shopmicro-aws-stage-01.1`; a tag original não deve ser movida.

## Executar um stage antigo

Depois que a tag estiver publicada:

```bash
git clone https://github.com/d3v0psti/shopmicro.git
cd shopmicro
git switch --detach shopmicro-aws-stage-01
```

Em seguida, siga o `STAGE.md` daquela versão. Isso preserva código, Dockerfiles,
Compose, Terraform e documentação no ponto em que o stage foi validado.

Stages antigos são material de estudo. Dependências externas podem deixar de
receber correções de segurança; não trate uma tag histórica como implantação de
produção sem atualizar e revalidar seus componentes.
