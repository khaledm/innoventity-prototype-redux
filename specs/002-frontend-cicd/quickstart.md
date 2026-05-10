# Quickstart: Frontend CI/CD Automation

**Feature**: Frontend CI/CD Automation
**Branch**: `002-frontend-cicd`
**Date**: April 12, 2026
**Audience**: Developers setting up the frontend CI/CD pipeline for the first time

---

## Prerequisites

Before starting, verify you have:

- [ ] **Azure Subscription**: Active subscription with owner/contributor access
- [ ] **Azure CLI**: Installed and authenticated (`az login`)
- [ ] **Terraform**: Version 1.6+ installed (`terraform version`)
- [ ] **GitHub CLI**: Installed and authenticated (`gh auth login`)
- [ ] **Git**: Repository cloned locally with `Main` branch up to date
- [ ] **Node.js**: Version 20.x LTS installed (`node --version`)
- [ ] **Repository Permissions**: Write access to `khaledm/innoventity-prototype-redux`
- [ ] **Azure Resources**: Resource group `innoventity-dev-rg` exists (from Phase 0)
- [ ] **Terraform State**: Backend configured at `innoventitydevtfstate` storage account (from Phase 0)

---

## Step 1: Provision Azure Static Web Apps (Terraform)

### 1.1 Navigate to Infrastructure Directory

```powershell
cd infrastructure/environments/dev/core
```

### 1.2 Review Terraform Changes

```powershell
terraform init  # Initialize if first time, otherwise skip
terraform plan  # Review changes (should show static_web_app resource creation)
```

**Expected output**:
```
Plan: 1 to add, 0 to change, 0 to destroy.

Changes to Outputs:
  + static_web_app_default_hostname = (known after apply)
  + static_web_app_api_key          = (sensitive value)
  + static_web_app_id               = (known after apply)
```

**Validation**:
- ✅ Plan shows **1 resource to add** (`azurerm_static_web_app.main`)
- ✅ No existing resources destroyed (infrastructure addition, not replacement)
- ✅ Outputs include `static_web_app_api_key` (marked sensitive)

### 1.3 Apply Infrastructure Changes

```powershell
terraform apply -auto-approve
```

**Expected duration**: 2-3 minutes

**Expected output**:
```
azurerm_static_web_app.main: Creating...
azurerm_static_web_app.main: Creation complete after 2m15s [id=/subscriptions/...]

Apply complete! Resources: 1 added, 0 changed, 0 destroyed.

Outputs:

static_web_app_default_hostname = "innoventity-dev-web.azurestaticapps.net"
static_web_app_api_key = <sensitive>
static_web_app_id = "/subscriptions/.../resourceGroups/innoventity-dev-rg/providers/Microsoft.Web/staticSites/innoventity-dev-web"
```

### 1.4 Extract Deployment Token

```powershell
terraform output -raw static_web_app_api_key
```

**Copy this value to clipboard** — you'll use it in Step 2.

**Security Note**: This is a sensitive secret. Do not commit it to Git or share publicly.

---

## Step 2: Configure GitHub Secret

### 2.1 Set Deployment Token as Repository Secret

```powershell
# Paste deployment token from Step 1.4 when prompted
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN
```

**Prompt**: `? Paste your secret ›`
**Action**: Paste the token from Step 1.4, press Enter

**Expected output**:
```
✓ Set Actions secret AZURE_STATIC_WEB_APPS_API_TOKEN for khaledm/innoventity-prototype-redux
```

### 2.2 Verify Secret Created

```powershell
gh secret list
```

**Expected output** (should include):
```
AZURE_STATIC_WEB_APPS_API_TOKEN  Updated YYYY-MM-DD
```

---

## Step 3: Configure GitHub Environment (Production Approval Gate)

### 3.1 Create Production Environment

```powershell
gh api repos/khaledm/innoventity-prototype-redux/environments/production -X PUT
```

**Expected output**: `Created environment: production`

### 3.2 Add Required Reviewer

**Via GitHub CLI** (if available in your CLI version):
```powershell
# Add yourself as required reviewer (replace 'khaledm' with your GitHub username)
gh api repos/khaledm/innoventity-prototype-redux/environments/production/deployment-protection-rules -X POST -f type='required_reviewers' -f reviewers='[{"type":"User","id":YOUR_USER_ID}]'
```

