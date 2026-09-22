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
| ---------- | ----------------- | --------- |
| Crítica    | 15 min            | 4 h       |
| Alta       | 1 h               | 8 h       |
| Média      | 4 h               | 24 h      |
| Baixa      | 8 h               | 72 h      |

Regras:

- **Primeira resposta** é o primeiro comentário de quem não é o solicitante, ou o ato de pedir informação ao cliente, ou resolver. Só `Assumir` não conta.
- **Pausa:** em `AguardandoCliente` o relógio congela. Ao retomar, o tempo pausado é devolvido ao prazo de resolução.
- **Violação é permanente:** resolver ou responder fora do prazo continua registrado como violado depois.
- **Reabertura** reinicia os dois prazos a partir do momento da reabertura.
- Os prazos ficam **gravados no ticket** (`PrazosSla`); mudar a `PoliticaSla` depois não altera tickets já abertos.
- Tempo corrido, sem horário comercial (possível evolução).

### Autenticação e papéis

Três papéis, com JWT emitido pela própria API (ASP.NET Identity para usuários/senhas, sem cookies nem tela de cadastro):

- **Cliente:** abre tickets e só vê os próprios.
- **Atendente:** assume tickets e vê qualquer um do seu tenant.
- **Supervisor:** mesma visão do atendente (reatribuição e métricas ficam para depois).

```
POST /api/auth/login              { "tenantId": "...", "email": "...", "senha": "..." }  -> { "token": "...", "expiraEm": "..." }
GET  /api/tickets                  [autenticado] cliente vê só os próprios; atendente/supervisor veem a fila do tenant
POST /api/tickets                  [Cliente]     abre um ticket
GET  /api/tickets/{id}             [autenticado] cliente só o próprio; atendente/supervisor qualquer um do tenant
POST /api/tickets/{id}/assumir     [Atendente]   assume da fila
POST /api/tickets/{id}/comentarios [autenticado] mesma regra de acesso do GET por id
```

O `tenant_id` e o papel vêm como claims no próprio JWT; o filtro global do EF cuida do isolamento entre empresas a partir daí.

**E-mail é único por empresa, não globalmente:** duas empresas podem ter, cada uma, um usuário com o mesmo e-mail (`TenantAwareUserValidator`, substituindo o validador padrão do Identity). Por isso o login pede o `tenantId` explicitamente — hoje um placeholder honesto, já que ainda não existe um fluxo de escolha de empresa (subdomínio, convite, código); ver "Multi-tenancy" no roadmap.

### Decisões de modelagem

| Decisão                                                       | Motivo                                                                                            |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `Comentario` é **entidade filha** do `Ticket`                 | Tem autor, data e ordem; não é intercambiável como um Value Object                                |
| SLA é uma **`PoliticaSla`** separada de `Prioridade`          | Prioridade classifica; o prazo é regra e pode variar por cliente ou plano                         |
| Domínio recebe **`agora`** por parâmetro                      | O `TimeProvider` fica na Application; nos testes, `FakeTimeProvider` controla o relógio           |
| Solicitante e atendente entram só como **`Guid`**             | Identidade é outro contexto; o domínio não navega para `User`                                     |
| **`TenantId`** no ticket desde o início                       | Adicionar multi-tenancy depois, com dados no banco, custa muito mais                              |
| **Filtro global por tenant** no EF + checagem no `Adicionar`  | Leituras nunca cruzam empresas; gravações de outro tenant são recusadas                           |
| **`ITicketRepository` no Domain**, EF na Infrastructure       | O domínio declara o que precisa; quem persiste é detalhe de infraestrutura                        |
| **Concorrência otimista** (`xmin` do Postgres)                | Dois atendentes não assumem o mesmo ticket: o segundo recebe conflito                             |
| **Enums gravados como texto**                                 | Legível no banco e imune a reordenação do enum                                                    |
| **`UserValidator` do Identity trocado** por um tenant-aware   | O padrão checa unicidade de e-mail/username na tabela inteira; aqui é por tenant                  |
| **Limites de tamanho no domínio**                             | Estourar o limite vira `DomainException`, não erro de banco                                       |
| **ASP.NET Identity só com `AddIdentityCore`**, sem cookies    | A Api emite JWT; não precisa do esquema de sign-in completo do `AddIdentity`                      |
| **Papel (role) é conceito da Application/Api**, não do Domain | O agregado protege consistência (“só o responsável resolve”); quem pode fazer o quê é autorização |

## Arquitetura

Monolito modular, sem camadas por cerimônia.

