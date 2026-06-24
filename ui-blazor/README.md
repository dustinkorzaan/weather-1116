# ui-blazor

A Blazor UI application for building interactive web applications with C#.

## Overview

This project contains a Blazor WebAssembly or Server application for creating modern, responsive web interfaces using C# instead of JavaScript.

## Project Structure

```
ui-blazor/
├── uiblazor.sln
├── ui/
│   ├── ui.csproj
│   ├── App.razor
│   ├── Program.cs
│   ├── Pages/
│   ├── Components/
│   ├── wwwroot/
│   └── appsettings.json
└── README.md
```

## Prerequisites

- .NET 6.0 or higher
- Visual Studio, Visual Studio Code, or Rider (recommended)

## Getting Started

### Build the Solution

```bash
dotnet build
```

### Run the Application

```bash
dotnet run --project ui/ui.csproj
```

The application will be available at `https://localhost:5001`

## Development

- **Components** - Reusable Blazor components
- **Pages** - Page components for routing
- **wwwroot** - Static files (CSS, JavaScript, images)

## License

MIT
