# Data Model: Frontend CI/CD Automation

**Feature**: Frontend CI/CD Automation
**Branch**: `002-frontend-cicd`
**Date**: April 12, 2026
**Purpose**: Define key entities, states, relationships, and validation rules

---

## Conceptual Entities

### 1. GitHub Actions Workflow

**Definition**: Automated pipeline triggered on code changes, orchestrating build → test → deploy → validate stages for frontend code.

**Key Attributes**:
- `workflow_id` (string): GitHub workflow run ID (e.g., `"1234567890"`)
- `trigger_event` (enum): `push` | `pull_request` | `workflow_dispatch`
- `triggered_by` (string): GitHub username of committer or manual dispatcher
- `branch` (string): Branch name (e.g., `"002-frontend-cicd"`, `"Main"`)
- `commit_sha` (string): Git commit SHA triggering the workflow
- `status` (enum): `queued` | `in_progress` | `success` | `failure` | `cancelled`
- `started_at` (timestamp): Workflow start time
- `completed_at` (timestamp, nullable): Workflow completion time (null if in progress)
- `duration_seconds` (integer, computed): `completed_at - started_at`
- `conclusion` (enum, nullable): `success` | `failure` | `cancelled` | `timed_out` (null until completed)

**Lifecycle States**:
```
[Queued] → [In Progress] → [Success | Failure | Cancelled | Timed Out]
```

**Validation Rules**:
- Workflow MUST complete within 15 minutes (NFR-001) or timeout
- `commit_sha` MUST exist in repository
- `branch` MUST match Git reference
- Production deployments MUST only occur when `branch == "Main"`

**Relationships**:
- Has Many: **Job Executions** (build, test, deploy-preview, deploy-production)
- Triggers: **Deployment** (if deploy job succeeds)

---

### 2. Job Execution

**Definition**: Individual unit of work within a workflow (build, test, deploy-preview, deploy-production, validate).

**Key Attributes**:
- `job_id` (string): Unique job identifier within workflow
- `job_name` (enum): `build` | `test` | `deploy-preview` | `deploy-production` | `validate`
- `workflow_run_id` (string): Parent workflow ID (foreign key)
- `status` (enum): `queued` | `in_progress` | `success` | `failure` | `skipped` | `cancelled`
- `conclusion` (enum, nullable): `success` | `failure` | `cancelled` | `skipped` | `timed_out`
- `started_at` (timestamp)
- `completed_at` (timestamp, nullable)
- `duration_seconds` (integer, computed)
- `runner_os` (string): GitHub runner OS (e.g., `"ubuntu-22.04"`)
- `node_version` (string): Node.js version used (e.g., `"20.x"`)

**Lifecycle States**:
```
[Queued] → [In Progress] → [Success | Failure | Skipped | Cancelled]
```

**Conditional Execution**:
- `deploy-preview` job: Runs only if `branch != "Main"`
- `deploy-production` job: Runs only if `branch == "Main"` AND awaits approval
- `validate` job: Runs after deploy job completes

**Validation Rules**:
- `build` job MUST complete before `test` job (dependency)
- `test` job MUST complete before deploy jobs (dependency)
- `deploy-production` job MUST wait for approval gate before execution
- Jobs MUST run on GitHub-hosted runners (no self-hosted in Phase 1)

**Relationships**:
- Belongs To: **Workflow** (parent)
- Produces: **Build Artifact** (build job only)
- Triggers: **Deployment** (deploy jobs only)

---

### 3. Build Artifact

**Definition**: Compiled Angular application output (`dist/` folder) uploaded to GitHub Actions for reuse in deployment jobs.

**Key Attributes**:
- `artifact_id` (string): GitHub artifact ID
- `artifact_name` (string): Always `"frontend-build"` (convention)
- `workflow_run_id` (string): Workflow that created artifact
- `job_id` (string): Build job that created artifact
- `size_bytes` (integer): Artifact size (Angular prod build typically 500kB-1MB after compression)
- `created_at` (timestamp)
- `expires_at` (timestamp): GitHub auto-deletes after 90 days (configurable)
- `commit_sha` (string): Commit this artifact was built from

