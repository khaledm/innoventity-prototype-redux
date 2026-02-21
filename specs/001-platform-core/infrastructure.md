# Infrastructure Plan: Platform Core (v1.0)

**Branch**: `001-platform-core` | **Date**: February 8, 2026 | **Spec**: [spec.md](spec.md)
**Related**: [plan.md](plan.md) (Application Architecture)

## Ephemeral Environment Principles

**Hard Constraint**: ALL environments (Dev, Test, Prod) MUST be:
1. **Created from Infrastructure as Code** - Terraform modules provision complete environment
2. **Reproducible from scratch** - Re-running IaC creates identical environment
3. **Destroyable without data loss risk** - No critical state stored in environment itself

**Architecture Evaluation Criteria**:
- **Reproducibility**: Can this component be recreated via IaC with zero manual steps?
- **Blast Radius**: What breaks if this component is destroyed?
- **Cost of Recreation**: Time + money to rebuild from scratch
- **Observability**: Can we prove correctness in short-lived environments?

**Prohibited Patterns**:
- ❌ Manual Azure Portal configuration of managed resources (except initial service principal setup) — prevented in prod via Reader-only RBAC; detected in all environments via nightly `terraform plan -detailed-exitcode`
- ❌ Shared mutable resources across environments
- ❌ Long-lived secrets without rotation strategy
- ❌ Environment-specific code branches (use configuration instead)

## Infrastructure as Code Structure

**Tool**: Terraform 1.6+ with Azure Provider 3.x

### Directory Layout
```
infrastructure/
├── modules/
│   ├── app-service/       # Azure App Service + deployment slots
│   ├── sql-database/      # Azure SQL Database + firewall rules
│   ├── monitoring/        # Application Insights + Log Analytics
│   └── networking/        # Virtual Network (Phase 1+, currently public endpoints)
├── environments/
│   ├── dev/
│   │   ├── core/          # Stateless resources: App Service, App Insights
│   │   │   ├── main.tf
│   │   │   ├── terraform.tfvars
│   │   │   └── backend.tf # State: tfstate-dev-core
│   │   └── data/          # Stateful resources: SQL Server + Database
│   │       ├── main.tf
│   │       ├── terraform.tfvars
│   │       └── backend.tf # State: tfstate-dev-data
│   ├── test/
│   │   ├── core/
│   │   └── data/
│   └── prod/
│       ├── core/
│       └── data/
└── README.md              # Provisioning instructions
```

**Split-State Principle**: Stateless and stateful resources live in separate Terraform root modules with independent state files. `terraform destroy` on `core/` tears down App Service and App Insights cleanly. `data/` is a separate apply/destroy operation and is never touched by a routine environment teardown.

> ⚠️ **Why not `terraform destroy -target`?** Targeted destroy is explicitly warned against in Terraform documentation for routine workflow. It can leave orphaned resources, state inconsistencies, and hidden dependency violations. The split-state layout achieves the same isolation without any of those risks.

**Environment-Agnostic Principle**: All `core/` and `data/` root modules use the SAME reusable modules. Only `terraform.tfvars` differs per environment:
```hcl
# environments/dev/terraform.tfvars
environment         = "dev"
app_service_sku     = "B1"        # Basic tier for dev
sql_database_sku    = "Basic"     # 2GB DTU for dev
retention_days      = 7

# environments/prod/terraform.tfvars
environment         = "prod"
app_service_sku     = "P1v3"      # Premium tier for prod
sql_database_sku    = "S2"        # Standard 50 DTU for 100 concurrent users
retention_days      = 35
```

### Terraform State Management

**Backend**: Azure Storage Account with **two containers per environment** — one for core, one for data — to enforce state boundary.
```hcl
# environments/dev/core/backend.tf
terraform {
  backend "azurerm" {
    resource_group_name  = "innoventity-tfstate-rg"
    storage_account_name = "innoventitytfstate"
    container_name       = "tfstate-dev-core"
    key                  = "platform-core.tfstate"
  }
}

# environments/dev/data/backend.tf
terraform {
  backend "azurerm" {
    resource_group_name  = "innoventity-tfstate-rg"
    storage_account_name = "innoventitytfstate"
    container_name       = "tfstate-dev-data"
    key                  = "platform-core.tfstate"
  }
}
```

**State Isolation**: Separate state file per layer per environment. `core/` destroy never touches `data/` state — no `-target` required, no orphan risk.

**Bootstrap Requirement**: State storage account created ONCE manually, then managed via Terraform (state stored in itself).

## Azure Resource Definitions

### 1. Resource Group Module

**Purpose**: Logical container for all environment resources

**Terraform Module** (`modules/resource-group/main.tf`):
```hcl
variable "environment" {
  description = "Environment name (dev, test, prod)"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "East US"
}

resource "azurerm_resource_group" "main" {
  name     = "innoventity-${var.environment}-rg"
  location = var.location

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
    Project     = "Innoventity Platform Core"
  }
}

output "name" {
  value = azurerm_resource_group.main.name
}

output "location" {
  value = azurerm_resource_group.main.location
}
```

**Recreatability**: ✅ No data stored in resource group itself
**Blast Radius**: Destroying RG destroys all contained resources (expected behavior)
**Cost**: $0 (container only)

---

### 2. Azure SQL Database Module

**Purpose**: Relational database for application data

