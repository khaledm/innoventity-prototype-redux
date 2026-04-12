# Angular 19 + Vite + Azure Static Web Apps: Pre-Implementation Review
## Non-Destructive Architecture Review & Implementation Guide

**Review Date**: April 5, 2026
**Reviewer**: Senior Angular + Azure Deployment Architect
**Review Type**: Pre-Implementation Readiness Assessment
**Repository**: innoventity-prototype-redux (branch: 001-platform-core)

---

## Executive Summary

### Current State Assessment

**🚨 CRITICAL FINDING: Angular Client Does Not Exist Yet**

The repository currently contains:
- ✅ **Backend API**: ASP.NET Core 8 Minimal API (`src/Innoventity.API/`) — 100% complete, deployed to Azure App Service
- ✅ **Infrastructure**: Terraform modules for Azure resources (App Service, SQL Database, Application Insights)
- ✅ **CI/CD**: GitHub Actions workflows for API deployment (`deploy.yml`, `infra.yml`)
- ✅ **Specifications**: Comprehensive Angular 19 + Vite architecture documented in `specs/001-platform-core/`
- ❌ **Frontend Client**: **NOT IMPLEMENTED** — `src/Innoventity.Client/` does not exist

**Tasks T071-T075** (Phase 7: Frontend Phase 0 Shell) are pending implementation.

### Architectural Intent (from Specifications)

The specifications document a well-architected Angular 19 + Vite setup:
- **Framework**: Angular 19 (standalone components, Signal-based forms)
- **Build Tool**: Vite (ESM-first, default in Angular 19)
- **Deployment Target**: **Not specified** — review recommends **Azure Static Web Apps** (optimal for SPAs)
- **Backend Integration**: API hosted separately on Azure App Service (`innoventity-dev-api.azurewebsites.net`)
- **Testing**: Jest (unit), Playwright (E2E), Angular Testing Library (component)
- **State Management**: Signals (no NgRx for Phase 0)
- **API Client**: OpenAPI Generator from `specs/001-platform-core/contracts/openapi.yaml`

### Deployment Strategy Gap

**Current**: Backend deployed to Azure App Service via GitHub Actions
**Missing**: No deployment strategy for Angular SPA
**Recommended**: Azure Static Web Apps (rationale below)

---

## 1. Risk Assessment (Ranked by Severity)

### CRITICAL RISKS — Blockers for Implementation

| Risk ID | Risk Description | Impact | Root Cause | Mitigation Priority |
|---------|------------------|--------|------------|---------------------|
| **R1** | No Angular client codebase exists | **CRITICAL** | T071-T075 not implemented | Create client per T071 guidance |
| **R2** | No deployment target configured for SPA | **CRITICAL** | Spec doesn't specify Azure Static Web Apps vs App Service | Choose deployment model (SWA recommended) |
| **R3** | No CI/CD workflow for client build/deploy | **CRITICAL** | Only API deployment workflow exists | Create client deployment workflow |
| **R4** | CORS not configured for local dev + production | **HIGH** | Backend CORS policy requires explicit frontend origins | Add `http://localhost:4200` (dev) and SWA URL (prod) to CORS |
| **R5** | No API base URL environment handling | **HIGH** | Runtime API URL discovery not implemented | Implement compile-time or runtime config injection |

### HIGH RISKS — Major Implementation Gaps

| Risk ID | Risk Description | Impact | Root Cause | Mitigation Priority |
|---------|------------------|--------|------------|---------------------|
| **R6** | Vite config will need Angular-specific optimizations | **MEDIUM** | Vite default config doesn't handle Angular decorator metadata | Add `@angular-devkit/build-angular` Vite plugin |
| **R7** | Zone.js + TestBed incompatibility with Vitest | **MEDIUM** | Angular relies on zone.js; Vitest ESM-only mode has issues | Use Jest instead (proven compatibility) |
| **R8** | OpenAPI client generation not automated in build | **MEDIUM** | Manual generation required before build | Add prebuild script for OpenAPI codegen |
| **R9** | No static web app configuration (SPA routing) | **MEDIUM** | Azure SWA requires `staticwebapp.config.json` for SPA fallback | Create SWA config with route rewrites |
| **R10** | Secrets management for deployment | **MEDIUM** | Azure SWA deployment token not in GitHub Secrets | Add `AZURE_STATIC_WEB_APPS_API_TOKEN` secret |

### MEDIUM RISKS — Performance & Security

| Risk ID | Risk Description | Impact | Root Cause | Mitigation Priority |
|---------|------------------|--------|------------|---------------------|
| **R11** | No Content Security Policy headers | **LOW-MEDIUM** | Security headers not configured | Add CSP to `staticwebapp.config.json` |
| **R12** | Bundle size optimization not configured | **LOW** | Default Vite config; no tree-shaking verification | Review bundle analyzer output post-build |
| **R13** | No client-side environment variable injection | **LOW** | Hardcoded API URLs in code (anti-pattern) | Use Vite `import.meta.env` with `.env` files |

---

## 2. Deployment Architecture Recommendation

### Why Azure Static Web Apps (Recommended)

**Advantages over Azure App Service for Angular SPA:**

| Criterion | Azure Static Web Apps | Azure App Service | Winner |
|-----------|----------------------|-------------------|--------|
| **Cost** | Free tier (100GB bandwidth/month) | ~$13/month minimum (B1 tier) | **SWA** |
| **Global CDN** | Built-in Azure CDN (28 edge locations) | Requires separate CDN setup | **SWA** |
| **SPA Routing** | Native fallback to `index.html` | Requires web.config or custom routing | **SWA** |
| **CI/CD** | `azure/static-web-apps-deploy` GH Action | Manual Zip Deploy or Kudu | **SWA** |
| **HTTPS** | Auto-provisioned SSL (*.azurestaticapps.net) | Requires App Service TLS binding | **SWA** |
| **Preview Environments** | Auto-deployed for PRs | Manual slot creation | **SWA** |
| **Staging Slots** | Unlimited (PR-based) | Limited (requires Standard tier) | **SWA** |
| **Backend API Integration** | Can host Azure Functions (optional) | N/A (SPA-only) | Neutral |

