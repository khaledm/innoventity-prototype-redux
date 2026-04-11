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
- ❌ Manual Azure Portal configuration (except initial service principal setup)
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
│   │   ├── main.tf        # Dev environment composition
│   │   ├── terraform.tfvars  # Dev-specific config (SKUs, scale)
│   │   └── backend.tf     # Terraform state (Azure Storage)
│   ├── test/
│   │   ├── main.tf        # Test environment composition (same modules)
│   │   ├── terraform.tfvars  # Test-specific config
│   │   └── backend.tf
│   └── prod/
│       ├── main.tf        # Prod environment composition (same modules)
│       ├── terraform.tfvars  # Prod-specific config (higher SKU, scale settings)
│       └── backend.tf
└── README.md              # Provisioning instructions
```

**Environment-Agnostic Principle**: All `main.tf` files use SAME modules, only `terraform.tfvars` differs:
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

**Backend**: Azure Storage Account (per environment to prevent conflicts)
```hcl
# environments/dev/backend.tf
terraform {
  backend "azurerm" {
    resource_group_name  = "innoventity-tfstate-rg"
    storage_account_name = "innoventitytfstate"
    container_name       = "tfstate-dev"
    key                  = "platform-core.tfstate"
  }
}
```

**State Isolation**: Separate state file per environment prevents accidental cross-environment changes.

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
**Validation**: Run EF Core migrations + smoke test queries after creation

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
**Validation**: Health check endpoint (`GET /health`) returns 200 after deployment

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

> **📋 Full Runbook**: For the complete DEV teardown guide (pre-flight checks, all three teardown options, validation steps, post-teardown cleanup, and lessons learned), see **[teardown.md](teardown.md)**.

**Prerequisites**:
1. Confirm no active users (production environments)
2. Confirm Terraform state backend (`innoventity-tfstate-rg` / `innoventitytfstate`) is intact before proceeding

**Steps**:
```bash
# 1. Navigate to environment directory
cd infrastructure/environments/dev

# 2. Plan destruction (dry-run)
terraform plan -destroy -out=tfplan-destroy

# 3. Review resources to be destroyed
# Confirm innoventity-tfstate-rg and innoventitytfstate do NOT appear in the plan.
# If they do, ABORT immediately.

# 4. Destroy all resources
terraform apply tfplan-destroy

# 6. (Optional) Clean up local Terraform cache
# rm -rf .terraform tfplan-destroy
# ❌ DO NOT delete the remote state blob in Azure Storage during routine teardown:
# az storage blob delete --account-name innoventitytfstate --container-name tfstate-dev --name platform-core.tfstate
```

**Duration**: ~3-5 minutes

**Blast Radius**: All environment resources destroyed (App Service, SQL Database, Application Insights). Data loss unless backed up. Terraform state backend is preserved.

**Validation**: Verify resources deleted in Azure Portal (resource group should be empty or deleted). Confirm `az storage account show --name innoventitytfstate` still returns 200.

---

### Update Workflow (Infrastructure Drift)

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

## Component Validation Matrix

| Component | Recreatability | Blast Radius | Recreation Cost | Validation Method |
|-----------|----------------|--------------|-----------------|-------------------|
| **Resource Group** | ✅ Full | High (destroys all children) | $0, 30s | `az group show` returns 200 |
| **App Service** | ✅ Full | API downtime (2-3 min) | $0.10-0.50, 2 min | `/health` returns 200, AppInsights receives telemetry |
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
- **Development**: Contributor on dev resource group, Reader on test/prod
- **Production**: Reader only, CI/CD pipeline handles deployments

---

## Testing Requirements

### Infrastructure Testing

**Tool**: Terratest (Go-based Terraform testing framework)

**Tests to Implement**:
```go
// tests/infrastructure_test.go
func TestDevEnvironmentProvisioning(t *testing.T) {
    t.Parallel()
    
    terraformOptions := &terraform.Options{
        TerraformDir: "../infrastructure/environments/dev",
        Vars: map[string]interface{}{
            "environment": "test-" + strings.ToLower(random.UniqueId()),
            "sql_admin_password": "TestP@ssw0rd123!",
            "jwt_secret_key": generateRandomBase64(32),
        },
    }
    
    // Ensure cleanup
    defer terraform.Destroy(t, terraformOptions)
    
    // Apply infrastructure
    terraform.InitAndApply(t, terraformOptions)
    
    // Validate outputs
    appServiceURL := terraform.Output(t, terraformOptions, "app_service_url")
    assert.NotEmpty(t, appServiceURL)
    
    // Test health endpoint
    http_helper.HttpGetWithRetry(
        t,
        fmt.Sprintf("https://%s/health", appServiceURL),
        nil,
        200,
        "Healthy",
        10,
        30*time.Second,
    )
}

