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

Suba o ambiente completo com Docker Compose:

```powershell
docker compose up --build
```

Ou use os scripts de inicializacao, que criam um `.env` local a partir do `.env.example` quando necessario:

```powershell
.\start.ps1
```

```bash
./start.sh
```

Health check:

```http
GET /health
```

A documentacao do Scalar esta disponivel em ambiente de desenvolvimento em `/scalar`.

Servicos locais:

- API: `http://localhost:8080`
- Scalar: `http://localhost:8080/scalar`
- Jaeger: `http://localhost:16686`
- Mailpit: `http://localhost:8025`
- PostgreSQL: `localhost:5432`, database `pricewise`
- Redis: `localhost:6379`

As variaveis de ambiente ficam documentadas em `.env.example`. Para customizar credenciais, portas, JWT, Redis ou telemetria, crie um arquivo `.env` local.

O container `pricewise-api` executa as migrations automaticamente durante a inicializacao. O `docker-compose.yml` aguarda PostgreSQL e Redis ficarem saudaveis antes de iniciar a API.

## API Profissional

A versao atual da API responde pelo prefixo `v1`:

- Prefixo recomendado: `/api/v1`
- Prefixo legado mantido por compatibilidade: `/api`

Exemplo:

```http
GET /api/v1/products
```

Listagens principais retornam resposta paginada:

```json
{
  "page": 1,
  "pageSize": 20,
  "totalItems": 120,
  "totalPages": 6,
  "items": [],
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

Parametros de listagem:

- `page`
- `pageSize`
- `search`
- `isActive`
- `startDate`
- `endDate`
- `sortBy`
- `sortDirection` com `asc` ou `desc`

Endpoints com paginacao:

- `GET /api/v1/products`
- `GET /api/v1/stores`
- `GET /api/v1/products/{productId}/price-histories`
- `GET /api/v1/price-alerts`
- `GET /api/v1/alert-notifications`
- `GET /api/v1/notification-channels`
- `GET /api/v1/admin/users`

O `sortBy` e validado por whitelist em cada repository para evitar SQL injection. Valores desconhecidos usam uma ordenacao padrao segura.

Erros seguem o envelope padrao da API e incluem `traceId`, `correlationId` e `statusCode` quando disponiveis:

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "Auth.Forbidden",
    "message": "Voce nao possui permissao para acessar este recurso.",
    "traceId": "00-...",
    "correlationId": "0f4...",
    "statusCode": 403
  }
}
```

Tambem e possivel enviar `X-Correlation-Id` na requisicao para rastrear logs, traces e respostas.

## Auditoria e rastreabilidade

A API registra acoes relevantes em `AuditLog`, sempre que aplicavel com `UserId`, acao, entidade, valores antigos e novos, IP, User-Agent, `CorrelationId` e data de criacao.

Acoes auditadas:

- Login e logout
- Alteracao de senha
- Revogacao de refresh tokens
- Criacao, alteracao e remocao logica de cadastros principais
- Alteracao de role
- Ativacao e desativacao de usuarios
- Execucao manual do PriceCheck
- Envio de Webhook e e-mail, incluindo sucesso e falha

Endpoints administrativos:

- `GET /api/v1/admin/audit-logs`
- `GET /api/v1/admin/audit-logs/{id}`

Filtros disponiveis:

- `userId`
- `action`
- `entityName`
- `entityId`
- `startDate`
- `endDate`
- `search`
- `sortBy`
- `sortDirection`
- `page`
- `pageSize`

Os endpoints exigem policy `AdminOnly`. O `AuditLog` remove dados sensiveis antes de persistir `OldValues` e `NewValues`, incluindo campos como `Password`, `PasswordHash`, `RefreshToken` e `Token`.

O header `X-Correlation-Id` pode ser enviado pelo cliente. Quando ausente, a API cria um identificador automaticamente. O mesmo valor e retornado na resposta, incluído nos logs do Serilog, nos erros padronizados e nos registros de auditoria.

