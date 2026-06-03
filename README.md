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

## Development

Compile a solution:

```powershell
dotnet build PriceWise.slnx
```
