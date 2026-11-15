# CheckPoint API

CheckPoint is an API for students to share lecture notes, presentations, study questions, and past exam questions organized by university and course.

## Requirements

- .NET 9 SDK
- SQL Server LocalDB
- The `dotnet-ef` global tool

## Setup

Run commands from the repository root.

1. Clone the repository and open `CheckPoint.Api`.
2. Initialize user secrets if this is a fresh checkout:

   ```powershell
   dotnet user-secrets init --project .\CheckPoint.Api\CheckPoint.Api.csproj
   ```

   The project already has a `UserSecretsId`; initializing it again is unnecessary for an existing local checkout.

3. Configure a private JWT signing key and seed-admin credentials. Do not place these values in `appsettings.json` or commit them:

   ```powershell
   $bytes = New-Object byte[] 48
   $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
   $rng.GetBytes($bytes)
   $jwtKey = [Convert]::ToBase64String($bytes)
   dotnet user-secrets set "Jwt:Key" $jwtKey --project .\CheckPoint.Api\CheckPoint.Api.csproj

   dotnet user-secrets set "SeedAdmin:Email" "admin@example.com" --project .\CheckPoint.Api\CheckPoint.Api.csproj
   dotnet user-secrets set "SeedAdmin:Password" "<strong password of at least 8 characters>" --project .\CheckPoint.Api\CheckPoint.Api.csproj
   ```

   The generated key is 64 Base64 characters (48 cryptographically random bytes). The application requires a JWT key of at least 32 characters and stops at startup if it is missing or too short. Admin seeding is skipped unless both seed settings are supplied. The first configured admin is created when the application starts and no Admin already exists.

4. Apply database migrations:

   ```powershell
   dotnet ef database update --project .\CheckPoint.Api\CheckPoint.Api.csproj
   ```

   Alternatively, set `Database:AutoMigrate` to `true` in `appsettings.Development.json` (or with `dotnet user-secrets set "Database:AutoMigrate" "true" ...`) to apply pending migrations at startup. This only works in the Development environment.

5. Run the API:

   ```powershell
   dotnet run --project .\CheckPoint.Api\CheckPoint.Api.csproj
   ```