## Outbox, retries e PriceProvider

As notificacoes de alerta usam Outbox Pattern. Quando uma `AlertNotification` e criada, a API cria uma `OutboxMessage` para cada canal ativo do usuario. O request principal nao chama Webhook ou SMTP diretamente.

O `OutboxProcessorJob`, executado pelo Quartz.NET, processa mensagens pendentes em lote. Em caso de falha, a mensagem recebe nova tentativa com backoff exponencial simples. Quando o limite de tentativas e atingido, o status fica como `Failed`.

Configuracao:

```json
"Outbox": {
  "Enabled": true,
  "IntervalInSeconds": 30,
  "MaxRetries": 5,
  "BatchSize": 20
}
```

Status possiveis:

- `Pending`
- `Processing`
- `Processed`
- `Failed`

Endpoints administrativos:

- `GET /api/v1/admin/outbox`
- `GET /api/v1/admin/outbox/{id}`
- `POST /api/v1/admin/outbox/{id}/retry`

O retry manual so e permitido para mensagens com status `Failed` e exige policy `AdminOnly`.

O PriceCheck usa a abstracao `IPriceProvider`. A implementacao atual e `MockPriceProvider`, que gera precos realistas com pequena variacao em relacao ao ultimo preco conhecido. Essa estrutura deixa o projeto pronto para trocar por uma integracao real futuramente, sem alterar o fluxo do `PriceCheckService`.

Configuracao:

```json
"PriceProvider": {
  "MinimumBasePrice": 50,
  "MaximumBasePrice": 1500,
  "VariationPercentage": 0.03
}
```

## Dados de demonstracao

Em ambiente de desenvolvimento, a API pode criar dados iniciais para demonstrar o Dashboard e os principais fluxos do projeto.

Usuario demo:

- Email: `demo@pricewise.com`
- Senha: `Demo@123456`

A configuracao fica em `appsettings.json` ou nas variaveis do `.env`:

```json
"DataSeed": {
  "Enabled": true,
  "CreateDemoUser": true,
  "DemoUserEmail": "demo@pricewise.com",
  "DemoUserPassword": "Demo@123456",
  "CreateDemoData": true
}
```

Para desabilitar no Docker Compose, ajuste:

```env
DATA_SEED_ENABLED=false
```

O seed e idempotente, nao duplica dados em novas execucoes e nao roda em `Production`.

## Seguranca e autorizacao

A API possui roles no JWT para separar usuarios comuns e operacoes administrativas.

Roles disponiveis:

- `User`: role padrao de usuarios criados pelo endpoint publico de register e do usuario demo.
- `Admin`: role para operacoes administrativas e endpoints sensiveis.

Policies principais:

- `AuthenticatedUser`: endpoints autenticados comuns.
- `AdminOnly`: endpoints administrativos.
- `PriceCheckManagement`: gerenciamento manual do PriceCheck.
- `TelemetryManagement`: informacoes de telemetria.
- `SeedManagement`: execucao manual do seed de demonstracao.
- `SystemManagement`: reservada para endpoints administrativos de sistema.

Usuario admin de demonstracao:

- Email: `admin@pricewise.com`
- Senha: `Admin@123456`

Configuracao:

```json
"AdminSeed": {
  "Enabled": true,
  "Email": "admin@pricewise.com",
  "Password": "Admin@123456"
},
"AuthenticationSecurity": {
  "MaxFailedLoginAttempts": 5,
  "LockoutMinutes": 15
}
```

Endpoints de seguranca do usuario:

- `GET /api/auth/me`
- `POST /api/auth/change-password`
- `POST /api/auth/revoke-refresh-tokens`

Endpoints administrativos:

- `GET /api/admin/users?page=1&pageSize=20`
- `GET /api/admin/users/{id}`
- `PUT /api/admin/users/{id}/role`
- `PUT /api/admin/users/{id}/activate`
- `PUT /api/admin/users/{id}/deactivate`
- `POST /api/admin/users/{id}/revoke-refresh-tokens`

