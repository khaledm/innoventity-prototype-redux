# Implementation Verification Checklist: IaC & Deployment (T065 → T068 → T069 → T070)

**Feature**: Platform Core (v1.0)
**Domain**: Azure Infrastructure — Terraform modules, lifecycle scripts, validation
**Purpose**: Verify that T065, T068, T069, and T070 are implemented correctly and completely
**Created**: February 21, 2026
**Audience**: Author (self-review, executed during and after implementation)
**Spec Authority**: spec.md §7 FR7.1–FR7.8 | infrastructure.md | plan.md §CHK088–093

---

## Checklist Purpose

This checklist acts as the **"definition of done"** for the IaC task sequence. Items are ordered to match the implementation sequence. Mark each `[ ]` → `[x]` as the item is verified. Add a comment with evidence where non-trivial.

**What This Checklist Tests**:
- ✅ Terraform module structure matches architecture decisions in infrastructure.md
- ✅ Secret injection is correct and no secrets are committed
- ✅ Lifecycle scripts enforce the 10-minute SLA
- ✅ Idempotency and characterization failure modes are validated
- ✅ CHK129–CHK143 spec acceptance criteria are met

**What This Checklist Does NOT Test**:
- ❌ Application-layer business logic (covered by existing unit/integration tests)
- ❌ CI/CD pipeline definition (covered by T076–T078)
- ❌ Angular frontend (covered by T071–T075)

**Constitution references**:
- Principle 5 (Tests Must Prove They Work): All IaC validation must include characterization failure modes — if no failure scenario is tested, the validation has no validity.
- Principle 6 (AI Augments, Humans Decide): Every `azurerm_*` resource argument must be verified against [registry.terraform.io/providers/hashicorp/azurerm/latest/docs](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs) — do not trust hallucinated field names.
- Principle 3 (Simplicity Over Cleverness): Use current (non-deprecated) resource types only.

---

## Resolution Log

*Record evidence here as items are marked complete. Date and note what was verified.*

<!--
Example:
- IaC-F02 ✅ 2026-02-22: Second `terraform apply` on environments/dev/core/ shows "No changes. Your infrastructure matches the configuration." Exit code 0.
-->

---

## Category 1: T065 — Module Structure & Secret Injection

### Terraform Module Files

- [ ] **IaC-A01** — `infrastructure/modules/resource-group/main.tf` exists and provisions `azurerm_resource_group` with `name = "innoventity-${var.environment}-rg"`
  *[Spec §7 FR7.1 §1, infrastructure.md §Resource Group Module]*

- [ ] **IaC-A02** — `infrastructure/modules/sql-database/main.tf` uses `azurerm_mssql_server` + `azurerm_mssql_database` (NOT deprecated `azurerm_sql_server` / `azurerm_sql_database`)
  *[Principle 3 — use non-deprecated resources]*

- [ ] **IaC-A03** — `azurerm_mssql_database.main` has `lifecycle { prevent_destroy = true }` block present and contains comment referencing the split-state decision (2026-02-20)
  *[infrastructure.md §SQL Database Module]*

- [ ] **IaC-A04** — `azurerm_mssql_server.main` sets `minimum_tls_version = "1.2"` and `version = "12.0"`
  *[infrastructure.md §SQL Database Module, spec.md §7 FR7.5]*

- [ ] **IaC-A05** — `infrastructure/modules/app-service/main.tf` uses `azurerm_linux_web_app` (NOT deprecated `azurerm_app_service`)
  *[Principle 3 — use non-deprecated resources]*

- [ ] **IaC-A06** — App Service module sets: `https_only = true`, `minimum_tls_version = "1.2"`, `http2_enabled = true`, `ftps_state = "Disabled"`, `dotnet_version = "8.0"`
  *[infrastructure.md §App Service Module, spec.md §7 FR7.5, plan.md §CHK090]*

- [ ] **IaC-A07** — App Service CORS `allowed_origins` uses `["http://localhost:4200"]` for dev and `["https://innoventity-${var.environment}-web.azurestaticapps.net"]` for non-dev; no wildcard `*` origins
  *[plan.md §CHK092]*

- [ ] **IaC-A08** — `infrastructure/modules/monitoring/main.tf` provisions `azurerm_log_analytics_workspace` + `azurerm_application_insights` linked via `workspace_id`; NOT using classic (non-workspace) Application Insights
  *[infrastructure.md §Application Insights Module]*