**Decision**: Use **Azure Static Web Apps** for Angular 19 client deployment.

**Backend API**: Remains on Azure App Service (no change to existing infrastructure).

### Deployment Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ GitHub Repository (001-platform-core branch)                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  src/Innoventity.Client/              src/Innoventity.API/      │
│  ├── src/                             ├── Features/             │
│  ├── package.json                     ├── Domain/               │
│  ├── vite.config.ts                   └── Program.cs            │
│  ├── tsconfig.json                                              │
│  └── staticwebapp.config.json                                   │
│                                                                  │
└────────┬─────────────────────────────────────┬──────────────────┘
         │                                     │
         │ Push to src/Innoventity.Client/**   │ Push to src/Innoventity.API/**
         │                                     │
         ▼                                     ▼
┌────────────────────────────┐      ┌──────────────────────────────┐
│ GitHub Actions:            │      │ GitHub Actions:              │
│ client-deploy.yml          │      │ deploy.yml (existing)        │
├────────────────────────────┤      ├──────────────────────────────┤
│ 1. npm ci                  │      │ 1. dotnet restore            │
│ 2. npm run build:prod      │      │ 2. dotnet build              │
│ 3. npm test                │      │ 3. dotnet test               │
│ 4. Deploy to Azure SWA     │      │ 4. Deploy to App Service     │
└────────┬───────────────────┘      └──────────┬───────────────────┘
         │                                     │
         ▼                                     ▼
┌────────────────────────────┐      ┌──────────────────────────────┐
│ Azure Static Web Apps      │      │ Azure App Service            │
│ innoventity-client.        │◄─────┤ innoventity-dev-api.         │
│ azurestaticapps.net        │ API  │ azurewebsites.net            │
│                            │ calls│                              │
│ - Hosts: dist/client/      │      │ - Hosts: Innoventity.API.dll │
│ - CDN: Global edge cache   │      │ - DB: Azure SQL Database     │
│ - Routes: SPA fallback     │      │ - Auth: JWT bearer tokens    │
└────────────────────────────┘      └──────────────────────────────┘
         │                                     │
         │ User visits https://innoventity-   │
         │ client.azurestaticapps.net         │
         │                                     │
         └─────────────────────────────────────┘
           Angular SPA calls API endpoints
```

---

## 3. Actionable Remediation Steps (Grouped by Priority)

### CRITICAL — Must Implement Before Deployment

#### C1. Create Angular 19 Client Application (T071)

**File**: None (directory creation required)
**Action**: Initialize Angular 19 app with Vite builder
**Effort**: MEDIUM (2-3 hours)
**Priority**: P0 (blocks all other work)

**Commands** (to be run from repository root):

```bash
# Navigate to src directory
cd src

# Create Angular 19 app with Vite (Angular CLI 19+ uses Vite by default)
npx @angular/cli@19 new Innoventity.Client \
  --routing=true \
  --style=scss \
  --skip-git=true \
  --standalone=true \
  --ssr=false

# Verify Vite is configured (Angular 19 default)
cd Innoventity.Client
cat angular.json  # Should show "builder": "@angular-devkit/build-angular:application"
```

**Expected Output Structure**:
```
src/Innoventity.Client/
├── src/
│   ├── app/
│   │   ├── app.component.ts
│   │   ├── app.config.ts
│   │   └── app.routes.ts
│   ├── index.html
│   ├── main.ts
│   └── styles.scss
├── node_modules/
├── package.json
├── tsconfig.json
├── tsconfig.app.json
├── angular.json          # Angular CLI configuration
└── vite.config.ts        # Generated by Angular CLI (if present)
```

**Post-Creation Validation**:
```bash
# Verify Angular 19 + Vite
ng version  # Should show Angular CLI 19.x
npm run build  # Should produce dist/innoventity.client/ output
```

---

#### C2. Configure package.json Scripts

**File**: `src/Innoventity.Client/package.json` (to be created by C1)
**Action**: Add build/test/preview scripts with OpenAPI client generation
**Effort**: SMALL (30 minutes)
**Priority**: P0

**Recommended `package.json` additions** (merge with Angular CLI-generated file):

```json
{
  "name": "innoventity-client",
  "version": "1.0.0",
  "scripts": {
    "ng": "ng",
    "start": "ng serve --proxy-config proxy.conf.json",
    "prebuild": "npm run api:generate",
    "build": "ng build",
    "build:prod": "ng build --configuration production",
    "preview": "vite preview --outDir dist/innoventity.client",
    "test": "jest --config jest.config.js",
    "test:watch": "jest --watch",
    "test:coverage": "jest --coverage",
    "lint": "ng lint",
    "e2e": "playwright test",
    "api:generate": "openapi-generator-cli generate -i ../../specs/001-platform-core/contracts/openapi.yaml -g typescript-angular -o src/app/core/api-client --additional-properties=ngVersion=19,supportsES6=true,npmName=@innoventity/api-client"
  },
  "dependencies": {
    "@angular/animations": "^19.0.0",
    "@angular/common": "^19.0.0",
    "@angular/compiler": "^19.0.0",
    "@angular/core": "^19.0.0",
    "@angular/forms": "^19.0.0",
    "@angular/material": "^19.0.0",
    "@angular/platform-browser": "^19.0.0",
    "@angular/platform-browser-dynamic": "^19.0.0",
    "@angular/router": "^19.0.0",
    "rxjs": "^7.8.0",
    "tslib": "^2.6.0",
    "zone.js": "^0.15.0"
  },
  "devDependencies": {
    "@angular-devkit/build-angular": "^19.0.0",
    "@angular/cli": "^19.0.0",
    "@angular/compiler-cli": "^19.0.0",
    "@openapitools/openapi-generator-cli": "^2.13.0",
    "@playwright/test": "^1.40.0",
    "@types/jest": "^29.5.0",
    "@types/node": "^20.10.0",
    "jest": "^29.7.0",
    "jest-preset-angular": "^14.0.0",
    "typescript": "~5.4.0"
  },
  "engines": {
    "node": ">=18.19.0",
    "npm": ">=10.0.0"
  }
}
```

**Key Script Explanations**:
- `prebuild`: Auto-generates TypeScript API client from OpenAPI spec before build
- `start`: Runs dev server with proxy to backend API (see C3)
- `build:prod`: Production build with optimizations (tree-shaking, minification)
- `preview`: Preview production build locally before deploying
- `test`: Jest with Angular Testing Library (not Vitest — see R7)

---

#### C3. Create Proxy Configuration for Local Development

**File**: `src/Innoventity.Client/proxy.conf.json` (NEW)
**Action**: Proxy `/api/*` requests to backend during local dev
**Effort**: SMALL (15 minutes)
**Priority**: P0

**Content**:
```json
{
  "/api": {
    "target": "http://localhost:5001",
    "secure": false,
    "changeOrigin": true,
    "logLevel": "debug"
  }
}
```

**Explanation**:
- Proxies Angular dev server (`http://localhost:4200/api/auth/login`) → API (`http://localhost:5001/api/auth/login`)
- Avoids CORS issues during local development
- `changeOrigin: true` sets `Host` header to target (required for App Service local debug)

**Backend CORS Configuration** (verify in `src/Innoventity.API/Program.cs`):
```csharp
// Ensure this exists in Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular dev server
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

app.UseCors("AllowFrontend");
```

---

#### C4. Configure tsconfig for Angular 19 + Vite ESM

**File**: `src/Innoventity.Client/tsconfig.json` (Angular CLI generates this)
**Action**: Verify/update TypeScript config for ESM + Angular decorators
**Effort**: SMALL (15 minutes)
**Priority**: P0

**Recommended `tsconfig.json`** (merge with CLI-generated):

```json
{
  "compileOnSave": false,
  "compilerOptions": {
    "outDir": "./dist/out-tsc",
    "forceConsistentCasingInFileNames": true,
    "strict": true,
    "noImplicitOverride": true,
    "noPropertyAccessFromIndexSignature": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "skipLibCheck": true,
    "esModuleInterop": true,
    "sourceMap": true,
    "declaration": false,
    "experimentalDecorators": true,
    "moduleResolution": "bundler",
    "importHelpers": true,
    "target": "ES2022",
    "module": "ES2022",
    "lib": ["ES2022", "dom"],
    "useDefineForClassFields": false
  },
  "angularCompilerOptions": {
    "enableI18nLegacyMessageIdFormat": false,
    "strictInjectionParameters": true,
    "strictInputAccessModifiers": true,
    "strictTemplates": true
  }
}
```

**Critical Settings**:
- `module: "ES2022"`: ESM-first for Vite (not CommonJS)
- `moduleResolution: "bundler"`: Angular 19 + Vite requirement
- `experimentalDecorators: true`: Required for Angular decorators
- `useDefineForClassFields: false`: Required for Angular dependency injection

**Companion `tsconfig.app.json`**:
```json
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "outDir": "./out-tsc/app",
    "types": []
  },
  "files": ["src/main.ts"],
  "include": ["src/**/*.d.ts"]
}
```

---

#### C5. Configure Jest (NOT Vitest)

**File**: `src/Innoventity.Client/jest.config.js` (NEW)
**Action**: Configure Jest with Angular + zone.js support
**Effort**: SMALL (30 minutes)
**Priority**: P0

**Rationale**: Vitest has known issues with Angular's `TestBed` and `zone.js` in ESM mode. Jest + `jest-preset-angular` is the proven solution for Angular 19.

**Content** (`jest.config.js`):
```javascript
module.exports = {
  preset: 'jest-preset-angular',
  setupFilesAfterEnv: ['<rootDir>/setup-jest.ts'],
  testPathIgnorePatterns: ['/node_modules/', '/dist/', '/e2e/'],
  coverageDirectory: 'coverage',
  coverageReporters: ['html', 'lcov', 'text-summary'],
  collectCoverageFrom: [
    'src/app/**/*.ts',
    '!src/app/**/*.spec.ts',
    '!src/app/core/api-client/**',  // Exclude generated code
    '!src/main.ts'
  ],
  coverageThreshold: {
    global: {
      branches: 80,
      functions: 80,
      lines: 80,
      statements: 80
    }
  },
  moduleNameMapper: {
    '^@app/(.*)$': '<rootDir>/src/app/$1',
    '^@core/(.*)$': '<rootDir>/src/app/core/$1',
    '^@features/(.*)$': '<rootDir>/src/app/features/$1'
  },
  transform: {
    '^.+\\.(ts|js|html)$': [
      'jest-preset-angular',
      {
        tsconfig: '<rootDir>/tsconfig.spec.json',
        stringifyContentPathRegex: '\\.html$'
      }
    ]
  },
  transformIgnorePatterns: ['node_modules/(?!.*\\.mjs$)']
};
```

**Setup file** (`setup-jest.ts`):
```typescript
import 'jest-preset-angular/setup-jest';
import { TextEncoder, TextDecoder } from 'util';