Para testar no Scalar, faca login com o usuario admin, copie o `AccessToken` e use o botao de autorizacao com `Bearer {token}`. Usuarios com role `User` recebem `403 Forbidden` nos endpoints administrativos.

## Webhook Notifications

Quando uma `AlertNotification` e criada, canais ativos do tipo `Webhook` sao enfileirados na Outbox. O processor envia um `POST` com `application/json` para a URL configurada em `Destination`.

Configuracao:

```json
"WebhookNotifications": {
  "Enabled": true,
  "TimeoutInSeconds": 10,
  "MaxRetryAttempts": 3
}
```

Exemplo de payload:

```json
{
  "notificationId": "50000000-0000-0000-0000-000000000001",
  "userId": "10000000-0000-0000-0000-000000000001",
  "productId": "20000000-0000-0000-0000-000000000001",
  "priceAlertId": "30000000-0000-0000-0000-000000000001",
  "priceHistoryId": "40000000-0000-0000-0000-000000000001",
  "productName": "Notebook Demo",
  "targetPrice": 100.00,
  "triggeredPrice": 89.90,
  "triggeredAt": "2026-06-04T10:30:00Z",
  "message": "O produto Notebook Demo atingiu o preco de R$ 89,90. Alvo configurado: R$ 100,00."
}
```

Falhas de webhook sao registradas em log e auditoria. Quando uma excecao chega ao processor, a mensagem e reagendada pela Outbox sem interromper a criacao da notificacao de alerta. Para desabilitar no Docker Compose, ajuste `WEBHOOK_NOTIFICATIONS_ENABLED=false`.

## Email Notifications

Quando uma `AlertNotification` e criada, canais ativos do tipo `Email` sao enfileirados na Outbox. O processor envia uma mensagem SMTP para o e-mail configurado em `Destination`.

Configuracao:

```json
"EmailNotifications": {
  "Enabled": false,
  "Host": "localhost",
  "Port": 1025,
  "UseSsl": false,
  "UserName": "",
  "Password": "",
  "FromName": "PriceWise",
  "FromEmail": "noreply@pricewise.local",
  "TimeoutInSeconds": 10,
  "MaxRetryAttempts": 3
}
```

No Docker Compose, o SMTP local usa Mailpit:

- SMTP: `mailpit:1025`
- Interface web: `http://localhost:8025`

Para testar localmente, habilite no `.env`:

```env
EMAIL_NOTIFICATIONS_ENABLED=true
EMAIL_NOTIFICATIONS_HOST=mailpit
EMAIL_NOTIFICATIONS_PORT=1025
EMAIL_NOTIFICATIONS_USE_SSL=false
```

O e-mail possui versao HTML e texto puro, com produto, preco alvo, preco encontrado, data do disparo e link do produto quando disponivel. Falhas no SMTP sao registradas em log e auditoria, e nao interrompem a criacao da notificacao de alerta.

## Exportacoes CSV

A API permite exportar os dados principais em CSV, sempre filtrando pelo usuario autenticado.

Endpoints:

- `GET /api/exports/products.csv`
- `GET /api/exports/stores.csv`
- `GET /api/exports/price-histories.csv`
- `GET /api/exports/alert-notifications.csv`

Filtros opcionais:

- `startDate`
- `endDate`
- `productId`, quando aplicavel
- `storeId`, quando aplicavel

Exemplo autenticado:

```bash
curl -H "Authorization: Bearer {token}" \
  "http://localhost:8080/api/exports/price-histories.csv?productId={productId}&startDate=2026-06-01"
```

Configuracao:

```json
"CsvExport": {
  "MaxRows": 10000,
  "DateFormat": "yyyy-MM-dd HH:mm:ss"
}
```

O retorno usa `text/csv`, UTF-8, cabecalho na primeira linha e limite maximo configuravel por `CsvExport:MaxRows`.

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
No ambiente Docker Compose, o exporter OTLP aponta para o Jaeger em `http://jaeger:4317`.

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
