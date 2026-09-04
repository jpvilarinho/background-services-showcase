# Background Services Showcase (.NET 8)

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![xUnit](https://img.shields.io/badge/tests-xUnit-5D3FD3)](https://xunit.net/)
[![Quartz.NET](https://img.shields.io/badge/scheduler-Quartz.NET-orange)](https://www.quartz-scheduler.net/)
[![Hangfire](https://img.shields.io/badge/jobs-Hangfire-1E88E5)](https://www.hangfire.io/)
[![Polly](https://img.shields.io/badge/resilience-Polly-7B68EE)](https://github.com/App-vNext/Polly)
[![Serilog](https://img.shields.io/badge/logging-Serilog-1F4E4C)](https://serilog.net/)
[![OpenTelemetry](https://img.shields.io/badge/observability-OpenTelemetry-425CC7?logo=opentelemetry)](https://opentelemetry.io/)

Projeto de referência demonstrando os principais padrões de processamento
assíncrono e em segundo plano no ecossistema .NET: `IHostedService` /
`BackgroundService`, Worker Services standalone, agendamento com
**Quartz.NET**, jobs persistentes com **Hangfire**, resiliência com
**Polly** (retry + circuit breaker), injeção de dependência avançada em
serviços de longa duração e observabilidade com **Serilog** + **OpenTelemetry**.

## Arquitetura

```
BackgroundServicesShowcase.sln
└── src/
    ├── BackgroundServicesShowcase.Api/       (ASP.NET Core Web API)
    │   ├── Interfaces/
    │   │   ├── IEmailQueueService.cs
    │   │   └── IEmailSenderService.cs
    │   ├── Services/
    │   │   ├── EmailQueueService.cs           Channel<T> thread-safe, produtor/consumidor
    │   │   ├── EmailDispatcherBackgroundService.cs   BackgroundService principal
    │   │   ├── EmailSenderService.cs          Scoped, resolvido via IServiceScopeFactory
    │   │   └── EmailQueueHealthCheckService.cs
    │   ├── Security/
    │   │   └── HangfireAuthorizationFilter.cs Basic Auth protegendo o dashboard do Hangfire
    │   ├── Jobs/
    │   │   ├── InventorySyncJob.cs           Quartz.NET (cron)
    │   │   └── ReportCleanupJob.cs           Hangfire (recorrente + retry automático)
    │   ├── Controllers/
    │   │   ├── EmailController.cs            Enfileira e-mails na fila
    │   │   └── JobController.cs              Dispara/agenda jobs do Hangfire
    │   └── Extensions/ServiceCollectionExtension.cs  Toda a fiação de DI/Polly/Quartz/Hangfire/OTel
    └── BackgroundServicesShowcase.Worker/    (Worker Service standalone)
        └── FeedPollerWorker.cs               Daemon independente, também resiliente com Polly
tests/
└── BackgroundServicesShowcase.Api.Tests/     xUnit + Moq, cobrindo fila, dispatcher e health check
```

### Por que essa arquitetura

| Necessidade | Solução usada | Onde |
|---|---|---|
| Processar tarefas assíncronas dentro de uma API sem bloquear requests | `BackgroundService` + `Channel<T>` | `EmailDispatcherBackgroundService` |
| Resolver dependências *scoped* dentro de um serviço singleton de longa duração | `IServiceScopeFactory.CreateScope()` | `EmailDispatcherBackgroundService.DispatchBatchAsync` |
| Reconfigurar comportamento em runtime sem reiniciar o serviço | `IOptionsMonitor<T>` | `EmailDispatcherBackgroundService`, `EmailSender` |
| Agendamento tipo cron, com controle granular de trigger | **Quartz.NET** | `InventorySyncJob` |
| Job recorrente com persistência, retry automático e dashboard visual | **Hangfire** | `ReportCleanupJob` |
| Proteger chamadas HTTP externas contra falhas transitórias | **Polly** (retry exponencial + circuit breaker) via `IHttpClientFactory` | `AddResilientExternalApiClient` |
| Serviço isolado, sem dependência de host web (systemd/Windows Service) | Worker Service dedicado | `BackgroundServicesShowcase.Worker` |
| Rastreabilidade e métricas | Serilog (logs estruturados) + OpenTelemetry (traces/métricas) | `Program.cs`, `AddShowcaseObservability` |
| Proteger o dashboard de jobs contra acesso não autenticado | Basic Auth via `IDashboardAuthorizationFilter` | `HangfireAuthorizationFilter` |
| Garantir que a lógica assíncrona funciona sem depender de infraestrutura real | Testes unitários com xUnit + Moq | `tests/BackgroundServicesShowcase.Api.Tests` |

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Nenhuma infraestrutura externa é necessária: Hangfire usa `Hangfire.InMemory`
  e não exige SQL Server para rodar este showcase. Em produção, troque por
  `UseSqlServerStorage(...)` ou `UsePostgreSqlStorage(...)`.

## Como rodar

```bash
git clone https://github.com/jpvilarinho/background-services-showcase.git
cd background-services-showcase

dotnet restore
dotnet build

# Terminal 1 — API (BackgroundService, Quartz, Hangfire)
dotnet run --project src/BackgroundServicesShowcase.Api

# Terminal 2 — Worker standalone
dotnet run --project src/BackgroundServicesShowcase.Worker
```

A API sobe em `http://localhost:5090` com Swagger em `/swagger`.

## Endpoints para testar

```bash
# Enfileira um e-mail (consumido pelo EmailDispatcherBackgroundService)
curl -X POST http://localhost:5090/api/email \
  -H "Content-Type: application/json" \
  -d '{"to":"joao@example.com","subject":"Teste","body":"Hello"}'

# Profundidade atual da fila
curl http://localhost:5090/api/email/queue-depth

# Dispara o job do Hangfire manualmente
curl -X POST "http://localhost:5090/api/jobs/report-cleanup/run-once?retentionDays=30"

# Agenda o job como recorrente (diário)
curl -X POST "http://localhost:5090/api/jobs/report-cleanup/schedule-recurring?retentionDays=30"

# Health check (inclui profundidade da fila de e-mails)
curl http://localhost:5090/health
```

- **Dashboard do Hangfire**: `http://localhost:5090/hangfire` — protegido por
  Basic Auth. Credenciais padrão de desenvolvimento em `appsettings.json`
  (`HangfireDashboard:Username` / `HangfireDashboard:Password`); troque via
  `dotnet user-secrets` ou variáveis de ambiente antes de publicar em qualquer
  ambiente real, e nunca comite credenciais de produção no repositório.
- **Job do Quartz** (`InventorySyncJob`): roda sozinho a cada 30 segundos
  (cron `0/30 * * * * ?`), sem precisar de request nenhum — acompanhe pelo log.

## O que observar nos logs

- Retry e circuit breaker do Polly imprimem `[Polly] Retry ...` e
  `[Polly] Circuit opened/closed` quando o `ExternalApi` (apontando por
  padrão para `httpstat.us`) falha ou volta a responder.
- Cada log é estruturado (Serilog) e vai tanto para o console quanto para
  `logs/api-*.log` / `logs/worker-*.log`.
- Traces e métricas do OpenTelemetry são exportados no console
  (`OpenTelemetry.Exporter.Console`).

## Testes

```bash
dotnet test
```

O projeto `tests/BackgroundServicesShowcase.Api.Tests` cobre:

- **`EmailQueueServiceTests`** — ordem FIFO da fila e atualização de `ApproximateDepth`.
- **`EmailDispatcherBackgroundServiceTests`** — dispatch de todas as mensagens
  em fila, resolução de `IEmailSenderService` via `IServiceScopeFactory` real
  (DI de verdade, não só mock), e garantia de que uma falha no envio de uma
  mensagem não interrompe o processamento das demais.
- **`EmailQueueHealthCheckServiceTests`** — thresholds de `Healthy` vs
  `Degraded` conforme a profundidade da fila.

## Referência

Projeto criado a partir dos conceitos do ebook *Background Services no .NET* (André Baltieri / balta.io).

## Licença

Distribuído sob a licença MIT. Veja [LICENSE](LICENSE) para mais detalhes.