**Terraform Module** (`modules/sql-database/main.tf`):
```hcl
variable "environment" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "sku_name" {
  description = "Database SKU (Basic, S0, S1, S2)"
  type        = string
}
variable "max_size_gb" {
  description = "Database max size"
  type        = number
  default     = 2
}
variable "admin_password" {
  description = "SQL admin password (from Terraform variables or CI/CD secrets)"
  type        = string
  sensitive   = true
}

resource "azurerm_mssql_server" "main" {
  name                         = "innoventity-${var.environment}-sql"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = "innoventity-admin"
  administrator_login_password = var.admin_password
  minimum_tls_version          = "1.2"

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

resource "azurerm_mssql_database" "main" {
  name           = "Innoventity"
  server_id      = azurerm_mssql_server.main.id
  sku_name       = var.sku_name
  max_size_gb    = var.max_size_gb

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
  }

  # Decision (2026-02-20): Production database is persistent — use split-state layout.
  # The database lives in environments/{env}/data/ (separate Terraform root module).
  # Destroying environments/{env}/core/ (App Service, App Insights) never touches this state.
  # To intentionally destroy the database: cd environments/{env}/data && terraform destroy
  # That decision requires explicit change-control; it is NOT part of any routine workflow.
  lifecycle {
    prevent_destroy = true
  }
}

# Allow Azure services to access SQL Server (for App Service)
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# Dev environment: Allow local development (optional, remove for test/prod)
resource "azurerm_mssql_firewall_rule" "allow_local" {
  count            = var.environment == "dev" ? 1 : 0
  name             = "AllowLocalDevelopment"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"  # Replace with your IP range
  end_ip_address   = "255.255.255.255"
}

output "connection_string" {
  value       = "Server=tcp:${azurerm_mssql_server.main.fully_qualified_domain_name},1433;Database=${azurerm_mssql_database.main.name};User ID=${azurerm_mssql_server.main.administrator_login};Password=${var.admin_password};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  sensitive   = true
  description = "Database connection string (for App Service Configuration)"
}

output "server_name" {
  value = azurerm_mssql_server.main.fully_qualified_domain_name
}
```

**Recreatability**: ⚠️ **Partial** - Database schema recreated via EF Core migrations (see Database State Management below)
**Blast Radius**: Data loss if destroyed - mitigated by test data seeding scripts (for dev/test environments)
**Cost**: Dev $5/month (Basic), Prod $30-150/month (S2 Standard)
**Validation**: Run EF Core migrations + smoke test queries after creation; Pester `SqlDatabase.Tests.ps1` verifies `AllowAzureServices` rule and confirms no wide-open rule (`endIpAddress = 255.255.255.255`) exists in non-dev environments

**Firewall Constraints**:
- `AllowAzureServices` rule (`0.0.0.0/0.0.0.0`) MUST exist in all environments — required for App Service connectivity.
- `AllowLocalDevelopment` rule (`0.0.0.0–255.255.255.255`) is INTENTIONAL in `dev` only; MUST NOT survive a `test` or `prod` apply.
- **No firewall rule with `endIpAddress = 255.255.255.255` is permitted in `test` or `prod` environments**, regardless of rule name. This constraint covers manually-added portal rules as well as Terraform-managed ones. Verified by Pester `SqlDatabase.Tests.ps1` `"has no wide-open firewall rule"` assertion.

---

### 3. App Service Module

**Purpose**: Host ASP.NET Core 8 backend API

**Terraform Module** (`modules/app-service/main.tf`):
```hcl
variable "environment" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }
variable "sku_name" {
  description = "App Service Plan SKU (B1, S1, P1v3)"
  type        = string
}
variable "connection_string" {
  description = "SQL Database connection string"
  type        = string
  sensitive   = true
}
variable "jwt_secret_key" {
  description = "JWT signing key (256-bit base64)"
  type        = string
  sensitive   = true
}
variable "application_insights_key" {
  description = "Application Insights instrumentation key"
  type        = string
  sensitive   = true
}

resource "azurerm_service_plan" "main" {
  name                = "innoventity-${var.environment}-asp"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = var.sku_name

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

resource "azurerm_linux_web_app" "main" {
  name                = "innoventity-${var.environment}-api"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.main.id
  https_only          = true

  site_config {
    always_on        = var.sku_name != "B1" ? true : false  # Basic tier doesn't support always_on
    ftps_state       = "Disabled"
    http2_enabled    = true
    minimum_tls_version = "1.2"

    application_stack {
      dotnet_version = "8.0"
    }

    cors {
      allowed_origins = var.environment == "dev" ? ["http://localhost:4200"] : ["https://innoventity-${var.environment}-web.azurestaticapps.net"]
      support_credentials = true
    }
  }

  app_settings = {
    "ASPNETCORE_ENVIRONMENT" = var.environment == "prod" ? "Production" : "Development"
    "APPLICATIONINSIGHTS_CONNECTION_STRING" = var.application_insights_key
    "Jwt__SecretKey" = var.jwt_secret_key
    "Jwt__Issuer" = "https://innoventity-${var.environment}-api.azurewebsites.net"
    "Jwt__Audience" = "https://innoventity-${var.environment}-api.azurewebsites.net"
    "Jwt__AccessTokenExpiration" = "60"  # 1 hour
    "Jwt__RefreshTokenExpiration" = "10080"  # 7 days
  }

  connection_string {
    name  = "InnoventityDb"
    type  = "SQLAzure"
    value = var.connection_string
  }

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

# Deployment slot for staging (prod environment only)
resource "azurerm_linux_web_app_slot" "staging" {
  count              = var.environment == "prod" ? 1 : 0
  name               = "staging"
  app_service_id     = azurerm_linux_web_app.main.id
  https_only         = true

  site_config {
    always_on = true

    application_stack {
      dotnet_version = "8.0"
    }
  }

  # Staging uses same app settings as production (validated before swap)
  app_settings = azurerm_linux_web_app.main.app_settings

  tags = {
    Environment = "${var.environment}-staging"
    ManagedBy   = "Terraform"
  }
}

output "default_hostname" {
  value = azurerm_linux_web_app.main.default_hostname
  description = "App Service default URL"
}

output "staging_hostname" {
  value = var.environment == "prod" ? azurerm_linux_web_app_slot.staging[0].default_hostname : null
  description = "Staging slot URL (prod only)"
}
```

