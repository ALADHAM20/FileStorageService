# FileStorageService

Production-grade file storage service assessment built with ASP.NET Core 8, Clean Architecture, SQL Server, local filesystem storage, and Angular.

## Current Status

Initial backend structure is being set up. Feature implementation will be added phase by phase.

## Planned Architecture

- `FileStorageService.Api`: HTTP API, Swagger, authentication, middleware, health checks.
- `FileStorageService.Application`: use cases, DTOs, interfaces, validation rules.
- `FileStorageService.Domain`: core business entities and domain rules.
- `FileStorageService.Infrastructure`: EF Core, SQL Server, local filesystem storage implementations.
- `tests`: unit and integration tests.
- `frontend`: Angular client added later.

## Requirements

- .NET 8 SDK
- SQL Server for later phases
- Node.js LTS for later Angular phases
