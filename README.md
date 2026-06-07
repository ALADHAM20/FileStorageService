# FileStorageService

Production-grade file storage service assessment built with ASP.NET Core 8, Clean Architecture, SQL Server, local filesystem storage, and Angular.

## Current Status

Backend API foundation is implemented:

- Clean Architecture projects
- SQL Server metadata persistence with EF Core migrations
- Local filesystem storage under `_storage`
- Streaming multipart upload
- File listing, download, preview, soft delete, hard delete
- JWT authentication with `user` and `admin` roles
- RFC 7807 ProblemDetails error handling
- Serilog structured logging with `X-Correlation-ID`
- Health endpoints

## Architecture

- `FileStorageService.Api`: HTTP API, Swagger, authentication, middleware, health checks.
- `FileStorageService.Application`: use cases, DTOs, interfaces, validation rules.
- `FileStorageService.Domain`: core business entities and domain rules.
- `FileStorageService.Infrastructure`: EF Core, SQL Server, local filesystem storage implementations.
- `tests`: unit and integration tests.
- `frontend`: Angular client added later.

## Requirements

- .NET 8 SDK
- SQL Server
- Node.js 20 LTS recommended for Angular 17

## Backend Setup

Apply the EF Core migration from Visual Studio Package Manager Console:

```powershell
Update-Database -Context FileStorageDbContext -Project FileStorageService.Infrastructure -StartupProject FileStorageService.Api
```

Run the API:

```powershell
dotnet run --project src\FileStorageService.Api
```

Open Swagger:

```text
https://localhost:<port>/swagger
```

## Authentication

Create a mock JWT from Swagger:

```http
POST /api/auth/mock-token
```

User token body:

```json
{
  "userId": "user-1",
  "role": "user"
}
```

Admin token body:

```json
{
  "userId": "admin-1",
  "role": "admin"
}
```

Copy the returned `accessToken`, click Swagger **Authorize**, and paste:

```text
Bearer <access-token>
```

## API Endpoints

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| `GET` | `/` | Anonymous | API status |
| `POST` | `/api/auth/mock-token` | Anonymous | Create a development JWT |
| `POST` | `/api/files` | User/Admin | Streaming multipart upload |
| `GET` | `/api/files` | User/Admin | List file metadata with filters |
| `GET` | `/api/files/{id}/download` | User/Admin | Download file with Range support |
| `GET` | `/api/files/{id}/preview` | User/Admin | Inline preview for images/PDF |
| `DELETE` | `/api/files/{id}` | User/Admin | Soft delete metadata and return a status message |
| `DELETE` | `/api/files/{id}/hard` | Admin | Permanently delete metadata/content and return a status message |
| `GET` | `/health/live` | Anonymous | API process liveness |
| `GET` | `/health/ready` | Anonymous | SQL Server and filesystem readiness |

## Storage Layout

Uploaded file content is stored on local disk:

```text
_storage/
  yyyy/
    MM/
      dd/
        {generated-key}/
          content.bin
```

SQL Server stores metadata such as original name, stored key, size, content type, SHA-256 checksum, tags, timestamps, version, and owner.

## Upload Constraints

Upload limits are configured in `src/FileStorageService.Api/appsettings.json`:

```json
"Upload": {
  "MaxUploadBytes": 104857600,
  "AllowedContentTypes": [
    "application/pdf",
    "image/jpeg",
    "image/png"
  ]
}
```

The current maximum upload size is 100 MB.

## Delete Responses

Delete endpoints return a small JSON response instead of an empty `204 No Content` response:

```json
{
  "statusCode": 200,
  "message": "File was soft deleted successfully."
}
```

## Dynamic Backend Configuration

The backend avoids hardcoded operational rules where possible:

- SQL Server connection: `ConnectionStrings:DefaultConnection`
- Storage root: `Storage:RootPath`
- Upload max size and allowed content types: `Upload`
- Query page defaults and limits: `Files:Query`
- Previewable content types and prefixes: `Files:Preview`
- JWT issuer, audience, signing key, and mock token lifetime: `Jwt`
- Angular development origins: `Cors:AllowedOrigins`
- Serilog logging: `Serilog`

## Verification

```powershell
dotnet build FileStorageService.sln
dotnet test
```

## Frontend Setup

The Angular 17 frontend lives under:

```text
frontend/file-storage-client
```

Run the frontend from that folder:

```powershell
cd frontend/file-storage-client
npm.cmd start
```

Open:

```text
http://127.0.0.1:4200
```

Build the frontend:

```powershell
npm.cmd run build
```

The frontend uses `src/environments/environment.ts` for the backend API base URL.

Manual Swagger checklist:

- Create a user token.
- Upload a file using `POST /api/files`.
- Confirm a row appears in SQL Server `StoredFiles`.
- Confirm `content.bin` appears under `_storage`.
- List files with `GET /api/files`.
- Download with `GET /api/files/{id}/download`.
- Preview image/PDF files with `GET /api/files/{id}/preview`.
- Soft delete with `DELETE /api/files/{id}`.
- Create an admin token and hard delete with `DELETE /api/files/{id}/hard`.
- Check `/health/live` and `/health/ready`.
