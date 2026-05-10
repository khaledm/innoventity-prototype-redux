# innoventity-prototype-redux Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-02-21

## Active Technologies
- C# 12 / .NET 8 (backend)
- TypeScript 5.x / Angular 19 (frontend)
- TypeScript 5.7.2, Angular 19.2.0, Node.js 18.x/20.x LTS (GitHub Actions hosted runners) (002-frontend-cicd)
- Azure Static Web Apps (global CDN-backed static hosting), Azure Blob Storage (Terraform state), GitHub Actions artifacts (build outputs, 90-day retention) (002-frontend-cicd)

## Project Structure

```text
src/Innoventity.API/      (C# .NET 8 backend)
src/Innoventity.Client/   (Angular 19 frontend)
tests/                    (xUnit integration + E2E tests)
infrastructure/           (Terraform IaC + Pester/Terratest)
```

## Commands

npm test; npm run lint

## Code Style

C# 12 / .NET 8 (backend), TypeScript 5.x / Angular 19 (frontend): Follow standard conventions

## Recent Changes
- 002-frontend-cicd: Added TypeScript 5.7.2, Angular 19.2.0, Node.js 18.x/20.x LTS (GitHub Actions hosted runners)
- 001-platform-core: Added C# 12 / .NET 8 (backend), TypeScript 5.x / Angular 19 (frontend)

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
