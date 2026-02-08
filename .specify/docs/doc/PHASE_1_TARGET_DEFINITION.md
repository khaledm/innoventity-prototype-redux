# PHASE 1 — TARGET DEFINITION
## Define the Target Architecture (Before Writing Code)

**Document Version**: 2.0  
**Date**: February 8, 2026  
**Status**: ✅ ALL DECISIONS RESOLVED - Ready for Implementation  
**Prerequisites**: Phase 0 Orientation Complete (Inventory + Legacy Freeze)  
**Project Type**: 🎯 Solo Learning/Portfolio Project - Reference-Based Reimplementation

---

## 🔴 CRITICAL PROJECT CONTEXT

### Project Nature
**This is a SOLO LEARNING PROJECT** with the following characteristics:
- **Primary Goal**: Skill development in modern .NET + Angular stack
- **Secondary Goal**: Portfolio showcase piece demonstrating best practices
- **Timeline**: 10 months (40 weeks) - flexible for learning
- **Decision Authority**: Project owner has full authority (no committee approvals)
- **Scope**: v1.0 = Open Innovation only (simplified to focus learning)

### Critical Discovery: No Data Migration Needed 🎉
**The legacy MVC application is DORMANT with NO operational data.**

**This changes everything**:
- ❌ NOT a migration project (no data to migrate)
- ✅ IS a reference-based reimplementation
- ✅ Fresh modern database schema from day one
- ✅ No dual-write, no synchronization, no cutover complexity
- ✅ Legacy codebase = Domain knowledge reference + Business rules documentation
- ✅ Freedom to fix legacy naming (IdeaAuthor → IdeaGenerator)
- ✅ Seed data approach for master data and demos

### v1.0 Scope Decisions
**Strategic simplification for focused learning**:
- ✅ **Included**: Open Innovation mode (5 actor types)
- 📋 **Deferred to v2.0**: Closed/Hybrid modes, multi-tenancy, organizations
- ✅ **Actor Types**: IdeaGenerator, RDOrganization, Manufacturer, SalesMarketing, Investor
- 📋 **Deferred Actors**: Government/Technology Parks
- ✅ **Research Categories**: Simple enum (Management/Engineering/NaturalScience) - no workflow differences
- ✅ **Database**: Modern schema with optimized naming
- ✅ **Testing**: Automation from day one (learning opportunity)

---

## PURPOSE

This document defines **concrete architectural decisions** for the Innoventity re-engineering project. These decisions are binding for the duration of Phase 0-5 (foundation through launch).

**All critical decisions have been resolved. No open questions remain.**

---

## STEP 1.1 — CONCRETE STACK DECISIONS

### Backend Stack (ASP.NET Core 8)

| Component | Technology | Version | Rationale |
|-----------|-----------|---------|-----------|
| **Framework** | ASP.NET Core | 8.0 LTS | Long-term support until Nov 2026, proven stability |
| **API Style** | Minimal APIs | Built-in | Simpler than controllers, perfect for vertical slices |
| **ORM (Writes)** | Entity Framework Core | 8.0 | Modern, async, migrations, good for commands |
| **ORM (Reads)** | Dapper | 2.1+ | Raw SQL performance for queries, supplements EF |
| **Validation** | FluentValidation | 11.9+ | Expressive, testable, separation from domain |
| **Messaging** | Azure Service Bus | Latest | Keep existing integration, proven reliable |
| **Authentication** | ASP.NET Core Identity | 8.0 | Built-in, JWT support, role management |
| **Authorization** | Policy-Based Auth | Built-in | Flexible, declarative, testable |
| **Logging** | Serilog | 3.1+ | Structured logging, multiple sinks |
| **Observability** | OpenTelemetry | 1.7+ | Metrics + tracing, export to App Insights |
| **API Documentation** | Scalar UI + ASP.NET Core OpenAPI | Latest | Modern interactive OpenAPI docs (replaces Swashbuckle) |
| **Testing** | xUnit + WebApplicationFactory | Latest | Integration tests with in-memory host |
| **Containerization** | Docker | Latest | Local dev + production deployment |

