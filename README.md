# FileStorageService

Production-grade file storage service assessment built with ASP.NET Core 8, Clean Architecture, SQL Server, local filesystem storage, Angular 17, JWT authorization, and Docker Compose.

## What It Does

FileStorageService lets authenticated users upload files, store metadata in SQL Server, keep file content on the local filesystem, search stored files, preview images/PDFs, download content with HTTP range support, and delete records safely. Admin users can hard delete files and review audit logs.

## Tech Stack

- Backend: ASP.NET Core 8, EF Core 8, SQL Server, Serilog, Swagger
- Frontend: Angular 17 standalone components
- Storage: SQL Server for metadata, local filesystem for binary content
- Auth: Mock JWT endpoint with `user` and `admin` roles
- Containers: Docker Compose, SQL Server, API, Nginx-served Angular client

## Run With Docker Compose

Prerequisite: Docker Desktop must be running.

From the repository root:

```powershell
docker compose up --build
```

Open the frontend:

```text
http://localhost:4200
```

Open the API directly:

```text
http://localhost:5055/swagger
```

Docker services:

| Service | URL / Port | Purpose |
| --- | --- | --- |
| `frontend` | `http://localhost:4200` | Angular client served by Nginx |
| `api` | `http://localhost:5055` | ASP.NET Core API |
| `sqlserver` | `localhost,14333` | SQL Server container |

Docker applies EF Core migrations automatically through `Database:ApplyMigrationsOnStartup=true`. Local development keeps this setting disabled by default.

To stop containers:

```powershell
docker compose down
```

To remove container volumes as well:

```powershell
docker compose down -v
```

Optional environment values can be copied from `.env.example` into `.env`.

## Local Backend Setup

Requirements:

- .NET 8 SDK
- SQL Server or SQL Server Express
- Visual Studio Package Manager Console or `dotnet ef`

Apply migrations:

```powershell
Update-Database -Context FileStorageDbContext -Project FileStorageService.Infrastructure -StartupProject FileStorageService.Api
```

Run the API:

```powershell
dotnet run --project src\FileStorageService.Api
```

Swagger is available in development:

```text
https://localhost:7265/swagger
```

## Local Frontend Setup

Requirements:

- Node.js
- npm

Run Angular:

```powershell
cd frontend/file-storage-client
npm install
npm start
```

Open:

```text
http://127.0.0.1:4200
```

The local Angular environment points to:

```text
https://localhost:7265
```

The Docker Angular production environment uses same-origin `/api` calls through Nginx.

## Authentication

Create a mock token from the login screen or Swagger:

```http
POST /api/auth/mock-token
```

User body:

```json
{
  "userId": "user-1",
  "role": "user"
}
```

Admin body:

```json
{
  "userId": "admin-1",
  "role": "admin"
}
```

For Swagger, copy `accessToken`, click **Authorize**, and paste:

```text
Bearer <access-token>
```

## API Endpoints

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| `GET` | `/` | Anonymous | API status |
| `POST` | `/api/auth/mock-token` | Anonymous | Create mock JWT |
| `POST` | `/api/files` | User/Admin | Streaming multipart upload |
| `GET` | `/api/files` | User/Admin | Search files with pagination and filters |
| `GET` | `/api/files/{id}/download` | User/Admin | Streaming download with HTTP Range support |
| `GET` | `/api/files/{id}/preview` | User/Admin | Inline image/PDF preview |
| `DELETE` | `/api/files/{id}` | User/Admin | Soft delete |
| `DELETE` | `/api/files/{id}/hard` | Admin | Hard delete |
| `POST` | `/api/upload-sessions` | User/Admin | Create resumable upload session |
| `PUT` | `/api/upload-sessions/{id}/chunks` | User/Admin | Append resumable upload chunk |
| `POST` | `/api/upload-sessions/{id}/complete` | User/Admin | Complete resumable upload |
| `GET` | `/api/audit-logs` | Admin | Search audit logs |
| `GET` | `/health/live` | Anonymous | API liveness |
| `GET` | `/health/ready` | Anonymous | SQL Server and filesystem readiness |

## Architecture

```text
FileStorageService/
  src/
    FileStorageService.Api/
    FileStorageService.Application/
    FileStorageService.Domain/
    FileStorageService.Infrastructure/
  tests/
    FileStorageService.UnitTests/
    FileStorageService.IntegrationTests/
  frontend/
    file-storage-client/
```

Layer responsibilities:

- `Api`: controllers, Swagger, auth, filters, middleware, ProblemDetails, health checks.
- `Application`: use cases, DTOs, service interfaces, validation, options.
- `Domain`: core entities, enums, business rules, entity helper validation.
- `Infrastructure`: EF Core, repositories, SQL Server mapping, local filesystem implementations.
- `frontend/file-storage-client`: Angular 17 client with `core`, `shared`, and `storage` folders.

## Storage Layout

Binary file content is stored locally:

```text
_storage/
  yyyy/
    MM/
      dd/
        {generated-key}/
          content.bin
```

SQL Server stores metadata:

- Original name
- Stored key
- Size in bytes
- Content type
- SHA-256 checksum
- Tags
- Created/deleted timestamps
- Version
- Created by user id

## Design Decisions

- SQL Server stores metadata because metadata is searchable, filterable, and relational.
- Local filesystem stores file content because the requirement explicitly asks for local filesystem storage only.
- Upload and download are streamed so large files do not need to be loaded fully into memory.
- Standard multipart upload disables automatic form model binding to avoid request buffering.
- Files are written atomically through a temporary file before final commit.
- SHA-256 checksum is calculated while writing the file so integrity can be tracked.
- JWT roles protect admin-only actions like hard delete and audit log viewing.
- ProblemDetails is used for consistent RFC 7807 error responses.
- Serilog and correlation IDs make request tracing easier.
- Upload limits, preview rules, query paging, JWT settings, storage path, and resumable session expiry are configuration-based.
- Docker frontend uses Nginx as a reverse proxy so browser calls to `/api` reach the API container without extra client-side configuration.

## Frontend Features

- Mock JWT login for user/admin.
- Drag-and-drop upload.
- Standard and resumable upload modes.
- Progress indication and retry handling.
- File listing with pagination.
- Filters by name, tag, content type, and date range.
- Image/PDF preview page.
- File download.
- Soft delete.
- Hard delete for admin.
- Admin audit log grid.
- Toast notifications.
- E2E test for upload -> list -> download.

## Verification

Backend:

```powershell
dotnet build FileStorageService.sln
dotnet test FileStorageService.sln --no-build
```

Frontend:

```powershell
cd frontend/file-storage-client
npm run build
```

E2E:

```powershell
cd frontend/file-storage-client
npm run e2e
```

The Playwright test uses Chrome channel and validates upload, list, and download.

## Known Limitations

- JWT generation is intentionally mocked for the assessment.
- Local filesystem storage is single-node and not suitable for distributed production without shared storage.
- Resumable upload is implemented simply for assessment bonus coverage, not as a full TUS-compatible protocol.
- Audit logging records important file actions but is not a full compliance-grade audit subsystem.
- Docker uses development-friendly defaults; production secrets should be supplied securely.

## Future Enhancements

- Use a real identity provider.
- Add virus scanning.
- Add richer audit events and export.
- Add ETag-aware frontend caching behavior.
- Add resumable upload resume discovery by file fingerprint.
- Add more integration tests around range downloads, delete permissions, and upload constraints.
