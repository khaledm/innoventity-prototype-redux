# Phase 0 Research: Platform Core Technical Decisions

**Feature**: Platform Core (v1.0)  
**Date**: February 8, 2026  
**Status**: Complete (No unknowns - decisions from constitution)

---

## Overview

This research phase consolidates technical decisions already established in the project constitution (v1.0.0, ratified February 8, 2026). No unknowns require investigation—all technology choices, architectural patterns, and quality standards are pre-defined to support the learning objectives of this solo portfolio project.

---

## Decision 1: Backend Technology Stack

**Decision**: ASP.NET Core 8 with Minimal APIs + EF Core 8

**Rationale**:
- **Learning Objective**: Mastering modern .NET development is a primary project goal (per Constitution Constraint 3)
- **Production-Ready**: ASP.NET Core 8 LTS provides stable, performant, well-documented framework
- **Minimal APIs**: Lightweight, testable, aligns with Principle 3 (Simplicity Over Cleverness)
- **EF Core 8**: Modern ORM with excellent Azure SQL integration, migration support
- **Ecosystem**: Rich testing tools (xUnit, WebApplicationFactory), Azure deployment tooling

**Alternatives Considered**:
1. **Node.js + Express**: Rejected - learning objective is .NET mastery
2. **ASP.NET MVC Controllers**: Rejected - Minimal APIs simpler, more modern
3. **Dapper (micro-ORM)**: Rejected - EF Core migrations critical for iterative development

**Best Practices Applied**:
- Feature-based organization (Vertical Slice Architecture)
- Repository abstraction for v2.0 extensibility
- Dependency injection built-in
- Built-in support for health checks, logging, OpenAPI

---

## Decision 2: Frontend Technology Stack

**Decision**: Angular v18 with Standalone Components + Signals

**Rationale**:
- **Learning Objective**: Angular v18 is explicitly mentioned in Constitution Constraint 3
- **Microsoft Template**: Use `dotnet new angular` template for structure consistency
- **Standalone Components**: Modern Angular pattern (no NgModules), simpler mental model
- **Signals**: Reactive state management with fine-grained updates, better performance
- **TypeScript**: Strong typing improves maintainability, refactoring safety

**Alternatives Considered**:
1. **React**: Rejected - learning objective is Angular mastery
2. **Vue.js**: Rejected - same reason as React
3. **Blazor**: Rejected - want full-stack .NET+JS experience

**Best Practices Applied**:
- Feature-based folder structure (auth, innovations, bids, etc.)
- Angular Material for consistent, accessible UI components
- RxJS for async operations (HTTP calls, real-time notifications)
- Environment-specific configuration

---

## Decision 3: Authentication & Authorization

**Decision**: JWT-based authentication with policy-based authorization

**Rationale**:
- **Stateless**: JWT tokens enable horizontal scaling (Azure App Service)
- **Token Expiry**: 1hr access + 7d refresh balances security and UX (per Constitution Section 5)
- **Policy-Based Auth**: Composable, testable, extensible for v2.0 (organization policies)
- **Password Security**: BCrypt hashing (per Constitution Section 5 - Security Standards)

**Implementation Details**:
- ASP.NET Core Authentication middleware (JWT Bearer)
- Authorization policies:
  - `IAuthorizationRequirement` implementations for resource ownership
  - `IAuthorizationHandler` for policy evaluation
  - Example: `InnovationOwnerRequirement` validates user owns innovation before edit/selection

**Alternatives Considered**:
1. **Cookie-based auth**: Rejected - complicates SPA architecture, CORS issues
2. **OAuth2/OIDC**: Rejected - **added complexity for v1.0** (defer to v2.0 if external auth needed)
3. **Azure AD B2C**: Rejected - same reason as OAuth2

---

## Decision 4: Database & Persistence

**Decision**: Azure SQL Database with EF Core 8 Code-First

**Rationale**:
- **No Data Migration**: Fresh schema optimized for v1.0 (per Constitution Constraint 2)
- **Code-First**: Migrations enable iterative schema evolution during development
- **Azure SQL**: Managed service (scaling, monitoring)
- **EF Core Abstractions**: Repository pattern enables v2.0 multi-tenancy filtering

**Schema Approach**:
- Entities map directly to specification business rules
- Inheritance for Actor types (TPH - Table Per Hierarchy for simplicity)
- Value objects for immutable domain concepts (e.g., IPRStatus, ResearchCategory)
- Audit fields (CreatedAt, UpdatedAt) on all entities

**Alternatives Considered**:
1. **PostgreSQL**: Rejected - Azure SQL better integrated with Azure ecosystem
2. **Cosmos DB**: Rejected - relational data model fits SQL better, complex querying
3. **SQLite**: Rejected - not production-suitable for Azure deployment

---

## Decision 5: Testing Strategy

**Decision**: Multi-layered testing with TDD workflow + mutation testing

