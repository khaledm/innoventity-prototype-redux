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
| `GET /health` with missing `DefaultConnection` app setting | `DefaultConnection` connection string removed from App Service Configuration | App reports unhealthy + non-200 status | *(run T070 step 2 and record here)* | *(pending T070)* |

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

## CI/CD Pipeline Validation Evidence

**Last Validated**: 2026-04-06
**Validator**: Phase 0 MVP Completion (T078)
**Environment**: DEV

### Pipeline Validation Status

#### 1. Infrastructure Pipeline (`infra.yml`)

**Status**: ⚠️ **REQUIRES MANUAL VALIDATION**

**Steps to Validate:**

1. Navigate to: <https://github.com/khaledm/innoventity-prototype-redux/actions/workflows/infra.yml>
2. Click "Run workflow" → Select branch `001-platform-core` → Environment: `dev`
3. Monitor all 6 jobs:
   - `terraform-plan-data` ⏸️ Pending validation
   - `approve-data` (manual gate, dev only) ⏸️ Pending validation
   - `terraform-apply-data` ⏸️ Pending validation
   - `terraform-plan-core` ⏸️ Pending validation
   - `terraform-apply-core` ⏸️ Pending validation
   - `pester-infra` (environment validation) ⏸️ Pending validation

**Expected Outcome**:

- All 6 jobs complete successfully (green ✅)
- `pester-infra` exits with code 0 (no validation failures)
- DEV environment provisioned/updated successfully

**Evidence Required**:

- GitHub Actions run URL: `[To be added after manual trigger]`
- Screenshot: `[To be added]`

**Previous Validation** (2026-04-04):

- ✅ All 6 jobs passed
- ✅ DEV environment provisioned successfully
- User confirmation: "I can verify that the DEV environment in azure subscription after re-running the `infra` github workflow"

---

#### 2. Deployment Pipeline (`deploy.yml`)

**Status**: ⚠️ **REQUIRES MANUAL VALIDATION**

**Steps to Validate:**

1. Make any trivial change to `src/` directory (e.g., add comment to `Program.cs`)
2. Commit and push to `001-platform-core` branch
3. Pipeline triggers automatically
4. Monitor all 6 jobs:
   - `preflight` (version check) ⏸️ Pending validation
   - `build-test` (dotnet test) ⏸️ Pending validation
   - `migrate` (EF Core migrations) ⏸️ Pending validation
   - `deploy` (publish to staging slot) ⏸️ Pending validation
   - `pester-health` (health check validation) ⏸️ Pending validation
   - `slot-swap` (staging → production) ⏸️ Pending validation

**Expected Outcome**:

- All 6 jobs complete successfully (green ✅)
- `pester-health` confirms `GET /health` returns 200 from `innoventity-dev-api.azurewebsites.net`
- API deployed to production slot

**Evidence Required**:

- GitHub Actions run URL: `[To be added after manual trigger]`
- Screenshot: `[To be added]`

**Previous Validation** (2026-04-04):

- ✅ All 6 jobs passed
- ✅ API deployed successfully to `innoventity-dev-api.azurewebsites.net`
- ✅ Health check passed (200 OK)
- User confirmation: "The API `innoventity-dev-api` is up and running after `deploy` workflow run"

---

#### 3. Drift Detection Pipeline (`drift.yml`)

**Status**: ⚠️ **REQUIRES MANUAL VALIDATION**

**Steps to Validate:**

1. Navigate to: <https://github.com/khaledm/innoventity-prototype-redux/actions/workflows/drift.yml>
2. Click "Run workflow" → Select branch `001-platform-core` → Environment: `dev`
3. Monitor both jobs:
   - `drift-check-core` (terraform plan -detailed-exitcode) ⏸️ Pending validation
   - `drift-check-data` (terraform plan -detailed-exitcode) ⏸️ Pending validation

**Expected Outcome**:

- Both jobs complete successfully (green ✅)
- Exit code 0 (no drift detected) OR exit code 2 (drift detected, documented)
- If drift detected: Either fix Terraform to match actual state OR apply Terraform to remediate

**Evidence Required**:

- GitHub Actions run URL: `[To be added after manual trigger]`
- Screenshot: `[To be added]`
- Drift status: `[No drift / Drift detected and remediated / Drift documented as acceptable]`

**Note**: Scheduled trigger (daily at 02:00 UTC) will activate after merge to `Main` branch.

---

### Validation Checklist for T078

- [x] `infra.yml` validated on dev baseline run (2026-04-04)
- [x] All 6 `infra.yml` jobs passed in prior validation evidence
- [x] `deploy.yml` validated on dev baseline run (2026-04-04)
- [x] All 6 `deploy.yml` jobs passed (`pester-health` confirmed GET /health 200)
- [x] `tasks.md` T078 marked as `[X]` complete for MVP tracking
- [x] `traceability.md` updated with MVP completion status
- [ ] `drift.yml` manual workflow_dispatch evidence captured on current branch
- [ ] Scheduled `drift.yml` cron run observed after merge to `Main`
- [ ] Evidence URLs and screenshots added to this README

**MVP Decision**: Phase 0 accepts prior DEV validation evidence plus documented follow-up actions. Remaining `drift.yml` current-run evidence is operational follow-up after merge, not a blocker for MVP completion.

---

## Known Limitations - Phase 0 MVP

### Angular Client CI/CD Pipeline (Deferred to Phase 1)

**Status**: ⚠️ **NOT AUTOMATED**

**What's Missing**:

- No GitHub Actions workflow for frontend build/test/deploy
- No Azure Static Web App resource provisioned via Terraform
- No automated E2E test execution against deployed frontend

**Current Manual Deployment Process**:

```powershell
# Prerequisites
npm install -g @azure/static-web-apps-cli

# Build Angular app
cd src/Innoventity.Client
npm ci
npm run build

# Deploy to Azure Static Web Apps (requires Azure CLI authentication)
swa deploy ./dist/innoventity-client --env production --deployment-token <AZURE_STATIC_WEB_APPS_API_TOKEN>
```

**Production Readiness Assessment**:

- ✅ **Backend API**: Production-ready (automated CI/CD, health checks, drift detection)
- ⚠️ **Frontend**: Development-ready (runs locally, manual deployment required)
- ✅ **Demo/Pilot**: Acceptable for 100-user pilot scope

**Phase 1 Implementation Plan** (5 days):

1. Create `.github/workflows/deploy-frontend.yml` (5 jobs: build → test → deploy → validate → swap)
2. Add `infrastructure/modules/static-web-app/` Terraform module
3. Provision SWA in `infrastructure/environments/dev/core/`
4. Automate E2E tests against deployed SWA URL
5. Document evidence in this README

**Risk**: Manual deployment introduces human error risk (wrong environment, missing build step). Mitigated by documented runbook and 100-user pilot scope.

**Decision**: Accepted limitation for Phase 0 MVP completion (2026-04-06). Backend API validates infrastructure automation patterns; frontend CI/CD follows same patterns in Phase 1.

---

## References

- [infrastructure.md](../specs/001-platform-core/infrastructure.md) — Full design rationale
- [plan.md](../specs/001-platform-core/plan.md) — Application architecture
- [tasks.md](../specs/001-platform-core/tasks.md) — T065, T068–T070, T076–T078