- [ ] **IaC-A09** — All Terraform `output` blocks that expose secrets (connection string, keys) have `sensitive = true`
  *[infrastructure.md §SQL Database Module output, spec.md §7 FR7.5 §2]*

- [ ] **IaC-A10** — No hardcoded secret values appear in any `*.tf` or `*.tfvars.example` file; all secrets are declared as `variable` with `sensitive = true` and no `default` value
  *[spec.md §7 FR7.5 §1]*

### Secret Injection (T065 core deliverable)

- [ ] **IaC-A11** — App Service `app_settings` block injects: `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenExpiration`, `Jwt__RefreshTokenExpiration`, `APPLICATIONINSIGHTS_CONNECTION_STRING`
  *[infrastructure.md §App Service Module, plan.md §CHK089, plan.md §CHK093]*

- [ ] **IaC-A12** — App Service `connection_string` block injects `InnoventityDb` of type `SQLAzure`; value sourced from variable, NOT hardcoded
  *[infrastructure.md §App Service Module, plan.md §CHK093]*

- [ ] **IaC-A13** — No Azure Key Vault resources (`azurerm_key_vault*`) are present anywhere in `infrastructure/`
  *[plan.md §CHK093 decision — Key Vault deferred, App Service Configuration is the v1.0 pattern]*

- [ ] **IaC-A14** — No SendGrid or email-related configuration appears in any Terraform module (deferred to Phase 1+)
  *[tasks.md T065 note, spec.md §8 Out of Scope]*

- [ ] **IaC-A15** — No `azurerm_servicebus_*` resources and no `Messaging/` folder under `infrastructure/`
  *[spec.md §Clarifications 2026-02-21 Q3 — Service Bus deferred to Phase 1+]*

### Resource Tags

- [ ] **IaC-A16** — Every `azurerm_*` resource block that supports `tags` includes at minimum: `Environment = var.environment` and `ManagedBy = "Terraform"`
  *[infrastructure.md — all module examples]*

### Pester-Verified Configuration Constraints (added 2026-02-21)

- [ ] **IaC-A17** — Pester `AppService.Tests.ps1` `"alwaysOn is false on B1, true on non-B1 SKUs"` test passes: `az appservice plan show` returns the live SKU; for B1 `alwaysOn = false` is asserted; for all other SKUs `alwaysOn = true` is asserted
  *[infrastructure.md §App Service Module §Configuration Constraints — alwaysOn conditional]*

- [ ] **IaC-A18** — Pester `AppService.Tests.ps1` `"ASPNETCORE_ENVIRONMENT matches the target environment"` test passes: `ASPNETCORE_ENVIRONMENT = "Production"` in `prod`, `"Development"` in `dev`/`test`; verified via `$env:ENVIRONMENT` set by `validate-environment.ps1`
  *[infrastructure.md §App Service Module §Configuration Constraints — ASPNETCORE_ENVIRONMENT]*

- [ ] **IaC-A19** — Pester `SqlDatabase.Tests.ps1` `"has no wide-open firewall rule"` test passes in `test`/`prod` environments: no firewall rule with `endIpAddress = 255.255.255.255` exists; test is skipped (not failed) in `dev`
  *[infrastructure.md §SQL Database Module §Firewall Constraints — no wide-open rule in non-dev]*

---

## Category 2: T068 — Split-State Environment Composition

### Directory Structure

- [ ] **IaC-B01** — `infrastructure/environments/dev/core/` exists with `main.tf`, `terraform.tfvars`, `backend.tf`
  *[infrastructure.md §Directory Layout, spec.md §7 FR7.1 §1]*

- [ ] **IaC-B02** — `infrastructure/environments/dev/data/` exists with `main.tf`, `terraform.tfvars`, `backend.tf`
  *[infrastructure.md §Directory Layout]*

- [ ] **IaC-B03** — `core/backend.tf` references container `tfstate-dev-core`; `data/backend.tf` references container `tfstate-dev-data` — they are DIFFERENT containers
  *[infrastructure.md §Terraform State Management — state isolation]*

- [ ] **IaC-B04** — `core/main.tf` provisions ONLY App Service + App Insights; it does NOT reference `azurerm_mssql_*` resources
  *[infrastructure.md §Split-State Principle]*

- [ ] **IaC-B05** — `data/main.tf` provisions ONLY SQL Server + Database; it does NOT reference `azurerm_linux_web_app` resources
  *[infrastructure.md §Split-State Principle]*

