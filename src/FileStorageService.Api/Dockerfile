FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY FileStorageService.sln ./
COPY global.json ./
COPY src/FileStorageService.Api/FileStorageService.Api.csproj src/FileStorageService.Api/
COPY src/FileStorageService.Application/FileStorageService.Application.csproj src/FileStorageService.Application/
COPY src/FileStorageService.Domain/FileStorageService.Domain.csproj src/FileStorageService.Domain/
COPY src/FileStorageService.Infrastructure/FileStorageService.Infrastructure.csproj src/FileStorageService.Infrastructure/

RUN dotnet restore src/FileStorageService.Api/FileStorageService.Api.csproj

COPY src/ src/
RUN dotnet publish src/FileStorageService.Api/FileStorageService.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FileStorageService.Api.dll"]
