# PriceWise API

PriceWise API is a portfolio project for product price monitoring and price history.

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
├── PriceWise.Api
├── PriceWise.Application
├── PriceWise.Domain
├── PriceWise.Infrastructure
└── PriceWise.Tests
```

## Running locally

Start PostgreSQL:

```powershell
docker compose up -d
```

Run the API:

```powershell
dotnet run --project src/PriceWise.Api/PriceWise.Api.csproj
```

Health check:

```http
GET /health
```

Scalar documentation is available in development at `/scalar`.

## Development

Build the solution:

```powershell
dotnet build PriceWise.slnx
```