**Recreatability**: ✅ Fully recreatable - stateless application, config injected via Terraform
**Blast Radius**: API downtime during recreation (~2-3 minutes), no data loss
**Cost**: Dev $13/month (B1), Prod $100-200/month (P1v3)
**Validation**: Health check endpoint (`GET /health`) returns 200 after deployment; Pester `AppService.Tests.ps1` verifies `alwaysOn` SKU-conditional and `ASPNETCORE_ENVIRONMENT` exact-match per environment

**Configuration Constraints**:
- `always_on = var.sku_name != "B1" ? true : false` — B1 does not support `always_on`; any other SKU with `alwaysOn = false` is an availability misconfiguration (cold starts). Verified by Pester `AppService.Tests.ps1` against live App Service Plan SKU via `az appservice plan show`.
- `ASPNETCORE_ENVIRONMENT` MUST equal `"Production"` in `prod`, `"Development"` in all other environments. Deviations alter logging verbosity, error detail exposure, developer-exception pages, and HTTPS redirection behaviour. Verified by Pester `AppService.Tests.ps1` using `$env:ENVIRONMENT` set by `validate-environment.ps1`.

---

### 4. Application Insights Module

**Purpose**: APM monitoring, distributed tracing, error logging

**Terraform Module** (`modules/monitoring/main.tf`):
```hcl
variable "environment" { type = string }
variable "resource_group_name" { type = string }
variable "location" { type = string }

resource "azurerm_log_analytics_workspace" "main" {
  name                = "innoventity-${var.environment}-logs"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = var.environment == "prod" ? 90 : 30

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

resource "azurerm_application_insights" "main" {
  name                = "innoventity-${var.environment}-appinsights"
  resource_group_name = var.resource_group_name
  location            = var.location
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"

  tags = {
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

output "connection_string" {
  value       = azurerm_application_insights.main.connection_string
  sensitive   = true
  description = "Application Insights connection string"
}

output "instrumentation_key" {
  value       = azurerm_application_insights.main.instrumentation_key
  sensitive   = true
  description = "Application Insights instrumentation key (legacy)"
}
```

**Recreatability**: ✅ Fully recreatable
**Blast Radius**: Loss of historical telemetry data (30-90 days retention), no application impact
**Cost**: ~$2-5/month per environment (first 5GB/month free)
**Validation**: Check for telemetry ingestion after app deployment (query for requests in last 5 minutes)

---

## Database State Management

**Challenge**: Ephemeral database instances conflict with schema evolution and test data needs.

**Strategy**: Decouple schema from instance lifecycle

### Schema Provisioning (EF Core Migrations)

**Approach**: Idempotent migrations applied automatically during app startup (dev/test) or CI/CD pipeline (prod)

**Implementation** (ASP.NET Core startup):
```csharp
// Program.cs
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
{
    // Auto-apply migrations on startup (dev/test only)
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}
```

**Production**: Migrations applied via CI/CD pipeline BEFORE app deployment:
```bash
# Azure DevOps pipeline step
- task: DotNetCoreCLI@2
  displayName: 'Apply EF Core Migrations'
  inputs:
    command: 'custom'
    custom: 'ef'
    arguments: 'database update --connection "$(SQL_CONNECTION_STRING)"'
    workingDirectory: 'src/Innoventity.API'
```

**Validation**: EF Core `EnsureCreated()` throws if migrations incomplete, health check fails.

### Test Data Seeding

**Development Environment**: Seed data applied automatically after migrations

```csharp
// Data/DbSeeder.cs
public static async Task SeedDevelopmentDataAsync(ApplicationDbContext context)
{
    if (await context.Actors.AnyAsync()) return;  // Skip if already seeded

    var ideaGenerator = new Actor
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),  // Fixed GUID for reproducibility
        Email = "ideagen@example.com",
        ActorType = ActorType.IdeaGenerator,
        AccountStatus = AccountStatus.Active,
        // ... other properties
    };

    var innovation = new Innovation
    {
        Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
        Title = "AI-Powered Medical Diagnostic Tool",
        OwnerId = ideaGenerator.Id,
        // ... other properties
    };

    context.Actors.Add(ideaGenerator);
    context.Innovations.Add(innovation);
    await context.SaveChangesAsync();
}

// Program.cs
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedDevelopmentDataAsync(dbContext);
}
```

**Test Environment**: Each test class seeds own test data (isolated via transactions or database per test)

**Reproducibility**: Fixed GUIDs + idempotent seed logic = identical state across recreations

---

## Environment Lifecycle Workflows

### Creation Workflow

**Prerequisites**:
1. Azure subscription with Contributor role
2. Terraform 1.6+ installed
3. Azure CLI authenticated (`az login`)
4. Secret values prepared (JWT key, SQL admin password)