**OR Via GitHub Web UI** (recommended for Phase 1):
1. Navigate to: https://github.com/khaledm/innoventity-prototype-redux/settings/environments
2. Click **production** environment
3. Check **Required reviewers**
4. Add your GitHub username (e.g., `khaledm`)
5. Set **Wait timer** to `0` minutes (no automatic delay)
6. Under **Deployment branches**, select **Selected branches**
7. Add rule: `Main` (only allow production deployments from Main branch)
8. Click **Save protection rules**

### 3.3 Verify Environment Configuration

```powershell
gh api repos/khaledm/innoventity-prototype-redux/environments/production | jq '{name:.name, protection_rules:.protection_rules}'
```

**Expected output**:
```json
{
  "name": "production",
  "protection_rules": [
    {
      "type": "required_reviewers",
      "reviewers": [
        {
          "type": "User",
          "reviewer": {
            "login": "khaledm"
          }
        }
      ]
    }
  ]
}
```

---

## Step 4: Create and Push Workflow File

### 4.1 Create Workflow Directory (if not exists)

```powershell
cd $env:REPO_ROOT  # Return to repository root
New-Item -ItemType Directory -Path ".github/workflows" -Force
```

### 4.2 Create Workflow File

**File**: `.github/workflows/deploy-frontend.yml`

**Minimal Version** (for initial test):
```yaml
name: Deploy Frontend

on:
  push:
    branches:
      - '**'
    paths:
      - 'src/Innoventity.Client/**'
      - '.github/workflows/deploy-frontend.yml'
  workflow_dispatch:

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20.x'
          cache: 'npm'
          cache-dependency-path: 'src/Innoventity.Client/package-lock.json'

      - name: Install dependencies
        working-directory: src/Innoventity.Client
        run: npm ci

      - name: Build production
        working-directory: src/Innoventity.Client
        run: npm run build:prod

      - name: Upload build artifact
        uses: actions/upload-artifact@v4
        with:
          name: frontend-build
          path: src/Innoventity.Client/dist/
          retention-days: 90

  deploy-preview:
    runs-on: ubuntu-latest
    needs: [build]
    if: github.ref != 'refs/heads/Main'
    steps:
      - name: Download build artifact
        uses: actions/download-artifact@v4
        with:
          name: frontend-build
          path: dist

      - name: Deploy to Azure Static Web Apps (Preview)
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: 'upload'
          app_location: '/'
          app_artifact_location: 'dist/innoventity.client'
          skip_app_build: true

  deploy-production:
    runs-on: ubuntu-latest
    needs: [build]
    if: github.ref == 'refs/heads/Main'
    environment:
      name: production
      url: https://innoventity-dev-web.azurestaticapps.net
    timeout-minutes: 1440  # 24 hours
    steps:
      - name: Download build artifact
        uses: actions/download-artifact@v4
        with:
          name: frontend-build
          path: dist

      - name: Deploy to Azure Static Web Apps (Production)
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: 'upload'
          app_location: '/'
          app_artifact_location: 'dist/innoventity.client'
          skip_app_build: true
          production_branch: 'Main'
```

**Note**: This is a simplified version for initial testing. Full implementation includes test job, validation job, and cleanup logic (see tasks.md for complete workflow).

### 4.3 Commit and Push Workflow

```powershell
git add .github/workflows/deploy-frontend.yml
git commit -m "feat: add frontend CI/CD workflow (initial version)"
git push origin 002-frontend-cicd
```

---

## Step 5: Test Feature Branch Deployment (Preview)

### 5.1 Trigger Workflow on Feature Branch

Workflow should automatically trigger on push (Step 4.3).

### 5.2 Monitor Workflow Execution

```powershell
gh run watch
```

**OR via GitHub Web UI**: https://github.com/khaledm/innoventity-prototype-redux/actions

**Expected stages**:
1. **build**: Installs dependencies, compiles Angular app (~5 minutes)
2. **deploy-preview**: Uploads to Azure SWA preview environment (~3 minutes)

**Expected output** (in deploy-preview job logs):
```
Azure Static Web Apps CI/CD
==========================
Deploying to preview environment...
Preview URL: https://innoventity-dev-web-abc123.azurestaticapps.net
```

### 5.3 Verify Preview Deployment

1. **Find preview URL**: Check deploy-preview job logs OR PR comment (if PR created)
2. **Open in browser**: `https://innoventity-dev-web-<random>.azurestaticapps.net`
3. **Verify Angular app loads**: Should see Innoventity homepage

