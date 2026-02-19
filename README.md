# innoventity-prototype-redux

A .NET 8 Minimal API prototype for the Innoventity platform — a B2B innovation marketplace connecting Idea Generators with Manufacturing, R&D, Sales/Marketing, and Investor partners.

---

## Project Structure

```
src/
  Innoventity.API/          — ASP.NET Core Minimal API (Vertical Slice Architecture)
    Domain/Entities/        — Domain entities (Actor, Innovation, Bid, Industry, ...)
    Features/               — Vertical slices (one file per endpoint)
      Authentication/       — Register, Login, Activate, RefreshToken
      Innovations/          — CRUD + submission workflow
      Bids/                 — Bid submission, viewing, updating
      Industries/           — Reference data
    Infrastructure/         — EF Core, JWT, Error handling
tests/
  Innoventity.API.Tests/
    E2E/Journeys/           — Subcutaneous journey tests (J1, J2)
    Integration/Features/   — Per-endpoint integration tests
    Unit/                   — Domain entity unit tests
specs/
  001-platform-core/        — Phase 0–0.5: Auth, domain model, base infrastructure
  002-domain-enhancements/  — Phase 0.5: Domain model refactoring
  003-api-completion/       — Phase 0.6: ✅ COMPLETE — Full API + subcutaneous tests
```

---

## Running the API

```bash
cd src/Innoventity.API
dotnet run
```

- Swagger UI: `http://localhost:5000/swagger`
- Scalar API Reference: `http://localhost:5000/scalar/v1`
- Health check: `GET /health`

---

## Running Tests

```bash
dotnet test
```

Current status: **94/97 passing** (3 Phase 1 tests deferred, 0 failures).

---

## Phase 0.6 — API Completion & Subcutaneous Testing ✅ COMPLETE

**Branch**: `003-api-completion`
**Status**: ✅ COMPLETE — 17/17 tasks, ready for merge into `001-platform-core`

### New API Endpoints (Phase 0.6)

| Endpoint | Method | Description | Auth |
|----------|--------|-------------|------|
| `/innovations` | POST | Create innovation draft | IdeaGenerator |
| `/innovations/{id}` | PUT | Update innovation draft | IdeaGenerator (owner) |
| `/innovations/{id}/submit` | PATCH | Publish innovation | IdeaGenerator (owner) |
| `/innovations` | GET | Discover published innovations | All authenticated |
| `/industries` | GET | Fetch industry reference list | Public |
| `/innovations/{id}/bids` | POST | Submit partnership bid | Manufacturing, R&D, SalesMarketing, Investor |
| `/innovations/{id}/bids` | GET | View bids for innovation | IdeaGenerator (owner) |
| `/bids/{id}` | PUT | Update submitted bid | Bidder (own bid) |

### Test Coverage

| Category | Tests | Status |
|----------|-------|--------|
| Subcutaneous Journey Tests (J1, J2) | 8 | ✅ All passing |
| Integration Tests (per-endpoint) | 34 | ✅ All passing |
| Unit Tests (domain + infrastructure) | 55 | ✅ 52 passing, 3 skipped (Phase 1 deferred) |
| **Total** | **97** | **94/97 passing, 0 failing** |

### Journey Coverage

**Journey 1 — Innovation Submission**
> Register → Activate → Login → Create Draft → Edit Draft → Submit/Publish

Validated by: [Journey1_InnovationSubmissionTests.cs](tests/Innoventity.API.Tests/E2E/Journeys/Journey1_InnovationSubmissionTests.cs) (4 tests ✅)

**Journey 2 — Discovery & Bidding**
> Register X2 → Activate → Login → Discover Innovation → Submit Bid → View Bids

Validated by: [Journey2_BiddingTests.cs](tests/Innoventity.API.Tests/E2E/Journeys/Journey2_BiddingTests.cs) (3 tests ✅)

### Documentation

- [Specification](specs/003-api-completion/spec.md) — User stories, acceptance criteria, API contracts
- [Implementation Plan](specs/003-api-completion/plan.md) — Tech stack, architecture, file structure
- [Task List](specs/003-api-completion/tasks.md) — 17 tasks with completion status
- [Traceability Matrix](specs/003-api-completion/traceability.md) — User stories → endpoints → tests

---

## Architecture Notes

- **Vertical Slice Architecture**: Each feature is a single `.cs` file containing the request/response types, handler, and route registration.
- **Minimal APIs**: All endpoints use `app.MapPost(...)` / `app.MapGet(...)` etc. No controllers.
- **EF Core + In-Memory for Tests**: Production uses SQL Server; tests use `UseInMemoryDatabase` with unique names per test class.
- **JWT Bearer Auth**: Tokens issued on login, validated on protected endpoints. Swagger UI includes JWT Bearer input.

---

## Deferred to Phase 1

- Rich Innovation domain composition (IdeaSummary, Product, Market owned entities)
- Polymorphic Bid response types (ManufacturingResponse, InvestorResponse, etc.)
- Partner selection workflow (SelectPartners state machine)
- NPV financial modeling domain service