**Steps**:
```bash
# 1. Navigate to environment directory
cd infrastructure/environments/dev

# 2. Initialize Terraform (download providers, configure backend)
terraform init

# 3. Create terraform.tfvars with environment-specific config
cat > terraform.tfvars <<EOF
environment          = "dev"
location             = "East US"
app_service_sku      = "B1"
sql_database_sku     = "Basic"
sql_admin_password   = "$(openssl rand -base64 16)"  # Generate secure password
jwt_secret_key       = "$(openssl rand -base64 32)"  # Generate JWT key
EOF

# 4. Plan infrastructure changes (dry-run)
terraform plan -out=tfplan

# 5. Review plan output for unexpected changes
# 6. Apply infrastructure changes
terraform apply tfplan

# 7. Capture outputs (connection strings, URLs)
terraform output -json > outputs.json

# 8. Deploy application code
# (CI/CD pipeline or manual: dotnet publish + az webapp deployment)

# 9. Apply database migrations
dotnet ef database update --project src/Innoventity.API \
  --connection "$(terraform output -raw sql_connection_string)"

# 10. Seed test data (dev/test only)
dotnet run --project src/Innoventity.API --seed-data

# 11. Validate environment
curl https://$(terraform output -raw app_service_hostname)/health
# Expected: 200 OK {"status":"Healthy"}
```

**Duration**: ~5-7 minutes (Azure resource provisioning) + 2-3 minutes (app deployment)

**Validation Checklist**:
- [ ] Terraform apply completed with 0 errors
- [ ] App Service health endpoint returns 200
- [ ] Database migrations applied successfully (check `__EFMigrationsHistory` table)
- [ ] Application Insights receiving telemetry (query for requests in last 5 minutes)
- [ ] Test authentication flow (register → activate → login → call protected endpoint)

---

### Destruction Workflow

**Prerequisites**:
1. Confirm no active users (production environments)

**Destroy stateless resources (core) — routine teardown:**
```bash
# Tears down App Service, App Insights. Database is untouched (separate state).
cd infrastructure/environments/dev/core
terraform plan -destroy -out=tfplan-destroy
terraform apply tfplan-destroy
```

**Destroy data layer — intentional only, under change-control:**
```bash
# Only run this when you explicitly intend to drop the database.
# Requires removing prevent_destroy from the SQL module or it will hard-fail.
cd infrastructure/environments/dev/data
terraform plan -destroy -out=tfplan-destroy
terraform apply tfplan-destroy
```

> ⚠️ **Do not use `terraform destroy -target`** for either layer. The split-state design means you never need it. Targeted destroy leaves state inconsistencies and is not recommended by Terraform for routine workflow.

**Duration**: Core ~2-3 minutes. Data ~1-2 minutes.

**Blast Radius**: Core destroy removes App Service and App Insights only; no data loss. Data destroy is irreversible for production data.

**Validation**: Verify core resources deleted in Azure Portal; SQL Server and Database should remain until data layer is explicitly destroyed.

---

### Update Workflow (Intentional Infrastructure Changes)

**Scenario**: Change App Service SKU, add firewall rule, update secret rotation

**Steps**:
```bash
# 1. Modify terraform.tfvars or module code
# Example: Increase SQL Database SKU
sed -i 's/sql_database_sku = "Basic"/sql_database_sku = "S1"/' terraform.tfvars

# 2. Plan changes
terraform plan -out=tfplan

# 3. Review changes (ensure no unintended resource recreation)
# Look for:
#   - "will be updated in-place" = safe
#   - "must be replaced" = resource recreation (potential downtime)

# 4. Apply changes
terraform apply tfplan

# 5. Validate no application errors
curl https://$(terraform output -raw app_service_hostname)/health
```

**Recreate vs Update**:
- **Update in-place**: App Service SKU change, firewall rule addition, app settings modification
- **Recreate**: SQL Server name change, App Service name change (DNS-dependent resources)

**Downtime Strategy**: Use deployment slots (prod environment) to minimize downtime during recreation

---

### Unintentional Drift: Prevention and Detection

**Problem**: Someone with portal access changes App Service configuration, a firewall rule, or an app setting outside Terraform. The Terraform state no longer reflects reality. The next `terraform apply` may overwrite the change silently, or produce unexpected plan output.

**Control options evaluated**:

| Option | Mechanism | Verdict for this project |
|--------|-----------|-------------------------|
| **Azure Policy** | Audit/deny resource properties at create/modify time | ⚠️ Partial — does not cover all config properties (e.g. `app_settings`); adds authoring/testing overhead. Overkill for Phase 0. |
| **Deny Assignments** (Azure Blueprints / Deployment Stacks) | Block changes even at Owner level | ❌ Too heavy — designed for enterprise compliance. Azure Blueprints is deprecated. Deployment Stacks is the successor but adds significant complexity. |
| **RBAC restriction** | Remove portal write access for humans | ✅ Primary prevention control — already applied for prod (Reader-only). The correct structural answer for drift prevention. |
| **Nightly drift detection pipeline** | Scheduled `terraform plan -detailed-exitcode`; alert on exit code 2 | ✅ Detection layer — simple, transparent, fits the "pipeline only" apply model. 24h detection window acceptable for this scale. |

**Decision**: RBAC is the primary prevention layer. Nightly drift detection is the detection layer. Azure Policy and Deny Assignments are deferred.

**Prevention (RBAC — already partially enforced)**:
- Production: developers have Reader-only access → portal changes are blocked by RBAC. Full prevention.
- Development: developers currently have Contributor access → portal changes are technically possible. Drift in dev is recoverable (`terraform apply` corrects it), so this is an accepted risk for dev agility.

