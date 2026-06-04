FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PriceWise.slnx ./
COPY src/PriceWise.Api/PriceWise.Api.csproj src/PriceWise.Api/
COPY src/PriceWise.Application/PriceWise.Application.csproj src/PriceWise.Application/
COPY src/PriceWise.Domain/PriceWise.Domain.csproj src/PriceWise.Domain/
COPY src/PriceWise.Infrastructure/PriceWise.Infrastructure.csproj src/PriceWise.Infrastructure/
COPY src/PriceWise.Tests/PriceWise.Tests.csproj src/PriceWise.Tests/
RUN dotnet restore PriceWise.slnx
COPY . .
RUN dotnet publish src/PriceWise.Api/PriceWise.Api.csproj --configuration Release --output /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PriceWise.Api.dll"]
