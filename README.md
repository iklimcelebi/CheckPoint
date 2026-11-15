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

| Key | Default / source | Description |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server LocalDB `CheckPointDb` in `appsettings.json` | Database connection |
| `Jwt:Issuer` | `CheckPoint.Api` | JWT issuer |
| `Jwt:Audience` | `CheckPoint.Client` | JWT audience |
| `Jwt:ExpiryMinutes` | `60` | Token lifetime in minutes |
| `Jwt:Key` | User secrets | HS256 signing key; never commit |
| `SeedAdmin:Email` | User secrets | Optional initial admin email |
| `SeedAdmin:Password` | User secrets | Optional initial admin password |
| `FileStorage:RootPath` | `uploads` | Local storage root, relative to the API project unless absolute |
| `FileStorage:MaxFileSizeMB` | `20` | Maximum upload size in MiB |
| `Kestrel:Limits:MaxRequestBodySize` | `1048576` (1 MiB) | Body size limit for every endpoint except uploads; uploads allow `MaxFileSizeMB` + 1 MiB |
| `RateLimiting:Auth:PermitLimit` | `10` | Register/login requests allowed per client IP per window |
| `RateLimiting:Auth:WindowSeconds` | `60` | Rate-limit window length in seconds |
| `Cors:AllowedOrigins` | empty (Development: `http://localhost:5173`) | Browser origins allowed to call the API; empty means cross-origin calls are blocked |

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
