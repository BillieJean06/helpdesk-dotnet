# helpdesk-dotnet

[![CI](https://github.com/BillieJean06/helpdesk-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/BillieJean06/helpdesk-dotnet/actions/workflows/ci.yml)

Sistema de tickets de suporte construído para **aprender .NET e DDD na prática**, com back-end em ASP.NET Core e front-end em React + TypeScript.

> Projeto de estudo, em evolução. O histórico de commits é parte da documentação: cada passo é pequeno e descreve a mudança real.

## O domínio

Alguém abre um chamado, ele entra numa fila, um atendente assume, conversa com quem abriu, resolve, e o chamado é fechado. Por trás disso existem prazo (SLA), prioridade, histórico e permissões.

Um único bounded context, **Suporte**, com o agregado raiz `Ticket`.

### Máquina de estados

```
Aberto -> EmAtendimento -> AguardandoCliente -> EmAtendimento
                        -> Resolvido -> Fechado
Resolvido -> Reaberto -> EmAtendimento
Fechado   -> Reaberto  (reabertura formal, volta à fila sem responsável)
```

As transições vivem dentro do agregado (`Assumir()`, `Resolver()`, `Reabrir()`), nunca em quem chama. Ninguém troca o campo `status` de fora.

### SLA

Cada ticket tem dois prazos, definidos pela prioridade na abertura (valores genéricos, inventados para o estudo):

| Prioridade | Primeira resposta | Resolução |
|---|---|---|
| Crítica | 15 min | 4 h |
| Alta | 1 h | 8 h |
| Média | 4 h | 24 h |
| Baixa | 8 h | 72 h |

Regras:

- **Primeira resposta** é o primeiro comentário de quem não é o solicitante, ou o ato de pedir informação ao cliente, ou resolver. Só `Assumir` não conta.
- **Pausa:** em `AguardandoCliente` o relógio congela. Ao retomar, o tempo pausado é devolvido ao prazo de resolução.
- **Violação é permanente:** resolver ou responder fora do prazo continua registrado como violado depois.
- **Reabertura** reinicia os dois prazos a partir do momento da reabertura.
- Os prazos ficam **gravados no ticket** (`PrazosSla`); mudar a `PoliticaSla` depois não altera tickets já abertos.
- Tempo corrido, sem horário comercial (possível evolução).

### Decisões de modelagem

| Decisão                                              | Motivo                                                                    |
| ---------------------------------------------------- | ------------------------------------------------------------------------- |
| `Comentario` é **entidade filha** do `Ticket`        | Tem autor, data e ordem; não é intercambiável como um Value Object        |
| SLA é uma **`PoliticaSla`** separada de `Prioridade` | Prioridade classifica; o prazo é regra e pode variar por cliente ou plano |
| Domínio recebe **`agora`** por parâmetro             | O `TimeProvider` fica na Application; nos testes, `FakeTimeProvider` controla o relógio |
| Solicitante e atendente entram só como **`Guid`**    | Identidade é outro contexto; o domínio não navega para `User`             |
| **`TenantId`** no ticket desde o início              | Adicionar multi-tenancy depois, com dados no banco, custa muito mais      |

## Arquitetura

Monolito modular, sem camadas por cerimônia.

```
src/
  Helpdesk.Domain          agregados, value objects, eventos (sem dependências)
  Helpdesk.Application     casos de uso
  Helpdesk.Infrastructure  EF Core, repositórios
  Helpdesk.Api             ASP.NET Core, autenticação, policies
tests/
  Helpdesk.Domain.Tests    testes unitários puros, sem banco
```

Regra de dependência: `Api -> Infrastructure -> Application -> Domain`. O `Domain` não referencia ninguém.

## Como rodar

Requisitos: [.NET SDK 8](https://dotnet.microsoft.com/download).

```bash
dotnet build
dotnet test
dotnet run --project src/Helpdesk.Api
```

Configurações sensíveis (connection strings, chaves) ficam em `appsettings.Development.json` ou user-secrets, ambos fora do versionamento.

## Roadmap

- [x] Estrutura da solution
- [x] Agregado `Ticket` com máquina de estados e testes
- [x] Cálculo de SLA (primeira resposta e resolução) com pausa em `AguardandoCliente`
- [ ] Persistência com EF Core
- [ ] API com autenticação e policies por papel (cliente, atendente, supervisor)
- [ ] Front-end React + TypeScript (Vite, TanStack Query)
- [ ] Domain Events (`TicketResolvido`, `TicketReaberto`)
- [ ] Outbox Pattern
- [ ] Tempo real com SignalR
- [ ] Verificação de SLA vencido em `BackgroundService`
- [ ] Multi-tenancy

## Licença

[MIT](LICENSE)
