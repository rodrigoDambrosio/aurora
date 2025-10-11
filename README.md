# Aurora - Mobile-First Personal Planner

A clean, well-structured template for a .NET 9 backend with React 19 frontend, following Clean Architecture principles.

## 📋 Prerequisites

Before you begin, ensure you have the following installed on your machine:

### Required Software
- **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** - Latest version
- **[Node.js](https://nodejs.org/)** - Version 18 or higher (includes npm)
- **[Git](https://git-scm.com/)** - For cloning the repository
- **[Visual Studio Code](https://code.visualstudio.com/)** - Recommended IDE

### Recommended VS Code Extensions
- **C# Dev Kit** - Microsoft C# support
- **Thunder Client** or **REST Client** - API testing
- **ES7+ React/Redux/React-Native snippets** - React development
- **Prettier** - Code formatting
- **Auto Rename Tag** - HTML/JSX tag management

## Getting Started

### 1. Clone the Repository
```bash
git clone https://github.com/rodrigoDambrosio/aurora.git
cd aurora
```

### 2. Verify Prerequisites
```bash
# Check .NET version (should be 9.x)
dotnet --version

# Check Node.js version (should be 18+)
node --version

# Check npm version
npm --version
```

### 3. Backend Setup

#### Install Backend Dependencies
```bash
# Navigate to backend folder
cd backend

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build
```

#### Run Backend
```bash
# Run from backend folder
dotnet run --project Aurora.Api/Aurora.Api.csproj

# OR run from project root
dotnet run --project backend/Aurora.Api/Aurora.Api.csproj
```

The backend will start at: **http://localhost:5291**

### 4. Frontend Setup

#### Install Frontend Dependencies
```bash
# Navigate to frontend folder (from project root)
cd frontend

# Install npm packages
npm install
```

#### Run Frontend
```bash
# Start development server (from frontend folder)
npm run dev
```

The frontend will start at: **http://localhost:5173**

## Quick Commands

### Using VS Code Tasks (Recommended)
Open VS Code in the project root and use these tasks:

```
Ctrl+Shift+P → "Tasks: Run Task" → Select:
```
- **`build-backend`** - Build backend only
- **`start-frontend`** - Start frontend only  
- **`start-fullstack`** - Start both backend and frontend

### Manual Commands
```bash
# Backend (from project root)
dotnet run --project backend/Aurora.Api/Aurora.Api.csproj

# Frontend (from frontend folder)
cd frontend && npm run dev

# Build backend
dotnet build backend/Aurora.Api/Aurora.Api.csproj

# Build frontend for production
cd frontend && npm run build
```

## Verify Installation

### 1. Test Backend Health
Open your browser or use curl:
```bash
# Health check
curl http://localhost:5291/api/health

# Test data endpoint
curl http://localhost:5291/api/health/test
```

Expected response:
```json
{
  "status": "Healthy",
  "message": "Aurora API is running successfully!",
  "timestamp": "2025-09-27T...",
  "version": "1.0.0",
  "environment": "Development"
}
```

### 2. Test Frontend
1. Navigate to **http://localhost:5173**
2. You should see the Aurora connectivity test page
3. Click "Probar Estado del Servidor" - should show green success
4. Click "Obtener Datos de Prueba" - should show test data

### 3. Test Full Integration
The frontend automatically tests backend connectivity and displays:
- Backend connection status
- Sample data retrieval
- Request/response information

## 📁 Project Structure

```
Aurora/
├── backend/                    # .NET 9 Backend (Clean Architecture)
│   ├── Aurora.Api/            # Web API Layer
│   │   ├── Controllers/          # API Controllers
│   │   ├── Properties/           # Launch settings
│   │   ├── Program.cs           # App configuration
│   │   └── Aurora.Api.csproj    # Project file
│   ├── Aurora.Application/    # Application Services Layer
│   ├── Aurora.Domain/         # Domain Entities & Business Logic
│   │   └── Entities/            # Domain entities (User, Event, etc.)
│   ├── Aurora.Infrastructure/ # Data Access & External Services
│   └── Aurora.sln               # Solution file
├── frontend/                   # React 19 + TypeScript Frontend
│   ├── src/
│   │   ├── components/        # React components
│   │   ├── services/          # API service layer
│   │   ├── App.tsx              # Main app component
│   │   └── main.tsx             # App entry point
│   ├── package.json             # npm dependencies
│   ├── vite.config.ts          # Vite configuration
│   └── tsconfig.json           # TypeScript configuration
├── .vscode/                   # VS Code settings
└── README.md                    # This file
```

## ⚙️ Configuration Details

### Backend Configuration
- **Port**: 5291 (HTTP)
- **Environment**: Development
- **CORS**: Enabled for `http://localhost:5173`
- **Database**: SQLite (configured, not implemented yet)
- **Validation**: FluentValidation ready

### Frontend Configuration
- **Port**: 5173 (Vite default)
- **Proxy**: `/api` requests → `http://localhost:5291`
- **Build Tool**: Vite
- **Framework**: React 19 + TypeScript

### VS Code Tasks
- All tasks defined in `.vscode/tasks.json`
- Use `Ctrl+Shift+P` → "Tasks: Run Task" to access
- Background tasks for development servers

## Troubleshooting

### Common Issues

#### 1. "Failed to determine HTTPS port for redirect"
**Solution**: Already fixed! Backend runs HTTP-only in development.

#### 2. Backend won't start
```bash
# Check if port 5291 is in use
netstat -ano | findstr :5291

# Kill process if needed (replace PID)
taskkill /PID <process_id> /F

# Try running again
dotnet run --project backend/Aurora.Api/Aurora.Api.csproj
```

#### 3. Frontend can't connect to backend
- Ensure backend is running on port 5291
- Check browser console for CORS errors
- Verify proxy configuration in `frontend/vite.config.ts`

#### 4. npm install fails
```bash
# Clear npm cache
npm cache clean --force

# Delete node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

#### 5. .NET build errors
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

### Getting Help

1. **Check terminal output** for specific error messages
2. **Open browser DevTools** to inspect network requests
3. **Verify all prerequisites** are installed with correct versions
4. **Check VS Code Problems panel** for build errors

## 📝 Development Guidelines

### Code Standards
- **Language**: Code in English, UI text in Spanish
- **Backend**: PascalCase for public members, camelCase for private
- **Frontend**: camelCase for variables, PascalCase for components
- **Validation**: Use FluentValidation (already configured)
- **API**: Centralized in `frontend/src/services/apiService.ts`

### Architecture Principles
- **Clean Architecture** with proper layer separation
- **Dependency Injection** ready in all layers
- **SOLID principles** followed
- **Mobile-first** responsive design
- **API-first** development approach

## What's Included

This is a **clean template** with:
- Working health check endpoints (backend to frontend connectivity test)
- Proper Clean Architecture structure
- CORS configuration for development
- TypeScript setup with proper error handling
- VS Code tasks for easy development
- All necessary dependencies configured
- Mobile-responsive frontend framework

**No business features implemented** - just a solid foundation to build upon!

## Support

If you encounter any issues:
1. Check the troubleshooting section above
2. Ensure all prerequisites are correctly installed
3. Verify the step-by-step installation process
4. Check that both backend (5291) and frontend (5173) ports are available

---

**Happy Coding!**