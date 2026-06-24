# UI React

A modern React application built with Vite.

## Overview

This is a React-based user interface built with the Vite build tool for fast development and optimized production builds.

## Getting Started

### Prerequisites
- Node.js 18+
- npm or yarn

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

Starts the Vite development server on http://localhost:3000.

### Build

```bash
npm run build
```

Creates an optimized production build in the `dist` directory.

### Preview

```bash
npm run preview
```

Previews the production build locally.

## Technologies

- React 18
- Vite 4
- Node.js

## Project Structure

```
ui-react/
├── .devcontainer/      # VS Code devcontainer configuration
├── src/               # Source files
├── package.json       # Project dependencies
├── vite.config.js     # Vite configuration
└── bite.config.js     # Bite configuration
```

## Development Notes

- Uses ES modules for better code splitting and tree-shaking
- Fast HMR (Hot Module Replacement) with Vite
- Optimized build output for production deployments

## License

MIT
