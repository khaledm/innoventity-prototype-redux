# Quickstart Guide: Innoventity Platform Development

Get the Innoventity Platform running locally for Phase 0 development in under 30 minutes.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Clone Repository](#clone-repository)
- [Backend Setup](#backend-setup)
- [Frontend Setup](#frontend-setup)
- [Running Tests](#running-tests)
- [Deployment to Azure](#deployment-to-azure)
- [Common Issues](#common-issues)
- [Next Steps](#next-steps)

---

## Prerequisites

Ensure you have the following tools installed before proceeding:

### Required Tools

| Tool | Version | Purpose | Installation |
|------|---------|---------|--------------|
| **.NET SDK** | 8.0+ | Backend API runtime | [Download](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Node.js** | 18.x or 20.x LTS | Frontend build tooling | [Download](https://nodejs.org/) |
| **Angular CLI** | 18.x | Frontend development server | `npm install -g @angular/cli@18` |
| **Git** | 2.40+ | Version control | [Download](https://git-scm.com/) |
| **SQL Server** | 2019+ or LocalDB | Database (local dev) | [Download SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads) |
| **Azure CLI** *(optional)* | 2.50+ | Azure deployment | [Download](https://learn.microsoft.com/cli/azure/install-azure-cli) |
| **Azure Developer CLI** *(optional)* | 1.5+ | Simplified Azure deployment | [Install azd](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd) |

### Optional Tools (Recommended)

- **Docker Desktop**: Run SQL Server in a container (avoids local SQL Server installation)
- **Visual Studio 2022** or **VS Code**: IDEs with excellent .NET/Angular support
- **Postman** or **Scalar**: API testing tools (Scalar UI is auto-generated at `/scalar` endpoint)

### Verify Installations

```powershell
# Verify .NET SDK
dotnet --version
# Expected: 8.0.x

# Verify Node.js and npm
node --version
npm --version
# Expected: v18.x or v20.x, 9.x or 10.x

# Verify Angular CLI
ng version
# Expected: Angular CLI: 18.x

# Verify Git
git --version
# Expected: 2.40+

# Verify SQL Server (if installed locally)
sqlcmd -?
# OR check Docker
docker --version
```

---

## Clone Repository

1. **Clone the repository**:

```powershell
git clone https://github.com/yourusername/innoventity-prototype-redux.git
cd innoventity-prototype-redux
```

2. **Checkout the feature branch**:

```powershell
git checkout 001-platform-core
```

3. **Verify you're on the correct branch**:

```powershell
git branch --show-current
# Expected: 001-platform-core
```

---

## Backend Setup

### Step 1: Restore NuGet Packages

```powershell
cd src/Innoventity.API
dotnet restore
```

**Expected output**: Packages restored successfully without errors.

### Step 2: Configure Database Connection String

Choose **one** of the following options:

#### Option A: Local SQL Server (Windows)

Create or edit `src/Innoventity.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=InnoventityDB;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "Jwt": {
    "SecretKey": "dev-secret-key-change-in-production-min-32-chars",
    "Issuer": "https://localhost:5001",
    "Audience": "https://localhost:5001",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

#### Option B: Docker Container (Cross-platform)

Start SQL Server in Docker:

```powershell
docker run --name innoventity-sql `
  -e "ACCEPT_EULA=Y" `
  -e "SA_PASSWORD=YourStrong!Passw0rd" `
  -p 1433:1433 `
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Then create `appsettings.Development.json` with:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=InnoventityDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=true;"
  }
}
```

#### Option C: User Secrets (Recommended for Security)

Store sensitive data outside source control:

```powershell
cd src/Innoventity.API

# Initialize user secrets
dotnet user-secrets init

# Set connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=InnoventityDB;Trusted_Connection=true;TrustServerCertificate=true;"

# Set JWT secret key
dotnet user-secrets set "Jwt:SecretKey" "dev-secret-key-change-in-production-min-32-chars"

# List secrets
dotnet user-secrets list
```

### Step 3: Install EF Core Tools (if not already installed)

```powershell
dotnet tool install --global dotnet-ef
# OR update if already installed
dotnet tool update --global dotnet-ef
```

### Step 4: Run Database Migrations

```powershell
# From src/Innoventity.API directory
dotnet ef database update

# Expected output:
# Build started...
# Build succeeded.
# Applying migration '20260208_InitialCreate'.
# Done.
```

**What this does**: Creates the `InnoventityDB` database with Phase 0 tables (Actor, Industry, Innovation) and indexes.

### Step 5: Seed Initial Data (Optional)

If a seed script exists:

```powershell
dotnet run --project src/Innoventity.API -- seed-data
```

**Manual seeding** (if no script):
- Connect to database using SQL Server Management Studio (SSMS) or Azure Data Studio
- Insert test industries (Sectors and Subsectors)
- Insert test actors with activated accounts

### Step 6: Run the Backend API

```powershell
# From src/Innoventity.API directory
dotnet run
```

**Expected output**:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Step 7: Verify Backend is Running

Open your browser and navigate to:

- **Scalar API Documentation**: [https://localhost:5001/scalar](https://localhost:5001/scalar)
- **Swagger/OpenAPI JSON**: [https://localhost:5001/swagger/v1/swagger.json](https://localhost:5001/swagger/v1/swagger.json)

You should see interactive API documentation with all Phase 0 endpoints:
- `POST /api/auth/register`
- `POST /api/auth/activate`
- `POST /api/auth/login`
- `POST /api/auth/refresh-token`
- `GET /api/innovations/{id}`

---

## Frontend Setup

### Step 1: Install NPM Dependencies

```powershell
cd src/Innoventity.Client
npm install
```

**Expected output**: Dependencies installed without errors (warnings about peer dependencies are normal).

### Step 2: Configure API Endpoint

Edit `src/Innoventity.Client/src/environments/environment.development.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api',
  apiTimeout: 30000, // 30 seconds
  enableDebugLogging: true
};
```

### Step 3: Run the Frontend Development Server

```powershell
# From src/Innoventity.Client directory
ng serve

# OR with custom port
ng serve --port 4200
```

**Expected output**:
```
✔ Browser application bundle generation complete.

Initial Chunk Files   | Names         |  Raw Size
main.js               | main          |  50.23 kB |

                      | Initial Total |  50.23 kB

Application bundle generation complete. [1.234 seconds]

Watch mode enabled. Watching for file changes...
  ➜  Local:   http://localhost:4200/
```

### Step 4: Verify Frontend is Running

Open your browser and navigate to: [http://localhost:4200](http://localhost:4200)

**Expected behavior**:
- Landing page displays
- Registration/Login forms are accessible
- Browser console shows no errors (F12 to open DevTools)

### Step 5: Test End-to-End Flow (Manual)

1. **Register a new actor**:
   - Navigate to registration page
   - Fill in form (email, full name, contact address, actor type, password)
   - Submit → Should see "Check your email for activation link" message

2. **Activate account** (dev workaround):
   - Check API logs or database for `ActivationToken`
   - Navigate to activation page with token: `http://localhost:4200/activate?token={token}`
   - OR use Scalar UI to call `POST /api/auth/activate` directly

3. **Login**:
   - Navigate to login page
   - Enter email, actor type, password
   - Submit → Should redirect to dashboard with JWT token stored
   - Verify tokens in browser DevTools: Application → Local Storage → `accessToken`, `refreshToken`

4. **View innovation** (if seeded):
   - Navigate to innovation detail page: `http://localhost:4200/innovations/{id}`
   - Should display innovation title, research background, owner info
   - Verify authenticated request: Network tab shows `Authorization: Bearer <token>` header

5. **Logout** (client-side only):
   - Click logout button (or manually test)
   - Tokens removed from localStorage: `localStorage.removeItem('accessToken')` and `localStorage.removeItem('refreshToken')`
   - User redirected to login page
   - Verify tokens cleared: Application → Local Storage (should be empty)
   - **Note**: No server-side revocation in Phase 0 - existing tokens remain valid until 1hr expiry (acceptable trade-off per KISS principle)

---

## Running Tests

### Backend Tests

#### Unit Tests (xUnit)

```powershell
# Run all unit tests
cd tests/Innoventity.API.Tests
dotnet test --filter "Category=Unit"

# Run with coverage
dotnet test --filter "Category=Unit" --collect:"XPlat Code Coverage"

# Generate coverage report (requires ReportGenerator tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

**Coverage Target**: > 80% for unit tests

#### Integration Tests (WebApplicationFactory)

```powershell
# Run all integration tests (uses in-memory database)
dotnet test --filter "Category=Integration"

# Run specific feature integration tests
dotnet test --filter "Category=Integration&FullyQualifiedName~Authentication"
```

**Coverage Target**: 100% endpoint coverage

### Frontend Tests

#### Unit Tests (Karma + Jasmine)

```powershell
cd src/Innoventity.Client

# Run tests once
ng test --watch=false --code-coverage

# Run tests in watch mode
ng test
```

**Coverage Target**: > 80% for unit tests

### End-to-End Tests (Playwright)

```powershell
# Install Playwright browsers (first time only)
cd tests/Innoventity.API.Tests/E2E
npx playwright install

# Run E2E tests (requires API and Client running)
# Terminal 1: Start API
cd src/Innoventity.API
dotnet run

# Terminal 2: Start Client
cd src/Innoventity.Client
ng serve

# Terminal 3: Run E2E tests
cd tests/Innoventity.API.Tests/E2E
npx playwright test

# Run with UI mode (interactive)
npx playwright test --ui

# Run specific test file
npx playwright test tests/user-registration.spec.ts
```

**Coverage**: P0 user journeys (J1: Register Actor, J2: Login, J3: View Innovation)

### Mutation Tests (Stryker.NET)

**Note**: Run mutation tests periodically (e.g., weekly), not on every commit (they take 10-30 minutes).

```powershell
# Install Stryker.NET (first time only)
dotnet tool install -g dotnet-stryker

# Run mutation tests on specific project
cd tests/Innoventity.API.Tests
dotnet stryker

# Generate HTML report
dotnet stryker --reporter html --reporter progress

# Open report
start StrykerOutput\reports\mutation-report.html
```

**Mutation Score Target**: > 70%

---

## Deployment to Azure

### Prerequisites

- Azure subscription (free trial available at [azure.microsoft.com/free](https://azure.microsoft.com/free))
- Azure Developer CLI (`azd`) installed
- Azure CLI (`az`) installed (for manual deployments)

### Option 1: Azure Developer CLI (Recommended)

The fastest way to deploy to Azure:

```powershell
# Login to Azure
azd auth login

# Initialize (first time only)
azd init

# Provision infrastructure + Deploy application
azd up

# Follow prompts:
# - Select subscription
# - Choose region (e.g., East US 2, West Europe)
# - Confirm resource creation
```

**What `azd up` does**:
1. Creates Azure Resource Group
2. Provisions Azure SQL Database
3. Provisions Azure App Service (Linux)
4. Provisions Azure Application Insights
5. Builds and deploys API to App Service
6. Builds and deploys Angular app to Azure Static Web Apps or App Service

**Expected output**:
```
SUCCESS: Your application was provisioned and deployed to Azure in 8 minutes.

You can view the resources created under the resource group innoventity-rg-dev in Azure Portal:
https://portal.azure.com/#@/resource/subscriptions/.../resourceGroups/innoventity-rg-dev

Endpoints:
- API: https://innoventity-api-dev.azurewebsites.net
- Client: https://innoventity-client-dev.azurewebsites.net
- Application Insights: https://portal.azure.com/...
```

### Option 2: Manual Deployment (Azure CLI)

For more control over infrastructure:

```powershell
# Login
az login

# Set subscription
az account set --subscription "Your Subscription Name"

# Create resource group
az group create --name innoventity-rg-dev --location eastus2

# Create Azure SQL Database
az sql server create `
  --name innoventity-sql-dev `
  --resource-group innoventity-rg-dev `
  --location eastus2 `
  --admin-user sqladmin `
  --admin-password "YourStrong!Passw0rd"

az sql db create `
  --resource-group innoventity-rg-dev `
  --server innoventity-sql-dev `
  --name InnoventityDB `
  --service-objective S0

# Create App Service Plan
az appservice plan create `
  --name innoventity-plan-dev `
  --resource-group innoventity-rg-dev `
  --sku B1 `
  --is-linux

# Create Web App
az webapp create `
  --name innoventity-api-dev `
  --resource-group innoventity-rg-dev `
  --plan innoventity-plan-dev `
  --runtime "DOTNETCORE:8.0"

# Deploy API
cd src/Innoventity.API
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip
az webapp deploy --resource-group innoventity-rg-dev --name innoventity-api-dev --src-path ./publish.zip
```

### Post-Deployment Steps

1. **Run migrations on Azure SQL**:
   ```powershell
   # Get connection string from Azure Portal
   # Update appsettings.json or use environment variable
   dotnet ef database update --connection "Azure SQL connection string"
   ```

2. **Configure Application Insights**:
   - Copy instrumentation key from Azure Portal
   - Add to `appsettings.json` or App Service Configuration

3. **Seed initial data** (industries, test actors if needed)

4. **Verify deployment**:
   - Navigate to `https://innoventity-api-dev.azurewebsites.net/scalar`
   - Test registration and login flows

---

## Common Issues

### Issue 1: Database Connection Failures

**Symptom**: `SqlException: A network-related or instance-specific error occurred while establishing a connection to SQL Server.`

**Solutions**:
- **LocalDB not running**: Start SQL Server LocalDB: `sqllocaldb start mssqllocaldb`
- **Docker container stopped**: Restart container: `docker start innoventity-sql`
- **Connection string typo**: Verify connection string in `appsettings.Development.json` or user secrets
- **SQL Server not installed**: Install SQL Server Express or use Docker (see Backend Setup Step 2)

**Verify connection**:
```powershell
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT @@VERSION"
# OR for Docker
sqlcmd -S localhost,1433 -U sa -P "YourStrong!Passw0rd" -Q "SELECT @@VERSION"
```

---

### Issue 2: Port Conflicts (5001, 4200 already in use)

**Symptom**: `EADDRINUSE: address already in use :::5001` or `Port 4200 is already in use`

**Solutions**:

**Backend (API)**:
Edit `src/Innoventity.API/Properties/launchSettings.json`:
```json
{
  "profiles": {
    "https": {
      "applicationUrl": "https://localhost:5501;http://localhost:5500"
    }
  }
}
```

**Frontend (Angular)**:
```powershell
ng serve --port 4300
```

**OR** find and kill process using the port (Windows):
```powershell
# Find process on port 5001
netstat -ano | findstr :5001
# Note the PID (last column)
taskkill /PID <PID> /F
```

---

### Issue 3: Migration Failures

**Symptom**: `dotnet ef database update` fails with "Cannot find compilation library location for package 'Microsoft.EntityFrameworkCore.Design'"

**Solutions**:
- Ensure you're in the correct directory: `src/Innoventity.API`
- Restore packages: `dotnet restore`
- Update EF Core tools: `dotnet tool update --global dotnet-ef`
- Verify SDK version: `dotnet --version` (should be 8.0+)

**Nuclear option** (recreate database):
```powershell
# Delete database
dotnet ef database drop --force

# Recreate from migrations
dotnet ef database update
```

---

### Issue 4: JWT Token Errors (401 Unauthorized)

**Symptom**: API returns 401 Unauthorized even after successful login

**Solutions**:
- **Token expired**: Access tokens expire after 1 hour (check `exp` claim in JWT at [jwt.io](https://jwt.io))
- **Refresh token**: Call `POST /api/auth/refresh-token` to get new access token
- **Clock skew**: Ensure system clock is accurate (JWT validation is time-sensitive)
- **Secret key mismatch**: Verify `Jwt:SecretKey` in `appsettings.json` matches between issuer and validator
- **CORS**: Check browser console for CORS errors (ensure API allows frontend origin)

**Decode JWT token** (PowerShell):
```powershell
$token = "your.jwt.token"
$parts = $token.Split(".")
$payload = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($parts[1]))
$payload | ConvertFrom-Json | Format-List
```

---

### Issue 5: Angular Compilation Errors

**Symptom**: `ng serve` fails with "Cannot find module '@angular/core'"

**Solutions**:
- Delete `node_modules` and reinstall:
  ```powershell
  rm -r -fo node_modules
  rm package-lock.json
  npm install
  ```
- Clear Angular cache:
  ```powershell
  ng cache clean
  ```
- Verify Node.js version: `node --version` (should be 18.x or 20.x LTS)
- Update Angular CLI:
  ```powershell
  npm uninstall -g @angular/cli
  npm cache clean --force
  npm install -g @angular/cli@18
  ```

---

### Issue 6: CORS Errors in Browser Console

**Symptom**: `Access to XMLHttpRequest at 'https://localhost:5001/api/auth/login' from origin 'http://localhost:4200' has been blocked by CORS policy`

**Solutions**:
- Ensure CORS is configured in `Program.cs`:
  ```csharp
  builder.Services.AddCors(options =>
  {
      options.AddDefaultPolicy(policy =>
      {
          policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
      });
  });
  
  // BEFORE app.MapControllers()
  app.UseCors();
  ```
- Verify frontend is calling correct API URL in `environment.development.ts`
- Check browser DevTools Network tab for preflight (OPTIONS) requests

---

## Next Steps

### Continue Development

1. **Review Plan Phase Deliverables**:
   - [Implementation Plan](./plan.md) - Technical architecture decisions
   - [Research Document](./research.md) - Technology choices and rationale
   - [Data Model](./data-model.md) - Database schema and entity relationships
   - [API Contracts](./contracts/openapi.yaml) - OpenAPI specification

2. **Execute TASKS Phase**:
   ```powershell
   # Generate work breakdown structure
   /speckit.tasks specs/001-platform-core/plan.md
   ```

3. **Begin Implementation**:
   - Follow Vertical Slice Architecture (one feature at a time)
   - Use TDD workflow: Red → Green → Refactor
   - Reference [Specification](./spec.md) for acceptance criteria

### Phase 0 Implementation Order

Recommended sequence (1-2 weeks):

1. **Week 1: Backend + Database**
   - Day 1-2: Setup project structure, EF Core entities, migrations
   - Day 3-4: Authentication endpoints (register, activate, login, refresh) + unit tests
   - Day 5: Innovation retrieval endpoint + integration tests

2. **Week 2: Frontend + E2E**
   - Day 1-2: Registration + activation components + services
   - Day 3-4: Login component + JWT interceptor + auth guard
   - Day 5: Innovation detail component
   - Weekend: E2E tests for P0 journeys (Playwright)

### Learning Resources

- **ASP.NET Core 8**: [Official Docs](https://learn.microsoft.com/aspnet/core/)
- **Entity Framework Core**: [EF Core Docs](https://learn.microsoft.com/ef/core/)
- **Angular 19**: [Angular Docs](https://angular.dev/)
- **Testing ASP.NET Core**: [Integration Testing Guide](https://learn.microsoft.com/aspnet/core/test/integration-tests)
- **Playwright**: [Playwright Docs](https://playwright.dev/)
- **Azure Deployment**: [Azure Developer CLI Docs](https://learn.microsoft.com/azure/developer/azure-developer-cli/)

### Get Help

- **Specification Questions**: Review [spec.md](./spec.md) Section 4 (Business Rules) with Gherkin acceptance tests
- **Architecture Questions**: Review [research.md](./research.md) Decision Records for rationale
- **Database Questions**: Review [data-model.md](./data-model.md) for entity definitions and validation rules
- **API Questions**: Review [OpenAPI spec](./contracts/openapi.yaml) for request/response schemas

### Join the Community

- **GitHub Discussions**: Ask questions, share progress
- **Pull Requests**: Submit code for review per contribution guidelines
- **Issues**: Report bugs or request features

---

**Happy Coding! 🚀**

For technical support, consult the [Implementation Plan](./plan.md) or reach out via GitHub Issues.
