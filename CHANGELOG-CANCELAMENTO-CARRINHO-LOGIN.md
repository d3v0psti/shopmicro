# Mudanças — Cancelamento de pedido, dados completos, carrinho como drawer, login sem abas

## 1. Cancelar pedido (pra excluir a conta sem problema)

- **Backend**: novo endpoint `POST /api/v1/orders/{id}/cancel`. Valida que
  o pedido não está `Shipped` (pedido já enviado não pode ser cancelado
  por aqui), marca como `Cancelled`, registra `CancelledAt`, e **devolve
  os itens ao estoque**.
- **Frontend**: modal "📦 Meus Pedidos" (acessível pelo botão no modal de
  perfil), lista os pedidos do cliente logado com botão "Cancelar
  Pedido" nos que ainda estão `Pending`/`Confirmed`.
- **Exclusão de conta**: a checagem antes de deletar agora ignora pedidos
  já `Cancelled` — só bloqueia se existir pedido **ativo**. Cancele os
  pedidos pendentes em "Meus Pedidos" e a exclusão passa a funcionar.

> Tecnicamente a exclusão de conta **nunca dependeu** de uma foreign key
> entre `Order` e `User` — o pedido já guarda só o e-mail do cliente, sem
> referência direta. O bloqueio é uma regra de negócio no frontend, não
> uma restrição do banco. Ou seja, mesmo sem cancelar nada, deletar a
> conta nunca quebraria o banco — a mudança aqui é só liberar o fluxo
> pretendido (cancelar antes de excluir).

## 2. Dados completos do pedido no banco

O `Order` ganhou os campos que já eram coletados no formulário de
checkout mas nunca chegavam no backend:

- `CustomerCpf`, `CustomerPhone`, `CustomerCep`, `CustomerAddress`
- `PaymentMethod` (pix / credit / boleto)
- `CancelledAt` (preenchido automaticamente ao cancelar)

Todos são **snapshot** — uma cópia dos dados no momento da compra, sem
referência viva à tabela de usuários. Isso significa que mesmo que o
cliente edite o perfil ou delete a conta depois, o histórico do pedido
continua íntegro e correto (é assim que e-commerce de verdade funciona:
a nota fiscal não muda se você trocar de endereço depois).

O `app.js` agora envia todos esses campos no `POST /api/v1/orders`.

## 3. Carrinho removido da tela inicial (agora é drawer, como Mercado Livre/Americanas)

- O `<aside class="cart-section">` deixou de ficar fixo ao lado da
  vitrine. Agora é um **drawer** (painel lateral) escondido por padrão,
  que desliza da direita quando o cliente clica no resumo do carrinho no
  header (`🛒 X itens | R$ ...`, que agora é um botão de verdade).
- A vitrine de produtos passa a ocupar a largura inteira da tela.
- Um overlay escurece o fundo quando o carrinho está aberto; clicar fora
  ou no ✕ fecha o drawer.

## 4. Login sem opção de criar conta dentro do "Entrar"

- Removidas as abas "Entrar na Conta" / "Criar Conta" que ficavam dentro
  do mesmo modal.
- Clicar em "🔑 Entrar" agora mostra **só** o formulário de login.
- A única forma de chegar no cadastro é o link "Cadastre-se aqui" no
  rodapé do formulário de login, que troca para o formulário de
  cadastro (mesma modal, sem abas visíveis).

## ⚠️ Ação necessária no banco antes de subir

O projeto usa `EnsureCreated()` no `Program.cs` (não usa Migrations do EF
Core). Isso quer dizer que ele **cria** as tabelas na primeira vez, mas
**não altera** tabelas que já existem. Como a tabela `orders` já existe
no seu RDS (do jeito antigo, sem os novos campos), você precisa de uma
dessas duas opções antes de subir esta versão:

**Opção A — recriar o banco do zero** (mais simples, mas apaga os pedidos
de teste que já existem):
```sql
DROP TABLE IF EXISTS order_items;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS products;
DROP TABLE IF EXISTS users;
```
Na próxima subida do backend, `EnsureCreated()` recria tudo do zero, já
com os novos campos, e repopula os produtos de exemplo.

**Opção B — alterar a tabela existente sem perder dados**:
```sql
ALTER TABLE orders
  ADD COLUMN "CustomerCpf" varchar(20),
  ADD COLUMN "CustomerPhone" varchar(20),
  ADD COLUMN "CustomerCep" varchar(10),
  ADD COLUMN "CustomerAddress" varchar(300),
  ADD COLUMN "PaymentMethod" varchar(30) NOT NULL DEFAULT 'pix',
  ADD COLUMN "CancelledAt" timestamp with time zone;
```

## Limpeza do repositório (não pedida, mas corrigida)

- Removidas as pastas duplicadas `infra/backend/` e `infra/frontend/`
  (cópias idênticas das pastas raiz, não usadas pelo build)
- Removida a pasta `obj/` (artefato de build do .NET) que estava
  commitada
- Criado o `.gitignore` que não existia no repositório
- `infra/.env` (que estava commitado com valores de placeholder) virou
  `infra/.env.managed-db.example` — copie para `.env` localmente e
  preencha com os valores reais, sem commitar
