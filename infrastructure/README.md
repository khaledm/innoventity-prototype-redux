# Infrastructure: Innoventity Platform Core

This directory contains the Terraform code for provisioning the Innoventity Platform Core Azure environment.

**Tool**: Terraform 1.6+ with Azure Provider 3.x  
**Design**: Split-state layout — `core/` (stateless) and `data/` (stateful) are independent root modules with independent state backends.

---

## Directory Structure

```
infrastructure/
├── modules/
│   ├── app-service/       # Azure App Service Plan + Linux Web App + staging slot
│   ├── sql-database/      # Azure SQL Server + Database + firewall rules
│   └── monitoring/        # Log Analytics Workspace + Application Insights
├── environments/
│   └── dev/
│       ├── core/          # Stateless: App Service, App Insights → state: tfstate-dev-core
│       └── data/          # Stateful:  Resource Group, SQL         → state: tfstate-dev-data
├── tests/
│   ├── pester/            # Post-apply environment state validation (Pester v5)
│   └── terratest/         # IaC code correctness (Terratest v0.46+ / Go 1.21+)
└── scripts/
    ├── create-environment.ps1   # Orchestrates full environment lifecycle
    └── validate-environment.ps1 # Invokes Pester -CI against live environment
```

---

## Prerequisites

1. Azure subscription with Contributor access
2. Terraform 1.6+ (`terraform -version`)
3. Azure CLI authenticated (`az login`)
4. Go 1.21+ for Terratest (`go version`) — local test runs only
5. PowerShell + Pester v5 (`Install-Module Pester -Force`)

### One-Time Bootstrap (state storage)

Run once; these resources are shared across all environments:

```powershell
az group create --name innoventity-tfstate-rg --location "East US"

az storage account create `
  --name innoventitytfstate `
  --resource-group innoventity-tfstate-rg `
  --sku Standard_LRS

# Create one container per environment per layer
az storage container create --name tfstate-dev-core  --account-name innoventitytfstate
az storage container create --name tfstate-dev-data  --account-name innoventitytfstate
```

---

## Creation Workflow

Apply `data/` first (creates resource group + SQL), then `core/` (creates App Service + App Insights).

### Step 1 — Apply data layer

```powershell
cd infrastructure/environments/dev/data

# Copy terraform.tfvars.example → terraform.tfvars and fill in sql_admin_password
Copy-Item terraform.tfvars.example terraform.tfvars
notepad terraform.tfvars   # fill in sql_admin_password

terraform init
terraform plan -out=tfplan
terraform apply tfplan

# Capture connection string for next step
$CONNECTION_STRING = terraform output -raw connection_string
```

### Step 2 — Apply core layer

```powershell
cd infrastructure/environments/dev/core

# Copy terraform.tfvars.example → terraform.tfvars
Copy-Item terraform.tfvars.example terraform.tfvars
# Fill in jwt_secret_key (generate one below) and paste connection_string from Step 1
$JWT_KEY = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))

terraform init
terraform plan -out=tfplan `
  -var "connection_string=$CONNECTION_STRING" `
  -var "jwt_secret_key=$JWT_KEY"
terraform apply tfplan
```

### Step 3 — Apply EF Core migrations

```powershell
dotnet ef database update `
  --project src/Innoventity.API `
  --connection "$CONNECTION_STRING"
```

### Step 4 — Validate

```powershell
$APP_HOSTNAME = terraform output -raw app_service_hostname   # from core/
Invoke-RestMethod "https://$APP_HOSTNAME/health"
# Expected: { "status": "Healthy", "checks": [...] }
```

Total provisioning time (target): ≤10 minutes from `terraform apply data/` to `/health` returning 200.

---

## Destruction Workflow

### Routine teardown (core layer only — no data loss)

Tears down App Service and App Insights. SQL Server, SQL Database, and Resource Group are untouched.

```powershell
cd infrastructure/environments/dev/core
terraform plan -destroy -out=tfplan-destroy
terraform apply tfplan-destroy
```

### Full teardown (data layer — intentional, change-controlled)

⚠️ **Irreversible for production data.** Requires removing `prevent_destroy = true` from `modules/sql-database/main.tf` lifecycle block.

```powershell
# 1. Remove prevent_destroy from modules/sql-database/main.tf
# 2. Then:
cd infrastructure/environments/dev/data
terraform plan -destroy -out=tfplan-destroy
terraform apply tfplan-destroy
```

> ❌ **Do not use `terraform destroy -target`** for either layer. See infrastructure.md §Split-State Principle.

---

## Idempotency Validation (T070)

Run a second `terraform apply` on both layers and assert "No changes":

```powershell
# core/
cd infrastructure/environments/dev/core
terraform apply      # must exit 0 with "No changes. Your infrastructure matches the configuration."

# data/
cd infrastructure/environments/dev/data
terraform apply      # must exit 0 with "No changes. Your infrastructure matches the configuration."
```

---

## Characterisation Failure Mode Record

> **Principle 5 (Mark Seemann)**: Record the *actual* observed behaviour when the DB connection string is missing. If this has never been tested, the Pester/Terratest suite has no validity.

| Test | Condition | Expected | Actual observed | Date recorded |
|------|-----------|----------|-----------------|---------------|
| `GET /health` with missing `DefaultConnection` app setting | `DefaultConnection` connection string removed from App Service Configuration | App reports unhealthy + non-200 status | _(run T070 step 2 and record here)_ | _(pending T070)_ |

**T070 procedure for this record**:
1. Temporarily remove `DefaultConnection` from App Service Configuration (via Terraform/tfvars)
2. Run `GET /health` and record the **exact HTTP status code** and **response body** in the table above
3. Restore the connection string
4. Confirm `GET /health` returns 200 again

---

## Running Pester Tests

```powershell
# Set required environment variables
$env:APP_SERVICE_NAME    = "innoventity-dev-api"     # from: terraform output -raw app_service_name
$env:RESOURCE_GROUP_NAME = "innoventity-dev-rg"      # from: terraform output -raw resource_group_name

# Run all Pester tests
Invoke-Pester -CI ./infrastructure/tests/pester/

# Run health check only (used by deploy.yml pester-health job)
Invoke-Pester -CI ./infrastructure/tests/pester/HealthCheck.Tests.ps1
```

## Running Terratest (local only — Phase 0)

```powershell
cd infrastructure/tests/terratest
go test ./... -timeout 30m -v
```

> Terratest provisions a real Azure environment with a randomised name, asserts via Azure SDK (non-tautological), and destroys via `defer terraform.Destroy` on completion.

---

## Secret Injection

| Secret | Local (dev) | CI/CD (pipeline) |
|--------|-------------|------------------|
| `jwt_secret_key` | `terraform.tfvars` (gitignored) | GitHub Secret `JWT_SECRET_KEY` |
| `sql_admin_password` | `terraform.tfvars` (gitignored) | GitHub Secret `SQL_ADMIN_PASSWORD` |
| `connection_string` | Copied from `data/` output | Read from `data/` output in pipeline |

⚠️ `terraform.tfvars` files are `.gitignored`. **Never commit them.** Use `terraform.tfvars.example` as the committed template.

---

## References

- [infrastructure.md](../specs/001-platform-core/infrastructure.md) — Full design rationale
- [plan.md](../specs/001-platform-core/plan.md) — Application architecture
- [tasks.md](../specs/001-platform-core/tasks.md) — T065, T068–T070, T076–T078