// Polyfills for Node 18+
Object.assign(global, { TextDecoder, TextEncoder });
```

**Companion `tsconfig.spec.json`**:
```json
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "outDir": "./out-tsc/spec",
    "types": ["jest", "node"]
  },
  "include": ["src/**/*.spec.ts", "src/**/*.d.ts"]
}
```

---

#### C6. Create Azure Static Web Apps Configuration

**File**: `src/Innoventity.Client/staticwebapp.config.json` (NEW)
**Action**: Configure SPA routing, security headers, caching
**Effort**: SMALL (30 minutes)
**Priority**: P0

**Content**:
```json
{
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/assets/*", "/api/*"]
  },
  "routes": [
    {
      "route": "/assets/*",
      "headers": {
        "cache-control": "public, max-age=31536000, immutable"
      }
    },
    {
      "route": "/*",
      "headers": {
        "cache-control": "no-cache, no-store, must-revalidate",
        "Content-Security-Policy": "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' https://innoventity-dev-api.azurewebsites.net;",
        "X-Content-Type-Options": "nosniff",
        "X-Frame-Options": "DENY",
        "X-XSS-Protection": "1; mode=block",
        "Referrer-Policy": "strict-origin-when-cross-origin"
      }
    }
  ],
  "globalHeaders": {
    "Strict-Transport-Security": "max-age=31536000; includeSubDomains"
  },
  "responseOverrides": {
    "404": {
      "rewrite": "/index.html",
      "statusCode": 200
    }
  }
}
```

**Key Configuration**:
- **SPA Fallback**: All non-asset routes return `index.html` (Angular handles routing)
- **Asset Caching**: `assets/*` cached for 1 year (immutable, hashed filenames)
- **HTML Caching**: Index.html never cached (ensures latest app version)
- **CSP**: Allows API calls to `innoventity-dev-api.azurewebsites.net` (update for production URL)
- **Security Headers**: HSTS, XSS protection, frame denial

**⚠️ ACTION REQUIRED**: Update `connect-src` in CSP when API URL changes (dev → staging → prod).

---

#### C7. Create GitHub Actions Workflow for Client Deployment

**File**: `.github/workflows/client-deploy.yml` (NEW)
**Action**: Build and deploy Angular app to Azure Static Web Apps
**Effort**: MEDIUM (1 hour)
**Priority**: P0

**Content**:
```yaml
name: Deploy Angular Client

on:
  push:
    branches: [001-platform-core, Main]
    paths:
      - 'src/Innoventity.Client/**'
      - 'specs/001-platform-core/contracts/openapi.yaml'
  pull_request:
    types: [opened, synchronize, reopened, closed]
    branches: [Main]
    paths:
      - 'src/Innoventity.Client/**'
  workflow_dispatch:

permissions:
  contents: read
  pull-requests: write

jobs:
  build-and-deploy:
    name: Build and Deploy
    if: github.event_name == 'push' || (github.event_name == 'pull_request' && github.event.action != 'closed')
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: src/Innoventity.Client

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '18.19.0'
          cache: 'npm'
          cache-dependency-path: src/Innoventity.Client/package-lock.json

      - name: Install dependencies
        run: npm ci

      - name: Generate OpenAPI client
        run: npm run api:generate

      - name: Lint code
        run: npm run lint

      - name: Run unit tests
        run: npm run test:coverage

      - name: Build production bundle
        run: npm run build:prod
        env:
          NODE_ENV: production

      - name: Upload build artifacts
        uses: actions/upload-artifact@v4
        with:
          name: angular-build
          path: src/Innoventity.Client/dist/innoventity.client
          retention-days: 7

      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: 'upload'
          app_location: 'src/Innoventity.Client'
          output_location: 'dist/innoventity.client'
          skip_app_build: true  # We already built with npm run build:prod

  close-pull-request:
    name: Close Pull Request
    if: github.event_name == 'pull_request' && github.event.action == 'closed'
    runs-on: ubuntu-latest
    steps:
      - name: Close Pull Request Environment
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          action: 'close'
```

**GitHub Secrets Required**:
1. **`AZURE_STATIC_WEB_APPS_API_TOKEN`**:
   - Obtain from Azure Portal → Static Web Apps → Deployment → Deployment token
   - Add to GitHub: Repository Settings → Secrets and variables → Actions → New repository secret

**Workflow Features**:
- **Trigger**: Runs on push to client files or OpenAPI spec changes
- **PR Preview**: Auto-deploys preview environments for pull requests
- **Caching**: npm cache speeds up builds (5-10x faster on cache hit)
- **Quality Gates**: Lint + unit tests must pass before deploy
- **Artifact Upload**: 7-day retention for debugging failed deployments

---

#### C8. Configure Environment Variables (Runtime API URL)

**File**: `src/Innoventity.Client/src/environments/environment.ts` (NEW)
**Action**: Inject API base URL at build time
**Effort**: SMALL (30 minutes)
**Priority**: P0

**Create environment files**:

**`src/environments/environment.ts`** (default/dev):
```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5001/api',  // Local backend
};
```

**`src/environments/environment.production.ts`**:
```typescript
export const environment = {
  production: true,
  apiBaseUrl: 'https://innoventity-dev-api.azurewebsites.net/api',
};
```

**Update `angular.json`** to use file replacements:
```json
{
  "projects": {
    "Innoventity.Client": {
      "architect": {
        "build": {
          "configurations": {
            "production": {
              "fileReplacements": [
                {
                  "replace": "src/environments/environment.ts",
                  "with": "src/environments/environment.production.ts"
                }
              ]
            }
          }
        }
      }
    }
  }
}
```

**Usage in code** (e.g., `src/app/core/api-client/configuration.ts`):
```typescript
import { environment } from '@environments/environment';

export const API_CONFIG = {
  basePath: environment.apiBaseUrl,
};
```

**Path alias in `tsconfig.json`**:
```json
{
  "compilerOptions": {
    "paths": {
      "@environments/*": ["src/environments/*"]
    }
  }
}
```

---

### RECOMMENDED — Improve Reliability & Performance

#### R1. Add Vite Configuration (if not auto-generated)

**File**: `src/Innoventity.Client/vite.config.ts` (may be created by Angular CLI)
**Action**: Optimize Vite for Angular 19 + Material
**Effort**: SMALL (30 minutes)
**Priority**: P1

**Content** (if file doesn't exist):
```typescript
import { defineConfig } from 'vite';
import angular from '@analogjs/vite-plugin-angular';

export default defineConfig({
  plugins: [angular()],

  optimizeDeps: {
    include: [
      '@angular/common',
      '@angular/core',
      '@angular/forms',
      '@angular/material',
      'rxjs',
    ],
    exclude: ['zone.js'],
  },

  build: {
    target: 'es2022',
    outDir: 'dist/innoventity.client',
    rollupOptions: {
      output: {
        manualChunks: {
          'angular-core': ['@angular/core', '@angular/common'],
          'angular-material': ['@angular/material'],
          vendor: ['rxjs'],
        },
      },
    },
  },

  server: {
    port: 4200,
    proxy: {
      '/api': {
        target: 'http://localhost:5001',
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
```

**Note**: Angular CLI 19+ may auto-generate this. If `vite.config.ts` exists, merge the `optimizeDeps` and `build.rollupOptions` sections.

---

#### R2. Add Bundle Analyzer

**File**: `src/Innoventity.Client/package.json`
**Action**: Add bundle size analysis script
**Effort**: SMALL (15 minutes)
**Priority**: P2

**Add to `devDependencies`**:
```json
{
  "devDependencies": {
    "rollup-plugin-visualizer": "^5.12.0"
  }
}
```

**Add script**:
```json
{
  "scripts": {
    "analyze": "ng build --stats-json && npx webpack-bundle-analyzer dist/innoventity.client/stats.json"
  }
}
```

**Usage**:
```bash
npm run analyze
# Opens browser with interactive bundle size visualization
```

**Acceptance Criteria**:
- Initial bundle (main.js) < 500 KB gzipped
- Material chunk < 300 KB gzipped
- Lazy-loaded routes < 100 KB each

---

#### R3. Add Playwright E2E Tests (T075)

**File**: `src/Innoventity.Client/playwright.config.ts` (NEW)
**Action**: Configure Playwright for E2E testing
**Effort**: MEDIUM (1 hour)
**Priority**: P2

**Content**:
```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html'], ['junit', { outputFile: 'test-results/junit.xml' }]],

  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
  ],

  webServer: {
    command: 'npm run start',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
});
```

**Example test** (`e2e/journey-1.spec.ts`):
```typescript
import { test, expect } from '@playwright/test';

test.describe('Journey 1: Registration → Login → View Innovation', () => {
  test('should complete full authentication flow', async ({ page }) => {
    // 1. Navigate to login page
    await page.goto('/login');

    // 2. Fill login form
    await page.getByLabel('Email').fill('test@example.com');
    await page.getByLabel('Password').fill('Test123!@#');
    await page.getByRole('button', { name: 'Log In' }).click();

    // 3. Verify redirect to innovations
    await expect(page).toHaveURL('/innovations');

    // 4. Navigate to innovation detail
    await page.goto('/innovations/123e4567-e89b-12d3-a456-426614174000');

    // 5. Verify innovation loaded
    await expect(page.getByRole('heading')).toContainText('Innovation');
  });
});
```

**Add to CI workflow** (`.github/workflows/client-deploy.yml`):
```yaml
- name: Run E2E tests
  run: npm run e2e
  env:
    CI: true
```

---

#### R4. Add Pre-commit Hooks (Code Quality)

**File**: `src/Innoventity.Client/package.json`
**Action**: Auto-format and lint before commits
**Effort**: SMALL (30 minutes)
**Priority**: P2

**Install Husky + lint-staged**:
```json
{
  "devDependencies": {
    "husky": "^9.0.0",
    "lint-staged": "^15.2.0",
    "prettier": "^3.2.0"
  },
  "scripts": {
    "prepare": "cd ../.. && husky install src/Innoventity.Client/.husky"
  },
  "lint-staged": {
    "*.ts": [
      "prettier --write",
      "eslint --fix"
    ],
    "*.html": [
      "prettier --write"
    ],
    "*.scss": [
      "prettier --write"
    ]
  }
}
```

**Initialize Husky**:
```bash
cd src/Innoventity.Client
npm run prepare
npx husky add .husky/pre-commit "cd src/Innoventity.Client && npx lint-staged"
```

---

### OPTIONAL — Nice-to-Have Enhancements

#### O1. Add Progressive Web App (PWA) Support

**File**: `src/Innoventity.Client/package.json`
**Action**: Enable offline support via service worker
**Effort**: SMALL (1 hour)
**Priority**: P3

**Install PWA schematics**:
```bash
ng add @angular/pwr
```

**Configuration** (auto-generated `ngsw-config.json`):
```json
{
  "index": "/index.html",
  "assetGroups": [
    {
      "name": "app",
      "installMode": "prefetch",
      "resources": {
        "files": ["/favicon.ico", "/index.html", "/*.css", "/*.js"]
      }
    },
    {
      "name": "assets",
      "installMode": "lazy",
      "updateMode": "prefetch",
      "resources": {
        "files": ["/assets/**"]
      }
    }
  ]
}
```

**Benefits**:
- Offline support for authenticated users
- Faster repeat visits (cached bundles)
- Install prompt on mobile devices

---

#### O2. Add Monitoring (Application Insights)

**File**: `src/Innoventity.Client/src/app/app.config.ts`
**Action**: Integrate Azure Application Insights
**Effort**: SMALL (30 minutes)
**Priority**: P3

**Install SDK**:
```bash
npm install @microsoft/applicationinsights-web
```

**Configuration** (`app.config.ts`):
```typescript
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { environment } from '@environments/environment';

const appInsights = new ApplicationInsights({
  config: {
    connectionString: environment.appInsightsConnectionString,
    enableAutoRouteTracking: true,
  }
});

appInsights.loadAppInsights();
appInsights.trackPageView();

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: ApplicationInsights, useValue: appInsights },
    // ... other providers
  ],
};
```

**Add to `environment.production.ts`**:
```typescript
export const environment = {
  appInsightsConnectionString: 'InstrumentationKey=xxx-xxx-xxx',
};
```

---

## 4. CI Acceptance Criteria & Test Matrix

### Pipeline Success Criteria

| Gate | Check | Pass Condition | Failure Action |
|------|-------|----------------|----------------|
| **G1** | npm ci | exit code 0 | Fail pipeline |
| **G2** | npm run lint | exit code 0 | Fail pipeline |
| **G3** | npm test | exit code 0, coverage ≥80% | Fail pipeline |
| **G4** | npm run build:prod | exit code 0, dist/ exists | Fail pipeline |
| **G5** | Bundle size | main.js < 500KB gzipped | Warn (non-blocking) |
| **G6** | Playwright e2e | exit code 0 | Fail pipeline (prod only) |
| **G7** | Azure SWA deploy | exit code 0, URL accessible | Fail pipeline |

### GitHub Actions Test Matrix (Optional)

```yaml
strategy:
  matrix:
    node-version: [18.19.0, 20.11.0]
    os: [ubuntu-latest, windows-latest]
```

**Recommendation**: Start with single config (`ubuntu-latest`, `node 18.19.0`) until client is stable.

---

## 5. Angular + Vite Caveats & Workarounds

### Known Issues & Solutions

#### Issue 1: TestBed + zone.js in Vitest

**Problem**: Vitest ESM-only mode conflicts with Angular's `zone.js` patches.
**Symptom**: `TestBed.configureTestingModule()` throws `zone is not defined`.
**Solution**: **Use Jest instead** (see C5). Jest + `jest-preset-angular` has proven zone.js compatibility.

**Alternative** (if Vitest required):
```typescript
// vitest.config.ts (NOT RECOMMENDED)
import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['src/test-setup.ts'],
    include: ['src/**/*.spec.ts'],
  },
});
```

```typescript
// src/test-setup.ts
import 'zone.js';
import 'zone.js/testing';
import { getTestBed } from '@angular/core/testing';
import { BrowserDynamicTestingModule, platformBrowserDynamicTesting } from '@angular/platform-browser-dynamic/testing';