**Detection (Nightly Drift Scan — T076 pipeline addition)**:
```yaml
# .github/workflows/drift-detection.yml  (or equivalent pipeline step)
schedule:
  - cron: '0 2 * * *'   # 02:00 UTC nightly

jobs:
  drift-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Terraform Init (core)
        run: terraform -chdir=infrastructure/environments/prod/core init
      - name: Terraform Plan (core)
        id: plan_core
        run: |
          terraform -chdir=infrastructure/environments/prod/core \
            plan -detailed-exitcode -out=tfplan 2>&1 | tee plan_output.txt
          echo "exitcode=$?" >> $GITHUB_OUTPUT
      - name: Fail if drift detected
        if: steps.plan_core.outputs.exitcode == '2'
        run: |
          echo "::error::Infrastructure drift detected in prod/core. Review plan output."
          cat plan_output.txt
          exit 1
      # Repeat for prod/data
```

> `terraform plan -detailed-exitcode` returns exit code `0` (no changes), `1` (error), or `2` (changes pending). Exit code `2` means drift exists.

**Scope**: Drift detection runs against `prod/core` and `prod/data` only. Dev drift is acceptable and not monitored.

**Implementation**: Drift detection is implemented as the standalone `drift.yml` workflow (see §Testing Requirements §Three-Pipeline CI/CD Architecture). It is a separate workflow from `deploy.yml` — never runs on push, never applies.

## Component Validation Matrix

| Component | Recreatability | Blast Radius | Recreation Cost | Validation Method |
|-----------|----------------|--------------|-----------------|-------------------|
| **Resource Group** | ✅ Full | High (destroys all children) | $0, 30s | `az group show` returns 200 |
| **App Service** | ✅ Full | API downtime (2-3 min) | $0.10-0.50, 2 min | `/health` returns 200, AppInsights receives telemetry; Pester: `alwaysOn` SKU-conditional (B1=false, non-B1=true), `ASPNETCORE_ENVIRONMENT` exact-match per env |
| **SQL Database** | ⚠️ Schema only | Data loss if not backed up | $0.20-2.00, 3 min | EF Core migration check, test query returns expected rows |
| **Application Insights** | ✅ Full | Historical telemetry loss (30-90d) | $0, 1 min | Query for requests in last 5 min returns >0 results |
| **Service Plan** | ✅ Full | All hosted apps restart | $0, 2 min | Apps return to healthy state after plan recreation |

**Validation Automation** (Terraform post-deployment script):
```bash
#!/bin/bash
# infrastructure/scripts/validate-environment.sh

APP_URL=$(terraform output -raw app_service_hostname)
SQL_SERVER=$(terraform output -raw sql_server_name)

echo "=== Environment Validation ==="

# 1. Health check
echo "Testing health endpoint..."
if curl -f "https://$APP_URL/health" | grep -q "Healthy"; then
  echo "✅ Health check passed"
else
  echo "❌ Health check failed"
  exit 1
fi

# 2. Database connectivity
echo "Testing database connection..."
if dotnet ef database get-migrations --project src/Innoventity.API --connection "$(terraform output -raw sql_connection_string)" | grep -q "InitialCreate"; then
  echo "✅ Database migrations applied"
else
  echo "❌ Database migrations incomplete"
  exit 1
fi

# 3. Application Insights telemetry
echo "Waiting for telemetry ingestion (30s)..."
sleep 30
TELEMETRY_COUNT=$(az monitor app-insights metrics show \
  --app $(terraform output -raw appinsights_name) \
  --resource-group $(terraform output -raw resource_group_name) \
  --metric requests/count \
  --interval PT1M \
  --query "value.timeseries[0].data[-1].total" \
  --output tsv)

if [ "$TELEMETRY_COUNT" -gt 0 ]; then
  echo "✅ Application Insights receiving telemetry"
else
  echo "❌ No telemetry data in Application Insights"
  exit 1
fi

echo "=== All validations passed ==="
```

---

## Cost Analysis (Ephemeral Environment Overhead)

**Baseline Costs** (per environment, monthly):

| Environment | App Service | SQL Database | Application Insights | Total/Month |
|-------------|-------------|--------------|----------------------|-------------|
| **Dev** | $13 (B1) | $5 (Basic) | $2 (5GB free tier) | **$20** |
| **Test** | $13 (B1) | $5 (Basic) | $2 | **$20** |
| **Prod** | $100 (P1v3) | $75 (S2) | $5 | **$180** |

**Ephemeral Lifecycle Costs** (assuming 10 recreations per month):
- **Terraform apply time**: 5 min × 10 = 50 min developer time (~$40/hr = $33)
- **Validation time**: 3 min × 10 = 30 min developer time (~$20)
- **Azure resource creation**: $0 (billed by runtime hours, not creation count)
- **Total overhead**: ~$53/month for 10 recreations across all environments

**Cost Optimization**:
- **Auto-shutdown dev/test**: Azure Automation script stops App Service + SQL Database during off-hours (8PM-8AM) = ~50% cost reduction
- **Minimize environment count**: Keep only dev + prod, use deployment slots for staging (eliminates test environment)
- **Delete after testing**: Destroy dev environment after each feature branch merge (pay only during active development)

---

## Observability Requirements

**Challenge**: Ephemeral environments have short lifetimes, need to prove correctness quickly.

### Logging Strategy

