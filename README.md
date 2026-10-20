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

Uploaded files are stored outside `wwwroot` and are not served as static files. The API saves files under a generated GUID filename; the original filename is sanitized and kept only as metadata. `CheckPoint.Api/uploads/` is ignored by Git.

## Endpoints

All endpoints require a valid JWT unless the access column says otherwise. `User` and `Admin` are the supported roles.

| Method | Route | Access | Description |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Register a User |
| POST | `/api/auth/login` | Public | Sign in and receive a JWT |
| GET | `/api/users/me` | User, Admin | Get the authenticated user's profile |
| GET | `/api/users/me/resources` | User, Admin | Paginated resources uploaded by the authenticated user |
| GET | `/api/Universities` | User, Admin | List universities |
| GET | `/api/Universities/{id}` | User, Admin | Get a university |
| POST | `/api/Universities` | Admin | Create a university |
| PUT | `/api/Universities/{id}` | Admin | Update a university |
| DELETE | `/api/Universities/{id}` | Admin | Delete a university without courses |
| GET | `/api/courses` | User, Admin | List courses; optional `universityId` filter |
| GET | `/api/courses/{id}` | User, Admin | Get a course |
| POST | `/api/courses` | User, Admin | Create a course |
| PUT | `/api/courses/{id}` | User, Admin | Update a course; university cannot be changed |
| GET | `/api/resources` | User, Admin | List resources with optional filters and pagination |
| GET | `/api/resources/{id}` | User, Admin | Get resource details |
| GET | `/api/resources/{id}/download` | User, Admin | Download the file as an attachment |
| POST | `/api/resources` | User, Admin | Upload a resource using `multipart/form-data` |
| PUT | `/api/resources/{id}` | Owner, Admin | Update title, description, and type |
| DELETE | `/api/resources/{id}` | Owner, Admin | Delete a resource and its stored file |

Resource listing accepts optional `universityId`, `courseId`, `type`, and `q` filters, plus `page` (default `1`) and `pageSize` (default `20`, maximum `100`). Results are ordered by upload time, newest first.

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