- [ ] **IaC-B06** — Neither `core/` nor `data/` contains a `-target` flag in any documented command, script comment, or README snippet
  *[infrastructure.md — "Why not terraform destroy -target?" section]*

- [ ] **IaC-B07** — `infrastructure/environments/dev/terraform.tfvars` (or equivalent) sets: `app_service_sku = "B1"`, `sql_database_sku = "Basic"`, `retention_days = 7`
  *[infrastructure.md §Environment-Agnostic Principle tfvars example]*

### Terraform Version Constraints

- [ ] **IaC-B08** — Root modules declare `required_version = ">= 1.6"` and `required_providers { azurerm = { version = "~> 3.0" } }`
  *[infrastructure.md §Tool — Terraform 1.6+ with Azure Provider 3.x]*

### .gitignore

- [ ] **IaC-B09** — `.gitignore` (or `infrastructure/.gitignore`) contains all three entries: `**/*.tfvars`, `**/.terraform/`, `**/terraform.tfstate*`
  *[infrastructure.md §Secret Injection — Required .gitignore entries, spec.md §7 FR7.5 §1]*

---

## Category 3: T069 — Lifecycle Scripts & 10-Minute SLA

### Script Existence & Safety

- [ ] **IaC-C01** — `infrastructure/scripts/create-environment.sh` (or `.ps1`) exists and orchestrates: (1) `terraform init` + `terraform apply` in `core/`, then (2) `terraform init` + `terraform apply` in `data/`
  *[infrastructure.md §Creation Workflow]*

- [ ] **IaC-C02** — `infrastructure/scripts/destroy-core.sh` (or `.ps1`) runs `terraform plan -destroy` + `terraform apply` ONLY against `core/`; it does NOT touch `data/`
  *[infrastructure.md §Destruction Workflow — "Destroy stateless resources (core) — routine teardown"]*

- [ ] **IaC-C03** — No script contains `terraform destroy -target` or `terraform apply -target` in any form (commented out or active)
  *[infrastructure.md prohibited patterns, Principle 3]*

- [ ] **IaC-C04** — Scripts require secrets to be passed as environment variables or interactive prompts; they do NOT read from committed `.tfvars` files with real values
  *[spec.md §7 FR7.5 §1]*

### 10-Minute SLA Measurement

- [ ] **IaC-C05** — `create-environment.sh` records `START_TIME` at script entry and logs elapsed seconds at the point where `GET /health` first returns HTTP 200 (full-stack completion — not just `terraform apply` exit)
  *[infrastructure.md §"What is included in the 10-minute SLA?" decision, spec.md §7 FR7.1 §1.3, spec.md §7 FR7.8 §1.3]*

- [ ] **IaC-C06** — The SLA timer includes: `terraform apply` completion + EF Core migrations applied + `/health` → 200. A schema-less but provisioned environment does NOT count as complete
  *[infrastructure.md §Open Questions — "What is included in the 10-minute SLA?" decision 2026-02-20]*

- [ ] **IaC-C07** — Script prints a clear PASS / WARN line: `PASS: Environment ready in <N>s (SLA: 600s)` or `WARN: SLA exceeded — <N>s`; no hard failure on SLA breach (Phase 0 soft target)
  *[infrastructure.md §"Is the 10-minute SLA hard?" decision — soft target Phase 0, hard from Phase 1]*

### Validation Script (infrastructure.md §Validation Automation)

- [ ] **IaC-C08** — `infrastructure/scripts/validate-environment.sh` (or equivalent) exists with three verification steps: (1) health endpoint HTTP 200, (2) database migrations applied (`__EFMigrationsHistory` has ≥ 1 row), (3) Application Insights telemetry received
  *[infrastructure.md §Validation Automation, spec.md §7 FR7.8 §3]*

- [ ] **IaC-C09** — Health check assertion queries `https://<app_url>/health` and asserts response body contains `"Healthy"` — NOT purely checking HTTP 200
  *[spec.md §7 FR7.2 §1, infrastructure.md §Validation Checklist]*

- [ ] **IaC-C10** — Database connectivity assertion queries Azure SQL directly (via `sqlcmd` or connection string `SELECT COUNT(*) FROM __EFMigrationsHistory`) — NOT comparing Terraform output to Terraform input (non-tautological)
  *[infrastructure.md §Epistemic Discipline — "GOOD TEST" example, spec.md §7 FR7.3 §2, Principle 5]*