**Validation**:
- ✅ URL returns HTTP 200
- ✅ Page displays Angular app (not Azure error page)
- ✅ Network tab shows `main.*.js`, `polyfills.*.js` loaded from SWA CDN

---

## Step 6: Test Production Deployment (Main Branch)

### 6.1 Create Pull Request to Main

```powershell
gh pr create --title "feat: frontend CI/CD automation" --body "Initial frontend deployment automation setup" --base Main --head 002-frontend-cicd
```

### 6.2 Review and Merge PR

1. **Review PR**: Verify preview deployment works in Step 5
2. **Approve PR**: (if required reviewers configured)
3. **Merge PR**: `gh pr merge --squash` OR via GitHub UI

### 6.3 Monitor Production Deployment Workflow

```powershell
# After merge to Main, workflow should trigger automatically
gh run list --branch Main --limit 1
gh run watch <RUN_ID>  # Use run ID from above command
```

**Expected stages**:
1. **build**: Compiles Angular app
2. **deploy-production**: Pauses at **approval gate**

**Expected output**:
```
deploy-production  ⏸  Waiting for approval (timeout: 24 hours)
```

### 6.4 Approve Production Deployment

**Via GitHub Web UI** (recommended):
1. Navigate to: https://github.com/khaledm/innoventity-prototype-redux/actions
2. Click the running workflow
3. Click **Review deployments**
4. Select **production** environment
5. Add optional comment (e.g., "Approved - initial deployment test")
6. Click **Approve and deploy**

**OR via GitHub CLI** (if available):
```powershell
gh run approve <RUN_ID> --environment production
```

### 6.5 Verify Production Deployment

After approval granted (~5 minutes for deployment to complete):

1. **Open production URL**: https://innoventity-dev-web.azurestaticapps.net
2. **Verify Angular app loads**: Should see Innoventity homepage
3. **Check deployment logs**: Azure Portal → Static Web Apps → innoventity-dev-web → Deployments

**Validation**:
- ✅ Production URL returns HTTP 200
- ✅ Page displays Angular app with latest changes
- ✅ Azure Portal shows deployment status "Succeeded"

---

## Step 7: Verify Workflow Automation

### 7.1 Make Frontend Change

```powershell
# Create a minor change to verify automation
code src/Innoventity.Client/src/app/app.component.html  # Or any frontend file
# Make a small change (e.g., update page title)
git add .
git commit -m "test: verify frontend CI/CD automation"
git push origin 002-frontend-cicd
```

### 7.2 Verify Automatic Deployment

1. **Check GitHub Actions**: Workflow should trigger automatically (no manual dispatch needed)
2. **Verify preview URL updated**: Visit preview URL from Step 5.3, see changes reflected

**Success Criteria**:
- ✅ Workflow triggers within 60 seconds of push
- ✅ Build completes in <5 minutes
- ✅ Preview deployment succeeds
- ✅ Preview URL shows updated changes within 5 minutes

---

## Step 8: Clean Up Test Resources

### 8.1 Close Feature Branch PR (if still open)

```powershell
gh pr close <PR_NUMBER>
```

**Expected behavior**: Preview environment auto-deleted within 1 hour (Azure SWA automatic cleanup)

### 8.2 Verify Preview Cleanup

```powershell
# Check Azure Portal
az staticwebapp environment list --name innoventity-dev-web --resource-group innoventity-dev-rg
```

**Expected output**: Empty list OR only production environment (no preview environments listed)

---

## Troubleshooting

### Common Issues

#### Issue: Workflow doesn't trigger on push

**Symptoms**: No workflow run appears in GitHub Actions after pushing to feature branch

**Possible Causes**:
- Push didn't include changes to `src/Innoventity.Client/**` (workflow filtered by paths)
- Workflow file has syntax errors (check YAML linting)

**Resolution**:
```powershell
# Verify workflow file syntax
gh workflow list  # Should show "Deploy Frontend"

# Trigger manually to test
gh workflow run deploy-frontend.yml --ref 002-frontend-cicd
```

---

#### Issue: Build job fails with "npm ci: command not found"

**Symptoms**: Build job fails at "Install dependencies" step

**Possible Cause**: `actions/setup-node` not configured correctly