**Test Pyramid**:
1. **Unit Tests**: xUnit for business logic, domain entities, value objects
   - Target: >80% coverage (Constitution Section 5)
   - Test data builders for readable setup
   - Focus: Business rules, validation, state transitions

2. **Integration Tests**: WebApplicationFactory for HTTP endpoint testing
   - Target: 100% endpoint coverage (Constitution Section 5)
   - In-memory or test database (separate from dev database)
   - Focus: Request/response shapes, authorization, error handling

3. **End-to-End Tests**: Playwright for critical user journeys
   - Target: P0 journeys (J1-J3: innovation submission, bidding, partner selection)
   - Headless execution in CI/CD, headed for debugging
   - Focus: User workflows, UI interactions, multi-step processes

4. **Mutation Testing**: Stryker.NET validates test quality
   - Target: >70% mutation score (Constitution Section 5)
   - Run periodically (not every commit - performance cost)
   - Focus: Critical business logic paths

**TDD Workflow** (Constitution Principle 5):
1. **Red**: Write failing test that describes desired behavior
2. **Green**: Implement minimal code to make test pass
3. **Refactor**: Improve code while keeping tests green
4. **Validate**: Mutation testing proves tests catch bugs

**Alternatives Considered**:
1. **BDD with SpecFlow**: Rejected - Gherkin tests in specification are sufficient, adds tooling complexity
2. **Jest for .NET**: Rejected - xUnit is standard, well-integrated with Visual Studio/.NET CLI

---

## Decision 6: Architecture Pattern

**Decision**: Vertical Slice Architecture

**Rationale**:
- **Feature Cohesion**: Each feature (auth, innovations, bids) contains its own endpoints, logic, data access  
- **Reduced Coupling**: Changes to one feature don't ripple through shared layers
- **Spec-Kit Alignment**: One specification often maps to one or more vertical slices
- **Simplicity**: Easier to navigate than traditional layered architecture (per Principle 3)
- **Solo Development**: Reduces cognitive load (entire feature in one place)

**Structure**:
```
src/Innoventity.API/Features/
├── Authentication/
│   ├── Register.cs              # Endpoint + handler + validation + persistence
│   ├── Login.cs
│   └── Activate.cs
├── Innovations/
│   ├── CreateDraft.cs
│   ├── SubmitForPublication.cs
│   ├── GetById.cs               # Phase 0 first slice
│   └── GetByIndustry.cs
```

**Shared Concerns** (extracted when truly shared):
- Domain entities (Innovation, Actor, Bid) in `Domain/Entities/`
- Repository abstractions (`IInnovationRepository`) in `Domain/Interfaces/`
- Infrastructure (DbContext, JWT generator) in `Infrastructure/`

**Alternatives Considered**:
1. **Clean Architecture (Onion)**: Rejected - over-engineered for this project, violates Principle 3
2. **Traditional Layered (MVC)**: Rejected - high coupling, feature changes touch many layers
3. **Modular Monolith**: Rejected - premature for v1.0, consider for v2.0

---

## Decision 7: API Design

**Decision**: RESTful HTTP APIs documented with OpenAPI/Scalar

**Rationale**:
- **OpenAPI**: Auto-generated from endpoints (ASP.NET Core built-in support)
- **Scalar**: Modern OpenAPI UI (per Constitution Section 6 - Definition of Done)
- **REST Conventions**: Standard patterns (GET/POST/PUT/DELETE, status codes)
- **Minimal API Endpoints**: Lightweight, functional style, easy to test