**Centralized Logging**: All environments log to Application Insights (retained 30-90 days, survives environment destruction)

**Structured Logging** (ASP.NET Core):
```csharp
// Use structured logging with correlation IDs
_logger.LogInformation("User {UserId} registered with email {Email}", userId, email);

// Include environment context
_logger.LogWarning("Database query slow: {QueryDuration}ms, Environment: {Environment}",
    duration,
    builder.Environment.EnvironmentName);
```

**Log Queries** (Kusto Query Language in Application Insights):
```kql
// Find errors in last environment deployment
traces
| where timestamp > ago(1h)
| where severityLevel >= 3  // Warning or higher
| where cloud_RoleName == "innoventity-dev-api"
| project timestamp, message, severityLevel, customDimensions
| order by timestamp desc

// Track environment recreation success
customEvents
| where name == "EnvironmentProvisioned"
| summarize Count=count() by tostring(customDimensions.Environment)
```

### Health Checks

**Implementation** (ASP.NET Core):
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddApplicationInsightsPublisherHealthCheck("appinsights");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(result);
    }
});
```

**Validation**: CI/CD pipeline fails if health check returns non-200 status after deployment.

### Metrics & Alerts

**Key Metrics** (Application Insights):
- **Availability**: `/health` endpoint uptime (target: 99%+)
- **Performance**: API response time p95 <200ms, database query p95 <100ms
- **Errors**: HTTP 5xx count (alert if >5 in 5 minutes)
- **Environment Recreation**: Track `EnvironmentProvisioned` custom event (alert if >10/day = unexpected churn)

**Alerting** (Azure Monitor):
```hcl
# modules/monitoring/alerts.tf
resource "azurerm_monitor_metric_alert" "high_error_rate" {
  name                = "innoventity-${var.environment}-high-errors"
  resource_group_name = var.resource_group_name
  scopes              = [azurerm_application_insights.main.id]
  description         = "Alert when error rate exceeds threshold"

  criteria {
    metric_namespace = "microsoft.insights/components"
    metric_name      = "exceptions/count"
    aggregation      = "Total"
    operator         = "GreaterThan"
    threshold        = 5
  }

  window_size        = "PT5M"
  frequency          = "PT1M"
  severity           = 2

  action {
    action_group_id = azurerm_monitor_action_group.main.id
  }
}
```

---

## Security Considerations

### Secret Injection for Ephemeral Environments

**Problem**: Terraform requires secret values (JWT key, SQL password) at apply time, but secrets should never be committed to source control.

**Solutions** (in order of security):

1. **CI/CD Pipeline Variables** (Recommended for prod):
   - Store secrets in Azure DevOps Library / GitHub Secrets
   - Inject as Terraform variables during pipeline execution
   ```yaml
   # azure-pipelines.yml
   - task: TerraformTaskV2@2
     inputs:
       command: 'apply'
       environmentServiceNameAzureRM: 'AzureServiceConnection'
       commandOptions: '-var="jwt_secret_key=$(JWT_SECRET_KEY)" -var="sql_admin_password=$(SQL_ADMIN_PASSWORD)"'
   ```

2. **Local Development** (terraform.tfvars NOT committed):
   ```hcl
   # infrastructure/environments/dev/terraform.tfvars (in .gitignore)
   jwt_secret_key     = "local-dev-key-not-for-production"
   sql_admin_password = "P@ssw0rd123!"
   ```

**Required .gitignore entries**:
```
infrastructure/**/*.tfvars
infrastructure/**/.terraform/
infrastructure/**/terraform.tfstate*
```

### Least Privilege Access

**Terraform Service Principal** (Azure AD):
- **Role**: Contributor on subscription or resource group (limited to innoventity-* resources)
- **Rotation**: Service principal credentials rotated every 90 days

**Developer Access** (Azure RBAC):
- **Development**: Contributor on dev resource group — this is intentional for dev agility. Consequence: portal changes in dev are possible and constitute accepted drift (recoverable via `terraform apply`).
- **Production**: Reader only — portal changes are blocked by RBAC. This is the primary drift prevention control for prod.

> **Why RBAC is the right primary control, not Azure Policy**: Policy is useful for enforcing naming conventions and allowed SKUs at create time, but doesn't prevent in-place modification of `app_settings`, firewall rules, or connection strings by a Contributor. RBAC's Reader restriction is the only control that fully blocks portal-originated drift in prod.

---

## Testing Requirements

### Infrastructure Testing: Terratest (IaC Code Correctness)

**Tool**: Terratest v0.46+ / Go 1.21+

**Purpose**: Validate that Terraform module *code* produces the correct Azure resources — catches wrong resource types, missing arguments, and tautological configurations. Runs `InitAndApply → assert via Azure SDK → Destroy`.

**Test Files** (`infrastructure/tests/terratest/`):
- `app_service_module_test.go` — validates `azurerm_linux_web_app` outputs (`https_only`, TLS version, `app_settings` keys present)
- `sql_database_module_test.go` — validates `azurerm_mssql_server` + `azurerm_mssql_database` (version, SKU, `prevent_destroy` comment present)
- `dev_environment_test.go` — end-to-end: provisions both `core/` + `data/`, asserts `GET /health` returns 200 with body containing `"Healthy"`

**Required pattern** (`defer terraform.Destroy` is non-negotiable):
```go
func TestDevEnvironmentProvisioning(t *testing.T) {
    t.Parallel()
    terraformOptions := &terraform.Options{
        TerraformDir: "../../environments/dev/core",
        Vars: map[string]interface{}{
            "environment":        "test-" + strings.ToLower(random.UniqueId()),
            "sql_admin_password": "TestP@ssw0rd123!",
            "jwt_secret_key":     generateRandomBase64(32),
        },
    }
    defer terraform.Destroy(t, terraformOptions)

    terraform.InitAndApply(t, terraformOptions)

    // Non-tautological: query Azure SDK, not terraform.Output() comparison to input
    appServiceName := terraform.Output(t, terraformOptions, "app_service_name")
    appService := azure.GetAppService(t, appServiceName, "innoventity-test-rg", "")
    assert.True(t, *appService.HTTPSOnly, "App Service must enforce HTTPS")

    http_helper.HttpGetWithRetry(t,
        fmt.Sprintf("https://%s.azurewebsites.net/health", appServiceName),
        nil, 200, "Healthy", 10, 30*time.Second)
}
```

**Characterisation failure mode** (Principle 5 — mandatory, Mark Seemann): Record in `infrastructure/README.md` the actual HTTP status code returned when the DB connection string is omitted from `app_settings`. If no failure scenario has been observed, the test suite has no validity.

**Phase**: Local only (Phase 0). Add as PR gate in `infra.yml` from Phase 1 onward.

---

### Environment State Validation: Pester (Post-Apply Safety)

**Tool**: Pester v5 (PowerShell)

**Purpose**: Validate live environment state *after* `terraform apply` — detects misconfigured firewall rules, wrong TLS settings, or missing `app_settings` that Terratest cannot catch (Terratest writes then reads from the same apply; Pester reads from a running environment independently).

**Test Files** (`infrastructure/tests/pester/`):
- `AppService.Tests.ps1` — asserts `httpsOnly = true`, `minTlsVersion = 1.2`, required `app_settings` keys present
- `SqlDatabase.Tests.ps1` — asserts firewall rule allows Azure services only (`startIpAddress = "0.0.0.0"`, `endIpAddress = "0.0.0.0"`)
- `Secrets.Tests.ps1` — asserts no plaintext secret values appear in `app_settings` responses
- `HealthCheck.Tests.ps1` — asserts `GET /health` returns 200 + JSON body has `"status":"Healthy"` for each dependency

**Required pattern** (`-CI` flag mandatory — exits non-zero on any failure):
```powershell
# infrastructure/tests/pester/HealthCheck.Tests.ps1
Describe "App Service Health" {
    It "returns 200 with status Healthy" {
        $url = "https://$env:APP_SERVICE_NAME.azurewebsites.net/health"
        $response = Invoke-RestMethod -Uri $url -Method Get
        $response.status | Should -Be "Healthy"
    }
}