**No alternatives being evaluated. This is the stack.**

#### Why Scalar for API Documentation?

**Scalar** (https://scalar.com/) is an open-source, modern alternative to Swagger UI for visualizing OpenAPI specifications:

**Benefits over Swashbuckle/Swagger UI**:
- **Better Developer Experience**: Modern, responsive design with improved readability
- **Interactive Testing**: Built-in API testing with request/response examples
- **Native ASP.NET Core Integration**: Works seamlessly with ASP.NET Core's built-in OpenAPI support (introduced in .NET 8+)
- **Zero Configuration**: No need for Swashbuckle middleware, uses standard OpenAPI endpoint
- **Open Source**: MIT licensed, actively maintained, community-driven
- **Performance**: Lightweight, fast rendering of large API specifications

**Migration Path**: ASP.NET Core 8+ has built-in OpenAPI document generation. Scalar consumes this standard OpenAPI spec, making it a drop-in replacement for Swagger UI without changing the API contract.

**Portfolio Value**: Demonstrates awareness of modern API documentation tools and ability to evaluate alternatives beyond defaults.

### Frontend Stack (Angular 18)

| Component | Technology | Version | Rationale |
|-----------|-----------|---------|-----------|
| **Framework** | Angular | 18+ | Standalone components, signals, modern DX |
| **Build System** | esbuild (via Angular CLI) | Built-in | Fast builds, HMR, tree-shaking |
| **UI Components** | Angular Material | 18+ | Consistent design system, accessibility |
| **Forms** | Reactive Forms | Built-in | Type-safe, testable, validation control |
| **State Management** | NgRx | 18+ | For complex state (collab workspace), signals for simple state |
| **HTTP Client** | Angular HttpClient | Built-in | Typed, interceptors, observables |
| **API Client Generation** | OpenAPI Generator | 7.3+ | Generate TypeScript clients from Swagger spec |
| **Real-time** | @microsoft/signalr | 8.0+ | WebSocket fallback, reconnection logic |
| **Testing** | Jest + Testing Library | Latest | Fast, component testing |
| **E2E Testing** | Playwright | 1.41+ | Cross-browser, reliable selectors |
| **Routing** | Angular Router | Built-in | Lazy loading, guards |
| **Internationalization** | @angular/localize | Built-in | Multi-language support (future) |

**No alternatives being evaluated. This is the stack.**

### Infrastructure Stack

| Component | Technology | Rationale |
|-----------|-----------|-----------|
| **Database** | Azure SQL Database | Keep existing SQL Server schema, managed service |
| **Blob Storage** | Azure Blob Storage | Documents, business plans, images |
| **Message Bus** | Azure Service Bus | Keep existing integration |
| **Caching** | Azure Redis Cache | Session data, query cache |
| **CDN** | Azure CDN | Static assets, global distribution |
| **Hosting (API)** | Azure App Service (Linux) | Managed, auto-scale, deployment slots |
| **Hosting (Web)** | Azure Static Web Apps | CDN, auto-deploy, custom domains |
| **CI/CD** | GitHub Actions | Already using GitHub, free for public repos |
| **Monitoring** | Azure Application Insights | APM, logs, metrics, alerts |
| **Secrets Management** | Azure Key Vault | API keys, connection strings |
| **Container Registry** | Azure Container Registry | Docker images for API |

**Cloud Provider**: Azure (existing investments, keep it)

---

## STEP 1.2 — KEY ARCHITECTURAL PRINCIPLES

These are **non-negotiable** architectural principles for the new platform:

### Principle 1: Vertical Slice Architecture

**Statement**: Features are organized as **self-contained vertical slices**, not horizontal layers.

**What this means**:
```
✅ DO THIS:
Features/
  Innovations/
    Submit/
      SubmitInnovation.cs          # Endpoint + Handler + DTOs
      SubmitInnovationValidator.cs # Validation
      SubmitInnovation.Tests.cs    # Tests
    GetById/
      GetInnovationById.cs
      GetInnovationById.Tests.cs

❌ NOT THIS (layered):
Controllers/
  InnovationController.cs
Services/
  InnovationService.cs
Repositories/
  InnovationRepository.cs
```

**Benefits**:
- Feature changes touch one directory
- Easy to delete unused features
- Clear ownership boundaries
- Parallel team development

**Enforcement**: Code reviews reject cross-slice dependencies.

---

### Principle 2: Spec-First Development

**Statement**: Every feature starts with a **YAML specification** before any code is written.

**What this means**:
1. Write spec in `platform/specs/[feature]/[usecase].spec.yaml`
2. Generate scaffolding from spec (using Spec-Kit or templates)
3. Implement domain logic
4. Write tests matching spec scenarios
5. Deploy

**Example**:
```yaml
# specs/innovations/submit.spec.yaml
useCase:
  name: SubmitInnovation
  api:
    method: POST
    route: /api/innovations
  businessRules:
    - "Title must be unique for user"
    - "Research background minimum 100 characters"
  testScenarios:
    - name: "Valid submission succeeds"
```

**Benefits**:
- Specifications are durable (survive code rewrites)
- Clear contract for frontend/backend
- Test scenarios defined upfront
- Domain knowledge captured

**Enforcement**: PR template requires spec file link.

---

### Principle 3: Domain Logic in Domain, Not Infrastructure

**Statement**: Business rules live in **domain entities**, not in repositories, controllers, or services.

**What this means**:
```csharp
✅ DO THIS (domain entity):
public class Innovation {
    public void SelectPartners(List<Bid> selectedBids) {
        if (Status != InnovationStatus.ReceivingBids)
            throw new InvalidOperationException("Cannot select partners");
        
        if (!HasRequiredBidTypes(selectedBids))
            throw new InvalidOperationException("Missing required bid types");
        
        // Business logic here
        Status = InnovationStatus.PartnersSelected;
    }
}

❌ NOT THIS (controller/service):
public class PartnerService {
    public void SelectPartners(Innovation innovation, List<Bid> bids) {
        if (innovation.Status != InnovationStatus.ReceivingBids) { }
        // Business logic scattered across services
    }
}
```

**Benefits**:
- Business rules testable in isolation
- Domain model is self-documenting
- Easy to find where rules live
- Prevents anemic domain model

**Enforcement**: Code reviews check for business logic in services/controllers.

---

### Principle 4: API-First, UI-Second

**Statement**: The **API is the product**. The Angular SPA is one consumer among many.

**What this means**:
- API must be usable standalone (Scalar UI, Postman, or any OpenAPI client)
- API returns proper HTTP status codes (201 Created, 409 Conflict)
- API uses RFC 7807 Problem Details for errors
- API is versioned (`/api/v1/...`)
- API has OpenAPI spec (automatically generated)

**Benefits**:
- Mobile apps can consume same API
- Third-party integrations possible
- Decoupled frontend (can replace Angular later)
- Testable without UI

**Enforcement**: Every endpoint must have OpenAPI documentation (visible in Scalar UI).

---

### Principle 5: Fail Fast with Validation

**Statement**: **Validate at the boundary**. Return 400 Bad Request immediately if input is invalid.

**What this means**:
```csharp
// Request enters API
↓
1. Model binding (deserialize JSON)
2. FluentValidation (business rules)
3. If invalid → return 400 + ProblemDetails
4. If valid → execute handler
```

**No validation in domain entities** (they assume valid input).  
**No validation in database** (constraints are backup only).

**Benefits**:
- Fast feedback for API consumers
- Clear error messages
- Domain logic doesn't need defensive checks
- Validation rules testable in isolation

**Enforcement**: Every command has a validator class.

---

### Principle 6: AI-Augmented Development with Epistemic Rigor

**Statement**: AI tools (GitHub Copilot, Copilot Chat) augment capabilities but do not replace architectural thinking, domain modeling, or test-first discipline. **Tests must fail first** to have validity—LLM acceleration cannot skip red phase of TDD.

**Critical Reference**: Mark Seemann, "[AI-generated tests as ceremony](https://blog.ploeh.dk/2026/01/26/ai-generated-tests-as-ceremony/)"

**What this means**:
```csharp
✅ DO USE AI FOR:
// Scaffolding repository implementations
// Generating DTO classes from domain entities
// Creating test stubs from specifications
// Writing boilerplate validation code

❌ DO NOT USE AI FOR (Human-Critical):
// Business logic decisions (domain model design)
// Security-critical code (authorization rules)
// Architectural pattern selection
// Accepting tests that never failed
```

**Epistemic Requirements**:
- Every test must be **observed failing FIRST** or validated through characterization (deliberately break system)
- AI-generated tests undergo human review: "Can I explain what bug this catches?"
- Critical business logic (partner selection, business plans, authorization) requires human-written tests
- Coverage metrics secondary to mutation testing score (>70%)

**The Epistemic Problem**: Tests that never fail have no validity. If you generate a test after implementation and it passes immediately, you have:
1. No proof the test actually validates behavior
2. Possible tautological assertions (testing framework, not logic)
3. False confidence in test suite

**Approved Patterns**:
1. **Human-First TDD**: Human writes test → observe FAIL → LLM implements → observe PASS
2. **Spec-First with Critical Review**: Spec-Kit generates test → human validates → run test (must FAIL) → implement
3. **Characterization Testing**: LLM generates code + tests → deliberately inject bugs → verify tests detect them

**Enforcement**:
- PR template includes: "Have tests been observed failing?" (Yes/No + proof)
- Code review rejects AI tests with generic assertions ("returns 200" not sufficient)
- Definition of Done includes AI-specific quality gates

**Benefits**:
- Accelerates mechanical code (2x faster on DTOs, scaffolding)
- Maintains epistemic control (humans understand WHY tests work)
- Portfolio demonstrates disciplined AI usage (interview talking point)

**Reference**: See [`AI_ASSISTED_DEVELOPMENT_PRINCIPLES.md`](AI_ASSISTED_DEVELOPMENT_PRINCIPLES.md) for comprehensive guidance.

---

## STEP 1.3 — DEPLOYMENT MODEL

### Environments

| Environment | Purpose | URL Pattern | Data |
|-------------|---------|-------------|------|
| **Development** | Local laptop | http://localhost:4200 | Synthetic test data |
| **Staging** | Pre-production testing | https://staging.innoventity.com | Copy of production data (anonymized) |
| **Production** | Live system | https://app.innoventity.com | Real user data |

**No other environments.** No "QA environment", no "UAT environment". Keep it simple.

---

### Deployment Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Azure Front Door                      │
│              (CDN, SSL, WAF, DDoS Protection)           │
└────────────┬──────────────────────┬─────────────────────┘
             │                      │
    ┌────────▼─────────┐   ┌───────▼──────────┐
    │  Static Web App  │   │   App Service    │
    │   (Angular SPA)  │   │  (ASP.NET API)   │
    │                  │   │                  │
    │  - /            │   │  - /api/v1/*     │
    │  - /assets/*    │   │  - /health       │
    │                  │   │  - /scalar       │
    └──────────────────┘   └──────┬───────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    │                             │
           ┌────────▼─────────┐        ┌─────────▼────────┐
           │   Azure SQL      │        │  Azure Service   │
           │    Database      │        │       Bus        │
           │                  │        │                  │
           │  - Geo-replicate │        │  - Topics        │
           │  - Auto-backup   │        │  - Subscriptions │
           └──────────────────┘        └──────────────────┘
```

---

### Deployment Strategy: Blue-Green

**Why Blue-Green**: Zero-downtime deployments, instant rollback.

**How it works**:
1. **Blue** = Current production (e.g., v1.2.0)
2. **Green** = New version (e.g., v1.3.0)
3. Deploy Green to staging slot
4. Run smoke tests on Green
5. Swap slots (Green becomes production)
6. Monitor for 1 hour
7. If issues → swap back to Blue (instant rollback)
8. If stable → keep Green

**Azure Implementation**:
- App Service Deployment Slots (staging + production)
- Slot swap takes ~5 seconds
- Previous version kept in staging slot (instant rollback)

---

### CI/CD Pipeline (GitHub Actions)

**On Pull Request**:
```yaml
1. Build API (.NET)
2. Build Web (Angular)
3. Run unit tests
4. Run integration tests
5. Run linters (dotnet format, ESLint)
6. SonarCloud code quality check
7. Security scan (Dependabot)
```

**On Merge to `main`**:
```yaml
1. Run all PR checks
2. Build Docker image (API)
3. Push to Azure Container Registry
4. Deploy to Staging
5. Run E2E tests (Playwright) against Staging
6. If tests pass → Manual approval gate
7. Deploy to Production (slot swap)
8. Post-deployment smoke tests
```

**Deployment frequency**: Multiple times per day (after Phase 0 stabilizes).

---

### Database Migrations

**Strategy**: EF Core Migrations + Manual Review

**Process**:
1. Developer creates migration: `dotnet ef migrations add AddInnovationTable`
2. Migration generates SQL in `Migrations/` folder
3. **Code review includes reviewing generated SQL**
4. Staging deployment runs migrations automatically
5. **Production migrations require manual approval** (via GitHub Actions gate)
6. Rollback plan: Keep previous migration scripts

**Safety**:
- Migrations run in transaction (rollback on failure)
- Never auto-delete columns (use deprecation strategy)
- Test migrations on staging with production data copy

---

### Monitoring & Alerts

**What we monitor**:

| Metric | Threshold | Alert |
|--------|-----------|-------|
| **API Response Time (p95)** | > 500ms | Slack + Email |
| **Error Rate** | > 1% of requests | Slack + Email + SMS |
| **Database CPU** | > 80% for 5 min | Email |
| **Failed Deployments** | Any | Slack + Email |
| **Auth Failures** | > 100/min | Slack (potential attack) |

**Tools**:
- Application Insights: APM, logs, metrics
- Azure Monitor: Alerts, dashboards
- Slack integration: Real-time alerts

---

### Disaster Recovery

**RTO (Recovery Time Objective)**: 4 hours  
**RPO (Recovery Point Objective)**: 5 minutes

**Backup Strategy**:
- **Database**: Automated daily backups (Azure SQL), 30-day retention
- **Blobs**: Geo-redundant storage (GRS), automatic replication
- **Secrets**: Azure Key Vault with soft-delete enabled
- **Configuration**: Infrastructure-as-Code in Git (always recoverable)

**Disaster Scenarios**:

| Scenario | Recovery Plan |
|----------|---------------|
| **Region outage** | Failover to secondary Azure region (manual, 4 hours) |
| **Data corruption** | Restore from point-in-time backup (15 min RPO) |
| **Bad deployment** | Slot swap rollback (instant) |
| **Security breach** | Rotate all secrets, audit logs, notify users |

---

## TEAM AGREEMENTS

### Code Ownership

**Model**: Collective ownership (any developer can change any code)

**Rules**:
- PR requires 1 approval from any team member
- Domain expert review required for business logic changes
- Security changes require security lead approval

---

### Definition of Done

A feature is "done" when:

- [ ] Spec exists in `specs/` directory
- [ ] Code implements spec requirements
- [ ] **AI-generated code reviewed for architectural compliance** (Vertical Slice, no cross-slice dependencies)
- [ ] Unit tests pass (80%+ coverage)
- [ ] **Tests observed failing FIRST** (red phase in TDD) or validated via characterization
- [ ] Integration tests pass
- [ ] E2E test for happy path passes
- [ ] **AI-generated tests reviewed for meaningful assertions** (not just status codes, verify business logic)
- [ ] API documented in OpenAPI (Scalar UI)
- [ ] Code reviewed and approved
- [ ] Deployed to staging
- [ ] Manual QA on staging (for UI-heavy features)
- [ ] No critical/high security issues (Dependabot)
- [ ] Merged to `main`

---

### Communication Channels

| Channel | Purpose |
|---------|---------|
| **GitHub Issues** | Feature requests, bug reports |
| **GitHub PRs** | Code review, discussion |
| **Slack #innoventity-dev** | Daily standup, quick questions |
| **Slack #innoventity-deploys** | Deployment notifications |
| **Weekly Team Meeting** | Architecture decisions, blockers |
| **Doc/ADR/** | Architecture Decision Records |

---

## DECISIONS LOG

### Technical Stack Decisions

| Decision | Date | Rationale |
|----------|------|-----------|
| **Use .NET 8 (not .NET 9)** | 2026-02-08 | LTS version, stable until Nov 2026 |
| **Use Minimal APIs (not Controllers)** | 2026-02-08 | Simpler, less boilerplate, better for vertical slices |
| **Use Angular Material (not custom CSS)** | 2026-02-08 | Faster development, accessibility built-in |
| **Use Azure (not AWS)** | 2026-02-08 | Existing Azure subscription, learning focus |
| **Use GitHub Actions (not Azure DevOps)** | 2026-02-08 | Code and CI/CD in one place |
| **Blue-Green deployment (not rolling)** | 2026-02-08 | Instant rollback, simpler than canary |
| **Vertical Slices (not layers)** | 2026-02-08 | Better for feature isolation, clear boundaries |

### Scope & Planning Decisions ✅ **ALL RESOLVED**

| Decision | Date | Rationale |
|----------|------|-----------|
| **Q1a: Research Categories** | 2026-02-08 | ✅ **Option A Selected: Simple enum, metadata only**. Values: Management/Engineering/NaturalScience. Used for filtering/analytics. NO category-driven workflows. 1-day implementation effort. Legacy free-text too complex. |
| **Q2: Government/Tech Park Actors** | 2026-02-08 | Deferred to v2.0. Focus v1.0 on 5 core actor types. Adds complexity without clear v1.0 benefit. |
| **Q3a: Database Schema Strategy** | 2026-02-08 | Modern schema with naming fixes (IdeaAuthor → IdeaGenerator). NO legacy data to preserve. Fresh start advantage. |
| **Q3b: Legacy Data Migration** | 2026-02-08 | NONE NEEDED - Legacy is dormant with no operational data. Seed data approach instead. |
| **Q4a: Project Team Composition** | 2026-02-08 | Solo learning/portfolio project. Flexible 10-month timeline. Full decision authority. |
| **Q4b: Testing Approach** | 2026-02-08 | Developer-owned automated testing from day one. xUnit, Playwright, JMeter mandatory. Learning opportunity. |
| **Q5a: Azure Resources** | 2026-02-08 | Active Azure subscription confirmed. Budget approved for App Service, SQL Database, Service Bus. |
| **Q5c: Azure Budget** | 2026-02-08 | Development tier resources acceptable. Cost optimization part of learning. |
| **Q6: Spec-Kit Usage** | 2026-02-08 | Write specs first, then use Spec-Kit for controlled code generation. Scaffold + hand-written domain logic. |
| **Q7: Sign-off Authority** | 2026-02-08 | Project owner has full authority (solo project). No external approvals needed. |
| **Q8: Legacy Freeze Confirmation** | 2026-02-08 | Confirmed dormant with no operational data. Reference architecture only. |
| **Strategic Scope: v1.0 = Open Innovation Only** | 2026-02-08 | Defer Closed/Hybrid modes to v2.0. Simplified scope for focused learning. 10 months realistic. |

### Architectural Pattern Review Decisions

| Pattern | Decision | Date | Rationale |
|---------|----------|------|-----------|
| **Hub-and-Spoke Architecture** | ❌ REJECTED | 2026-02-08 | Wrong topology - Innoventity is network collaboration (actors collaborate directly), not hub-mediated system |
| **Micro-Frontends** | ❌ REJECTED | 2026-02-08 | Over-engineering for scale. Angular feature modules + lazy loading achieve isolation without complexity |
| **Strategy Pattern for UI** | ❌ REJECTED | 2026-02-08 | Pattern mismatch - actors need different forms/views, not algorithms. Use polymorphism + routing instead |
| **RBAC with ASP.NET Identity** | ✅ APPROVED | 2026-02-08 | Native framework support, clear fit for role-based actor types |
| **Domain Events (Scoped)** | ✅ APPROVED | 2026-02-08 | Only for real-time collaboration workspace (SignalR). Not event-sourcing everything. |
| **Shared/Core Modules** | ✅ APPROVED | 2026-02-08 | Angular shared module for UI, Backend Core for cross-cutting. Clear boundaries. |

**Key Learning**: Simplicity over cleverness. Use patterns where they solve actual problems, not where they sound impressive.

---

## WHAT WE'RE NOT DOING

**Explicitly out of scope** (to prevent scope creep):

### ❌ Architectural Patterns NOT Using
- **Microservices**: Monolith first, split later if scale requires
- **Hub-and-Spoke**: Wrong topology for network collaboration
- **Micro-Frontends**: Over-engineering, use Angular feature modules
- **Strategy Pattern for UI**: Pattern mismatch, use polymorphism + routing
- **Event Sourcing**: Not needed, standard CRUD is fine
- **CQRS with separate databases**: Overkill, use Dapper for reads

### ❌ Technologies NOT Using
- **GraphQL**: REST is sufficient, less complexity  
- **React/Vue**: Angular chosen, no debates  
- **NoSQL**: Keep SQL Server, proven for relational data  
- **Kubernetes**: App Service is enough, add K8s only if scale requires  
- **gRPC**: REST + SignalR covers all needs  

### ❌ Infrastructure NOT Implementing (v1.0)
- **Multi-region active-active**: Single region, add if needed
- **Closed Innovation Mode**: Deferred to v2.0 (multi-tenancy complexity)
- **Hybrid Innovation Mode**: Deferred to v2.0
- **Organization Management**: Deferred to v2.0
- **Government/Tech Park Actors**: Deferred to v2.0

**If someone proposes these later, point them to this section.**

---

## SIGN-OFF STATUS

**Solo Project Authority**: ✅ Project owner has full decision authority

This document is **APPROVED and BINDING** for Phase 0-5 (v1.0 implementation).

~~Traditional sign-off not applicable (solo learning project).~~

Changes require:
1. Written proposal in `doc/ADR/` (Architecture Decision Record)
2. Team meeting discussion
3. Documented rationale for change
4. Update this document

---

## NEXT STEPS

With target defined, proceed to:

1. **Create project structure** (`platform/api`, `platform/web`)
2. **Setup Docker Compose** (local SQL Server, Redis)
3. **Initialize API project** (dotnet new webapi)
4. **Initialize Angular project** (ng new)
5. **Write first spec** (`specs/innovations/get-by-id.spec.yaml`)
6. **Implement first slice** (GetInnovationById endpoint + component)
7. **Deploy to staging** (prove the pipeline works)

**Target**: First working slice in Week 1.

---

**Document Status**: ✅ ALL DECISIONS RESOLVED - Ready for Implementation  
**Last Updated**: February 8, 2026  
**Related Documents**:
- [PROJECT_KNOWLEDGE_BASE.md](PROJECT_KNOWLEDGE_BASE.md) - Complete conversation journey and lessons learned
- [RE-ENGINEERING_STRATEGY.md](RE-ENGINEERING_STRATEGY.md) - Overall implementation strategy
- [ARCHITECTURAL_INVENTORY.md](ARCHITECTURAL_INVENTORY.md) - Legacy system reference architecture
- Phase 2 Implementation Guide (coming next)
