# MarketPlace Technical Documentation

## 1. Solution Overview

The `MarketPlace` solution is a .NET 10 multi-project application composed of a server-side web client, a backend Web API, shared DTO contracts, and supporting application and persistence layers.

At the time of writing, the implemented UI uses **ASP.NET Core MVC controllers and Razor views**, not Razor Pages, even though the workspace supports Razor assets.

### Primary runtime flow

1. A user interacts with the `Market.Client` web application.
2. `Market.Client` calls the backend through `Market.Client.API`.
3. `Market.Client.API` sends HTTP requests to `Market.API` using `HttpClient` with retry and circuit breaker policies.
4. `Market.API` routes requests to MediatR handlers in `Market.API.Application`.
5. The application layer uses abstractions such as `IIdentityService`.
6. `Market.API.Persistence` implements those abstractions using ASP.NET Core Identity and Entity Framework Core.
7. Request and response DTOs shared between the client and API live in `Market.Shared.Contracts`.

## 2. Solution Project Catalog

| Project | Type | Target Framework | Purpose |
|---|---|---:|---|
| `Market.Client` | ASP.NET Core Web | `net10.0` | End-user web application with MVC controllers and Razor views |
| `Market.Client.API` | Class Library | `net10.0` | HTTP integration layer used by `Market.Client` to call the backend API |
| `Market.Shared.Contracts` | Class Library | `net10.0` | Shared request/response DTO contracts |
| `Market.API` | ASP.NET Core Web API | `net10.0` | Backend API host, middleware, controllers, logging, and response wrapping |
| `Market.API.Application` | Class Library | `net10.0` | Application layer with MediatR commands, validators, and service abstractions |
| `Market.API.Persistence` | Class Library | `net10.0` | EF Core, ASP.NET Identity, JWT token generation, and persistence wiring |
| `Market.API.Domain` | Class Library | `net10.0` | Intended domain layer; currently placeholder only |
| `Market.API.Infrastructure` | Class Library | `net10.0` | Intended infrastructure layer; currently placeholder only |

## 3. Dependency Map

### Project references

- `Market.Client` -> `Market.Client.API`
- `Market.Client.API` -> `Market.Shared.Contracts`
- `Market.API` -> `Market.API.Application`
- `Market.API` -> `Market.API.Infrastructure`
- `Market.API` -> `Market.API.Persistence`
- `Market.API` -> `Market.Shared.Contracts`
- `Market.API.Application` -> `Market.API.Domain`
- `Market.API.Infrastructure` -> `Market.API.Application`
- `Market.API.Infrastructure` -> `Market.API.Domain`
- `Market.API.Persistence` -> `Market.API.Application`

### Architectural intent

The current layering suggests a clean or onion-style architecture:

- **Presentation layer**: `Market.Client`, `Market.API`
- **Application layer**: `Market.API.Application`
- **Persistence layer**: `Market.API.Persistence`
- **Contracts layer**: `Market.Shared.Contracts`
- **Domain and infrastructure layers**: reserved, but not yet implemented beyond placeholders

## 4. Project Details

### 4.1 `Market.Client`

**Purpose**
- Hosts the user-facing web experience.
- Manages cookie authentication and server-side session state.
- Calls backend authentication endpoints through `IAuthApiClient`.

**Key files**
- `Market.Client/Program.cs`
- `Market.Client/Controllers/AccountController.cs`
- `Market.Client/Filters/GlobalExceptionFilter.cs`
- `Market.Client/Views/Account/Login.cshtml`
- `Market.Client/Views/Account/Register.cshtml`

**Important implementation notes**
- Registers MVC with views via `AddControllersWithViews`.
- Enables session state with `AddDistributedMemoryCache` and `AddSession`.
- Uses cookie authentication with login and access denied path set to `/Account/Login`.
- Delegates API communication to `AddMarketApiClient(builder.Configuration)`.
- Uses Serilog for console and rolling file logging.

**Authentication flow in client**
- `AccountController.Login` posts `LoginRequest` to `IAuthApiClient.LoginAsync`.
- On success, the JWT token is stored in session under `JwtToken`.
- A cookie identity is created from returned user fields and roles.
- On logout, session is cleared and the authentication cookie is removed.

