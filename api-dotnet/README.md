# API DotNet

A .NET 7 REST API with ASP.NET Core.

## Overview

This is a backend API built with ASP.NET Core featuring a layered architecture with separate API and Core projects.

## Projects

- **api**: ASP.NET Core Web API project with controllers and endpoints
- **core**: Core business logic and data models library

## Getting Started

### Prerequisites
- .NET 7 SDK or later
- Visual Studio 2022 or Visual Studio Code with C# extension

### Build

```bash
dotnet build
```

### Development

```bash
cd api
dotnet watch run
```

Starts the development server on https://localhost:7001.

### Run Tests

```bash
dotnet test
```

### Publish

```bash
dotnet publish -c Release
```

Creates a production build.

## Project Structure

```
api-dotnet/
├── api/                      # ASP.NET Core Web API
│   ├── api.csproj           # API project file
│   ├── Program.cs           # Startup configuration
│   ├── Controllers/         # API endpoints
│   └── appsettings.json     # Configuration
├── api/
│   ├── core.csproj          # Core library project file
│   ├── Models/              # Domain models
│   └── Services/            # Business logic
├── apidotnet.sln            # Solution file
└── README.md
```

## Technologies

- ASP.NET Core 7
- .NET 7
- C#
- Swagger/OpenAPI

## Development Notes

- Uses dependency injection for loose coupling
- Includes Swagger UI for API documentation
- Supports CORS for cross-origin requests
- Ready for database integration

## License

MIT
