# PriceWise API Bruno Collection

Use o ambiente `Local` e execute primeiro `Authentication/Login` ou `Admin/Login Admin`.

Os scripts de post-response salvam `accessToken` e `refreshToken` no ambiente quando a API retorna o envelope padrao com `data.accessToken` e `data.refreshToken`.

Fluxo sugerido:

1. `Authentication/Login`
2. `Products/Create Product`
3. `Stores/Create Store`
4. `NotificationChannels/Create Webhook Channel`
5. `PriceAlerts/Create Price Alert`
6. `PriceHistory/Create Price History`
7. `Dashboard/Summary`

Para endpoints administrativos, execute `Admin/Login Admin` antes dos requests da pasta `Admin`.