getTestBed().initTestEnvironment(
  BrowserDynamicTestingModule,
  platformBrowserDynamicTesting()
);
```

**Status**: Experimental. Jest is production-ready.

---

#### Issue 2: CommonJS Dependencies

**Problem**: Some npm packages (e.g., `lodash`, older Material libs) are CommonJS-only.
**Symptom**: Vite dev server errors: `require is not defined`.
**Solution**: Add to `vite.config.ts` `optimizeDeps.include`:

```typescript
optimizeDeps: {
  include: ['lodash-es'],  // Use ESM version
  exclude: ['lodash'],     // Exclude CommonJS version
}
```

**Check package module format**:
```bash
npm show lodash-es | grep module  # ESM = has "module" field
npm show lodash | grep module     # CommonJS = no "module" field
```

---

#### Issue 3: Angular Material Theming in Vite

**Problem**: SCSS `@use` imports for Material themes may fail.
**Solution**: Configure SCSS in `angular.json`:

```json
{
  "projects": {
    "Innoventity.Client": {
      "architect": {
        "build": {
          "options": {
            "styles": [
              "src/styles.scss"
            ],
            "stylePreprocessorOptions": {
              "includePaths": ["node_modules"]
            }
          }
        }
      }
    }
  }
}
```

**In `styles.scss`**:
```scss
@use '@angular/material' as mat;
@include mat.core();