**Lifecycle**:
```
[Created] → [Available] → [Expired/Deleted]
```

**Validation Rules**:
- Artifact MUST contain `dist/innoventity.client/` folder structure
- Artifact MUST include `index.html`, `main.*.js`, `polyfills.*.js`, `styles.*.css`
- Artifact size SHOULD be <1MB (Angular prod build optimized)
- Artifact MUST be created by `build` job before `deploy` jobs can download

**Relationships**:
- Created By: **Job Execution** (build job)
- Used By: **Deployment** (deploy jobs download and deploy this artifact)

---

### 4. Environment

**Definition**: Deployment target context with protection rules and approval gates (GitHub Environments feature).

**Key Attributes**:
- `environment_name` (enum): `production` (only environment defined in Phase 1)
- `protection_rules` (object):
  - `required_reviewers` (array of strings): GitHub usernames authorized to approve (e.g., `["khaledm"]`)
  - `wait_timer_minutes` (integer): Delay before approval gate (Phase 1: `0`, no automatic delay)
  - `deployment_branches` (array of strings): Allowed branches (Phase 1: `["Main"]` only)
- `environment_url` (string): Production URL `https://innoventity-dev-web.azurestaticapps.net`

**Validation Rules**:
- `production` environment MUST restrict to `Main` branch only
- `required_reviewers` MUST include at least one authorized user
- `environment_url` MUST be valid HTTPS URL

**Relationships**:
- Protected By: **Approval Gate**
- Target Of: **Deployment** (production deployments only)

---

### 5. Approval Gate

**Definition**: Manual review step requiring human decision before production deployment proceeds.

**Key Attributes**:
- `approval_id` (string): GitHub approval ID
- `environment_name` (string): Always `"production"` (Phase 1)
- `workflow_run_id` (string): Workflow awaiting approval
- `job_id` (string): `deploy-production` job paused at gate
- `requested_at` (timestamp): When approval was requested
- `status` (enum): `pending` | `approved` | `rejected` | `timed_out`
- `decision_made_at` (timestamp, nullable): When reviewer approved/rejected (null if pending/timed_out)
- `decided_by` (string, nullable): GitHub username of reviewer (null if pending/timed_out)
- `timeout_minutes` (integer): 1440 (24 hours per NFR requirement)
- `expires_at` (timestamp, computed): `requested_at + timeout_minutes`

**Lifecycle States**:
```
[Pending] → [Approved | Rejected | Timed Out]
```

**Transition Rules**:
- **Pending → Approved**: Reviewer clicks "Approve deployment" within 24 hours
- **Pending → Rejected**: Reviewer clicks "Reject deployment" within 24 hours
- **Pending → Timed Out**: 24 hours elapse without reviewer action

**Validation Rules**:
- Reviewer MUST be in `environment.required_reviewers` list
- Approval MUST occur within 24 hours or workflow auto-fails (timeout-minutes setting)
- Only ONE approval needed (not multi-stage approval in Phase 1)
- Approval decision is IRREVERSIBLE (cannot un-approve and re-reject)

**Relationships**:
- Belongs To: **Environment** (production)
- Blocks: **Deployment** (until approved)
- Decided By: **User** (GitHub reviewer)

---

### 6. Deployment

**Definition**: Physical deployment of build artifacts to Azure Static Web Apps (preview or production environment).

