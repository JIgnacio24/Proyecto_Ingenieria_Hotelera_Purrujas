FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Backend-Ingenieria-Purrujas.sln ./
COPY Backend-Ingenieria-Purrujas/src/Directory.Build.props Backend-Ingenieria-Purrujas/src/
COPY Backend-Ingenieria-Purrujas/src/Api/Backend-Ingenieria-Purrujas.Api.csproj Backend-Ingenieria-Purrujas/src/Api/
COPY Backend-Ingenieria-Purrujas/src/Application/Backend-Ingenieria-Purrujas.Application.csproj Backend-Ingenieria-Purrujas/src/Application/
COPY Backend-Ingenieria-Purrujas/src/Domain/Backend-Ingenieria-Purrujas.Domain.csproj Backend-Ingenieria-Purrujas/src/Domain/
COPY Backend-Ingenieria-Purrujas/src/Infrastructure/Backend-Ingenieria-Purrujas.Infrastructure.csproj Backend-Ingenieria-Purrujas/src/Infrastructure/

RUN dotnet restore Backend-Ingenieria-Purrujas.sln

COPY Backend-Ingenieria-Purrujas/ Backend-Ingenieria-Purrujas/
RUN dotnet publish Backend-Ingenieria-Purrujas/src/Api/Backend-Ingenieria-Purrujas.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5234
EXPOSE 5234

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Backend-Ingenieria-Purrujas.Api.dll"]