---

## Category 4: T070 — Idempotency & Characterization Tests

### Idempotency Validation

- [ ] **IaC-D01** — Running `terraform apply` a second time against `core/` produces exit code `0` and output contains `"No changes. Your infrastructure matches the configuration."`
  *[spec.md §7 FR7.2 §3, spec.md §7 FR7.3 §3, infrastructure.md §Idempotency Testing]*

- [ ] **IaC-D02** — Running `terraform apply` a second time against `data/` produces exit code `0` and output contains `"No changes. Your infrastructure matches the configuration."`
  *[spec.md §7 FR7.2 §3]*

- [ ] **IaC-D03** — Idempotency result is recorded in `infrastructure/README.md` with the actual output line as evidence (not just "it passed")
  *[Principle 5 — tests must be observed; Principle 6 — validate AI output]*

### Characterization Tests (Constitution Principle 5 — failure modes must be proved)

- [ ] **IaC-D04** — **Failure mode proved**: DB connection string deliberately omitted from App Service `app_settings` → `GET /health` returns non-200 (503 or 500). Actual response status is documented in `infrastructure/README.md`
  *[infrastructure.md §Validation Tests — "❌ Health check returns 500 if database connection string missing", spec.md §7 FR7.3 §3, Principle 5]*

- [ ] **IaC-D05** — **Failure mode result**: the above test was REVERTED after verification (connection string restored; health check returns 200 again)

- [ ] **IaC-D06** — `infrastructure/README.md` contains a "Validation Evidence" section documenting both: (a) second-apply "No changes" output, and (b) broken-config → non-200 health result
  *[Principle 5 — epistemic discipline; Principle 6 — AI output validation]*

### Non-Tautological External Validation

- [ ] **IaC-D07** — At least one assertion in the validation script queries Azure directly via `az webapp show ... --query httpsOnly` and verifies `true` — NOT comparing Terraform output variable to Terraform input variable
  *[infrastructure.md §Epistemic Discipline — "GOOD TEST" example, spec.md §7 FR7.3 §2]*

- [ ] **IaC-D08** — At least one assertion verifies database firewall rule allows Azure services (`start_ip_address = "0.0.0.0"`, `end_ip_address = "0.0.0.0"`) by querying Azure API directly, NOT from Terraform state
  *[infrastructure.md §SQL Database Module — firewall rule, Principle 6]*

---

## Category 5: Cross-Cutting (All Tasks)

### Prohibited Patterns (Verified Absent)

- [ ] **IaC-E01** — No `terraform destroy -target` or `terraform apply -target` exists anywhere in `infrastructure/` (scripts, READMEs, comments, or inline documentation)
  *[infrastructure.md §Prohibited Patterns, §"Why not terraform destroy -target?" section]*

- [ ] **IaC-E02** — `lifecycle { prevent_destroy = true }` exists ONLY on `azurerm_mssql_database.main`; it is NOT present on App Service, App Insights, or SQL Server resources
  *[infrastructure.md §SQL Database module comment "Decision 2026-02-20"]*

- [ ] **IaC-E03** — No `azurerm_servicebus_*`, `azurerm_servicebus_namespace`, `azurerm_servicebus_queue`, or `azurerm_servicebus_topic` resources exist anywhere in `infrastructure/`
  *[spec.md §Clarifications 2026-02-21 GAP-1]*

- [ ] **IaC-E04** — `infrastructure/` directory has NO `Messaging/` folder at any depth
  *[plan.md GAP-1 correction 2026-02-21]*

- [ ] **IaC-E05** — `git ls-files --error-unmatch infrastructure/**/*.tfvars` returns non-zero (i.e., no `.tfvars` with real values is tracked by git)
  *[spec.md §7 FR7.5 §1, infrastructure.md §Required .gitignore entries]*

### Resource Name Verification (Constitution Principle 6)

- [ ] **IaC-E06** — Each of the following resource types has been verified as current (non-deprecated) in the official Azure provider docs before use:
  - `azurerm_linux_web_app` ✅ / ❌
  - `azurerm_service_plan` ✅ / ❌
  - `azurerm_mssql_server` ✅ / ❌
  - `azurerm_mssql_database` ✅ / ❌
  - `azurerm_mssql_firewall_rule` ✅ / ❌
  - `azurerm_application_insights` ✅ / ❌
  - `azurerm_log_analytics_workspace` ✅ / ❌
  *[Principle 6 — all AI output validated against official docs: registry.terraform.io/providers/hashicorp/azurerm/latest/docs]*