```
src/
  Helpdesk.Domain          agregados, value objects, eventos (sem dependências)
  Helpdesk.Application     casos de uso
  Helpdesk.Infrastructure  EF Core, repositórios
  Helpdesk.Api             ASP.NET Core, autenticação, policies
tests/
  Helpdesk.Domain.Tests           testes unitários puros, sem banco
  Helpdesk.Application.Tests      casos de uso com repositório em memória (fakes)
  Helpdesk.Infrastructure.Tests   integração com PostgreSQL real (migrations incluídas)
  Helpdesk.Api.Tests              emissão/validação de JWT
```

Regra de dependência: `Api -> Infrastructure -> Application -> Domain`. O `Domain` não referencia ninguém.

## Como rodar

Requisitos: [.NET SDK 8](https://dotnet.microsoft.com/download) e Docker.

**1. Banco de desenvolvimento** (credenciais fixas do `docker-compose.yml`, sem valor fora do seu ambiente local):

```bash
docker compose up -d
cp .env.example .env
```

**2. Connection string e chave do JWT via user-secrets** (fica fora do repositório; a API lê daqui, não do `.env`):

```bash
dotnet user-secrets set "ConnectionStrings:Helpdesk" "Host=localhost;Database=helpdesk_dev;Username=helpdesk;Password=helpdesk" --project src/Helpdesk.Api
dotnet user-secrets set "Jwt:Key" "<qualquer string com pelo menos 32 caracteres>" --project src/Helpdesk.Api
```

**3. Migrations e execução:**

```bash
dotnet tool restore
source .env && dotnet ef database update -p src/Helpdesk.Infrastructure
dotnet run --project src/Helpdesk.Api
```

Em ambiente de Development, a API cria sozinha os três papéis e um usuário de exemplo por papel (login abaixo), se ainda não existirem. Credenciais públicas de propósito, só para rodar o projeto localmente — nunca use esse padrão em produção. `tenantId`: `11111111-1111-1111-1111-111111111111`.

| Papel      | E-mail                      | Senha      |
| ---------- | --------------------------- | ---------- |
| Cliente    | `cliente@helpdesk.local`    | `Demo123$` |
| Atendente  | `atendente@helpdesk.local`  | `Demo123$` |
| Supervisor | `supervisor@helpdesk.local` | `Demo123$` |

**Testes:**

```bash
dotnet test   # unitários; os de integração são ignorados se HELPDESK_TEST_CONNECTION não estiver definida
source .env && dotnet test   # inclui os de integração, contra o Postgres do docker-compose
```

Os testes de integração criam um banco descartável por execução e o removem no final. No CI, rodam contra um serviço Postgres efêmero do próprio workflow.

**Segredos nunca entram no repositório.** As credenciais em `docker-compose.yml` e `.env.example` são fixas de propósito, só valem para o container local e não protegem nada sensível; em qualquer ambiente real (staging, produção), a connection string vem de user-secrets, variável de ambiente ou de um cofre de segredos, nunca de um arquivo versionado.

### Front-end

Vite + React + TypeScript, TanStack Query para chamadas à API e React Router para as rotas. Login, fila de tickets (cliente vê os próprios, atendente/supervisor veem a fila do tenant), abrir ticket, assumir e comentar.

```bash
cd frontend
npm install
cp .env.example .env
npm run dev   # http://localhost:5173, com a API já rodando em http://localhost:5099
```

**Tipos gerados do OpenAPI**, não escritos à mão — zero divergência entre os DTOs do C# e o TypeScript:

```bash
npm run generate:types   # precisa da API rodando; escreve src/api/schema.d.ts
```

Depois de mudar qualquer DTO ou endpoint no back-end, rode esse comando de novo e o `tsc` acusa, no front, todo lugar que quebrou.

Decisões: token JWT em `localStorage` (simples para um projeto de estudo; um app real preferiria cookie `HttpOnly`, decisão documentada em `src/auth/token.ts`); papel do usuário lido decodificando o JWT no cliente, só para ajustar a UI — quem garante que o token é legítimo é sempre a Api.

## Roadmap

- [x] Estrutura da solution
- [x] Agregado `Ticket` com máquina de estados e testes
- [x] Cálculo de SLA (primeira resposta e resolução) com pausa em `AguardandoCliente`
- [x] Persistência com EF Core (PostgreSQL, migrations, testes de integração)
- [x] API com autenticação (ASP.NET Identity + JWT) e policies por papel (cliente, atendente, supervisor)
- [x] Front-end React + TypeScript (Vite, TanStack Query, tipos gerados do OpenAPI)
- [ ] Domain Events (`TicketResolvido`, `TicketReaberto`)
- [ ] Outbox Pattern
- [ ] Tempo real com SignalR
- [ ] Verificação de SLA vencido em `BackgroundService`
- [ ] Multi-tenancy

## Licença

[MIT](LICENSE)