**Error handling**
- `GlobalExceptionFilter` logs unhandled exceptions and redirects to `Home/Error`.

### 4.2 `Market.Client.API`

**Purpose**
- Encapsulates outbound HTTP communication from the client application to the API.
- Centralizes resiliency behavior and auth header propagation.

**Key files**
- `Market.Client.API/DependencyInjection.cs`
- `Market.Client.API/Handlers/AuthHeaderHandler.cs`
- `Market.Client.API/Service/Auth/AuthApiClient.cs`
- `Market.Client.API/Interface/Auth/IAuthApiClient.cs`

**Important implementation notes**
- Registers `HttpClient` for `IAuthApiClient`.
- Reads API base URL from `ApiSettings:BaseUrl`.
- Applies timeout from `ApiSettings:TimeoutSeconds`.
- Adds Polly retry and circuit breaker policies.
- Adds `AuthHeaderHandler` to attach the JWT bearer token to non-auth requests.

**Current auth transport behavior**
- Login and registration are posted to:
  - `api/auth/login`
  - `api/auth/register`
- The API wraps responses inside a common envelope. The client auth API layer now expects and handles that wrapped response shape.

### 4.3 `Market.Shared.Contracts`

**Purpose**
- Contains DTOs shared between the client and server.
- Prevents duplicated request/response contract definitions.

**Key files**
- `Market.Shared.Contracts/Auth/LoginRequest.cs`
- `Market.Shared.Contracts/Auth/RegisterRequest.cs`
- `Market.Shared.Contracts/Auth/AuthResponse.cs`

**Contracts currently defined**
- `LoginRequest(string Email, string Password)`
- `RegisterRequest(string FullName, string Email, string Password, Guid? StoreId)`
- `AuthResponse(string Token, DateTime ExpiresAt, string Email, string FullName, IList<string> Roles)`

### 4.4 `Market.API`

**Purpose**
- Hosts HTTP endpoints for backend operations.
- Configures middleware, startup services, custom logging, and API response formatting.

**Key files**
- `Market.API/Program.cs`
- `Market.API/Extensions/ServiceCollectionExtensions.cs`
- `Market.API/Controllers/AuthController.cs`
- `Market.API/Filters/ApiResponseWrapperFilter.cs`
- `Market.API/Middleware/GlobalExceptionMiddleware.cs`
- `Market.API/Logging/RollingFileLoggerProvider.cs`
- `Market.API/Common/ApiResponse.cs`

**Important implementation notes**
- Registers controllers and globally adds `ApiResponseWrapperFilter`.
- Calls `AddMarketApiServices(builder.Configuration)` to wire application and persistence layers.
- Configures ASP.NET Core Identity with custom password and user settings.
- Uses a custom rolling file logger provider instead of default logging providers.
- Uses `GlobalExceptionMiddleware` to convert unhandled exceptions into structured JSON responses.

**Response wrapping behavior**
- Successful `ObjectResult` responses are wrapped as:
  - `Success`
  - `Message`
  - `Data`
  - `Errors`
- This behavior is implemented in `ApiResponseWrapperFilter`.
- As a result, client callers should deserialize a response envelope rather than the raw payload type.

**Controllers currently present**
- `AuthController`
- `WeatherForecastController` (template/sample controller)

### 4.5 `Market.API.Application`

**Purpose**
- Holds use-case logic, command handlers, validators, and service interfaces.
- Acts as the orchestration layer between HTTP endpoints and persistence implementations.

**Key files**
- `Market.API.Application/DependencyInjection.cs`
- `Market.API.Application/Auth/Commands/Login/LoginCommand.cs`
- `Market.API.Application/Auth/Commands/Register/RegisterCommand.cs`
- `Market.API.Application/Common/Interfaces/IIdentityService.cs`

**Important implementation notes**
- Registers MediatR handlers from the application assembly.
- Registers FluentValidation validators from the same assembly.
- Defines `IIdentityService` as the abstraction for registration and login operations.

**Implemented use cases**
- `LoginCommand` -> returns `AuthResult?`
- `RegisterCommand` -> returns created user `Guid`