- [ ] **IaC-E07** — `azurerm_linux_web_app` `connection_string` block uses argument `type` (not `connection_string_type` or similar hallucinated name); verified against provider docs
  *[Principle 6 — field name verification]*

---

## Category 6: Spec FR7.1–FR7.8 Acceptance Criteria Mapping

*These items correspond directly to the Gherkin scenarios in spec.md §7. Mark each `[x]` when the relevant implementation proves the scenario.*

### FR7.1 — Ephemeral Environment Provisioning

- [ ] **IaC-F01** — "Environment created within 10 minutes" scenario: create-environment script execution time logged and ≤ 600 seconds end-to-end (full-stack: apply → migrations → `/health` 200)
  *[spec.md §7 FR7.1 Acceptance Criteria Scenario 1]*

- [ ] **IaC-F02** — "Environment can be destroyed and recreated" scenario: `destroy-core.sh` completes cleanly, `create-environment.sh` re-provisions the same functional state; seed data GUIDs are identical after recreation
  *[spec.md §7 FR7.1 Acceptance Criteria Scenario 2]*

- [ ] **IaC-F03** — "Production and development use same architecture" scenario: `environments/dev/core/main.tf` and (future) `environments/prod/core/main.tf` reference the SAME modules; only `.tfvars` differ
  *[spec.md §7 FR7.1 Acceptance Criteria Scenario 3]*

### FR7.2 — Component Validation

- [ ] **IaC-F04** — "Service provides health endpoint" scenario: `GET /health` from provisioned App Service returns HTTP 200 with JSON body containing `"status":"Healthy"` and per-dependency entries
  *[spec.md §7 FR7.2 Acceptance Criteria Scenario 1]*

- [ ] **IaC-F05** — "Infrastructure provisioning is idempotent" scenario: second `terraform apply` reports "No changes needed" (exit code 0) — evidence recorded in `infrastructure/README.md`
  *[spec.md §7 FR7.2 Acceptance Criteria Scenario 2, IaC-D01/D02/D03]*

- [ ] **IaC-F06** — "Database migrations are idempotent" scenario: `dotnet ef database update` re-executed on existing schema completes without errors and schema is unchanged
  *[spec.md §7 FR7.2 Acceptance Criteria Scenario 3]*

### FR7.3 — Testing & Validation Standards

- [ ] **IaC-F07** — "Infrastructure validation detects missing configuration" scenario (characterization test): removing DB connection string causes health check to fail; result documented with actual HTTP status returned
  *[spec.md §7 FR7.3 Acceptance Criteria Scenario 3, IaC-D04/D05/D06]*

### FR7.5 — Secret Management

- [ ] **IaC-F08** — "Secrets are not in source control" scenario: `git log --all -- "*.tfvars"` returns no commits containing real secret values; `*.tfvars` files are gitignored
  *[spec.md §7 FR7.5 Acceptance Criteria Scenario 1, IaC-B09]*

- [ ] **IaC-F09** — "Secret rotation is configuration-driven" scenario: updating `Jwt__SecretKey` in App Service Configuration and restarting app service changes signing key without any code change
  *[spec.md §7 FR7.5 Acceptance Criteria Scenario 3, plan.md §CHK093]*

### FR7.7 — Data Persistence

- [ ] **IaC-F10** — "Database schema applied via migrations" scenario: fresh database post-`terraform apply` has `__EFMigrationsHistory` table with ≥ 1 row after `dotnet ef database update`
  *[spec.md §7 FR7.7 Acceptance Criteria Scenario 1]*

- [ ] **IaC-F11** — "Seed data uses fixed identifiers" scenario: after recreating dev environment and re-running seed script, `Actor` records have the same GUIDs as the original seed (idempotency verified by query)
  *[spec.md §7 FR7.7 Acceptance Criteria Scenario 3, infrastructure.md §Test Data Seeding]*

### FR7.8 — Environment Lifecycle Documentation

- [ ] **IaC-F12** — `infrastructure/README.md` has a "Prerequisites" section listing: Terraform 1.6+, Azure CLI (`az login`), .NET SDK, required environment variables/secrets
  *[spec.md §7 FR7.8 §1.2]*

