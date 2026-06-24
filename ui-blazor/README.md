# UI Blazor

A Blazor WebAssembly user interface application.

## Overview

This project is a Blazor WebAssembly-based user interface that runs in the browser using .NET/C#.

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
dotnet watch run
```

Starts the development server on https://localhost:7001.

### Publish

```bash
dotnet publish -c Release
```

Creates a production build in the `bin/Release/net7.0/publish` directory.

## Project Structure

```
ui-blazor/
├── ui/                        # Blazor WebAssembly project
│   ├── ui.csproj             # Project file
│   ├── App.razor             # Root component
│   ├── Program.cs            # Entry point
│   └── Pages/                # Page components
├── uiblazor.sln              # Solution file
└── README.md
```

## Technologies

- Blazor WebAssembly
- .NET 7
- C#

## Development Notes

- Blazor components are written in C# with Razor syntax
- The application runs in the browser via WebAssembly
- Hot reload is supported during development with `dotnet watch`

## License

MIT