**Validation**
- Registration currently validates email format, password minimum length, and full name maximum length.
- Login has command handling but no dedicated FluentValidation validator in the current code.

### 4.6 `Market.API.Persistence`

**Purpose**
- Implements data access and identity operations.
- Configures EF Core database access and ASP.NET Core Identity persistence.
- Generates JWT tokens during login.

**Key files**
- `Market.API.Persistence/DependencyInjection.cs`
- `Market.API.Persistence/AppDbContext.cs`
- `Market.API.Persistence/AppDbContextFactory.cs`
- `Market.API.Persistence/Identity/ApplicationUser.cs`
- `Market.API.Persistence/Identity/ApplicationRole.cs`
- `Market.API.Persistence/Identity/IdentityService.cs`
- `Market.API.Persistence/Migrations/20260719141800_InitIdentitySchema.cs`

**Important implementation notes**
- Registers `AppDbContext` with SQL Server.
- Stores migration history in schema `core` under `__EFMigrationsHistory`.
- Implements `IIdentityService` through `IdentityService`.
- Extends `IdentityUser<Guid>` with:
  - `FullName`
  - `StoreId`
  - `IsActive`
  - `CreatedAt`

**Database model behavior**
- Identity tables are renamed into the `core` schema.
- Examples:
  - `tbl_Users`
  - `tbl_Roles`
  - `tbl_UserRoles`
  - `tbl_UserClaims`
  - `tbl_UserLogins`
  - `tbl_RoleClaims`
  - `tbl_UserTokens`

**Authentication behavior**
- Looks up users by email.
- Rejects inactive users.
- Verifies passwords with `UserManager.CheckPasswordAsync`.
- Loads user roles from ASP.NET Identity.
- Generates JWT tokens using settings from `Jwt` configuration.
- Returns an `AuthResult` containing token, expiration, email, full name, and roles.
- Logs timing metrics for slow or failed login attempts.

### 4.7 `Market.API.Domain`

**Purpose**
- Intended to hold domain entities, value objects, and business rules.

**Current state**
- The project currently contains only `Class1.cs` and is effectively a placeholder.
- No active domain model has been implemented yet.

### 4.8 `Market.API.Infrastructure`

**Purpose**
- Intended to host infrastructure concerns that are not persistence-specific, such as external service integrations.

**Current state**
- The project currently contains only `Class1.cs` and is effectively a placeholder.
- `Market.API/Extensions/ServiceCollectionExtensions.cs` contains a commented-out infrastructure registration line, indicating the layer is planned but not wired yet.

## 5. Request and Authentication Flows

### 5.1 Registration flow

1. User submits registration form in `Market.Client`.
2. `AccountController.Register` builds a `RegisterRequest`.
3. `Market.Client.API.AuthApiClient.RegisterAsync` posts to `api/auth/register`.
4. `Market.API.Controllers.AuthController.Register` sends `RegisterCommand` through MediatR.
5. `RegisterCommandHandler` calls `IIdentityService.CreateUserAsync`.
6. `IdentityService.CreateUserAsync` creates the ASP.NET Identity user and assigns either `StoreOwner` or `Customer` role.
7. API returns the created user ID wrapped in the common API response envelope.

### 5.2 Login flow

1. User submits login form in `Market.Client`.
2. `AccountController.Login` builds a `LoginRequest`.
3. `Market.Client.API.AuthApiClient.LoginAsync` posts to `api/auth/login`.
4. `AuthController.Login` sends `LoginCommand` through MediatR.
5. `LoginCommandHandler` calls `IIdentityService.LoginAsync`.
6. `IdentityService.LoginAsync` validates the account, loads roles, and generates a JWT.
7. `Market.API` wraps the response in `ApiResponse<T>`.
8. `Market.Client.API` unwraps the response and returns `AuthResponse` to the MVC controller.
9. `Market.Client` stores the JWT in session and signs in the user with a cookie identity.

### 5.3 Outbound authenticated requests from client

1. MVC action uses a typed API client registered in `Market.Client.API`.
2. `AuthHeaderHandler` reads `JwtToken` from session.
3. If present, it adds `Authorization: Bearer <token>`.
4. Requests to `/api/auth` are excluded from bearer token injection.