- [ ] **IaC-F13** — `infrastructure/README.md` documents the creation workflow step-by-step (matching infrastructure.md §Creation Workflow); destruction workflow documented (core-only vs full teardown distinguished)
  *[spec.md §7 FR7.8 §1/§2]*

- [ ] **IaC-F14** — Monthly cost per environment type is documented: Dev ~$20/month, Test ~$20/month, Prod ~$180/month (or updated actuals)
  *[spec.md §7 FR7.8 §4, infrastructure.md §Cost Analysis]*

---

## Category 7: T076–T078 — CI/CD Pipeline Implementation

### `infra.yml` Workflow

- [ ] **IaC-G01** — `.github/workflows/infra.yml` exists and triggers on `push` to `infrastructure/**` and on `workflow_dispatch`; it does NOT trigger on `src/**` push
  *[infrastructure.md §Testing Requirements §Three-Pipeline CI/CD Architecture — infra.yml trigger]*

- [ ] **IaC-G02** — `infra.yml` contains exactly these jobs in order: `terraform-plan-core`, `terraform-apply-core`, `terraform-plan-data`, `approve-data`, `terraform-apply-data`, `pester-infra`
  *[infrastructure.md §Testing Requirements §Three-Pipeline CI/CD Architecture — authoritative job names]*

- [ ] **IaC-G03** — `approve-data` job uses a GitHub Environment protection rule requiring at least one manual reviewer; it activates on `prod` environment only (dev environment has no manual gate)
  *[infrastructure.md §Testing Requirements §Three-Pipeline CI/CD Architecture — approve-data: Manual gate (prod only)]*

- [ ] **IaC-G04** — `pester-infra` job runs `Invoke-Pester -CI ./infrastructure/tests/pester/` and fails the workflow if Pester exits non-zero
  *[infrastructure.md §Testing Requirements §Environment State Validation: Pester]*

### `deploy.yml` Workflow

- [ ] **IaC-G05** — `.github/workflows/deploy.yml` exists and triggers on `push` to `src/**` only; it does NOT contain any `terraform apply` or `terraform destroy` step
  *[infrastructure.md §Testing Requirements §Three-Pipeline CI/CD Architecture — deploy.yml; Principle 3 — "terraform apply never runs on code push"]*

- [ ] **IaC-G06** — `deploy.yml` contains exactly these jobs in order: `preflight`, `build-test`, `migrate`, `deploy`, `pester-health`, `slot-swap`
  *[infrastructure.md §Testing Requirements §Three-Pipeline CI/CD Architecture — authoritative job names]*

- [ ] **IaC-G07** — `build-test` job runs `dotnet test` and fails the workflow on non-zero exit (unit + integration)
  *[tasks.md T077 gate 1 — fail on test failures]*

- [ ] **IaC-G08** — `pester-health` job runs `Invoke-Pester -CI ./infrastructure/tests/pester/HealthCheck.Tests.ps1` and fails the workflow if `GET /health` does not return 200 with `"status":"Healthy"`
  *[tasks.md T077 gate 2 — fail on health check non-200; infrastructure.md §Testing Requirements §pester-health]*

- [ ] **IaC-G09** — `pester-health` job receives `APP_SERVICE_NAME` and `RESOURCE_GROUP_NAME` from GitHub Secrets or environment variables — no hardcoded values
  *[infrastructure.md §Testing Requirements §Pester — "Inputs from environment variables"]*

- [ ] **IaC-G10** — `slot-swap` job runs only on the `prod` environment (conditional: `if: github.ref == 'refs/heads/main'` or equivalent)
  *[infrastructure.md §Testing Requirements §Three-Pipeline CI/CD Architecture — slot-swap: prod only]*

### `drift.yml` Workflow

- [ ] **IaC-G11** — `.github/workflows/drift.yml` exists and triggers ONLY on `cron: "0 2 * * *"` schedule; there is NO `push` or `pull_request` trigger
  *[infrastructure.md §Testing Requirements §Three-Pipeline CI/CD Architecture — drift.yml trigger; tasks.md T076 §drift.yml]*

- [ ] **IaC-G12** — `drift.yml` contains exactly two jobs: `drift-check-core` and `drift-check-data`
  *[infrastructure.md §Testing Requirements §Three-Pipeline CI/CD Architecture — authoritative job names]*

