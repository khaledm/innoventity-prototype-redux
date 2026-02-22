# Infrastructure Code Review Prompt
# .specify/memory/infra-review-prompt.md
#
# Purpose: Constitution-aware review checklist for all infrastructure PRs.
# Load this file before performing any `/speckit.analyze` pass on a PR that
# touches infrastructure/ files. This prompt defines the normative review
# boundary and records accepted deferrals so they are NOT re-raised as findings.
#
# Usage:
#   "/speckit.analyze — use infra-review-prompt.md for context"
#   Apply as pre-merge gate for T070 and T076 (see tasks.md).

---

## Review Scope

Infrastructure PR review covers:

- `infrastructure/modules/**` (Terraform modules)
- `infrastructure/environments/**` (root modules + tfvars examples)
- `infrastructure/scripts/**` (PowerShell lifecycle scripts)
- `infrastructure/tests/**` (Pester, Terratest)
- `infrastructure/README.md`, `infrastructure.md`, `data-model.md`

---

## Constitution Alignment Checklist

Run each principle against the PR diff:

| Principle | Check |
|-----------|-------|
| P2 Quality Non-Negotiable | All new resources have at minimum one non-tautological assertion (Pester or Terratest) |
| P3 One Thing Well | No module exceeds one clear responsibility; file-split is optional (accepted, see Deferral 3) |
| P4 Explicit > Implicit | All variables with security impact have `validation` blocks; no default = `0.0.0.0/0` |
| P5 Tests Must Prove They Work | No `$app.` without `$script:` in Pester v5 `BeforeAll`; no `random.UniqueId()×3` for JWT keys |
| Constraint 4 Production-Ready | `prevent_destroy = true` on SQL Server + Database; staging slot mirrors production `site_config` |

---

## Security Baseline (Non-Negotiable)

All resources must satisfy:

- [ ] `minimum_tls_version = "1.2"` on SQL Server and App Service
- [ ] `ftps_state = "Disabled"` on all App Service slots
- [ ] `https_only = true` on App Service
- [ ] `developer_cidr != null` guard on any firewall rule (no `0.0.0.0–255.255.255.255`)
- [ ] `jwt_secret_key` validation: `length >= 44` AND base64 regex
- [ ] `azurerm_monitor_diagnostic_setting` present for every new resource type (FR7.6)

---

## Accepted Deferrals Registry

These items have been formally accepted as out-of-scope. Do NOT re-raise them as findings.

| ID | Item | Accepted In | Resolution Version |
|----|------|-------------|-------------------|
| DEF-1 | Azure Key Vault for secret storage (CHK093) | checklist web-app.md | v2.0+ |
| DEF-2 | Azure AD / Managed Identity for SQL auth (Constraint 1) | infrastructure.md | v2.0+ |
| DEF-3 | Terraform module file split (one resource per file, Principle 3) | constitution.md commentary | Deferred — single-module files acceptable for current scope |
| DEF-4 | Staging slot credential inheritance (TF12 false positive) | phase-0.6-review.md | Not a real finding — `app_settings = azurerm_linux_web_app.main.app_settings` is intentional |
| DEF-5 | SendGrid API key configuration | spec.md FR2.x | Phase 1+ (email notifications not yet implemented) |
| DEF-6 | `go.sum` not committed | Wave 1C | Requires Go 1.21+ installed locally — run `go mod tidy` before T070 Step 5 |

---

## Mandatory Pre-Merge Checks

Before approving any infrastructure PR:

1. **Provider version gate**: `required_providers` block in all modules; `~> 3.116` in root modules
2. **Lock file**: `terraform init -upgrade -backend=false` confirms no provider version conflict
3. **Pester v5 scope**: No bare `$varName` in `BeforeAll` accessed from nested `Describe` — must use `$script:`
4. **Key strength**: Any test generating `jwt_secret_key` uses `crypto/rand` + `base64.StdEncoding.EncodeToString` (≥32 bytes → 44 chars)
5. **Diagnostic settings**: Any new Azure resource type gets an `azurerm_monitor_diagnostic_setting`
6. **Idempotency**: Second `terraform plan -detailed-exitcode` after `apply` must return exit 0

---

## Known Outstanding Items

All spec-analysis findings (C1–C6, H1–H6) are resolved. T070 may now be closed.

Remaining pipeline validation (T077/T078 — not a blocker for T070):
- **T077**: Pipeline gates not yet verified — requires `infra.yml` green run on dev
- **T078**: Screenshot evidence not yet in `infrastructure/README.md` — complete after first successful pipeline run

---

## Commit References

| Wave | Commit | Fixes |
|------|--------|-------|
| 1A | 88242fb | C4 (developer-cidr), H1 (prevent_destroy), H2 (staging slot security), H4 (provider pin 3.116), H6 (required_providers) |
| 1B | 6cb2109 | C3 (Pester $script: scope), M1 (throw on health failure) |
| 1C | 2254a55 | C1 (crypto/rand JWT key), H3 (go.mod created), D1=OptionA (test rename + HTTP assertion removed) |
| 2A | 7ba7686 | C5 (diagnostic settings App Service + SQL, FR7.6) |
| 2B | 6319022 | C6 (destroy-environment.ps1, FR7.8), H5 (timing gate, idempotency step) |
| H3 close | 7281cf3 | H3 fully resolved: go.sum generated via `go mod tidy` (Go 1.26); go.mod expanded with indirect deps |
| T076 | pending | Three GitHub Actions workflows authored: infra.yml (CI wiring `sql_server_id` ✅), deploy.yml, drift.yml |