**Key Attributes**:
- `deployment_id` (string): Azure SWA deployment ID (from action output)
- `workflow_run_id` (string): Workflow that triggered deployment
- `job_id` (string): Job that executed deployment (`deploy-preview` or `deploy-production`)
- `deployment_type` (enum): `preview` | `production`
- `environment_name` (string, nullable): `"production"` for production deployments, `null` for previews
- `target_url` (string): Deployed URL (preview: `https://<swa>-<pr-number>-<region>.azurestaticapps.net`, production: `https://innoventity-dev-web.azurestaticapps.net`)
- `commit_sha` (string): Git commit deployed
- `branch` (string): Branch deployed from
- `pr_number` (integer, nullable): PR number for preview deployments (null for production)
- `status` (enum): `uploading` | `building` | `ready` | `failed`
- `deployed_at` (timestamp): When deployment started
- `ready_at` (timestamp, nullable): When deployment became accessible (null if failed)
- `deployment_token_used` (string, sensitive): `AZURE_STATIC_WEB_APPS_API_TOKEN` (GitHub secret, not logged)

**Lifecycle States**:
```
[Uploading] → [Building] → [Ready | Failed]
```

**Type-Specific Rules**:
- **Preview Deployment**:
  - Triggered when `branch != "Main"`
  - Creates temporary environment with unique URL
  - Automatically deleted when PR closed or after 7 days inactivity
  - Does NOT require approval gate
  - Limited to 3 concurrent preview environments (Free tier quota)
- **Production Deployment**:
  - Triggered when `branch == "Main"` AND approval granted
  - Deploys to fixed production URL
  - Persists indefinitely (not auto-deleted)
  - MUST pass approval gate before deployment
  - Only one production environment (no limit)

**Validation Rules**:
- Deployment MUST use build artifact from same `workflow_run_id`
- Production deployments MUST only occur after approval gate passed
- `target_url` MUST return HTTP 200 within 5 minutes of deployment (NFR-002)
- Preview deployments MUST have associated `pr_number`

**Relationships**:
- Belongs To: **Workflow** (parent)
- Triggered By: **Job Execution** (deploy job)
- Uses: **Build Artifact**
- Blocked By: **Approval Gate** (production only)
- Validated By: **Health Check**

---

### 7. Health Check

**Definition**: Automated validation that deployed application is accessible and serving correct content.

**Key Attributes**:
- `check_id` (string): Unique identifier
- `deployment_id` (string): Deployment being validated
- `job_id` (string): `validate` job executing check
- `target_url` (string): URL being validated
- `check_type` (enum): `http_status` | `content_type` | `content_validation`
- `executed_at` (timestamp): When check ran
- `result` (enum): `pass` | `fail`
- `http_status_code` (integer, nullable): Actual HTTP status (e.g., `200`, `404`, `500`)
- `content_type` (string, nullable): Actual Content-Type header (e.g., `"text/html"`)
- `response_time_ms` (integer): Response time in milliseconds
- `error_message` (string, nullable): Error details if check failed

**Check Types**:
1. **HTTP Status Check**: Validates URL returns 200 OK (not 404/500)
2. **Content Type Check**: Validates `Content-Type: text/html` (not JSON error response)
3. **Content Validation** (Phase 2+): Validates Angular app loaded (check for `<app-root>` in HTML)

**Pass Criteria**:
- HTTP status MUST be 200
- Content-Type MUST be `text/html` or `text/html; charset=utf-8`
- Response time SHOULD be <30 seconds (NFR timeout, typical is <2 seconds)

**Failure Scenarios**:
- URL returns 404 → Failed deployment (artifact not uploaded correctly)
- URL returns 500 → SWA internal error (runtime configuration issue)
- URL returns 503 → SWA service temporarily unavailable (retry or fail)
- Timeout (>30 seconds) → Network issue or SWA overloaded

**Validation Rules**:
- Health checks MUST run AFTER deployment completes (`needs: [deploy-preview]` or `needs: [deploy-production]`)
- Health checks MUST retry up to 3 times with 10-second delay (allow CDN propagation)
- Health check failure MUST fail workflow (prevent production approval if staging broken)

**Relationships**:
- Validates: **Deployment**
- Executed By: **Job Execution** (validate job)
- Blocks: **Approval Gate** (production deployment MUST pass staging health check first)

---

### 8. Preview Environment (Implicit Entity)

**Definition**: Temporary deployment environment auto-created by Azure SWA for pull requests.