- [ ] **IaC-G13** — Both drift jobs use `terraform plan -detailed-exitcode`; neither job calls `terraform apply` or `terraform destroy` in any step
  *[infrastructure.md §Drift Detection; tasks.md T077 — drift.yml never applies; Hashicorp docs on -detailed-exitcode]*

- [ ] **IaC-G14** — Both drift jobs emit `::error::` annotation and exit non-zero if `terraform plan -detailed-exitcode` returns exit code `2` (drift detected)
  *[tasks.md T077 gate 3 — fail on infrastructure drift detection; infrastructure.md §Drift Detection YAML snippet]*

### Validation (T078)

- [ ] **IaC-G15** — Green `infra.yml` run evidence: all 6 jobs passed on dev environment; `pester-infra` Pester output shows 0 failures; documented in `infrastructure/README.md` with run URL or screenshot
  *[tasks.md T078 — validate green pipeline run; Principle 5 — tests must prove they work]*

- [ ] **IaC-G16** — Green `deploy.yml` run evidence: `pester-health` job shows `GET /health` 200, `"status":"Healthy"`; documented in `infrastructure/README.md`
  *[tasks.md T078 — validate green pipeline run]*

---

## web-app.md CHK129–CHK143 Resolution Tracker

*These items live in `checklists/web-app.md`. Mark them here as IaC work completes them, then update web-app.md accordingly.*

| Item | Requirement | Resolved by | Status |
|------|-------------|-------------|--------|
| CHK129 | All 8 FR7.1–FR7.8 testable with clear acceptance criteria | IaC-F01 through IaC-F14 | `[ ]` |
| CHK130 | FR7.1 validates 10-minute creation SLA | IaC-C05, IaC-C06, IaC-F01 | `[ ]` |
| CHK131 | FR7.2 validates health endpoints and idempotency | IaC-C09, IaC-D01, IaC-F04, IaC-F05 | `[ ]` |
| CHK132 | FR7.3 validates failure before success (epistemic) | IaC-D04, IaC-D05, IaC-F07 | `[ ]` |
| CHK133 | FR7.4 validates SOLID/KISS/YAGNI enforcement | Code review (not IaC-specific) | `[ ]` |
| CHK134 | FR7.5 validates secret rotation without deployment | IaC-F09 | `[ ]` |
| CHK135 | FR7.6 validates correlation IDs in structured logs | T062 (already complete ✅) | `[x]` |
| CHK136 | FR7.7 validates migration idempotency and seed reproducibility | IaC-F06, IaC-F10, IaC-F11 | `[ ]` |
| CHK137 | FR7.8 validates creation/destruction workflows | IaC-F12, IaC-F13 | `[ ]` |
| CHK138 | spec.md FR7.5 aligns with plan.md CHK088–093 | IaC-A11, IaC-A12, IaC-A13 | `[ ]` |
| CHK139 | spec.md FR7.6 consistent with plan.md CHK097–100 | T062 (already complete ✅) | `[x]` |
| CHK140 | spec.md FR7.8 lifecycle covers plan.md CHK101–103 | IaC-F12, IaC-F13 | `[ ]` |
| CHK141 | infrastructure.md modules traceable to FR7.1 | IaC-B01 through IaC-B07 | `[ ]` |
| CHK142 | infrastructure.md workflows traceable to FR7.8 (10-min/5-min) | IaC-C05, IaC-C07, IaC-F01 | `[ ]` |
| CHK143 | FR7.7 data persistence consistent with data-model.md | IaC-F10, IaC-F11 | `[ ]` |

---

## Summary Statistics

- **Total Items**: 83 (IaC-A01–A19 = 19, IaC-B01–B09 = 9, IaC-C01–C10 = 10, IaC-D01–D08 = 8, IaC-E01–E07 = 7, IaC-F01–F14 = 14, IaC-G01–G16 = 16) + 15 CHK tracker rows
- **Resolved at creation**: 2 (CHK135, CHK139 — already satisfied by T062)
- **Remaining**: 78 items pending implementation of T065, T068, T069, T070, T076, T077, T078
- **Blocking items** (MUST resolve before marking task complete):
  - T065: IaC-A01–A16, IaC-E01–E07
  - T068: IaC-B01–B09
  - T069: IaC-C01–C10
  - T070: IaC-D01–D08, IaC-F01–F14
  - T076: IaC-G01–G14
  - T077: IaC-G07, IaC-G08, IaC-G13, IaC-G14
  - T078: IaC-G15, IaC-G16