**API Conventions**:
- **/api/auth/**: Authentication endpoints (register, login, activate, refresh-token)
- **/api/innovations/<id>**: Innovation CRUD and lifecycle operations  
- **/api/innovations/<id>/bids**: Bid submission and management
- **/api/innovations/<id>/select-partners**: Partner selection (POST, irreversible)
- **/api/incubator/<innovation-id>/**: Virtual incubator workspace operations

**Response Patterns**:
- Success: 200 OK (query), 201 Created (create), 204 No Content (delete)
- Validation Error: 400 Bad Request with problem details (RFC 7807)
- Auth Error: 401 Unauthorized (not authenticated), 403 Forbidden (not authorized)
- Not Found: 404 Not Found
- Server Error: 500 Internal Server Error (logged, monitored)

**Alternatives Considered**:
1. **GraphQL**: Rejected - added complexity, REST sufficient for v1.0
2. **gRPC**: Rejected - overkill for web application, tooling complexity
3. **OData**: Rejected - over-engineered querying, violates Principle 3

---

## Decision 8: Deployment & Hosting

**Decision**: Azure App Service (Linux) with Azure SQL Database

**Rationale**:
- **Managed Platform**: No infrastructure management (focus on code)
- **Azure Ecosystem**: Integrated with Application Insights, Service Bus
- **Staging Slots**: Blue/green deployment for zero-downtime releases
- **Auto-Scaling**: Horizontal scaling based on load (future optimization)
- **Linux Containers**: Cost-effective, .NET 8 runs natively on Linux

**Supporting Services**:
- **Azure SQL Database**: Managed relational database
- **Azure Application Insights**: Monitoring, telemetry, logging
- **Azure Service Bus**: Async messaging (email notifications, future event-driven features)
- **Azure Blob Storage**: Document storage (future v1.0 feature)

**CI/CD**:
- GitHub Actions for build, test, deploy pipeline
- Automated deployment to staging on PR merge
- Manual promotion to production (Constitution Section 7 - Launch Criteria)

**Alternatives Considered**:
1. **Azure Kubernetes Service (AKS)**: Rejected - over-engineered for v1.0 traffic, high complexity
2. **Azure Container Apps**: Rejected - App Service simpler, sufficient for v1.0
3. **VM-based hosting**: Rejected - requires infrastructure management, violates Constraint 1

---

## Decision 9: Observability & Monitoring

**Decision**: Azure Application Insights OTEL) + Structured Logging

**Rationale**:
- **Application Insights**: Automatic telemetry collection, dashboards, alerting
- **OpenTelemetry**: Industry-standard instrumentation (future vendor flexibility)
- **Structured Logging**: Serilog with JSON output (queryable, filterable)
- **Log Levels**: Debug (dev), Information (important events), Warning (recoverable), Error (failures)

**Instrumentation**:
- HTTP request/response logging (duration, status code, user ID)
- Database query performance (EF Core logging)
- Business event tracking (innovation submitted, bid submitted, partners selected)
- Exception tracking with stack traces

**Alerting** (per Constitution Section 7):
- API error rate >1% → alert
- API p95 latency >200ms → warning
- Database connection failures → immediate alert
- Authentication failures spike → security alert

**Alternatives Considered**:
1. **ELK Stack (Elasticsearch, Logstash, Kibana)**: Rejected - complexity, cost, Azure AI simpler
2. **Prometheus + Grafana**: Rejected - requires infrastructure setup, Azure AI managed

---

## Decision 10: Phase 0 Minimal Slice

**Decision**: Registration + Authentication + View Single Innovation

**Scope** (per Specification Section 6):
1. User can register as Idea Generator
2. User receives activation email and activates account
3. User logs in with JWT authentication
4. Authenticated user can retrieve innovation by ID

**Rationale**:
- **End-to-End Proof**: Validates entire stack (frontend → API → auth → database → response)
- **Foundation**: All features build on auth + database + API patterns
- **1-2 Week Target**: Realistic for solo developer, fits Principle 3 (Simplicity)
- **Testable**: Unit, integration, E2E tests prove TDD workflow

**What Phase 0 Proves**:
- ✅ ASP.NET Core API responding to HTTP requests
- ✅ EF Core database connectivity and queries
- ✅ JWT authentication working end-to-end
- ✅ Angular frontend calling backend APIs
- ✅ Deployment to Azure successful
- ✅ Monitoring/observability operational
- ✅ Test automation pipeline working

---

## Technology Matrix Summary

| Category | Technology | Version | Rationale |
|----------|------------|---------|-----------|
| **Backend Language** | C# | 12 | Modern .NET features, learning objective |
| **Backend Framework** | ASPNET Core | 8 (LTS) | Minimal APIs, performance, ecosystem |
| **ORM** | EF Core | 8 | Code-first migrations, Azure SQL integration |
| **Frontend Language** | TypeScript | 5.x | Strong typing, refactoring safety |
| **Frontend Framework** | Angular | 18 | Standalone components, signals, learning objective |
| **UI Library** | Angular Material | 18 | Consistent, accessible components |
| **Database** | Azure SQL | - | Managed, scalable, relational model fit |
| **Authentication** | JWT | - | Stateless, scalable, standard |
| **Password Hashing** | BCrypt | - | Strong hashing, constitution requirement |
| **Unit Testing** | xUnit | Latest | .NET standard, Visual Studio integrated |
| **E2E Testing** | Playwright | Latest | Cross-browser, modern, reliable |
| **Mutation Testing** | Stryker.NET | Latest | Test quality validation |
| **API Docs** | OpenAPI + Scalar** | - | Auto-generated, interactive, modern UI |
| **Hosting** | Azure App Service | Linux | Managed, integrated with Azure services |
| **Monitoring** | Azure Application Insights | - | Telemetry, logging, alerting |
| **Messaging** | Azure Service Bus | - | Async patterns, email notifications |

---

## Next Steps

**Phase 0 Complete** - All technical decisions documented. Proceed to **Phase 1: Design & Contracts**.

**Phase 1 Outputs**:
1. `data-model.md` - Entity relationship diagram, domain model
2. `contracts/` - OpenAPI specifications for Phase 0 endpoints
3. `quickstart.md` - Developer setup and onboarding guide

---

**END OF RESEARCH PHASE**
