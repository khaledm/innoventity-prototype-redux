# DEV Environment Teardown Runbook

**Branch**: `001-platform-core` | **Last Updated**: April 11, 2026
**Related**: [infrastructure.md](infrastructure.md) (full IaC reference) | [quickstart.md](quickstart.md) (environment setup)

---

## Purpose

This runbook provides step-by-step instructions to **completely tear down the DEV environment** while **preserving the foundational infrastructure** (Terraform state backend and service principal credentials). It synthesises all experience accumulated across Platform Core (001), Domain Enhancements (002), and API Completion (003) phases.

Use this when:

- Stopping active DEV work to save Azure costs (~$20/month for the DEV environment)
- Cleaning up after a feature branch merge
- Resetting a broken or drifted DEV environment before re-provisioning
- End of a development sprint

---

## Table of Contents

1. [Architecture Overview: What to Preserve vs. What to Destroy](#architecture-overview)
2. [Pre-Teardown Checklist](#pre-teardown-checklist)
3. [Option A: Terraform Teardown (Recommended)](#option-a-terraform-teardown-recommended)
4. [Option B: Azure Developer CLI Teardown](#option-b-azure-developer-cli-teardown)
5. [Option C: Manual Azure CLI Teardown](#option-c-manual-azure-cli-teardown)
6. [Post-Teardown Validation](#post-teardown-validation)
7. [Local Cleanup](#local-cleanup)
8. [Lessons Learned & Known Pitfalls](#lessons-learned--known-pitfalls)
9. [Re-Provisioning After Teardown](#re-provisioning-after-teardown)

---

## Architecture Overview

### 🔒 Foundational Infrastructure — DO NOT DESTROY

These resources were bootstrapped **once, manually**, and are shared across all environments. Destroying them loses Terraform state and locks you out of IaC management.

| Resource | Azure Name | Purpose |
| -------- | ---------- | ------- |
| Resource Group | `innoventity-tfstate-rg` | Container for Terraform state storage |
| Storage Account | `innoventitytfstate` | Hosts all Terraform remote state files |
| Blob Container | `tfstate-dev` | DEV-specific state container |
| State Blob | `platform-core.tfstate` | Terraform state for the DEV environment |
| Service Principal | Innoventity Terraform SP | Azure AD identity used by Terraform/CI-CD |
| GitHub Secrets | `AZURE_*`, `ARM_*`, `JWT_SECRET_KEY`, `SQL_ADMIN_PASSWORD` | CI/CD pipeline credentials |

> **Rule**: Never run `az storage blob delete` on `platform-core.tfstate` or `az group delete` on `innoventity-tfstate-rg` during a routine teardown. These actions are **irreversible** and require manual bootstrap recovery.

### 💣 DEV Environment Resources — SAFE TO DESTROY

DEV resource names differ by provisioning path. Use the correct set for the option you are executing.

#### Terraform-managed DEV (Option A)

Everything below lives in `innoventity-dev-rg` and is fully reproducible from Terraform.

| Resource | Azure Name | Blast Radius |
| -------- | ---------- | ------------ |
| Resource Group | `innoventity-dev-rg` | Destroys all children below |
| App Service | `innoventity-dev-api` | API goes offline |
| App Service Plan | `innoventity-dev-asp` | Hosting plan removed |
| SQL Server | `innoventity-dev-sql` | **Data loss** — test data only (expected) |
| SQL Database | `Innoventity` | All application data lost |
| Application Insights | `innoventity-dev-ai` | Historical telemetry loss (30–90d retained in workspace) |
| Log Analytics Workspace | `innoventity-dev-law` | Telemetry logs lost |
| Service Bus Namespace | `innoventity-dev-sb` | In-flight messages lost (negligible for DEV) |

#### azd/manual DEV (Option B and quickstart Azure CLI flow)

The quickstart `azd up` / manual Azure CLI examples use the following naming pattern.

| Resource | Azure Name Pattern |
| -------- | ------------------ |
| Resource Group | `innoventity-rg-dev` |
| App Service | `innoventity-api-dev` |
| App Service Plan | `innoventity-asp-dev` |
| SQL Server | `innoventity-sql-dev` |
| Application Insights | `innoventity-ai-dev` |

> If your names differ, discover them before deletion with: `az group list --query "[?contains(name, 'innoventity')].name" -o table`

> **Data Note**: The DEV database contains only seeded test data (quantum battery prototype, test actors, 10 ICB industries). No real user data is ever stored in DEV. Data loss is expected and acceptable.

---

## Pre-Teardown Checklist

Complete all items before executing any destroy commands.

### 1. Confirm No Active Work

- [ ] No open pull requests targeting `001-platform-core` that require the DEV environment
- [ ] No pending test runs (GitHub Actions workflows for the feature branch)
- [ ] Team members aware of teardown (solo project: just you)

### 2. Capture Any Important State

- [ ] Note the current App Service URL (for example, `https://innoventity-dev-api.azurewebsites.net` or `https://innoventity-api-dev.azurewebsites.net`)
- [ ] Note the current SQL Server name (for example, `innoventity-dev-sql` or `innoventity-sql-dev`)
- [ ] (Optional) Export Application Insights telemetry if you want to retain diagnostics data

### 3. Confirm Terraform State is Clean

```bash
# Check CORE layer state (App Service + App Insights)
cd infrastructure/environments/dev/core
terraform init  # Required to connect to remote backend
terraform state list
# Expected: module.app_service.azurerm_linux_web_app.main,
#           module.monitoring.azurerm_application_insights.main, etc.

# Check DATA layer state (Resource Group + SQL)
cd ../data
terraform init  # Required to connect to remote backend
terraform state list
# Expected: azurerm_resource_group.main,
#           azurerm_mssql_server.main, azurerm_mssql_database.main, etc.

# If either command errors, the state backend may be missing — STOP and investigate.
```

### 4. Verify GitHub Actions are Not Running

- Navigate to the repository's **Actions** tab on GitHub
- Confirm no in-progress workflow runs for `003-api-completion` or `001-platform-core` branches
- If runs are in progress, wait for them to complete or cancel them manually

---

## Option A: Terraform Teardown (Recommended)

**Use when**: The DEV environment was provisioned with Terraform (Tasks T068–T069).
**Duration**: ~5–8 minutes (two-phase destruction)
**Preserves**: Terraform state backend in `innoventity-tfstate-rg`/`innoventitytfstate`

**⚠️ CRITICAL**: DEV uses split-state architecture. You must destroy BOTH layers in order:

1. **CORE layer first** (`core/` - App Service + App Insights) - no dependencies on data
2. **DATA layer second** (`data/` - Resource Group + SQL) - contains the resource group

```bash
# ═══════════════════════════════════════════════════════════════════
# PHASE 1: Destroy CORE Layer (Stateless Resources)
# ═══════════════════════════════════════════════════════════════════

# 1. Authenticate to Azure
az login
# OR using service principal (recommended for CI/CD):
# az login --service-principal -u $ARM_CLIENT_ID -p $ARM_CLIENT_SECRET --tenant $ARM_TENANT_ID
az account set --subscription "<your-subscription-id>"

# 2. Navigate to CORE subdirectory
cd infrastructure/environments/dev/core

# 3. Initialize backend connection (REQUIRED on fresh clone or after cleanup)
terraform init

# 4. Generate destruction plan for CORE layer
# (terraform plan -destroy automatically refreshes state, so no separate refresh step needed)
terraform plan -destroy -out=tfplan-destroy-core

# 5. REVIEW the plan output carefully
# Expected resources: App Service, App Service Plan, App Insights only
# CONFIRM you do NOT see:
#   - azurerm_resource_group (owned by data layer)
#   - azurerm_mssql_server (owned by data layer)
#   - innoventity-tfstate-rg or innoventitytfstate (foundational infra)
# If any appear, ABORT immediately — something is wrong.

# 6. Execute CORE destruction
terraform apply tfplan-destroy-core
# Expected output: "Destroy complete! Resources: 3-5 destroyed."
# ✅ App Service and App Insights deleted

# ═══════════════════════════════════════════════════════════════════
# PHASE 2: Destroy DATA Layer (Stateful Resources)
# ═══════════════════════════════════════════════════════════════════

# 7. Navigate to DATA subdirectory
cd ../data

# 8. Initialize backend connection
terraform init

# 9. Generate destruction plan for DATA layer
terraform plan -destroy -out=tfplan-destroy-data

# 10. REVIEW the plan output carefully
# Expected resources: Resource Group, SQL Server, SQL Database
# CONFIRM you do NOT see innoventity-tfstate-rg or innoventitytfstate

# 11. Execute DATA destruction
terraform apply tfplan-destroy-data
# Expected output: "Destroy complete! Resources: 3-4 destroyed."
# ✅ All DEV resources deleted

# ═══════════════════════════════════════════════════════════════════
# Cleanup (Optional)
# ═══════════════════════════════════════════════════════════════════

# ⚠️ OPTIONAL — Only if permanently decommissioning (NOT for routine teardown):
# Delete local Terraform cache (safe — regenerated by terraform init)
# cd ../core && rm -rf .terraform
# cd ../data && rm -rf .terraform

# ❌ DO NOT run these commands during routine teardown:
# az storage blob delete --account-name innoventitytfstate --container-name tfstate-dev-core --name platform-core.tfstate
# az storage blob delete --account-name innoventitytfstate --container-name tfstate-dev-data --name platform-core.tfstate
# az group delete --name innoventity-tfstate-rg
```

> **State Isolation**: The `tfplan-destroy` plan is scoped to the resources in the Terraform state for this environment. It will NOT touch the `innoventity-tfstate-rg` resource group or the `innoventitytfstate` storage account because those were created manually outside Terraform state.

---

## Option B: Azure Developer CLI Teardown

**Use when**: The DEV environment was provisioned with `azd up` (as documented in quickstart.md Option 1).
**Duration**: ~5–8 minutes
**Preserves**: `azd`-managed resources only — does NOT touch the Terraform state backend

```bash
# 1. Ensure azd is authenticated
azd auth login

# 2. Target the dev environment
azd env select dev

# 3. Tear down all provisioned resources
azd down

# Follow the prompts:
# - Confirm resource group deletion: yes
# - Purge soft-deleted resources (Key Vault): yes (avoids name reservation issues on re-provision)
```

**Expected output**:

``` text
Deleting all resources and deployed code on Azure (azd down)

  (✓) Done: Deleting service innoventity-api
  (✓) Done: Deleting resource group <dev-resource-group>

SUCCESS: Your application has been removed from Azure in 6 minutes.
```

For quickstart `azd up`, `<dev-resource-group>` is typically `innoventity-rg-dev`. For Terraform Option A it is `innoventity-dev-rg`.

> **Note**: `azd down` only removes resources managed in the `azd` environment manifest. It does **not** touch the Terraform state storage account (`innoventitytfstate`) because that resource was created outside `azd`.

---

## Option C: Manual Azure CLI Teardown

**Use when**: Neither Terraform nor `azd` was used, OR as an emergency fallback if both tools fail.
**Duration**: ~2–4 minutes
**Warning**: This bypasses Terraform state — run `terraform state rm <resource>` for each deleted resource afterwards, or accept that the next `terraform plan` will show drift.

```bash
# 1. Authenticate
az login
az account set --subscription "<your-subscription-id>"

# 2. Delete the DEV resource group (destroys ALL contained resources simultaneously)
# This is the nuclear option — it deletes everything in the selected DEV resource group in one command.
# Pick the group that matches your provisioning path:
#   Terraform path: DEV_RG="innoventity-dev-rg"
#   azd/manual path: DEV_RG="innoventity-rg-dev"
DEV_RG="innoventity-dev-rg"

az group delete \
  --name "$DEV_RG" \
  --yes \
  --no-wait

# The --no-wait flag returns immediately; deletion runs asynchronously (~2-4 min).
# Monitor progress:
az group show --name "$DEV_RG" --query "properties.provisioningState" -o tsv
# Keep running until it shows "Deleting" then disappears entirely (404).

# 3. ⚠️ IMPORTANT: Sync Terraform state after manual deletion
# Since we bypassed Terraform, the state files still reference the deleted resources.
# Clean up BOTH state files (core and data):

# Clean CORE layer state
cd infrastructure/environments/dev/core
terraform init
terraform state list
# Should show resources, but they're already deleted in Azure

# Remove all resources from state
terraform state list | while IFS= read -r resource; do
  terraform state rm "$resource"
done

# Clean DATA layer state
cd ../data
terraform init
terraform state list
# Should show resources, but they're already deleted in Azure

# Remove all resources from state
terraform state list | while IFS= read -r resource; do
  terraform state rm "$resource"
done
```

> **Prefer Option A** (Terraform) over this whenever possible. Manual deletion creates state drift and requires additional cleanup.

---

## Post-Teardown Validation

Run these checks to confirm the teardown completed successfully.

### 1. Verify DEV Resource Group is Gone

```bash
DEV_RG="innoventity-dev-rg"  # or innoventity-rg-dev for azd/manual flow
az group show --name "$DEV_RG"
# Expected: "ResourceGroupNotFound" error (404)
# If 200 is returned, check Azure Portal for stuck resource locks
```

### 2. Verify App Service is Unreachable

```bash
APP_NAME="innoventity-dev-api"  # or innoventity-api-dev for azd/manual flow
curl -f "https://${APP_NAME}.azurewebsites.net/health"
# Expected: connection refused or DNS resolution failure
# (azurewebsites.net DNS will still resolve briefly — look for 503 or connection error)
```

### 3. Verify Foundational Infrastructure Intact

```bash
# Terraform state backend must still exist
az storage account show \
  --name innoventitytfstate \
  --resource-group innoventity-tfstate-rg \
  --query "name" -o tsv
# Expected: "innoventitytfstate"

# State file must still exist
az storage blob show \
  --account-name innoventitytfstate \
  --container-name tfstate-dev \
  --name platform-core.tfstate \
  --auth-mode login \
  --query "name" -o tsv
# Expected: "platform-core.tfstate"
# Note: `--auth-mode login` uses your Azure AD identity from `az login` (user or service principal).
# Ensure that identity has Storage Blob Data Reader (or higher) on the state storage account.

# Terraform state lists must return empty (not an error)
cd infrastructure/environments/dev/core
terraform init
terraform state list
# Expected: empty output (no CORE resources in state after successful destroy)

cd ../data
terraform init
terraform state list
# Expected: empty output (no DATA resources in state after successful destroy)
```

### 4. Verify SQL Server is Gone

```bash
SQL_SERVER_NAME="innoventity-dev-sql"  # or innoventity-sql-dev for azd/manual flow
DEV_RG="innoventity-dev-rg"            # or innoventity-rg-dev for azd/manual flow

az sql server show \
  --name "$SQL_SERVER_NAME" \
  --resource-group "$DEV_RG"
# Expected: "ResourceNotFound" error (404)
```

### 5. Confirm Monthly Cost Drop

- In Azure Portal → Cost Management → Filter by your DEV resource group (`innoventity-dev-rg` for Terraform flow, `innoventity-rg-dev` for azd/manual flow)
- Resource group should show $0 going forward (no running resources)
- Expected savings: ~$20/month (B1 App Service + Basic SQL + Application Insights)

---

## Local Cleanup

After the Azure resources are destroyed, clean up local development artefacts.

```bash
# 1. Remove local Terraform cache (safe to delete — re-created with terraform init)
cd infrastructure/environments/dev/core
rm -rf .terraform
rm -f tfplan-destroy-core

cd ../data
rm -rf .terraform
rm -f tfplan-destroy-data

# 2. Keep local terraform.tfvars by default; it contains values needed for re-provisioning.
# Only delete it if you have already archived required values in a secure secret store,
# or if you intend to rotate those secrets before the next deployment.
# rm -f terraform.tfvars

# 3. Remove any temporary development files (lessons learned: L7 from implementation-lessons.md)
# These should already be in .gitignore (commit-*.txt, test-*.txt) but clean up manually:
REPO_ROOT=$(git rev-parse --show-toplevel)
find "$REPO_ROOT" -maxdepth 2 \( -name "commit-*.txt" -o -name "test-*.txt" \) -delete

# 4. Clear local SQL database (if using localdb for development)
dotnet ef database drop --force --project src/Innoventity.API
```

### What to Keep Locally

| File/Directory | Keep? | Reason |
| -------------- | ----- | ------ |
| `infrastructure/environments/dev/core/.terraform/` | ❌ Delete | Regenerated by `terraform init` |
| `infrastructure/environments/dev/data/.terraform/` | ❌ Delete | Regenerated by `terraform init` |
| `infrastructure/environments/dev/core/terraform.tfvars` | ⚠️ Secure | Contains secrets — store in password manager |
| `infrastructure/environments/dev/data/terraform.tfvars` | ⚠️ Secure | Contains secrets — store in password manager |
| `infrastructure/environments/dev/core/tfplan-destroy-core` | ❌ Delete | Stale plan after successful destroy |
| `infrastructure/environments/dev/data/tfplan-destroy-data` | ❌ Delete | Stale plan after successful destroy |
| `infrastructure/modules/` | ✅ Keep | Module source — committed to git |
| `infrastructure/environments/dev/core/main.tf` | ✅ Keep | Environment definition — committed to git |
| `infrastructure/environments/dev/core/backend.tf` | ✅ Keep | State backend config — committed to git |
| `infrastructure/environments/dev/data/main.tf` | ✅ Keep | Environment definition — committed to git |
| `infrastructure/environments/dev/data/backend.tf` | ✅ Keep | State backend config — committed to git |

---

## Lessons Learned & Known Pitfalls

These are drawn from implementation experience across all three feature phases.

### P1: State Isolation Prevents Cross-Environment Accidents

**From**: infrastructure.md §Terraform State Management
Each environment (dev, test, prod) has **separate state files** in separate containers:

- `tfstate-dev-core` for core layer (App Service + App Insights)
- `tfstate-dev-data` for data layer (Resource Group + SQL)

Running `terraform destroy` in `infrastructure/environments/dev/core` can ONLY destroy CORE resources — it cannot touch DATA layer or other environments because there is no overlap in state.

**Lesson**: Always `cd` into the correct subdirectory (`core/` or `data/`) before running any Terraform command. Running commands at `infrastructure/environments/dev/` will fail with "empty directory" error.

### P2: HasData() Seeding Does Not Need Rollback on Teardown

**From**: implementation-lessons.md L1 (EF Core HasData() Seed Conflicts)
The ICB industry data (HLTH-001, TECH-001, ENRG-001, etc.) seeded via `AppDbContext.HasData()` lives entirely in the SQL Database. When the database is destroyed, all seeded data is deleted automatically — no separate cleanup step is needed.

**Lesson**: There is no separate data cleanup step before teardown. Destroy the database directly.

### P3: App Service Deployment Slots (Production Only)

**From**: infrastructure.md §App Service Module
The DEV App Service does NOT use deployment slots (staging slot is production-only). There is no slot to swap or delete separately.

**Lesson**: For DEV teardown, just destroy the single App Service instance.

### P4: gitignore Prevents Re-Committing Terraform State

**From**: infrastructure.md §Security Considerations | implementation-lessons.md L7
The `.gitignore` should include:

``` text
infrastructure/**/*.tfvars
infrastructure/**/.terraform/
infrastructure/**/terraform.tfstate*
```

These entries prevent accidentally committing secrets or local state files. Verify `.gitignore` is correct before pushing any infrastructure-related changes.

### P5: Application Insights Telemetry Survives Environment Destruction

**From**: infrastructure.md §Component Validation Matrix
Application Insights logs are retained for 30–90 days by default in the Log Analytics Workspace. After the workspace is destroyed, historical logs are permanently lost. If you need to retain diagnostic data from the DEV run, export it before teardown:

```bash
# Export recent traces via Azure CLI before destroying
APP_INSIGHTS_NAME="innoventity-dev-ai"  # or innoventity-ai-dev for azd/manual flow
DEV_RG="innoventity-dev-rg"             # or innoventity-rg-dev for azd/manual flow

az monitor app-insights query \
  --app "$APP_INSIGHTS_NAME" \
  --analytics-query "traces | where timestamp > ago(7d) | limit 1000" \
  --resource-group "$DEV_RG" \
  -o json > /tmp/dev-traces-$(date +%Y%m%d).json
```

### P6: Terraform Service Principal Credentials Are NOT Destroyed

**From**: infrastructure.md §Least Privilege Access
The Terraform Service Principal lives in Azure Active Directory (not in the `innoventity-dev-rg` resource group) and is NOT managed by the DEV environment Terraform state. It persists across environment teardowns.

**Lesson**: After teardown, the SP credentials in GitHub Secrets remain valid for re-provisioning.

### P7: Verify No Resource Locks Before Destroying

Azure resource locks (e.g., `CanNotDelete`) on the SQL Server or App Service will block `terraform destroy`. Check for locks if the destroy hangs:

```bash
az lock list --resource-group innoventity-dev-rg -o table
# If locks exist, remove them:
az lock delete --name <lock-name> --resource-group innoventity-dev-rg
```

---

## Re-Provisioning After Teardown

Once the DEV environment is torn down, re-provisioning from scratch takes ~10 minutes:

```bash
# 1. Navigate to DATA layer (create resource group + SQL first)
cd infrastructure/environments/dev/data

# 2. Re-initialise Terraform (downloads providers, re-connects to state backend)
terraform init

# 3. Recreate terraform.tfvars with fresh secrets
cat > terraform.tfvars <<EOF
environment          = "dev"
location             = "East US"
app_service_sku      = "B1"
sql_database_sku     = "Basic"
retention_days       = 7
sql_admin_password   = "$(python3 -c 'import secrets, string; chars = string.ascii_uppercase + string.ascii_lowercase + string.digits + "!@#%^&*"; pw = [secrets.choice(string.ascii_uppercase), secrets.choice(string.ascii_lowercase), secrets.choice(string.digits), secrets.choice("!@#%^&*")] + [secrets.choice(chars) for _ in range(12)]; import random; random.shuffle(pw); print("".join(pw))')"
jwt_secret_key       = "$(openssl rand -base64 32)"
EOF

# 4. Apply DATA layer (creates Resource Group + SQL)
terraform apply

# 5. Capture SQL connection string
CONNECTION_STRING=$(terraform output -raw connection_string)

# 6. Navigate to CORE layer
cd ../core
terraform init

# 7. Apply CORE layer (creates App Service + App Insights)
terraform apply -var="connection_string=$CONNECTION_STRING"

# 8. Deploy application code
PUBLISH_DIR=/tmp/publish
ZIP_PATH=/tmp/publish.zip

dotnet publish src/Innoventity.API -c Release -o "$PUBLISH_DIR"
(cd "$PUBLISH_DIR" && zip -r "$ZIP_PATH" .)

az webapp deployment source config-zip \
  --resource-group innoventity-dev-rg \
  --name innoventity-dev-api \
  --src "$ZIP_PATH"

# 6. Apply database migrations
dotnet ef database update \
  --project src/Innoventity.API \
  --connection "$(terraform output -raw sql_connection_string)"

# 7. Validate
curl https://$(terraform output -raw app_service_hostname)/health
# Expected: {"status":"Healthy","checks":[...]}
```

> **Reference**: Full provisioning guide: [infrastructure.md §Creation Workflow](infrastructure.md#creation-workflow) and [quickstart.md §Deployment to Azure](quickstart.md#deployment-to-azure).

---

## Teardown Decision Matrix

| Situation | Recommended Option |
| --------- | ------------------ |
| Standard cost-saving teardown (will re-provision later) | **Option A** (Terraform) |
| DEV was provisioned with `azd up` | **Option B** (azd down) |
| Terraform state is corrupt / tools not available | **Option C** (Azure CLI) |
| Resetting a drifted DEV to a clean state | **Option A** then re-provision |
| Emergency: resource group stuck with failed resources | **Option C** + contact Azure Support |
| Permanently decommissioning the project | All three options + delete foundational infra manually |

---

*This runbook was synthesised from:*

- *[infrastructure.md](infrastructure.md) — Terraform modules, lifecycle workflows, security considerations*
- *[quickstart.md](quickstart.md) — Azure Developer CLI deployment, Azure CLI manual deployment*
- *[implementation-lessons.md](../003-api-completion/implementation-lessons.md) — EF Core seeding patterns, gitignore pitfalls*
- *[tasks.md](tasks.md) — T068–T070 IaC task backlog*