$theme: mat.define-theme((
  color: (
    theme-type: light,
    primary: mat.$azure-palette,
  ),
));

@include mat.all-component-themes($theme);
```

---

#### Issue 4: OpenAPI Generator Output Compatibility

**Problem**: Generated TypeScript client may have Angular-incompatible imports.
**Solution**: Use `typescript-angular` generator (not `typescript-fetch`):

```bash
openapi-generator-cli generate \
  -g typescript-angular \
  --additional-properties=ngVersion=19,supportsES6=true
```

**Post-generation fixes** (if needed):
- Replace `rxjs/operators` imports with `rxjs`
- Update `HttpClient` imports to `@angular/common/http`

---

## 6. Production Readiness Checklist

### Pre-Deployment Validation

- [ ] **C1**: Angular 19 app created with Vite builder
- [ ] **C2**: `package.json` scripts configured (build, test, api:generate)
- [ ] **C3**: Proxy config for local dev (`proxy.conf.json`)
- [ ] **C4**: TypeScript ESM config (`tsconfig.json` module: ES2022)
- [ ] **C5**: Jest configured (NOT Vitest)
- [ ] **C6**: Azure SWA config (`staticwebapp.config.json`)
- [ ] **C7**: GitHub Actions workflow (`client-deploy.yml`)
- [ ] **C8**: Environment variables (API base URL)
- [ ] **Secrets**: `AZURE_STATIC_WEB_APPS_API_TOKEN` added to GitHub
- [ ] **CORS**: Backend allows `http://localhost:4200` (dev) + SWA URL (prod)