6. Open Swagger UI at [http://localhost:5118/swagger](http://localhost:5118/swagger). Use the **Authorize** button with a Bearer token returned by the login endpoint.

To run over HTTPS locally, trust the development certificate once and use the `https` launch profile. HTTP requests are then redirected to `https://localhost:7237`:

```powershell
dotnet dev-certs https --trust
dotnet run --project .\CheckPoint.Api\CheckPoint.Api.csproj --launch-profile https
```

With the default `http` profile no HTTPS port is known, so the startup log shows the warning `Failed to determine the https port for redirect` and requests are not redirected.

## Configuration

Settings are layered: `appsettings.json` (shared defaults) → `appsettings.{Environment}.json` → user secrets (Development only) → environment variables → command-line arguments. Environment variables use `__` instead of `:`, for example `Jwt__Key` or `ConnectionStrings__DefaultConnection`.

| Key | Secret? | Default / source | Description |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | Yes in Production (may contain a password) | Development: LocalDB `CheckPointDb` in `appsettings.Development.json`; no default elsewhere | Database connection; required |
| `Jwt:Key` | **Yes** | User secrets (Development); environment variable or key vault (Production) | HS256 signing key, at least 32 characters; required |
| `Jwt:Issuer` | No | `CheckPoint.Api` | JWT issuer; required |
| `Jwt:Audience` | No | `CheckPoint.Client` | JWT audience; required |
| `Jwt:ExpiryMinutes` | No | `60` | Token lifetime in minutes; required, positive |
| `SeedAdmin:Email` | Personal data | User secrets (Development); **environment variable only** in Production | Optional initial admin email |
| `SeedAdmin:Password` | **Yes** | User secrets (Development); **environment variable only** in Production | Optional initial admin password |
| `FileStorage:Provider` | No | `Local` | Storage implementation; only `Local` is supported today |
| `FileStorage:RootPath` | No | `uploads` | Local storage root, relative to the API project unless absolute; required |
| `FileStorage:MaxFileSizeMB` | No | `20` | Maximum upload size in MiB; required, positive |
| `Kestrel:Limits:MaxRequestBodySize` | No | `1048576` (1 MiB) | Body size limit for every endpoint except uploads; uploads allow `MaxFileSizeMB` + 1 MiB |
| `RateLimiting:Auth:PermitLimit` | No | `10` | Register/login requests allowed per client IP per window |
| `RateLimiting:Auth:WindowSeconds` | No | `60` | Rate-limit window length in seconds |
| `Cors:AllowedOrigins` | No | empty (Development: `http://localhost:5173`) | Browser origins allowed to call the API; empty means cross-origin calls are blocked |
| `Database:AutoMigrate` | No | `false` | Apply pending migrations at startup; honored only in Development |

The application validates the required settings at startup and stops with one message listing every missing or invalid key.

Uploaded files are stored outside `wwwroot` and are not served as static files. The API saves files under a generated GUID filename; the original filename is sanitized and kept only as metadata. `CheckPoint.Api/uploads/` is ignored by Git.

Array settings such as `Cors:AllowedOrigins` are overridden by index, for example with the environment variables `Cors__AllowedOrigins__0=https://app.example.com` and `Cors__AllowedOrigins__1=https://www.example.com`. Each origin must be a full `http` or `https` origin without a path; wildcards (`*`) are rejected at startup.

## Security notes

### JWT signing key

- Development: `Jwt:Key` lives in user secrets (see Setup).
- Production: user secrets are not loaded outside Development. Provide the key through the host's secret store, for example the environment variable `Jwt__Key` set in the hosting platform's protected configuration, or a key vault (such as Azure Key Vault) added as a configuration provider. Never put the key in `appsettings*.json`, Docker images, or source control.
- Use at least 32 random bytes encoded as Base64 (the setup command generates 48 bytes). The application refuses to start if the key is missing or shorter than 32 characters.
- Tokens are validated with zero clock skew, so a token stops working exactly at `expiresAt`.

### Rotating the JWT key

The API validates tokens with a single key. Rotating it:

1. Generate a new key with the same command used in Setup.
2. Replace `Jwt:Key` (user secret, environment variable, or vault secret) and restart the application.
3. Every token signed with the old key is rejected immediately with `401`; all users must sign in again. Plan rotations for a low-traffic window, or rotate immediately if the key may have leaked.

A token is also rejected if its user no longer exists or the user's role changed after the token was issued.

### Request protection

- **Rate limiting:** `POST /api/auth/register` and `POST /api/auth/login` share a fixed-window limit per client IP. Exceeding it returns `429` with a `Retry-After` header. Behind a reverse proxy, forwarded headers must be configured so the real client IP is used; otherwise every client shares the proxy's limit.
- **Security headers:** every response carries `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, and a restrictive `Content-Security-Policy` (`default-src 'none'`; Swagger UI in Development gets a policy that allows its own scripts and styles). `Strict-Transport-Security` is sent only in the Production environment and never for `localhost`.
- **CORS:** only origins listed in `Cors:AllowedOrigins` may call the API from a browser, using `GET`, `POST`, `PUT`, `DELETE` with the `Authorization` and `Content-Type` headers. `Content-Disposition` and `Retry-After` are exposed so a frontend can read download file names and rate-limit delays.
- **Request size:** non-upload requests are limited to 1 MiB; oversized bodies return `413`.

### File handling

- Downloads are served with the content type validated and stored at upload time, never the type the client claims later, and with `Content-Disposition: attachment` containing an ASCII fallback name plus an RFC 5987 UTF-8 `filename*`.
- `IFileScanner` (`Services/IFileScanner.cs`) is called for every upload before the file is stored. The default `NoOpFileScanner` accepts every file. To add malware scanning (for example ClamAV or a cloud scanning service), implement the interface and register it in `Program.cs`. `Infected` returns `400`; `Unavailable` returns `503` and nothing is stored.

## File storage

`FileStorage:Provider` selects the storage implementation. Only `Local` is available today; any other value stops the application at startup. Cloud storage will be added behind the same `IFileStorageService` interface.

### Error handling

- Disk full while saving: `503` with "Depolama alanı dolu…". The partly written file is removed and no record is created.
- Storage root missing, not writable, or otherwise failing: `503` with "Dosya depolama şu anda kullanılamıyor…". This applies to upload, download, and delete.
- A record whose file is missing on disk returns `404` on download.

### Keeping the database and files consistent

- **Upload:** the file is written first, then the record is inserted. If the insert fails, the file is deleted.
- **Delete:** the file is first renamed to `<name>.deleted`, then the record is deleted.
  - If deleting the record fails, the file is renamed back, so both the record and the file stay as they were.
  - If renaming fails because storage is unavailable, nothing is deleted and the API returns `503`.
  - After the record is deleted, the `.deleted` file is removed.

Residual cases, each requiring two independent failures in a row, are logged with the stored file name:

| Case | Leftover | Log level | Fix |
|---|---|---|---|
| The insert fails after an upload, and deleting the new file also fails | An unreferenced file | `Error` | Safe to delete |
| The record is deleted, but removing `<name>.deleted` fails | A `.deleted` file | `Warning` | Safe to delete |
| Deleting the record fails, and renaming the file back also fails | A record whose file ends in `.deleted` | `Error` | Rename the file back by removing the `.deleted` suffix |

### Backups (local storage)

Back up the database and the `FileStorage:RootPath` folder together:

1. Back up the database first (for example `BACKUP DATABASE`, or your host's automated backups).
2. Then copy the storage folder, for example `robocopy <RootPath> <backup>\uploads /E` on Windows or `rsync -a <RootPath>/ <backup>/uploads/` on Linux.

Stored files are never modified after upload; they are only added or removed. Taking the database backup first means every record in the backup has its file in the folder copy, unless the file was deleted between the two steps. Files added after the database backup are extra and harmless. Restore both from the same backup set. Skip `*.deleted` files when copying.

| Environment | Swagger UI / OpenAPI | Auto-migrate | Seed admin source | Logs | HSTS |
|---|---|---|---|---|---|
| `Development` | `/swagger`, `/openapi/v1.json` | when `Database:AutoMigrate=true` | any configuration source (normally user secrets) | plain console | no |
| `Production` | disabled (`404`) | never | environment variables `SeedAdmin__Email` and `SeedAdmin__Password` only; other sources are ignored with a warning | JSON console, UTC timestamps | yes |

Set the environment with `ASPNETCORE_ENVIRONMENT`. `launchSettings.json` sets `Development` for local runs. Production-only settings live in `appsettings.Production.json`, which contains no secrets and no connection string.

## Logging and health checks

- Every request produces one log entry from `Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware` with `Method`, `Path` (without the query string), `StatusCode`, `Duration` (ms), `TraceId`, and `UserId` when the caller is authenticated. Headers, query strings, and request/response bodies are never logged, so passwords, tokens, and e-mail addresses do not reach the logs. An automated test checks this.
- Unhandled exceptions are logged once at `Error` level with the stack trace; clients get a generic `500` response. Rejected oversized or malformed requests are logged at `Warning`.
- EF Core SQL command logs are reduced to `Warning`. SQL parameter values are never logged because sensitive data logging is off.
- `GET /health` returns `200 Healthy` while the process is running (liveness).
- `GET /health/ready` also checks that the database accepts connections, with a 5-second timeout, and returns `200 Healthy` or `503 Unhealthy` (readiness).
- Both health endpoints are anonymous, return only the word `Healthy` or `Unhealthy`, and expose no version, connection, or error details.

## Applying migrations in Production

The application never migrates a non-Development database on startup. Generate an idempotent SQL script, review it, and apply it with an account that has schema-change rights:

```powershell
dotnet ef migrations script --idempotent --project .\CheckPoint.Api\CheckPoint.Api.csproj -o .\artifacts\migrate.sql
sqlcmd -S <server> -d <database> -U <user> -i .\artifacts\migrate.sql
```

The script checks `__EFMigrationsHistory` before each migration, so running it against a database that is already partly migrated applies only the missing steps. Back up the database before applying it. Generating the script does not connect to any database, but the tool starts the application in the Development environment, so `Jwt:Key` must be available, for example from user secrets or an environment variable.

## Endpoints

All endpoints require a valid JWT unless the access column says otherwise. `User` and `Admin` are the supported roles.

| Method | Route | Access | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register a User |
| POST | `/api/auth/login` | Public | Sign in and receive a JWT |
| GET | `/api/users/me` | User, Admin | Get the authenticated user's profile |
| GET | `/api/users/me/resources` | User, Admin | Paginated resources uploaded by the authenticated user |
| GET | `/api/Universities` | User, Admin | Paginated universities; optional `q` search on name or city |
| GET | `/api/Universities/{id}` | User, Admin | Get a university |
| POST | `/api/Universities` | Admin | Create a university |
| PUT | `/api/Universities/{id}` | Admin | Update a university |
| DELETE | `/api/Universities/{id}` | Admin | Delete a university without courses |
| GET | `/api/courses` | User, Admin | Paginated courses; optional `universityId` filter and `q` search on name or code |
| GET | `/api/courses/{id}` | User, Admin | Get a course |
| POST | `/api/courses` | User, Admin | Create a course |
| PUT | `/api/courses/{id}` | User, Admin | Update a course; university cannot be changed |
| GET | `/api/resources` | User, Admin | List resources with optional filters and pagination |
| GET | `/api/resources/{id}` | User, Admin | Get resource details |
| GET | `/api/resources/{id}/download` | User, Admin | Download the file as an attachment |
| POST | `/api/resources` | User, Admin | Upload a resource using `multipart/form-data` |
| PUT | `/api/resources/{id}` | Owner, Admin | Update title, description, and type |
| DELETE | `/api/resources/{id}` | Owner, Admin | Delete a resource and its stored file |

All list endpoints (`/api/Universities`, `/api/courses`, `/api/resources`, `/api/users/me/resources`) return `{ items, page, pageSize, totalCount }` and accept `page` (default `1`) and `pageSize` (default `20`, maximum `100`). The `q` search text is limited to 100 characters. Universities and courses are ordered by name; resources by upload time, newest first.

Resource listing accepts optional `universityId`, `courseId`, `type`, and `q` filters.

A token is rejected with `401` if its user no longer exists or the user's role has changed since the token was issued; the user must sign in again.

## File upload rules

- Accepted extensions: `.pdf`, `.doc`, `.docx`, `.ppt`, `.pptx`, `.txt`, `.png`, `.jpg`, and `.jpeg`.
- The file extension and MIME content type must match.
- Files larger than `FileStorage:MaxFileSizeMB` (20 MiB by default) are rejected.
- Empty files are rejected.
- PDF, PNG, JPG/JPEG, DOCX, and PPTX uploads are checked against their basic magic-byte signature.
- Unsupported extensions or content types, and signature mismatches, return `415`; empty files return `400`; oversized files return `413`.
- A course must belong to the selected university.

## Verification checklist

Use Swagger at `http://localhost:5118/swagger`. Register two normal users and sign in to obtain tokens. An Admin token is available only after configuring the seed-admin secrets and starting the application. Include a valid `Authorization: Bearer <token>` header for protected endpoints.

### Successful scenarios

- Register a new user: `POST /api/auth/register` returns `201`.
- Login with correct credentials: `POST /api/auth/login` returns `200`.
- Read the current profile/resources: `GET /api/users/me` and `GET /api/users/me/resources` return `200`.
- Read universities and courses with a token: GET endpoints return `200`.
- Create a course for an existing university: `POST /api/courses` returns `201`; duplicate course codes are compared after trimming and uppercasing.
- Upload an allowed, non-empty file with matching MIME type and valid university/course: `POST /api/resources` returns `201`.
- Filter and paginate resources: `GET /api/resources` returns `200` with `items`, `page`, `pageSize`, and `totalCount`.
- Download an existing file: `GET /api/resources/{id}/download` returns `200`, attachment disposition, and `X-Content-Type-Options: nosniff`.
- Update/delete a resource as its owner: PUT returns `204`; DELETE returns `204` and removes the file.
- Update/delete another user's resource as Admin: PUT returns `204`; DELETE returns `204`.
- Create, update, and delete a university as Admin: returns `201`, `204`, and `204` respectively.

### Error scenarios

- Call a protected endpoint without a token: `401` with Problem Details.
- Attempt a User-only forbidden university mutation: `403` with Problem Details.
- Attempt to update/delete another user's resource as a different User: `403`.
- Register an existing username/email or create a duplicate university/course code: `409`.
- Submit invalid model data, a nonexistent university for a course, mismatched course/university, or invalid paging: `400`.
- Request a nonexistent university, course, or resource, or download a resource whose file is missing: `404`.
- Upload an empty file: `400`; upload a file larger than the configured limit: `413`.
- Upload an unsupported extension/MIME type or a file with a mismatched signature: `415`.
- Cause an unexpected server error in a controlled development test: `500` with a generic Problem Details response and no internal exception details.

The request examples in [CheckPoint.Api.http](./CheckPoint.Api/CheckPoint.Api.http) provide a manual test starting point.
