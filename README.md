# MarketPlace

A multi-store marketplace application built with **.NET 10**, **ASP.NET Core MVC**, and **ASP.NET Core Web API**. The solution separates the user-facing web application from backend API services and organizes application logic, persistence, and shared contracts into dedicated projects.

> **Project status:** In active development. Authentication and the initial application structure are implemented. Product catalog, ordering, payments, and other marketplace capabilities should be considered planned until they are implemented.

## Overview

MarketPlace is being developed as a foundation for a multi-store e-commerce platform. Its current focus is the application architecture, client-to-API communication, and user authentication.

### Current capabilities

- User registration and login through the web client and backend API.
- JWT generation and role-based identity integration with ASP.NET Core Identity.
- Cookie-based sign-in for the MVC web application.
- Shared request and response DTOs between client and API.
- API response envelopes and centralized exception handling.
- Configurable HTTP client timeout, retry, and circuit-breaker policies.
- Application logging for client and API components.

### Technology stack

| Area | Technologies |
| --- | --- |
| Framework | .NET 10 |
| Web client | ASP.NET Core MVC, Razor views |
| Backend | ASP.NET Core Web API |
| Application patterns | MediatR, FluentValidation |
| Authentication | ASP.NET Core Identity, JWT, cookie authentication |
| Data access | Entity Framework Core, SQL Server |
| HTTP resiliency | HttpClient, Polly |
| Logging | Serilog on the client; rolling-file logging in the API |
| Shared contracts | C# class library / DTOs |

## Solution structure

| Project | Responsibility |
| --- | --- |
| `Market.Client` | User-facing web application, MVC controllers and Razor views |
| `Market.Client.API` | Typed HTTP clients and communication from the web client to the backend |
| `Market.API` | Backend API host, controllers, middleware, and response formatting |
| `Market.API.Application` | Use cases, MediatR commands/handlers, validators, and application abstractions |
| `Market.API.Persistence` | Entity Framework Core, SQL Server, ASP.NET Core Identity, and data access |
| `Market.API.Domain` | Reserved for domain entities and business rules; currently a placeholder |
| `Market.API.Infrastructure` | Reserved for external infrastructure integrations; currently a placeholder |
| `Market.Shared.Contracts` | DTOs shared between the client and API |
| `MarketPlace.slnx` | .NET solution file |

## Architecture

The main request flow is:

```text
Browser
   |
   v
Market.Client
(ASP.NET Core MVC)
   |
   v
Market.Client.API
(HttpClient + resiliency)
   |
   v
Market.API
(ASP.NET Core Web API)
   |
   v
Market.API.Application
(MediatR + validation)
   |
   v
Application abstractions
   |
   v
Market.API.Persistence
(EF Core + ASP.NET Core Identity)
   |
   v
SQL Server
```

The client and API share request/response types through `Market.Shared.Contracts`. The architecture is separated into layers, while the Domain and Infrastructure projects are currently placeholders and should not be considered fully implemented.

## Getting started

### Prerequisites

Install the following before running the solution:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [SQL Server](https://www.microsoft.com/sql-server) or a compatible SQL Server instance
- An IDE or editor such as [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)

### 1. Clone the repository

```bash
git clone https://github.com/menghour-huy/MarketPlace.git
cd MarketPlace
```

### 2. Configure the backend API

Set the database connection string and JWT settings for `Market.API` using **User Secrets**, environment variables, or another secure configuration provider.

The main configuration keys are:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpiryMinutes`

For local development, you can use User Secrets from the API project directory:

```bash
cd Market.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_SQL_SERVER_CONNECTION_STRING"
dotnet user-secrets set "Jwt:Key" "YOUR_SECURE_RANDOM_SECRET_KEY"
dotnet user-secrets set "Jwt:Issuer" "YOUR_JWT_ISSUER"
dotnet user-secrets set "Jwt:Audience" "YOUR_JWT_AUDIENCE"
dotnet user-secrets set "Jwt:ExpiryMinutes" "60"
cd ..
```

Replace every placeholder with your local values. Use a sufficiently long, random JWT signing key and **never commit secrets, passwords, or production connection strings** to source control.

### 3. Configure the web client

Configure the API base URL in `Market.Client` using local configuration or environment variables:

- `ApiSettings:BaseUrl`
- `ApiSettings:TimeoutSeconds`

Make sure the URL points to the address and port where the backend API is listening.

### 4. Restore and build

From the repository root:

```bash
dotnet restore MarketPlace.slnx
dotnet build MarketPlace.slnx
```

### 5. Apply database migrations

The persistence project uses Entity Framework Core with SQL Server. Review the existing migrations and configured startup project before applying them. If migrations are already set up for your local database, a typical command is:

```bash
dotnet ef database update --project Market.API.Persistence --startup-project Market.API
```

If the EF Core CLI is not installed, install it using the official [EF Core tools documentation](https://learn.microsoft.com/ef/core/cli/dotnet). Verify the configured connection string and migration setup before running database commands.

### 6. Run the applications

Run the backend API and web client in separate terminals. For example:

Terminal 1 — API:

```bash
dotnet run --project Market.API
```

Terminal 2 — web client:

```bash
dotnet run --project Market.Client
```

Use the actual local URLs printed by each application. Ensure `Market.Client` has the correct `ApiSettings:BaseUrl` and that the API can connect to SQL Server.

## Authentication flow

1. A user submits the login or registration form in `Market.Client`.
2. The client API layer sends the request to `Market.API`.
3. The API forwards the use case to a MediatR command handler.
4. The persistence implementation uses ASP.NET Core Identity to manage user credentials and roles.
5. On successful login, the API returns an authentication response containing a JWT.
6. The web client stores the token in session and creates an authentication cookie for the signed-in user.
7. Authenticated API calls can attach the token as a bearer token.

The API uses a common response envelope. Client API calls should account for the wrapped response format rather than assuming every endpoint returns a raw DTO.

## Documentation

- [Technical Documentation](TECHNICAL_DOCUMENTATION.md) — solution projects, implementation notes, configuration, and request flows.
- [Timeout Fix Summary](TIMEOUT_FIX_SUMMARY.md) — notes on HTTP timeout troubleshooting and fixes.

## Security notes

- Keep secrets and credentials out of source control.
- Use HTTPS for non-local environments.
- Use secure, environment-specific JWT signing keys.
- Review cookie settings, token lifetime, authorization policies, and session handling before production deployment.
- Validate and test all configuration and database migrations before using real data.

## Roadmap

The broader marketplace roadmap may include the following areas as the project evolves:

- Store and seller management
- Product catalog and categories
- Product search and filtering
- Shopping cart and checkout
- Order management
- Payment and delivery integrations
- Public product pages and search-engine optimization
- Automated tests and deployment pipelines

These are roadmap ideas, not a claim that the features are already available.

## Contributing

Contributions and suggestions are welcome. For substantial changes, open an issue first to discuss the proposed approach. Keep changes focused, follow the existing project boundaries, and add tests when practical.

## License

No license has currently been specified. Unless a license is added to this repository, reuse and redistribution are not automatically granted.