## 6. Configuration

### `Market.Client/appsettings.json`

Relevant settings:
- `ApiSettings:BaseUrl`
- `ApiSettings:TimeoutSeconds`

Current purpose:
- Defines the backend API base address used by `HttpClient`.
- Defines the default request timeout for client-side API calls.

### `Market.API/appsettings.json`

Relevant settings:
- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpiryMinutes`

Current purpose:
- Defines SQL Server connectivity.
- Defines JWT signing and token metadata.

## 7. Logging and Error Handling

### Client logging
- `Market.Client` uses Serilog.
- Logs are written to the console and `Logs/client-log-.txt` rolling files.

### API logging
- `Market.API` clears default logging providers.
- A custom rolling file logger writes daily files under the API `Logs` folder.
- `IdentityService` includes timing-based operational logs around login performance.

### Error handling strategy
- Client-side unhandled MVC exceptions are redirected to `Home/Error`.
- API-side unhandled exceptions are converted to structured JSON through `GlobalExceptionMiddleware`.
- Validation failures are returned as `400 Bad Request` with an error collection.

## 8. External Frameworks and Libraries

### Shared platform
- .NET 10

### Client-side
- ASP.NET Core MVC
- Serilog
- Polly / `Microsoft.Extensions.Http.Polly`

### API-side
- ASP.NET Core Web API
- ASP.NET Core Identity
- Entity Framework Core SQL Server
- MediatR
- FluentValidation
- JWT token generation via `System.IdentityModel.Tokens.Jwt`

## 9. Known Gaps and Current Technical Notes

1. `Market.API.Domain` is not yet implemented beyond a placeholder class.
2. `Market.API.Infrastructure` is not yet implemented beyond a placeholder class.
3. Infrastructure registration in the API host is currently commented out.
4. `WeatherForecastController` appears to be template or scaffold content.
5. The solution uses MVC controllers and views for the client, not Razor Pages.
6. Because API responses are wrapped, any client integration must deserialize the envelope shape rather than raw payloads.
7. `Market.Client.API` references `Microsoft.AspNetCore.Http.Abstractions` version `2.3.11`, which differs from the rest of the solution's .NET 10 package baseline and may deserve later review.

## 10. Recommended Next Documentation Enhancements

If the solution grows, consider expanding this document with:
- Endpoint inventory and request/response examples
- Database schema diagram
- Authentication sequence diagram
- Deployment architecture
- Local development and migration steps
- Test strategy and coverage map

## 11. Reference File Index

- `Market.Client/Program.cs`
- `Market.Client/Controllers/AccountController.cs`
- `Market.Client.API/DependencyInjection.cs`
- `Market.Client.API/Handlers/AuthHeaderHandler.cs`
- `Market.Client.API/Service/Auth/AuthApiClient.cs`
- `Market.Shared.Contracts/Auth/LoginRequest.cs`
- `Market.Shared.Contracts/Auth/RegisterRequest.cs`
- `Market.Shared.Contracts/Auth/AuthResponse.cs`
- `Market.API/Program.cs`
- `Market.API/Extensions/ServiceCollectionExtensions.cs`
- `Market.API/Controllers/AuthController.cs`
- `Market.API/Filters/ApiResponseWrapperFilter.cs`
- `Market.API/Middleware/GlobalExceptionMiddleware.cs`
- `Market.API/Logging/RollingFileLoggerProvider.cs`
- `Market.API.Application/DependencyInjection.cs`
- `Market.API.Application/Common/Interfaces/IIdentityService.cs`
- `Market.API.Application/Auth/Commands/Login/LoginCommand.cs`
- `Market.API.Application/Auth/Commands/Register/RegisterCommand.cs`
- `Market.API.Persistence/DependencyInjection.cs`
- `Market.API.Persistence/AppDbContext.cs`
- `Market.API.Persistence/Identity/ApplicationUser.cs`
- `Market.API.Persistence/Identity/IdentityService.cs`
- `Market.API.Domain/Class1.cs`
- `Market.API.Infrastructure/Class1.cs`