**Note**: This is NOT a first-class GitHub or Azure entity with direct API—preview environments are a feature of Azure Static Web Apps tracked via deployment metadata.

**Key Attributes** (tracked via Deployment entity):
- `pr_number` (integer): Associated pull request
- `preview_url` (string): Unique URL for preview
- `created_at` (timestamp): When preview first deployed
- `last_deployed_at` (timestamp): Most recent deployment to this preview
- `status` (enum): `active` | `deleted`
- `auto_delete_after_days` (integer): 7 (Azure SWA default)

**Lifecycle**:
```
[Created on PR deploy] → [Active] → [Deleted on PR close or 7-day inactivity]
```

**Quota Limits** (Free Tier):
- Maximum 3 concurrent active preview environments
- Exceeding quota causes new preview deployments to fail with quota error
- Manual cleanup required if quota exhausted: delete via Azure Portal or close old PRs

**Validation Rules**:
- Preview URL MUST match pattern `https://<swa-name>-<pr-number>-<region>.azurestaticapps.net`
- Preview MUST be accessible within 5 minutes of deployment (NFR-002)
- Preview MUST auto-delete within 24 hours of PR closure (cleanup workflow)

**Relationships**:
- Associated With: **Pull Request** (GitHub)
- Backed By: **Deployment** (Azure SWA deployment resource)
- Consumed By: **Code Reviewer** (user)

---

## Entity Relationships Diagram

```
┌──────────────┐
│   Workflow   │ (GitHub Actions Run)
│              │
│ - run_id     │
│ - branch     │
│ - commit_sha │
│ - status     │
└──────┬───────┘
       │ Contains (1:N)
       ▼
┌──────────────┐
│ JobExecution │ (build, test, deploy-preview, deploy-production)
│              │
│ - job_name   │
│ - status     │
│ - duration   │
└─┬─────────┬──┘
  │ Creates │ Triggers
  │ (1:1)   │ (1:1)
  ▼         ▼
┌────────────┐  ┌────────────┐
│BuildArtifact│  │ Deployment │ (Azure SWA upload)
│            │  │            │
│ - size     │  │ - type     │ (preview | production)
│ - created  │  │ - url      │
└─────┬──────┘  │ - status   │
      │ Used by │            │
      │ (1:N)   └─────┬──────┘
      └──────────────►│ Validated by (1:N)
                      ▼
                ┌────────────┐
                │HealthCheck │ (HTTP validation)
                │            │
                │ - status   │ (pass/fail)
                │ - http_code│
                └────────────┘

Production Deployment Flow:
┌──────────────┐       ┌─────────────┐       ┌────────────┐
│ JobExecution │──────►│ApprovalGate │──────►│ Deployment │
│ (deploy-prod)│ Awaits│ (production)│ Grants│ (production│
└──────────────┘       │ - pending   │       │  env)      │
                       │ - approved  │       └────────────┘
                       └─────────────┘
                              ▲
                              │ Decided by
                       ┌──────┴──────┐
                       │ Environment │ (GitHub Environment)
                       │ (production)│
                       │ - reviewers │
                       └─────────────┘
```

---

## State Transition Diagrams

### Workflow Lifecycle

```
[Code Change] → [Workflow Queued]
                      ↓
              [Workflow In Progress]
                      ↓
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
   [Success]      [Failure]    [Cancelled]
```

### Production Deployment Flow (Main Branch)

```
[Build Job] → [Test Job] → [Deploy-Preview Job: SKIPPED]
                                  ↓
                          [Deploy-Production Job]
                                  ↓
                          [PAUSED: Approval Gate]
                                  ↓
                    ┌─────────────┼──────────────┐
                    ▼             ▼              ▼
              [Approved]     [Rejected]    [Timed Out]
                    ↓             ↓              ↓
              [Deploy to    [Workflow     [Workflow
              Production]    Failed]       Failed]
                    ↓
              [Validate]
                    ↓
              [Success/Failed]
```

### Preview Deployment Flow (Feature Branch)