### Post-Deployment Validation (Smoke Tests)

**Manual Tests** (run after first deployment):

1. **SPA Routing**:
   ```bash
   # Verify Angular handles routing (not 404)
   curl -I https://your-app.azurestaticapps.net/innovations
   # Expected: 200 OK (not 404)
   ```

2. **API Integration**:
   - Open DevTools → Network
   - Navigate to login page
   - Submit login form
   - Verify: `POST https://innoventity-dev-api.azurewebsites.net/api/auth/login`
   - Check CORS headers: `Access-Control-Allow-Origin` present

3. **Asset Loading**:
   ```bash
   # Verify hashed assets cached
   curl -I https://your-app.azurestaticapps.net/assets/logo.png
   # Expected: cache-control: public, max-age=31536000
   ```

4. **Health Endpoint** (optional):
   - Create `src/health.html` with `{ "status": "ok" }`
   - Deploy
   - Test: `curl https://your-app.azurestaticapps.net/health.html`

**Automated Smoke Test** (add to workflow):
```yaml
- name: Smoke test deployed app
  run: |
    sleep 30  # Wait for CDN propagation
    curl -f https://your-app.azurestaticapps.net || exit 1
    curl -f https://your-app.azurestaticapps.net/health.html || exit 1
```

---

## 7. Effort Estimates & Priority Ordering

| Task ID | Task | Effort | Priority | Dependencies | Est. Hours |
|---------|------|--------|----------|--------------|------------|
| **C1** | Create Angular 19 app | MEDIUM | P0 | None | 2-3 |
| **C2** | Configure package.json | SMALL | P0 | C1 | 0.5 |
| **C3** | Proxy config | SMALL | P0 | C1 | 0.25 |
| **C4** | tsconfig ESM | SMALL | P0 | C1 | 0.25 |
| **C5** | Jest config | SMALL | P0 | C1 | 0.5 |
| **C6** | SWA config | SMALL | P0 | C1 | 0.5 |
| **C7** | GitHub Actions workflow | MEDIUM | P0 | C1-C6 | 1 |
| **C8** | Environment variables | SMALL | P0 | C1 | 0.5 |
| **R1** | Vite config | SMALL | P1 | C1 | 0.5 |
| **R2** | Bundle analyzer | SMALL | P2 | C1 | 0.25 |
| **R3** | Playwright E2E | MEDIUM | P2 | C1-C8 | 1 |
| **R4** | Pre-commit hooks | SMALL | P2 | C1 | 0.5 |
| **O1** | PWA support | SMALL | P3 | C1-C8 | 1 |
| **O2** | App Insights | SMALL | P3 | C1 | 0.5 |

