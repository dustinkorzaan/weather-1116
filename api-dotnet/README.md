# api-dotnet

A .NET API application for building RESTful web services.

## Overview

This project contains a .NET API with separate projects for the API layer and core business logic.

## Project Structure

```
api-dotnet/
├── apidotnet.sln
├── api/
│   ├── api.csproj
│   ├── Program.cs
│   ├── Controllers/
│   ├── Services/
│   ├── appsettings.json
│   └── Startup.cs
├── core/
│   ├── core.csproj
│   ├── Models/
│   ├── Interfaces/
│   └── Services/
└── README.md
```

## Prerequisites

- .NET 8.0 or higher
- Visual Studio, Visual Studio Code, or Rider (recommended)

## Getting Started

### Build the Solution

```bash
dotnet build
```

### Run the API

```bash
dotnet run --project api/api.csproj
```

The API will be available at `https://localhost:5001`

## Project Details

### api.csproj
RESTful API endpoints and controllers

### core.csproj
Core business logic and models

## License

MIT