# Run via: Invoke-Pester -CI ./infrastructure/tests/pester/
```

**Inputs**: `APP_SERVICE_NAME` and `RESOURCE_GROUP_NAME` from environment variables (never hardcoded).

**Runs in**: `deploy.yml` job `pester-health` after every deploy; `infra.yml` job `pester-infra` after Terraform apply.

---

### Three-Pipeline CI/CD Architecture

Three workflows in `.github/workflows/`. No fourth workflow. Filenames and job names below are authoritative.

**`infra.yml`** — triggers on push to `infrastructure/**` or `workflow_dispatch`

| # | Job | Description |
|---|-----|-------------|
| 1 | `terraform-plan-core` | `terraform plan` on `environments/{env}/core/` |
| 2 | `terraform-apply-core` | `terraform apply` on `core/` |
| 3 | `terraform-plan-data` | `terraform plan` on `environments/{env}/data/` |
| 4 | `approve-data` | Manual gate (prod only) — GitHub Environment protection rule |
| 5 | `terraform-apply-data` | `terraform apply` on `data/` |
| 6 | `pester-infra` | `Invoke-Pester -CI ./infrastructure/tests/pester/` |

**`deploy.yml`** — triggers on push to `src/**`

| # | Job | Description |
|---|-----|-------------|
| 1 | `preflight` | Check env vars, connectivity |
| 2 | `build-test` | `dotnet build` + `dotnet test` (unit + integration) |
| 3 | `migrate` | `dotnet ef database update` |
| 4 | `deploy` | Publish API to App Service |
| 5 | `pester-health` | `Invoke-Pester -CI ./infrastructure/tests/pester/HealthCheck.Tests.ps1` |
| 6 | `slot-swap` | Swap staging slot to production (prod only) |

**`drift.yml`** — triggers on `cron: "0 2 * * *"` only (never on push)

| # | Job | Description |
|---|-----|-------------|
| 1 | `drift-check-core` | `terraform plan -detailed-exitcode` on `prod/core/` — detection only, never applies |
| 2 | `drift-check-data` | `terraform plan -detailed-exitcode` on `prod/data/` — detection only, never applies |

> `terraform apply` is **never** triggered by a code push. `infra.yml` triggers only on `infrastructure/**` changes. `deploy.yml` never calls Terraform.

---

## Phase 0 Deliverables

**Minimal Viable Infrastructure** (10-month project timeline, focus on application code first):

### Immediate (Week 1-2):
- [x] `infrastructure/` directory structure
- [ ] Resource Group module (5 lines Terraform)
- [ ] SQL Database module (30 lines Terraform)
- [ ] App Service module (40 lines Terraform)
- [ ] Application Insights module (15 lines Terraform)
- [ ] Dev environment composition (environments/dev/main.tf)
- [ ] Pester validation suite (`infrastructure/tests/pester/AppService.Tests.ps1`, `SqlDatabase.Tests.ps1`, `Secrets.Tests.ps1`, `HealthCheck.Tests.ps1`)
- [ ] Terratest suite — local run only (`infrastructure/tests/terratest/dev_environment_test.go`)
- [ ] `infrastructure/scripts/create-environment.ps1` (PowerShell, orchestrates Terraform lifecycle)
- [ ] `infrastructure/scripts/validate-environment.ps1` (PowerShell, invokes `Invoke-Pester -CI`)
- [ ] .gitignore for Terraform state/secrets

### Deferred (Phase 1+):
- ⏸️ Test/Prod environment compositions (copy dev, adjust SKUs)
- ⏸️ Terratest PR gate — add as `infra.yml` PR check from Phase 1+
- ⏸️ Three-pipeline CI/CD implementation (`infra.yml`, `deploy.yml`, `drift.yml`) — architecture defined in §Testing Requirements, implementation deferred to T076–T078
- ⏸️ Auto-shutdown scripts for cost optimization
- ⏸️ Azure Monitor alerts (Application Insights manual review sufficient for now)

### Documentation Priority:
- **High**: Creation workflow, validation checklist, secret injection strategy
- **Medium**: Destruction workflow, cost analysis
- **Low**: Terratest examples, alert configurations

---

## Open Questions / Decisions Needed

**Resolved decisions are recorded here for traceability.**

**✅ Is production database persistent?**
- **Decision (2026-02-20)**: Yes — production database is persistent.
- **Implementation**: Split-state layout: `environments/{env}/core/` (App Service, App Insights) and `environments/{env}/data/` (SQL Server + Database) are separate Terraform root modules with independent state files. Routine `terraform destroy` on `core/` never touches the data layer. `lifecycle { prevent_destroy = true }` is retained in the SQL module as a hard backstop.
- **Why not `prevent_destroy` + `-target`**: Targeted destroy is an anti-pattern (Terraform docs explicitly warn against it for routine workflow). It risks orphaned resources, state inconsistencies, and hidden dependency violations. The split-state layout is the correct structural solution — it makes the boundary explicit and enforced at the state level, not at the plan/apply invocation level.
- **Corollary**: FR7.1 §4 "Data Loss Prevention" is satisfied for production. Dev/test data loss on `core/` destroy is acceptable (schema recoverable from migrations, data recoverable from seed scripts).

**✅ Is production destroy allowed?**
- **Decision (2026-02-20)**: Yes, but `prevent_destroy = true` on the SQL module means a full `terraform destroy` will error in all environments. Intentional full destruction requires removing the lifecycle guard under change-control.

**✅ Are backups mandatory?**
- **Decision (2026-02-20)**: No explicit backup configuration. Azure SQL built-in PITR (7 days Basic, 35 days S2) provides passive protection at no extra cost. Sufficient for Phase 0 risk tolerance.

**✅ Is Key Vault mandatory?**
- **Decision (2026-02-20)**: No. App Service Configuration (slot settings) is the secret injection pattern. See plan.md §CHK093.

**✅ Are private endpoints required?**
- **Decision (2026-02-20)**: No. Public endpoints with TLS 1.2 minimum + Azure Services firewall rule. Networking module deferred to Phase 1+.

**✅ Are per-PR environments required?**
- **Decision (2026-02-20)**: No. Pipeline targets a single shared dev environment. Per-PR dynamic environments deferred to Phase 1+.

**✅ Who can run terraform apply/destroy?**
- **Decision (2026-02-20)**: Pipeline only. The CI/CD service principal holds credentials. Developers have read-only access to dev; no direct apply/destroy rights.

**✅ Is the 10-minute SLA hard?**
- **Decision (2026-02-20)**: Soft target for Phase 0; treated as hard from Phase 1 onward. T070 validation script will measure and report actual wall-clock time. No hard pipeline failure on breach until Phase 1.

**✅ What is included in the 10-minute SLA?**
- **Decision (2026-02-20)**: Full stack — `terraform apply` invocation → EF Core migrations applied → `GET /health` returns 200. A provisioned-but-schema-less environment does not count as complete.

**✅ Is environment parity architectural or availability-level?**
- **Decision (2026-02-20)**: Architectural only. Same Terraform modules, same topology. SKU/scale parameters differ per environment. No requirement for zone-redundancy or availability SLA parity between dev and prod.

---

1. **Terraform Backend Bootstrap**: Should state storage account be managed manually or via separate bootstrap Terraform?
   - **Recommendation**: Manual creation (one-time, shared across all environments)

2. **Secret Rotation Frequency**: How often should JWT keys and SQL passwords rotate?
   - **Recommendation**: 90 days for production, manual rotation on security events, automated rotation deferred to v2.0

3. **Infrastructure Testing Scope**: Should Terratest run in CI/CD or only locally during module development?
   - **Phase 0**: Local only (avoid CI/CD pipeline complexity)
   - **Phase 1+**: Add to PR validation pipeline (gate infrastructure changes)

4. **Shared Resources**: Should Application Insights Log Analytics Workspace be shared across environments or per-environment?
   - **Recommendation**: Per-environment (aligns with ephemeral principle, isolated blast radius)

---

## References

- **Application Architecture**: [plan.md](plan.md)
- **Feature Specification**: [spec.md](spec.md)
- **Terraform Azure Provider**: https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs
- **EF Core Migrations**: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- **Azure App Service Deployment**: https://learn.microsoft.com/en-us/azure/app-service/deploy-best-practices