**Resolution**: Verify `.github/workflows/deploy-frontend.yml` includes:
```yaml
- uses: actions/setup-node@v4
  with:
    node-version: '20.x'
```

---

#### Issue: Deploy job fails with "InvalidToken: Authentication failed"

**Symptoms**: `deploy-preview` or `deploy-production` fails with authentication error

**Possible Causes**:
- `AZURE_STATIC_WEB_APPS_API_TOKEN` secret not set
- Secret value incorrect (copied wrong token)
- Token rotated in Azure but not updated in GitHub

**Resolution**:
```powershell
# Regenerate token
cd infrastructure/environments/dev/core
terraform output -raw static_web_app_api_key

# Update GitHub secret
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN  # Paste new token
```

---

#### Issue: Validation job fails with "HTTP 404: Not Found"

**Symptoms**: Deployment succeeds but validation fails with 404 error

**Possible Causes**:
- Build artifact missing `index.html` (Angular build failed silently)
- SWA routing configuration incorrect (`staticwebapp.config.json`)
- CDN propagation delay (should resolve within 5 minutes)

**Resolution**:
```powershell
# Check build artifact locally
cd src/Innoventity.Client
npm run build:prod
ls dist/innoventity.client/  # Should include index.html

# If index.html exists, wait 5 minutes for CDN propagation
# If still failing, check staticwebapp.config.json routing rules
```

---

#### Issue: Approval gate times out after 24 hours

**Symptoms**: Production deployment auto-fails with "Workflow run exceeded timeout"

**Possible Causes**:
- No reviewer approved within 24 hours (timeout setting)
- Reviewer not notified (email notification issue)

**Resolution**:
```powershell
# Re-run workflow after timeout
gh run rerun <RUN_ID>

# Ensure reviewers are notified (check GitHub email settings)
# Consider reducing timeout for faster feedback (edit workflow: timeout-minutes: 60)
```

---

#### Issue: Preview environment quota exhausted

**Symptoms**: Deploy-preview fails with "QuotaExceeded: Preview limit reached (3/3)"

**Possible Cause**: 3 preview environments already active (Free tier limit)

**Resolution**:
```powershell
# List active previews
az staticwebapp environment list --name innoventity-dev-web --resource-group innoventity-dev-rg

# Delete oldest preview manually
az staticwebapp environment delete --name <ENVIRONMENT_NAME> --resource-group innoventity-dev-rg

# OR close stale PRs to trigger automatic cleanup
gh pr list --state open  # Find old PRs
gh pr close <PR_NUMBER>  # Close unused PRs
```

---

## Next Steps

After successful quickstart:

1. **Add Test Job**: Extend workflow with Jest + Playwright tests (see tasks.md)
2. **Add Validation Job**: Implement HTTP health checks after deployment
3. **Add Cleanup Workflow**: Auto-delete preview environments on PR closure
4. **Enable Branch Protection**: Require workflow status checks before merging to Main
5. **Document Runbook**: Add operational procedures in `docs/runbooks/frontend-deployment.md`

---

## Success Checklist

- [x] **Step 1**: Azure Static Web Apps provisioned via Terraform ✅
- [x] **Step 2**: GitHub secret `AZURE_STATIC_WEB_APPS_API_TOKEN` configured ✅
- [x] **Step 3**: Production environment with approval gate created ✅
- [x] **Step 4**: Workflow file created and pushed ✅
- [x] **Step 5**: Feature branch deployment tested (preview environment) ✅
- [x] **Step 6**: Production deployment tested (Main branch + approval) ✅
- [x] **Step 7**: Automation verified (push → deploy) ✅
- [x] **Step 8**: Test resources cleaned up ✅

---

## Reference Documentation

- **Specification**: [specs/002-frontend-cicd/spec.md](spec.md)
- **Data Model**: [specs/002-frontend-cicd/data-model.md](data-model.md)
- **Workflow Contract**: [specs/002-frontend-cicd/contracts/workflow-schema.md](contracts/workflow-schema.md)
- **Research**: [specs/002-frontend-cicd/research.md](research.md)
- **Azure SWA Docs**: https://learn.microsoft.com/en-us/azure/static-web-apps/
- **GitHub Actions SWA**: https://github.com/Azure/static-web-apps-deploy

---

**Quickstart Version**: 1.0.0
**Last Updated**: April 12, 2026
**Estimated Completion Time**: 30-45 minutes (first-time setup)
