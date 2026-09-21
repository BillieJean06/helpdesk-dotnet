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

### Decisões de modelagem

| Decisão                                              | Motivo                                                                    |
| ---------------------------------------------------- | ------------------------------------------------------------------------- |
| `Comentario` é **entidade filha** do `Ticket`        | Tem autor, data e ordem; não é intercambiável como um Value Object        |
| SLA é uma **`PoliticaSla`** separada de `Prioridade` | Prioridade classifica; o prazo é regra e pode variar por cliente ou plano |
| Tempo via **`System.TimeProvider`**                  | Permite testar SLA com `FakeTimeProvider`, sem depender do relógio real   |
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
- [ ] Cálculo de SLA com pausa (`AguardandoCliente`)
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