```
[Build Job] → [Test Job] → [Deploy-Preview Job]
                                  ↓
                          [Deploy to Preview URL]
                                  ↓
                          [Post URL in PR Comment]
                                  ↓
                          [ Validate Preview]
                                  ↓
                          [Success/Failed]
                                  ↓
                          [PR Closed]
                                  ↓
                          [Cleanup Preview Environment]
```

---

## Validation Rules Summary

### Global Constraints

1. **Workflow timeout**: All workflows MUST complete within 15 minutes (NFR-001) or auto-fail
2. **Production restriction**: Production deployments MUST only execute on `Main` branch (FR-014)
3. **Approval requirement**: Production deployments MUST require manual approval (FR-015)
4. **Approval timeout**: Approvals MUST be granted within 24 hours or workflow auto-fails (FR-041)
5. **Preview quota**: Maximum 3 concurrent preview environments (Free tier limit, FR-038)
6. **Artifact expiration**: Build artifacts retained for 90 days (NFR-004)

### Job Dependencies

```
build ─► test ─┬─► deploy-preview (if branch != Main)
               └─► deploy-production (if branch == Main AND approval granted)
                           ↓
                      validate ─► [Success/Failure]
```

### Deployment Prerequisites

**Preview Deployment** requires:
- ✅ Build job completed successfully
- ✅ Test job completed (Phase 1: warnings allowed, Phase 2+: must pass)
- ✅ Branch is NOT `Main`
- ✅ Build artifact available
- ✅ Preview quota not exhausted (<3 active previews)

**Production Deployment** requires:
- ✅ Build job completed successfully
- ✅ Test job completed (Phase 1: warnings allowed, Phase 2+: must pass)
- ✅ Branch IS `Main`
- ✅ Build artifact available
- ✅ Approval gate passed (manual reviewer approval within 24 hours)
- ✅ (Optional but recommended) Staging health check passed

---

## Error States & Recovery

### Workflow Failures

| Failure Scenario | Cause | Recovery |
|------------------|-------|----------|
| Build job fails | Angular compilation error (TypeScript, template, dependency) | Fix code errors, push new commit, workflow auto-retries |
| Test job fails (Phase 2+) | Failing unit/E2E tests | Fix tests or code, push new commit |
| Test job warnings (Phase 1) | Failing tests allowed with warnings | Deploy proceeds, log warning for manual review |
| Deploy job fails | SWA API error, invalid deployment token, quota exhaustion | Check Azure Portal for details, verify secret `AZURE_STATIC_WEB_APPS_API_TOKEN`, cleanup old previews if quota issue |
| Validate job fails | Deployment succeeded but URL returns 404/500 | Check SWA logs in Azure Portal, verify staticwebapp.config.json routing, check build artifact content |
| Approval timeout | No reviewer action within 24 hours | Re-run workflow, ensure reviewers notified, consider expedited approval process |
| Approval rejection | Reviewer explicitly rejects | Address concerns in PR feedback, push new commit, workflow auto-retries |

### Azure SWA Deployment Failures

| Error Code | Meaning | Recovery |
|------------|---------|----------|
| `QuotaExceeded` | Preview environment limit reached (3/3) | Delete old previews via Azure Portal or close stale PRs |
| `InvalidToken` | Deployment token invalid/expired | Regenerate token in Azure Portal, update GitHub secret `AZURE_STATIC_WEB_APPS_API_TOKEN` |
| `BuildFailed` | SWA internal build error | Check SWA logs, verify artifact structure, ensure `skip_app_build: true` in action (use prebuilt artifacts) |
| `DeploymentFailed` | Upload failed (network, timeout) | Retry workflow, check Azure service health status page |

---

## Next Steps (Contracts & Quickstart)

1. **contracts/workflow-schema.md**: Define GitHub Actions workflow inputs, outputs, secrets, expected behavior
2. **quickstart.md**: Step-by-step guide for initial setup (Terraform apply, GitHub secret creation, first deployment test)

---

**Data Model Complete**: April 12, 2026
**Next Phase**: Contracts & Quickstart Documentation