func TestDatabaseRecreation(t *testing.T) {
    // 1. Provision environment
    // 2. Seed test data (insert Actor record)
    // 3. Capture expected schema (query sys.tables, sys.columns)
    // 4. Destroy SQL Database (terraform destroy -target=module.sql_database)
    // 5. Recreate SQL Database (terraform apply)
    // 6. Apply EF Core migrations
    // 7. Validate schema matches expected (compare sys.tables, sys.columns)
    // 8. Assert data loss (Actor record no longer exists - expected behavior)
}
```

**Validation**: Tests MUST demonstrate failure scenarios:
- ❌ Health check returns 500 if database connection string missing (prove detection works)
- ❌ Terraform apply fails if JWT key <256 bits (prove validation works)

### Epistemic Discipline for LLM-Generated Code

**Problem**: Terraform modules and validation scripts may be LLM-generated, require proof of correctness.

**Validation Strategy**:
1. **Characterization Tests**: Run terraform apply in isolated subscription, capture actual resource properties
   ```bash
   # After first manual apply, capture expected state
   terraform show -json > expected-state.json
   
   # Future applies: compare against baseline
   terraform show -json | jq 'del(.format_version, .terraform_version)' > current-state.json
   diff expected-state.json current-state.json
   ```

2. **Manual Verification Checklist** (before trusting IaC):
   - [ ] App Service provisions with HTTPS-only enabled
   - [ ] SQL Database firewall rules allow ONLY Azure services (not 0.0.0.0/0)
   - [ ] Application Insights linked to Log Analytics Workspace
   - [ ] App Service app_settings correctly inject secrets
   - [ ] Deployment slot (prod) uses same configuration as production

3. **Idempotency Testing**: Run `terraform apply` twice, second run must show "No changes" (no drift)

4. **Destruction Testing**: Run `terraform destroy`, verify ALL resources deleted (no orphaned resources)

**No Tautological Assertions**: Avoid tests like "assert terraform output matches terraform input" - instead, query Azure API directly:
```bash
# BAD TEST (tautological)
assert terraform.output("app_service_name") == "innoventity-dev-api"

# GOOD TEST (external validation)
az webapp show --name innoventity-dev-api --resource-group innoventity-dev-rg --query "httpsOnly" --output tsv | grep -q "true"
```

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
- [ ] Validation script (`validate-environment.sh`)
- [ ] .gitignore for Terraform state/secrets

### Deferred (Phase 1+):
- ⏸️ Test/Prod environment compositions (copy dev, adjust SKUs)
- ⏸️ Terratest infrastructure tests (validate manually first)
- ⏸️ CI/CD pipeline integration (Azure DevOps YAML)
- ⏸️ Auto-shutdown scripts for cost optimization
- ⏸️ Azure Monitor alerts (Application Insights manual review sufficient for now)

### Documentation Priority:
- **High**: Creation workflow, validation checklist, secret injection strategy
- **Medium**: Destruction workflow, cost analysis
- **Low**: Terratest examples, alert configurations

---

## Open Questions / Decisions Needed

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
