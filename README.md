# PriceWise API

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

## Development

Compile a solution:

```powershell
dotnet build PriceWise.slnx
```
