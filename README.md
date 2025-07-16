# Bookstore API

## Overview

This repository contains a comprehensive .NET 8 Web API for managing a bookstore, complete with:

* **Books**: CRUD operations, price updates, genres, authors, and average review ratings
* **Authors & Genres**: Many-to-many relationships
* **Reviews**: Rating (1–5) and text
* **Top-10 Rated Endpoint**: Using raw SQL for performance
* **Scheduled Import**: Quartz.NET job that simulates importing 100,000 books hourly
* **Logging**: Structured logging with Serilog (console + rolling file)
* **Authentication & Authorization**: JWT-based, with `Read` and `ReadWrite` roles
* **Swagger UI**: With Bearer token support
* **Database Seeder**: Console app to seed initial data
* **Unit & Integration Tests**: xUnit tests covering services, jobs, and controllers
* **Fuzzy Matching**: Levenshtein algorithm to filter near-duplicate titles

The solution consists of five projects:

1. **BookstoreApi** – The ASP.NET Core Web API
2. **BookstoreSync** – Shared entities, EF Core `DbContext`, and migrations
3. **BookstoreSeeder** – Console application to seed the database
4. **BookstoreApi.UnitTests** – Unit tests for services and jobs
5. **BookstoreApi.IntegrationTests** – Integration tests using in-memory SQLite

---

## Prerequisites

* .NET 8 SDK
* SQL Server (for development/production)

---

## Solution Structure

```
/BookstoreApiDigacon.sln
  /BookstoreApi           # Web API project
  /BookstoreSync          # EF Core models, DbContext, Migrations
  /BookstoreSeeder        # Console app for seeding data
  /BookstoreApi.UnitTests # xUnit unit tests
  /BookstoreApi.IntegrationTests # xUnit integration tests
  /README.md              # This file
```

### Key Files

* **Program.cs**: Application startup—Serilog, DI, Quartz, JWT, Swagger, EF migrations
* **DependencyInjection.cs**: Extension methods to register DbContext, services, Quartz, JWT, Swagger
* **BookImportJob.cs**: Quartz job implementation (100k imports, Levenshtein fuzzy matching)
* **AuthController.cs**: JWT login endpoint
* **BooksController.cs**: CRUD endpoints with `[Authorize]` policies
* **BookstoreService.cs**: Business logic, EF Core queries, raw SQL for top-10

---

## Setup & Configuration

1. **Clone the repository**:

   ```bash
   git clone https://github.com/YourOrg/BookstoreApiDigacon.git
   cd BookstoreApiDigacon
   ```

2. **Configure connection string** in `BookstoreApi/appsettings.json`:

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=BookstoreDigaconDb;Trusted_Connection=True;TrustServerCertificate=True;"
     },
     "JwtSettings": {
       "Key": "<your-very-long-secure-key>",
       "Issuer": "BookstoreApi",
       "Audience": "BookstoreClient",
       "DurationInMinutes": 60
     },
     "BookImportSettings": {
       "FuzzyThreshold": 2
     }
   }
   ```

   > **Note:** Replace `<your-very-long-secure-key>` with a secure random string.

3. **Apply migrations and seed**:

   * **Option A**: Let the API apply migrations on startup and run seeder manually:

     ```bash
     cd BookstoreSeeder
     dotnet run
     ```
   * **Option B**: Automigrate and run API directly:

     ```bash
     dotnet run --project BookstoreApi
     ```

---

## Running the API

```bash
cd BookstoreApi
dotnet run
```

* The API listens on `https://localhost:7014`.
* Swagger is available at **`https://localhost:7014/swagger/index.html`** (in Development).

---

## Logging with Serilog

* **Console**: Colored, human-readable output
* **File**: `../Logs/apilogs-{Date}.txt`, daily rolling

Configured in `Program.cs` and `DependencyInjection` extensions.

---

## Authentication & Authorization

1. **JWT Settings** in `appsettings.json` → bound to `JwtSettings` POCO
2. **Register** in `DependencyInjection.AddJwtAuthentication`:

   ```csharp
   builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
       .AddJwtBearer(options => { /* ... */ });
   builder.Services.AddAuthorization(options =>
   {
       options.AddPolicy(AuthPolicies.RequireReadRole,
           policy => policy.RequireRole(AuthRoles.Read, AuthRoles.ReadWrite));
       options.AddPolicy(AuthPolicies.RequireReadWriteRole,
           policy => policy.RequireRole(AuthRoles.ReadWrite));
   });
   ```
3. **AuthController** issues tokens:

   ```csharp
   [HttpPost("login")]
   [AllowAnonymous]
   public IActionResult Login(LoginRequest req) { /*...*/ }
   ```
4. **Protect endpoints** in `BooksController`:

   ```csharp
   [Authorize(Policy = AuthPolicies.RequireReadRole)]  // GET
   [Authorize(Policy = AuthPolicies.RequireReadWriteRole)]  // POST/PUT/DELETE
   ```
5. **Swagger**: `AddSwaggerWithJwt` adds Bearer scheme and UI support

---

## Scheduled Import (Quartz.NET)

Configuration and job logic are defined in `DependencyInjection.AddQuartzService` and `BookImportJob`.
Manual trigger via `AdminController` at `/api/admin/trigger-import`.

---

## Database Seeder

Run the `BookstoreSeeder` console app to clear and seed initial data:

```bash
cd BookstoreSeeder
dotnet run
```

---

## Testing

### Unit Tests

```bash
dotnet test BookstoreApi.UnitTests
```

### Integration Tests

```bash
dotnet test BookstoreApi.IntegrationTests
```

---

## Fuzzy Matching

Configured via `BookImportSettings.FuzzyThreshold`, defaults to 2.
Levenshtein algorithm implemented in `BookImportJob` with unit tests in `BookImportJobTests`.