**Total Estimated Effort**: 10-12 hours

**Recommended Implementation Order**:
1. **Phase 1** (3-4 hours): C1 → C2 → C3 → C4 → C5 (local dev working)
2. **Phase 2** (2-3 hours): C6 → C7 → C8 (CI/CD pipeline deployed)
3. **Phase 3** (2-3 hours): R1 → R2 → R3 (optimization + E2E tests)
4. **Phase 4** (3-4 hours): R4 → O1 → O2 (quality + enhancements)

---

## 8. Example Patch Diffs (Non-Invasive)

### Patch 1: Add Backend CORS for SWA

**File**: `src/Innoventity.API/Program.cs`
**Action**: Add Azure Static Web Apps URL to allowed origins

```diff
 builder.Services.AddCors(options =>
 {
     options.AddPolicy("AllowFrontend", policy =>
     {
-        policy.WithOrigins("http://localhost:4200")
+        policy.WithOrigins(
+            "http://localhost:4200",
+            "https://innoventity-client.azurestaticapps.net"  // Update with actual SWA URL
+        )
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
     });
 });
```

**Deployment Note**: Final SWA URL won't be known until Azure resource created. Update after first deployment.

---

### Patch 2: Add npm Scripts (if missing)

**File**: `src/Innoventity.Client/package.json`

```diff
 {
   "scripts": {
     "ng": "ng",
     "start": "ng serve",
     "build": "ng build",
+    "prebuild": "npm run api:generate",
+    "build:prod": "ng build --configuration production",
+    "preview": "vite preview --outDir dist/innoventity.client",
     "test": "ng test",
+    "test:coverage": "jest --coverage",
+    "lint": "ng lint",
+    "e2e": "playwright test",
+    "api:generate": "openapi-generator-cli generate -i ../../specs/001-platform-core/contracts/openapi.yaml -g typescript-angular -o src/app/core/api-client --additional-properties=ngVersion=19"
   }
 }
```

---

### Patch 3: Update .gitignore for Client

**File**: `.gitignore` (repository root)

```diff
 # Node
 node_modules/
 npm-debug.log
+
+# Angular
+src/Innoventity.Client/dist/
+src/Innoventity.Client/.angular/
+src/Innoventity.Client/coverage/
+src/Innoventity.Client/src/app/core/api-client/
+
+# Playwright
+src/Innoventity.Client/test-results/
+src/Innoventity.Client/playwright-report/
```

---

## 9. Secrets Management

### Required GitHub Secrets

| Secret Name | Source | Purpose | Setup Instructions |
|-------------|--------|---------|-------------------|
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Azure Portal | Deploy client to SWA | 1. Create Azure Static Web App<br>2. Portal → Overview → Manage deployment token<br>3. Copy token<br>4. GitHub → Settings → Secrets → New secret |

### Obtaining Azure SWA Deployment Token

**Via Azure Portal**:
1. Navigate to Azure Portal → Static Web Apps
2. Click "Create" → Configure:
   - **Name**: `innoventity-client`
   - **Region**: Same as API (e.g., East US)
   - **Plan**: Free
   - **Deployment source**: Skip (manual setup)
3. After creation, go to Overview → "Manage deployment token"
4. Copy token and add to GitHub Secrets

**Via Azure CLI**:
```bash
az staticwebapp create \
  --name innoventity-client \
  --resource-group rg-innoventity-dev \
  --location eastus \
  --sku Free

az staticwebapp secrets list \
  --name innoventity-client \
  --query "properties.apiKey" \
  --output tsv
```

---

## 10. Information Missing — Verify Before Implementation

### Items Requiring Clarification

1. **✅ VERIFIED**: Backend API URL
   - **Dev**: `https://innoventity-dev-api.azurewebsites.net`
   - **Prod**: TBD (update `environment.production.ts` when known)

2. **⚠️ UNKNOWN**: Azure Static Web App resource location
   - **Recommendation**: Create in same region as backend API for latency
   - **Evidence needed**: Terraform module or manual creation confirmation

3. **⚠️ UNKNOWN**: Azure subscription limits
   - **Check**: Free tier SWA allows 100GB bandwidth/month
   - **Evidence needed**: Current subscription usage (run `az consumption usage list`)

4. **✅ VERIFIED**: OpenAPI spec location
   - **Path**: `specs/001-platform-core/contracts/openapi.yaml`
   - **Version**: 3.0.0 (14 endpoints documented)

5. **⚠️ UNKNOWN**: Custom domain for production
   - **Current**: `*.azurestaticapps.net` subdomain
   - **Future**: Custom domain (e.g., `app.innoventity.com`)
   - **Evidence needed**: DNS provider and SSL certificate strategy

6. **✅ VERIFIED**: Node.js version requirements
   - **Specified**: 18.19.0 (LTS)
   - **Package.json engine**: Add to `package.json` (see C2)

---

## 11. Deployment Architecture Diagram (ASCII)

