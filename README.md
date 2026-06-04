# PriceWise API

[![CI](https://github.com/AndreLBrito/PriceWise-API/actions/workflows/ci.yml/badge.svg)](https://github.com/AndreLBrito/PriceWise-API/actions/workflows/ci.yml)

A PriceWise API e um projeto de portfolio para monitoramento de precos de produtos e historico de precos.

## Stack

- .NET 10
- ASP.NET Core Minimal API
- PostgreSQL
- Dapper
- FluentMigrator
- FluentValidation
- Serilog
- Redis
- OpenTelemetry
- Scalar
- JWT Authentication
- xUnit
- FluentAssertions
- Testcontainers

## Architecture

```text
src/
|-- PriceWise.Api
|-- PriceWise.Application
|-- PriceWise.Domain
|-- PriceWise.Infrastructure
`-- PriceWise.Tests
```

## Running locally

Inicie o PostgreSQL:

```powershell
docker compose up -d
```

Execute a API:

```powershell
dotnet run --project src/PriceWise.Api/PriceWise.Api.csproj
```

Health check:

```http
GET /health
```

A documentacao do Scalar esta disponivel em ambiente de desenvolvimento em `/scalar`.

## Observabilidade

A API possui observabilidade com OpenTelemetry para traces e metricas. A configuracao fica em `appsettings.json`:

```json
"Telemetry": {
  "Enabled": true,
  "ServiceName": "PriceWise.Api",
  "ServiceVersion": "1.0.0",
  "Exporter": "Console",
  "EnableMetrics": true,
  "EnableTracing": true
}
```

Use `Exporter: Console` para visualizar traces e metricas no terminal durante o desenvolvimento. Use `Exporter: OTLP` para enviar dados para um collector compativel com OpenTelemetry.

Endpoint de informacoes:

```http
GET /api/telemetry/info
```

Health check de telemetria:

```http
GET /health/telemetry
```

Metricas customizadas:

- `products_created_total`
- `stores_created_total`
- `price_histories_created_total`
- `price_alerts_created_total`
- `alert_notifications_created_total`
- `manual_price_checks_total`
- `automatic_price_checks_total`

Os traces usam o `ActivitySource` `PriceWise.Application` e cobrem os principais services da aplicacao, incluindo autenticacao, produtos, lojas, historico de precos, alertas, notificacoes e verificacao de precos.

## Rate Limiting

A API usa o rate limiting nativo do ASP.NET Core para proteger endpoints sensiveis. A configuracao fica em `appsettings.json`:

```json
"RateLimiting": {
  "Enabled": true,
  "LoginPermitLimit": 5,
  "LoginWindowInMinutes": 1,
  "RefreshTokenPermitLimit": 10,
  "RefreshTokenWindowInMinutes": 1,
  "GeneralPermitLimit": 100,
  "GeneralWindowInMinutes": 1,
  "PriceCheckPermitLimit": 3,
  "PriceCheckWindowInMinutes": 5
}
```

Politicas aplicadas:

- Login e Register usam uma politica restritiva.
- Refresh Token usa uma politica intermediaria.
- `POST /api/price-check/run` usa uma politica restritiva.
- Endpoints autenticados usam a politica geral.

Quando o limite e excedido, a API retorna `HTTP 429` com resposta padronizada em portugues. Usuarios autenticados sao identificados pelo `UserId`; requisicoes anonimas sao particionadas por IP remoto.

## CI/CD

O projeto usa GitHub Actions no workflow `CI`, executado em pushes e pull requests para a branch `main`.

O pipeline executa:

- Checkout do codigo.
- Instalacao do .NET 10 SDK.
- `dotnet restore`.
- Validacao de formatacao com `dotnet format --verify-no-changes`.
- Build em `Release`.
- Testes em `Release`.
- Publicacao dos resultados de testes como artifact.
- Validacao do `docker-compose.yml` com `docker compose config`.

Os testes sao executados em runner Ubuntu do GitHub Actions, que possui Docker disponivel para cenarios com Testcontainers. Nenhum secret e necessario neste momento.

TODO: adicionar publicacao de imagem Docker quando houver alvo de deploy definido.

## Testes

Execute a suite completa:

```powershell
dotnet test PriceWise.slnx
```

Os testes de integracao usam Testcontainers com PostgreSQL real. Para executa-los localmente, mantenha o Docker Desktop iniciado e disponivel para o terminal.

Para executar apenas os testes unitarios:

```powershell
dotnet test PriceWise.slnx --filter FullyQualifiedName!~Integration
```

## Development

Compile a solution:

```powershell
dotnet build PriceWise.slnx
```