```
┌────────────────────────────────────────────────────────────────────┐
│                        DEVELOPER WORKFLOW                          │
└────────────────────────────────────────────────────────────────────┘
                                   │
                    1. Push code to GitHub
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────┐
│                      GITHUB ACTIONS PIPELINE                       │
├────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────────┐       ┌──────────────────┐                  │
│  │  Client Pipeline │       │   API Pipeline   │                  │
│  │ (client-deploy)  │       │    (deploy)      │                  │
│  ├──────────────────┤       ├──────────────────┤                  │
│  │ 1. npm ci        │       │ 1. dotnet restore│                  │
│  │ 2. npm run       │       │ 2. dotnet build  │                  │
│  │    api:generate  │       │ 3. dotnet test   │                  │
│  │ 3. npm run lint  │       │ 4. dotnet publish│                  │
│  │ 4. npm test      │       │ 5. az webapp     │                  │
│  │ 5. npm run       │       │    deploy        │                  │
│  │    build:prod    │       └────────┬─────────┘                  │
│  │ 6. SWA deploy    │                │                             │
│  └────────┬─────────┘                │                             │
│           │                          │                             │
└───────────┼──────────────────────────┼─────────────────────────────┘
            │                          │
            │                          │
            ▼                          ▼
┌─────────────────────┐     ┌────────────────────────┐
│ Azure Static Web    │     │ Azure App Service      │
│ Apps (SWA)          │     │ (API Backend)          │
├─────────────────────┤     ├────────────────────────┤
│ - Global CDN        │     │ - .NET 8 Runtime       │
│ - TLS/HTTPS auto    │◄────┤ - Azure SQL Database   │
│ - SPA routing       │ API │ - JWT authentication   │
│ - Static assets     │calls│ - App Insights         │
│                     │     │                         │
│ Hosting:            │     │ Hosting:                │
│ dist/               │     │ Innoventity.API.dll     │
│ innoventity.client/ │     │                         │
└─────────────────────┘     └────────────────────────┘
            │                          │
            │                          │
            └──────────┬───────────────┘
                       │
                       ▼
            ┌────────────────────┐
            │   END USER BROWSER │
            ├────────────────────┤
            │ 1. Loads Angular   │
            │    SPA from SWA    │
            │ 2. SPA calls API   │
            │    for data        │
            │ 3. JWT stored in   │
            │    localStorage    │
            └────────────────────┘
```

---

## 12. Final Recommendations

### Immediate Actions (Start Here)

1. **Create Angular 19 app** (C1): Run `npx @angular/cli@19 new` per T071
2. **Set up Azure Static Web App**: Create resource in Azure Portal
3. **Add GitHub Secret**: Copy SWA deployment token to `AZURE_STATIC_WEB_APPS_API_TOKEN`
4. **Implement configurations** (C2-C8): Copy configs from this document
5. **Deploy first version**: Push to trigger `client-deploy.yml` workflow

### Risk Mitigation Strategy

- **Start with minimal config**: Implement C1-C8 first (core functionality)
- **Test locally before CI**: Verify `npm run build:prod` works before pushing
- **Monitor bundle size**: Run `npm run analyze` after first build
- **Staged rollout**: Deploy to dev environment first, then production

### Success Metrics (30-day post-deployment)

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Build Success Rate** | ≥95% | GitHub Actions insights |
| **Deployment Time** | <10 minutes | Workflow run duration |
| **Bundle Size** | <500KB gzipped | Vite build output |
| **Lighthouse Score** | ≥90 | Chrome DevTools |
| **P95 Load Time** | <3 seconds | Azure SWA analytics |
| **CORS Errors** | 0 | Browser DevTools |

---

## 13. Conclusion

### Readiness Assessment: **NOT READY** (Expected State)

The repository has excellent backend infrastructure and comprehensive Angular 19 specifications, but the **frontend client does not exist yet**. This is intentional — tasks T071-T075 are the implementation phase.

### Blockers Identified

1. ✅ **RESOLVED**: Angular 19 migration complete (spec updated April 5, 2026)
2. ❌ **BLOCKING**: Client codebase creation (T071)
3. ❌ **BLOCKING**: Azure Static Web Apps provisioning
4. ❌ **BLOCKING**: GitHub Actions client deployment workflow

### Remediation Path

**Phase 1 (T071-T074)**: Implement client application (4-6 hours)
- Follow C1-C8 configurations from this review
- Result: Local dev server running, API integration working

**Phase 2 (T075 + CI)**: Deploy to Azure (2-4 hours)
- Create Azure SWA resource
- Configure GitHub Actions workflow
- Result: Production deployment live

**Phase 3 (Hardening)**: Testing & optimization (3-5 hours)
- Playwright E2E tests
- Bundle size optimization
- Result: Production-ready with quality gates

### Total Estimated Timeline: 10-15 hours

---

## Appendix A: Quick Start Commands

```bash
# 1. Create Angular 19 app
cd src
npx @angular/cli@19 new Innoventity.Client \
  --routing=true \
  --style=scss \
  --skip-git=true \
  --standalone=true \
  --ssr=false

# 2. Install dependencies
cd Innoventity.Client
npm install @angular/material@19 @openapitools/openapi-generator-cli jest jest-preset-angular @playwright/test

# 3. Generate API client
npm run api:generate

# 4. Start dev server
npm start
# Navigate to http://localhost:4200

# 5. Build for production
npm run build:prod
# Output: dist/innoventity.client/

# 6. Deploy to Azure SWA (after workflow configured)
git add .
git commit -m "feat: initialize Angular 19 client with Vite (T071)"
git push origin 001-platform-core
```

---

## Appendix B: Troubleshooting Common Issues

### Issue: CORS errors in dev

**Symptom**: `Access-Control-Allow-Origin` error when calling API
**Fix**: Verify `proxy.conf.json` configured and `ng serve --proxy-config proxy.conf.json`

### Issue: API client generation fails

**Symptom**: `npm run api:generate` throws error
**Fix**: Check OpenAPI spec validity: `npx swagger-cli validate ../../specs/001-platform-core/contracts/openapi.yaml`

### Issue: Build fails with zone.js error

**Symptom**: `zone is not defined` during `npm run build`
**Fix**: Verify `zone.js` imported in `src/main.ts`: `import 'zone.js';`

### Issue: SWA deployment succeeds but app shows blank page

**Symptom**: Deployment logs show success, but URL shows empty page
**Fix**: Check `output_location` in workflow matches build output directory (`dist/innoventity.client`)

---

**END OF REVIEW**

Document Version: 1.0
Next Review: After T071 implementation (client exists)
