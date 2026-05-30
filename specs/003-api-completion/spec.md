# Feature Specification: API Completion & Subcutaneous Testing

**Feature Branch**: `003-api-completion` (Phase 0.6)
**Created**: February 15, 2026
**Status**: ✅ IMPLEMENTED (17/17 tasks complete, 94/97 tests passing)
**Parent Branch**: `001-platform-core` (includes Phase 0 + Phase 0.5)
**Constitutional Impact**: Maintains 99/100 (completes backend API surface before frontend work)

---

## Context & Background

### Current State

After merging Phase 0.5 (Domain Model Refactoring) into `001-platform-core`, we have:
- ✅ **Authentication**: Register, Login, RefreshToken, Activate (4 endpoints)
- ✅ **Read Operations**: GetInnovation (1 endpoint)
- ✅ **Health**: HealthCheck (1 endpoint)
- ✅ **Domain Model**: EntityBase, Address value object, Actor refactoring complete
- ✅ **Test Infrastructure**: 97 tests — 94 passing, 3 skipped (Phase 1 deferred), 0 failing *(updated post-Phase 0.6)*
- ✅ **Test Status**: 94/97 passing (96.9%) — 0 failures; 3 Phase 1 domain validation tests intentionally deferred *(updated post-Phase 0.6)*

### Problem Statement

**Critical Gap**: The API is incomplete for testing core user journeys end-to-end without a UI (subcutaneous testing). Current failures:

1. **Journey 1 (Innovation Submission)**: Cannot test - missing POST/PUT/PATCH endpoints for innovation CRUD
2. **Journey 2 (Discovery & Bidding)**: Cannot test - missing bid submission endpoints
3. **Existing Subcutaneous Tests Failing**: 4 integration tests with database context/JWT issues

**Impact on Development Velocity**:
- Cannot validate backend completeness before frontend work (violates Principle 6: Incremental)
- Cannot use subcutaneous tests as "UI substitute" for rapid feedback
- Frontend team blocked - no complete API to integrate against
- Risk of discovering API gaps during expensive UI development phase

### Vision: Backend-First Validation

**Subcutaneous Testing Pattern** (Martin Fowler): Tests that operate just under the UI, exercising complete user journeys through API endpoints with real database interaction, avoiding UI fragility while validating end-to-end behavior.

**Phase 0.6 Goal**: Complete the backend API surface and fix all subcutaneous tests so that:
- ✅ All critical user journeys (J1, J2) testable without UI
- ✅ Zero test failures (94/97 passing; 3 Phase 1 domain tests intentionally skipped)
- ✅ Backend validated as complete and working before any frontend/deployment work
- ✅ Subcutaneous tests serve as living documentation of expected API behavior

**Constitutional Alignment**:
- **Principle 2**: Quality is Non-Negotiable (100% passing tests required)
- **Principle 5**: Test-First Discipline (tests as first-class deliverables)
- **Principle 6**: Incremental & Sustainable (complete each layer fully before moving to next)

---

## Phase 0.6 Scope Boundaries *(critical context)*

### What's IN Scope

**Infrastructure & API Patterns**:
✅ Subcutaneous testing approach (API-level E2E tests)
✅ Database context lifecycle fixes (constructor vs method scope)
✅ JWT token attachment patterns (per-request headers)
✅ Authentication/authorization validation (401/403 responses)

**Innovation Management**:
✅ Draft creation (POST /innovations)
✅ Draft editing (PUT /innovations/{id})
✅ Draft submission (PATCH /innovations/{id}/submit with state transition)
✅ Innovation discovery (GET /innovations with filtering)
✅ Industry reference data (GET /industries for dropdown UI)

**Bid Management**:
✅ Bid submission with basic validation (R4.1 eligibility, R4.2 content)
✅ Bid viewing (innovation owner can see all bids)
✅ Bid updating (bidder can modify before acceptance)
✅ Generic Bid entity with free-text proposal
✅ Simple status tracking (Pending/Accepted/Rejected)

**Testing**:
✅ Journey 1 test suite (innovation submission workflow)
✅ Journey 2 test suite (discovery and bidding workflow)
✅ 97 total tests — 8 subcutaneous journey tests, 34 integration tests, 55 unit/infra tests *(actual post-implementation)*

---

### What's OUT OF SCOPE - Intentional Deferrals to Phase 1

**Innovation Entity Architecture**:
❌ Domain composition (IdeaSummary, Product, Market, CollaborationRequirement owned entities)
❌ Rich validation methods (IsReadyForSubmission(), IsIdeaSummaryComplete(), etc.)
❌ Submission workflow state machine (Draft → Submitted with validation gates)
❌ Multi-step submission forms (progressive disclosure of child entities)

**Bid Entity Architecture**:
❌ Polymorphic FormalResponse hierarchy (ManufacturingResponse, SalesMarketingResponse, ResearchDevelopmentResponse, InvestorResponse)
❌ Financial projection structures (YearlyManufacturingCosts, YearlySales dictionaries with rationale fields)
❌ Type-specific validation (e.g., ManufacturingResponse requires ≥1 year of projections)
❌ NPV calculation support (IProjectValuationService domain service)

**Partner Selection Workflow**:
❌ Partner selection command (SelectPartners with validation rules)
❌ Minimum bids validation (HasReceivedEnoughOfFormalResponses)
❌ Irreversibility enforcement (HasPartnerSelectionCompleted)
❌ State transition to InCollaboration status

**Rationale for Deferral**:
Phase 0.6 focuses on **API infrastructure** and **authentication patterns**. The current flat `Innovation` and generic `Bid` entities are **intentionally simplified** to:
1. Validate authentication and authorization flows quickly
2. Test API endpoint structure (Minimal APIs approach)
3. Prove integration testing approach (WebApplicationFactory patterns)
4. Deliver MVP functionality without over-engineering

Rich domain modeling (composition, polymorphism, workflow validation) requires:
- Business plan integration (Phase 3)
- Virtual incubator workspace (Phase 3)
- Financial modeling domain service (Phase 2+)

See [../ROADMAP.md](../ROADMAP.md#phase-1-domain-richness--rich-behavior) for Phase 1 refactoring plan and [../../.specify/analysis/innovation-bid-domain-gap-analysis.md](../../.specify/analysis/innovation-bid-domain-gap-analysis.md) for comprehensive legacy system comparison.

**Constitutional Compliance**: ✅ ACCEPTABLE
- **Principle 3 (Simplicity Over Cleverness)**: Flat model is simplest for MVP validation
- **Principle 4 (Specification Drives Implementation)**: Spec explicitly defers composition to Phase 1
- **Principle 6 (Incremental & Sustainable)**: Infrastructure first, domain richness second

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fix Failing Subcutaneous Tests (Priority: P0 - CRITICAL)

**Status**: ✅ IMPLEMENTED
**Tests**: [Phase0JourneyTests.cs](../../tests/Innoventity.API.Tests/E2E/Journeys/Phase0JourneyTests.cs) · [GetInnovationTests.cs](../../tests/Innoventity.API.Tests/Integration/Features/Innovations/GetInnovationTests.cs)

As a **developer**, I need all existing subcutaneous tests to pass so that I can trust the test suite as a quality gate and build new features on a stable foundation.

**Why this priority**: CRITICAL - Cannot proceed with new API endpoints until existing tests are reliable. Database context issues and JWT token problems indicate infrastructure bugs that will affect all future work.

**Independent Test**: Current integration tests (Phase0JourneyTests, GetInnovationTests) already exist but failing. Success = all 4 tests pass.

**Acceptance Scenarios**:

1. **Given** Phase0JourneyTests.Phase0Journey_RegisterActivateLoginViewInnovation_Success exists, **When** test is executed, **Then** test passes with HTTP 200 OK response (not BadRequest)
2. **Given** GetInnovationTests.GetInnovation_WithValidId_ReturnsInnovationData exists, **When** test is executed, **Then** test passes with correct innovation data returned (not Unauthorized)
3. **Given** GetInnovationTests.GetInnovation_CrossActorAccess_Returns200 exists, **When** test is executed, **Then** test passes with HTTP 200 OK (not Unauthorized)
4. **Given** database context is created in test constructor and JWT token acquired in test method, **When** subsequent API calls use that token, **Then** database records are visible across both scopes

**Root Cause Analysis Required**:
- Database context lifecycle: Constructor scope vs method scope isolation
- JWT token attachment: HttpClient.DefaultRequestHeaders vs per-request headers
- Test database seeding: SeedTestData() visibility to subsequent test methods
- In-memory database provider isolation between test instances

---

### User Story 2 - Innovation Draft Management (Priority: P0 - CRITICAL)

**Status**: ✅ IMPLEMENTED
**Endpoints**: `POST /innovations` · `PUT /innovations/{id}`
**Endpoint Files**: [CreateInnovation.cs](../../src/Innoventity.API/Features/Innovations/CreateInnovation.cs) · [UpdateInnovation.cs](../../src/Innoventity.API/Features/Innovations/UpdateInnovation.cs)
**Tests**: [CreateInnovationTests.cs](../../tests/Innoventity.API.Tests/Integration/Features/Innovations/CreateInnovationTests.cs) · [UpdateInnovationTests.cs](../../tests/Innoventity.API.Tests/Integration/Features/Innovations/UpdateInnovationTests.cs)

As an **Idea Generator**, I need to create and edit innovation drafts via API so that I can incrementally build my innovation submission before publishing.

**Why this priority**: CRITICAL - Core value creation flow (Journey 1). Without draft CRUD, users cannot submit innovations.

**Independent Test**: Subcutaneous test creates draft via POST, updates via PUT, verifies via GET, all without UI.

**Acceptance Scenarios**:

1. **Given** authenticated Idea Generator, **When** POST /innovations with complete innovation data, **Then** system returns 201 Created with innovation ID and draft status
2. **Given** innovation draft owned by actor, **When** PUT /innovations/{id} with updated title, **Then** system returns 200 OK with updated innovation
3. **Given** innovation draft owned by different actor, **When** PUT /innovations/{id} attempted, **Then** system returns 403 Forbidden with ownership error message
4. **Given** unauthenticated request, **When** POST /innovations attempted, **Then** system returns 401 Unauthorized

**API Contract**:
```http
POST /innovations
Authorization: Bearer {jwt_access_token}
Content-Type: application/json

{
  "title": "Quantum Battery Prototype",
  "productType": "Energy Storage Device",
  "researchCategory": "Engineering",
  "researchBackground": "Lithium-air battery leveraging quantum tunneling...",
  "hasIPR": true,
  "hasRightToUse": true,
  "productDescription": "Next-generation battery technology...",
  "technologyDescription": "Quantum tunneling mechanism...",
  "targetBeneficiaries": "Electric vehicle manufacturers",
  "relevantMarketSize": 50000000000.00,
  "potentialMarketSize": 150000000000.00,
  "targetMarket": "Electric vehicle manufacturers, renewable energy storage",
  "targetCustomerBase": "Automotive OEMs, grid-scale energy providers",
  "targetCustomerType": "B2B",
  "targetIndustryIds": ["ELEC-001", "ENRG-001"],
  "partnersNeeded": ["RD", "Manufacturing", "SalesMarketing"]
}

Response 201 Created:
{
  "innovationId": "uuid",
  "ideaToken": "uuid",
  "status": "Draft",
  "createdAt": "2026-02-15T10:00:00Z",
  "ownerId": "uuid"
}
```

---

### User Story 3 - Innovation Publication (Priority: P0 - CRITICAL)

**Status**: ✅ IMPLEMENTED
**Endpoints**: `PATCH /innovations/{id}/submit`
**Endpoint File**: [SubmitInnovation.cs](../../src/Innoventity.API/Features/Innovations/SubmitInnovation.cs)
**Tests**: [SubmitInnovationTests.cs](../../tests/Innoventity.API.Tests/Integration/Features/Innovations/SubmitInnovationTests.cs)

As an **Idea Generator**, I need to publish my completed innovation via API so that it becomes visible to potential partners for bidding.

**Why this priority**: CRITICAL - Completes Journey 1 (Innovation Submission & Publication). Publication is irreversible state change requiring validation.

**Independent Test**: Subcutaneous test creates draft, submits via PATCH, verifies status change and visibility.

**Acceptance Scenarios**:

1. **Given** innovation draft with all sections complete, **When** PATCH /innovations/{id}/submit, **Then** system returns 200 OK, status changes to "Published", submissionTimestamp recorded
2. **Given** innovation draft with incomplete IdeaSummary, **When** PATCH /innovations/{id}/submit, **Then** system returns 400 Bad Request with validation errors listing missing fields
3. **Given** innovation already published, **When** PATCH /innovations/{id}/submit attempted again, **Then** system returns 409 Conflict with "Innovation already published" message
4. **Given** non-owner attempts to publish, **When** PATCH /innovations/{id}/submit, **Then** system returns 403 Forbidden

**Completeness Validation (R2.1)**:
- Title not empty and not placeholder text
- ProductType provided
- ResearchCategory selected (Management/Engineering/NaturalScience)
- ResearchBackground min 50 characters
- HasIPR declared (true/false)
- HasRightToUse = true
- ProductDescription provided
- TechnologyDescription provided
- TargetBeneficiaries provided
- RelevantMarketSize > 0
- PotentialMarketSize > 0
- ≥1 TargetIndustry selected
- ≥1 PartnersNeeded specified

**API Contract**:
```http
PATCH /innovations/{id}/submit
Authorization: Bearer {jwt_access_token}

Response 200 OK:
{
  "innovationId": "uuid",
  "status": "Published",
  "submittedAt": "2026-02-15T10:30:00Z"
}

Response 400 Bad Request (incomplete):
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "ResearchBackground": ["Research background must be at least 50 characters"],
    "TargetIndustries": ["At least one target industry is required"]
  }
}
```

---

### User Story 4 - Innovation Discovery (Priority: P0 - CRITICAL)

**Status**: ✅ IMPLEMENTED
**Endpoints**: `GET /innovations`
**Endpoint File**: [ListInnovations.cs](../../src/Innoventity.API/Features/Innovations/ListInnovations.cs)
**Tests**: [ListInnovationsTests.cs](../../tests/Innoventity.API.Tests/Integration/Features/Innovations/ListInnovationsTests.cs)

As a **Manufacturing/R&D/Sales actor**, I need to discover published innovations via API with industry filtering so that I can find relevant opportunities to bid on.

**Why this priority**: CRITICAL - Completes first half of Journey 2 (Innovation Discovery). Without discovery endpoint, actors cannot find innovations to bid on.

**Independent Test**: Subcutaneous test seeds 3 innovations (2 Electronics, 1 Healthcare), queries with industryId filter, verifies correct innovations returned.

**Acceptance Scenarios**:

1. **Given** 3 published innovations (2 Electronics, 1 Healthcare), **When** GET /innovations?industryId=ELEC-001, **Then** system returns 2 Electronics innovations only
2. **Given** authenticated actor, **When** GET /innovations (no filters), **Then** system returns all published innovations (not drafts)
3. **Given** innovations with different research categories, **When** GET /innovations?researchCategory=Engineering, **Then** system returns only Engineering category innovations
4. **Given** unauthenticated request, **When** GET /innovations, **Then** system returns 401 Unauthorized (authentication required)

**API Contract**:
```http
GET /innovations?industryId=ELEC-001&researchCategory=Engineering&page=1&pageSize=20
Authorization: Bearer {jwt_access_token}

Response 200 OK:
{
  "items": [
    {
      "innovationId": "uuid",
      "title": "Quantum Battery Prototype",
      "productType": "Energy Storage Device",
      "researchCategory": "Engineering",
      "status": "Published",
      "submittedAt": "2026-02-15T10:30:00Z",
      "owner": {
        "actorId": "uuid",
        "firstName": "Sarah",
        "lastName": "Chen",
        "displayName": "Dr. Sarah Chen"
      },
      "targetIndustries": ["Electronics", "Renewable Energy"],
      "partnersNeeded": ["RD", "Manufacturing", "SalesMarketing"]
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 20
}
```

---

### User Story 5 - Bid Submission (Priority: P0 - CRITICAL)

**Status**: ✅ IMPLEMENTED
**Endpoints**: `POST /innovations/{id}/bids`
**Endpoint File**: [SubmitBid.cs](../../src/Innoventity.API/Features/Bids/SubmitBid.cs)
**Tests**: [SubmitBidTests.cs](../../tests/Innoventity.API.Tests/Integration/Features/Bids/SubmitBidTests.cs)

As a **Manufacturing Company**, I need to submit formal partnership proposals via API so that I can express my interest in collaborating on an innovation.

**Why this priority**: CRITICAL - Completes Journey 2 (Innovation Discovery & Bid Submission). Core marketplace transaction after discovery.

**Independent Test**: Subcutaneous test registers Manufacturing actor, discovers innovation, submits bid, verifies bid recorded and owner notified.

**Acceptance Scenarios**:

1. **Given** authenticated Manufacturing actor and published innovation, **When** POST /innovations/{innovationId}/bids with valid proposal, **Then** system returns 201 Created with bid ID
2. **Given** Manufacturing actor already submitted bid for innovation, **When** POST /innovations/{innovationId}/bids attempted again, **Then** system returns 409 Conflict with "Duplicate bid" message
3. **Given** proposal text <200 characters, **When** POST /innovations/{innovationId}/bids, **Then** system returns 400 Bad Request with "Proposal must be at least 200 characters" error
4. **Given** Idea Generator actor, **When** POST /innovations/{innovationId}/bids attempted, **Then** system returns 403 Forbidden with "Idea Generators cannot submit bids" error
5. **Given** actor submits bid, **When** innovation owner queries GET /innovations/{innovationId}/bids, **Then** owner sees the bid in response

**Bid Eligibility Rules (R4.1)**:
- Only R&D, Manufacturing, SalesMarketing, Investor actors can submit bids
- Idea Generator actors cannot submit bids
- Actor cannot bid on their own innovations
- Innovation must be in Published status (accepting bids)
- Actor cannot submit duplicate bids for same innovation
- Location (geographic presence) required
- ParticipationType required
- ParticipationProposal required (min 200 characters)

**API Contract**:
```http
POST /innovations/{innovationId}/bids
Authorization: Bearer {jwt_access_token}
Content-Type: application/json

{
  "location": "Munich, Germany",
  "participationType": "Manufacturing Partner",
  "participationProposal": "We have 20 years of experience in precision battery manufacturing with ISO 9001 certification. Our facility in Munich has capacity for pilot production runs of 10,000 units/month with potential to scale to 100,000 units/month. We can provide design-for-manufacturing consultation during R&D phase and establish supply chain partnerships with tier-1 automotive suppliers."
}

Response 201 Created:
{
  "bidId": "uuid",
  "innovationId": "uuid",
  "actorId": "uuid",
  "location": "Munich, Germany",
  "participationType": "Manufacturing Partner",
  "submittedAt": "2026-02-15T11:00:00Z",
  "status": "Pending"
}

Response 409 Conflict (duplicate):
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "Duplicate Bid",
  "status": 409,
  "detail": "You have already submitted a bid for this innovation. You can edit your existing bid instead."
}
```

---

### User Story 6 - Bid Management (Priority: P1 - Important)

**Status**: ✅ IMPLEMENTED
**Endpoints**: `GET /innovations/{id}/bids` · `PUT /bids/{id}`
**Endpoint Files**: [GetBids.cs](../../src/Innoventity.API/Features/Bids/GetBids.cs) · [UpdateBid.cs](../../src/Innoventity.API/Features/Bids/UpdateBid.cs)
**Tests**: [GetBidsTests.cs](../../tests/Innoventity.API.Tests/Integration/Features/Bids/GetBidsTests.cs) · [UpdateBidTests.cs](../../tests/Innoventity.API.Tests/Integration/Features/Bids/UpdateBidTests.cs)

As a **Manufacturing Company**, I need to view and update my submitted bids via API so that I can refine my proposal before partner selection occurs.

**Why this priority**: Important but not blocking Journey 2 completion. Actors can submit once and wait for selection. Edit capability improves UX but not required for basic marketplace function.

**Independent Test**: Subcutaneous test submits bid, updates proposal text, verifies update successful and only author can edit.

**Acceptance Scenarios**:

1. **Given** Manufacturing actor submitted bid, **When** PUT /bids/{bidId} with updated proposal, **Then** system returns 200 OK with updated bid
2. **Given** bid already accepted by innovation owner, **When** PUT /bids/{bidId} attempted, **Then** system returns 409 Conflict with "Accepted bids cannot be modified" error
3. **Given** different actor attempts to edit another's bid, **When** PUT /bids/{bidId}, **Then** system returns 403 Forbidden
4. **Given** innovation owner queries bids, **When** GET /innovations/{innovationId}/bids, **Then** owner sees all bids with full proposal details

**API Contract**:
```http
PUT /bids/{bidId}
Authorization: Bearer {jwt_access_token}
Content-Type: application/json

{
  "location": "Munich, Germany",
  "participationType": "Manufacturing Partner",
  "participationProposal": "UPDATED: We have 20 years of experience... [updated text 200+ chars]"
}

Response 200 OK:
{
  "bidId": "uuid",
  "location": "Munich, Germany",
  "participationType": "Manufacturing Partner",
  "participationProposal": "UPDATED: We have 20 years...",
  "updatedAt": "2026-02-15T12:00:00Z"
}
```

---

### User Story 7 - Industry Master List (Priority: P0 - CRITICAL)

**Status**: ✅ IMPLEMENTED
**Endpoints**: `GET /industries`
**Endpoint File**: [GetIndustries.cs](../../src/Innoventity.API/Features/Industries/GetIndustries.cs)
**Tests**: [GetIndustriesTests.cs](../../tests/Innoventity.API.Tests/Integration/Features/Industries/GetIndustriesTests.cs)

As a **frontend developer**, I need the industry master list via API so that users can select target industries when creating innovations and setting actor affiliations.

**Why this priority**: CRITICAL - Required for innovation creation UI and actor profile management. Without this endpoint, users cannot select industries (innovation submission blocked).

**Independent Test**: Subcutaneous test queries GET /industries, verifies hierarchical structure and expected industries present.

**Acceptance Scenarios**:

1. **Given** system has seeded industry master list, **When** GET /industries, **Then** system returns all industries with hierarchy (Sector → Subsector structure if applicable)
2. **Given** industry list includes ICB top-level industries such as Health Care, Technology, Oil & Gas, and Consumer Goods, **When** GET /industries, **Then** response includes these industries with their IDs
3. **Given** this is reference data, **When** GET /industries, **Then** authentication NOT required (public endpoint)

**API Contract**:
```http
GET /industries

Response 200 OK:
{
  "industries": [
    {
      "industryId": "HLTH-001",
      "name": "Health Care",
      "description": "Health care equipment and services"
    },
    {
      "industryId": "TECH-001",
      "name": "Technology",
      "description": "Software, hardware, and technology services"
    },
    {
      "industryId": "ENRG-001",
      "name": "Oil & Gas",
      "description": "Oil, gas, and energy production services"
    },
    {
      "industryId": "AUTO-001",
      "name": "Consumer Goods",
      "description": "Automobiles, consumer products, and manufacturing"
    }
  ]
}
```

---

## System Requirements

### SR1: Database Context Lifecycle Management

**Requirement**: Test database context MUST be properly scoped to ensure data seeded in test setup is visible to test execution methods.

**Technical Specifications**:

1. **In-Memory Database Isolation**
   - Each test class instance MUST use unique database name to prevent cross-test pollution
   - Database name pattern: `TestDb_{TestClassName}_{Guid.NewGuid()}`
   - Database MUST be created in test constructor
   - Database MUST be dropped in test disposal

2. **Context Lifecycle**
   - Context created in constructor MUST be scoped to entire test class lifetime
   - Seed data operations MUST use same context instance as test methods
   - WebApplicationFactory MUST be configured to use same in-memory database as test context
   - Service provider scoping MUST be explicit (singleton for test database)

3. **Data Visibility**
   - Data seeded via `SeedTestData()` in constructor MUST be queryable in test methods
   - JWT token generation queries MUST see seeded actor records
   - API endpoint queries MUST see seeded innovation records
   - No scope boundary between constructor and test method execution

**Acceptance Tests**:
```csharp
[Fact]
public void DatabaseContext_SeededDataVisibleToTestMethod()
{
    // Arrange: Seed in constructor
    // Act: Query in test method
    var actor = _dbContext.Actors.Find(_testActorId);

    // Assert
    actor.Should().NotBeNull();
    actor.Email.Should().Be("test@example.com");
}
```

**Implementation Guidance**:
```csharp
public class Phase0JourneyTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _databaseName;

    public Phase0JourneyTests()
    {
        // Unique database per test instance
        _databaseName = $"TestDb_Phase0Journey_{Guid.NewGuid()}";

        // Create context with in-memory provider
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();

        // Seed test data using same context
        SeedTestData(_dbContext);

        // Configure factory to use same database
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove default DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // Register with same database name
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(_databaseName));
                });
            });

        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
    }
}
```

---

### SR2: JWT Token Management in Tests

**Requirement**: Integration tests MUST properly attach JWT tokens to HTTP requests for authenticated endpoints.

**Technical Specifications**:

1. **Token Generation**
   - Test MUST authenticate via POST /auth/login to obtain valid JWT token
   - Token MUST be stored for reuse across multiple requests in same test
   - Token expiration MUST be configured for test environment (e.g., 60 minutes)

2. **Token Attachment**
   - Token MUST be attached via Authorization header: `Bearer {token}`
   - Header MUST be set per-request (not DefaultRequestHeaders) for test isolation
   - Helper method `SetAuthorizationHeader(HttpClient, token)` recommended

3. **Token Validation**
   - Test JWT configuration MUST match application JWT configuration
   - Signing key MUST be consistent between test setup and app configuration
   - Issuer and Audience MUST match expected values

**Acceptance Tests**:
```csharp
[Fact]
public async Task AuthenticatedRequest_WithValidToken_ReturnsSuccess()
{
    // Arrange
    var loginResponse = await LoginAsTestActor();
    var token = loginResponse.AccessToken;

    // Act
    var request = new HttpRequestMessage(HttpMethod.Get, "/innovations/12345");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var response = await _client.SendAsync(request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

---

### SR3: Subcutaneous Test Requirements

**Requirement**: All critical user journeys MUST be validated via subcutaneous tests that exercise complete flows through API endpoints without UI.

**Technical Specifications**:

1. **Journey Coverage**
   - Journey 1 (Innovation Submission & Publication): MUST have end-to-end subcutaneous test
   - Journey 2 (Innovation Discovery & Bid Submission): MUST have end-to-end subcutaneous test
   - Each journey test MUST orchestrate 5+ API calls in sequence
   - Each journey test MUST validate state changes in database

2. **Test Structure**
   - Test class per journey (Journey1Tests.cs, Journey2Tests.cs)
   - One primary test method per complete journey flow
   - Additional test methods for journey variations (error paths, edge cases)
   - Test names follow pattern: `Journey{N}_{ActorRole}_{Action}_{ExpectedOutcome}`

3. **Test Independence**
   - Each test MUST create its own test data (no shared state between tests)
   - Each test MUST use isolated database instance
   - Tests MUST be runnable in any order
   - Tests MUST be runnable in parallel (when xUnit collection configured)

4. **Assertions**
   - MUST assert HTTP status codes for each API call
   - MUST assert response body structure and content
   - MUST assert database state changes (query after API call)
   - MUST assert business rule enforcement (validation errors, authorization failures)

**Example Journey Test Structure**:
```csharp
public class Journey1_InnovationSubmissionTests : IDisposable
{
    private readonly TestFixture _fixture;

    public Journey1_InnovationSubmissionTests()
    {
        _fixture = new TestFixture("Journey1");
    }

    [Fact]
    public async Task Journey1_IdeaGeneratorSubmitsInnovation_PublishedSuccessfully()
    {
        // Step 1: Register
        var registration = await _fixture.RegisterActor(ActorType.IdeaGenerator);
        registration.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 2: Activate
        var activation = await _fixture.ActivateAccount(registration.Data.ActivationToken);
        activation.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Login
        var login = await _fixture.Login(registration.Data.Email, "TestPassword123!");
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = login.Data.AccessToken;

        // Step 4: Create Draft
        var draft = await _fixture.CreateInnovation(token, TestData.CompleteInnovation());
        draft.StatusCode.Should().Be(HttpStatusCode.Created);
        var innovationId = draft.Data.InnovationId;

        // Step 5: Submit for Publication
        var submit = await _fixture.SubmitInnovation(innovationId, token);
        submit.StatusCode.Should().Be(HttpStatusCode.OK);
        submit.Data.Status.Should().Be("Published");

        // Step 6: Verify in Database
        var innovation = await _fixture.DbContext.Innovations.FindAsync(innovationId);
        innovation.Should().NotBeNull();
        innovation.Status.Should().Be(InnovationStatus.Published);
        innovation.SubmissionTimestamp.Should().NotBeNull();

        // Step 7: Verify Discoverable by Other Actors
        var manufacturingLogin = await _fixture.LoginAsManufacturing();
        var discoveries = await _fixture.ListInnovations(manufacturingLogin.Data.AccessToken);
        discoveries.Data.Items.Should().Contain(i => i.InnovationId == innovationId);
    }
}
```

---

## Success Metrics

### Test Quality Metrics

**Test Pass Rate** *(updated post-implementation)*:
- Phase 0.6 Start: 53/60 passing (88.3%)
- Phase 0.6 Achieved: **94/97 passing** (0 failures; 3 Phase 1 domain tests intentionally skipped)
  - Fixed 4 failing subcutaneous tests (database context lifecycle + JWT token scope)
  - Added 37 new tests (integration + journey) exceeding original 7-test estimate

**Subcutaneous Test Coverage**:
- Journey 1 (Innovation Submission): 1 complete journey test + 3 error path tests
- Journey 2 (Discovery & Bidding): 1 complete journey test + 2 error path tests
- Total: 2 complete journey orchestrations, 5 error path variations

**Test Execution Speed**:
- Full test suite execution time: <30 seconds (acceptable for 67 tests)
- Journey test execution time: <5 seconds per journey (rapid feedback)
- Test setup/teardown overhead: <1 second per test

---

### API Completeness Metrics

**Endpoint Coverage**:
- Before Phase 0.6: 6 endpoints (Authentication + GetInnovation + Health)
- After Phase 0.6: 14 endpoints (100% Journey 1 + Journey 2 coverage)
  - Authentication: 4 endpoints (existing) ✅
  - Innovation CRUD: 4 endpoints (new) ➕
  - Bid Management: 3 endpoints (new) ➕
  - Reference Data: 1 endpoint (new) ➕
  - Health: 1 endpoint (existing) ✅
  - Read Operations: 1 endpoint (existing) ✅

**OpenAPI Documentation Completeness**:
- All 14 endpoints documented with XML comments ✅
- Request/response schemas defined ✅
- Validation rules documented ✅
- Error responses with examples ✅

---

### Development Velocity Metrics

**Blockers Removed**:
- Frontend team unblocked: Complete API available for integration
- Manual testing reduced: Subcutaneous tests provide automated validation
- Bug discovery shifted left: API bugs caught in integration tests, not during UI development

**Lead Time Reduction**:
- Time from "API design" to "validated working API": Reduced from 2-3 weeks (with UI development + debugging) to 1 week (subcutaneous tests provide immediate validation)
- Time from "bug reported" to "bug confirmed": Reduced from hours (reproduce via UI) to minutes (write failing subcutaneous test)

---

## Phase 0.6 Task Breakdown

### Phase 1: Fix Failing Subcutaneous Tests (3 days, ~10 hours)

**T001**: Diagnose database context lifecycle issues
- **Objective**: Understand why SeedTestData() in constructor is not visible to test methods
- **Actions**:
  1. Add logging to SeedTestData() to confirm execution
  2. Add logging to test methods querying seeded data
  3. Inspect in-memory database provider scope behavior
  4. Document findings in diagnostic report
- **Deliverable**: Root cause analysis document
- **Success Criteria**: Exact cause of data visibility issue identified

**T002**: Implement database context fix
- **Objective**: Ensure seeded data visible across test lifecycle
- **Actions**:
  1. Refactor test base class to use consistent database naming
  2. Configure WebApplicationFactory to use same in-memory database as test context
  3. Verify DbContext singleton scoping in test service provider
  4. Update all test classes to use new pattern
- **Deliverable**: Fixed database context lifecycle in all integration test classes
- **Success Criteria**: SeedTestData() records queryable in test methods

**T003**: Fix JWT token attachment issues
- **Objective**: Ensure authenticated requests properly include Authorization header
- **Actions**:
  1. Review current token attachment implementation in GetInnovationTests
  2. Create helper method `SetAuthorizationHeader(HttpClient, token)`
  3. Update all authenticated requests to use helper
  4. Verify JWT configuration matches between tests and app
- **Deliverable**: JWT token helper method and updated integration tests
- **Success Criteria**: GetInnovationTests pass with HTTP 200 OK (not Unauthorized)

**T004**: Validate all existing tests pass
- **Objective**: Achieve 57/60 passing (exclude 3 Phase 1 domain tests)
- **Actions**:
  1. Run full test suite: `dotnet test`
  2. Verify Phase0JourneyTests passes
  3. Verify GetInnovationTests (3 tests) pass
  4. Document remaining 3 failures as expected (Phase 1 work)
- **Deliverable**: Test execution report showing 57/60 passing
- **Success Criteria**: 95% pass rate (57/60), all subcutaneous infrastructure tests passing

---

### Phase 2: Innovation CRUD API Implementation (5 days, ~15 hours)

**T005**: Implement POST /innovations (Create Draft)
- **Objective**: Enable innovation draft creation via API
- **Actions**:
  1. Create `CreateInnovation.cs` endpoint file in Features/Innovations
  2. Implement request validation (required fields, data format)
  3. Implement authorization check (authenticated Idea Generator only)
  4. Implement database insert with Draft status
  5. Add XML documentation with examples
  6. Write unit tests for validation logic (3 tests)
  7. Write integration test: `CreateInnovationTests.CreateInnovation_WithCompleteData_Returns201`
- **Deliverable**: POST /innovations endpoint with 4 tests passing
- **Success Criteria**: Integration test creates innovation, verifies 201 response and database record

**T006**: Implement PUT /innovations/{id} (Update Draft)
- **Objective**: Enable innovation updates for draft status only
- **Actions**:
  1. Create `UpdateInnovation.cs` endpoint file
  2. Implement ownership validation (only owner can update)
  3. Implement status validation (only Draft status editable)
  4. Implement partial update logic (merge provided fields)
  5. Add XML documentation
  6. Write integration tests: Update as owner (success), Update as non-owner (403), Update published innovation (409)
- **Deliverable**: PUT /innovations/{id} endpoint with 3 integration tests passing
- **Success Criteria**: Owner can update draft, non-owner gets 403, published innovation cannot be edited

**T007**: Implement PATCH /innovations/{id}/submit (Publish)
- **Objective**: Enable innovation publication with completeness validation
- **Actions**:
  1. Create `SubmitInnovation.cs` endpoint file
  2. Implement completeness validation per R2.1 (13 validation rules)
  3. Implement ownership check (only owner can submit)
  4. Implement status transition (Draft → Published)
  5. Record submission timestamp
  6. Add XML documentation with validation error examples
  7. Write integration tests: Submit complete (success), Submit incomplete (400 with errors), Submit already published (409)
- **Deliverable**: PATCH /innovations/{id}/submit endpoint with 3 integration tests passing
- **Success Criteria**: Complete innovation publishes successfully, incomplete returns validation errors

**T008**: Implement GET /innovations (List with Filters)
- **Objective**: Enable innovation discovery with industry/category filtering
- **Actions**:
  1. Create `ListInnovations.cs` endpoint file
  2. Implement query filters: industryId, researchCategory, status
  3. Implement pagination (page, pageSize parameters)
  4. Implement authorization (authenticated users only)
  5. Return only Published innovations (not drafts)
  6. Add XML documentation
  7. Write integration tests: Filter by industry (returns matched), Filter by category (returns matched), No filters (returns all published)
- **Deliverable**: GET /innovations endpoint with 3 integration tests passing
- **Success Criteria**: Industry filter returns only matching innovations, pagination works correctly

**T009**: Implement GET /industries (Master List)
- **Objective**: Enable industry selection for innovation creation and actor profiles
- **Actions**:
  1. Create `GetIndustries.cs` endpoint file
  2. Seed industry master list in database using ICB taxonomy (HLTH-001, TECH-001, ENRG-001, AUTO-001 at minimum)
  3. Return all industries (public endpoint, no auth required)
  4. Add XML documentation
  5. Write integration test: `GetIndustriesTests.GetIndustries_ReturnsAll()`
- **Deliverable**: GET /industries endpoint with 1 integration test passing
- **Success Criteria**: Returns ≥4 industries including HLTH-001, TECH-001, ENRG-001, AUTO-001

---

### Phase 3: Bid Submission API Implementation (3 days, ~10 hours)

**T010**: Implement POST /innovations/{innovationId}/bids (Submit Bid)
- **Objective**: Enable partnership proposals via API
- **Actions**:
  1. Create `SubmitBid.cs` endpoint file in Features/Bids
  2. Implement bid eligibility validation per R4.1 (actor type, duplicate check)
  3. Implement content validation (location, participationType, proposal min 200 chars)
  4. Record bid with timestamp and Pending status
  5. Add XML documentation with eligibility rules
  6. Write integration tests: Submit valid bid (201), Duplicate bid (409), Idea Generator attempts bid (403), Proposal too short (400)
- **Deliverable**: POST /innovations/{innovationId}/bids endpoint with 4 integration tests passing
- **Success Criteria**: Manufacturing actor submits bid successfully, duplicate returns 409, Idea Generator gets 403

**T011**: Implement GET /innovations/{innovationId}/bids (List Bids)
- **Objective**: Enable innovation owner to review received bids
- **Actions**:
  1. Create `GetBids.cs` endpoint file
  2. Implement authorization check (only innovation owner can access)
  3. Return all bids for the innovation with full proposal details
  4. Add XML documentation
  5. Write integration tests: Get bids as owner (returns all), Get bids as non-owner (403)
- **Deliverable**: GET /innovations/{innovationId}/bids endpoint with 2 integration tests passing
- **Success Criteria**: Owner sees all bids, non-owner gets 403 Forbidden

**T012**: Implement PUT /bids/{bidId} (Update Bid)
- **Objective**: Enable actors to refine proposals before partner selection
- **Actions**:
  1. Create `UpdateBid.cs` endpoint file
  2. Implement authorization (only bid author can update)
  3. Implement status validation (only Pending bids editable)
  4. Update proposal content and timestamp
  5. Add XML documentation
  6. Write integration tests: Update as author (success), Update as different actor (403), Update accepted bid (409)
- **Deliverable**: PUT /bids/{bidId} endpoint with 3 integration tests passing
- **Success Criteria**: Author updates bid successfully, non-author gets 403, accepted bid cannot be edited

---

### Phase 4: Subcutaneous Journey Tests Implementation (4 days, ~12 hours)

**T013**: Build Journey 1 Complete Test Suite
- **Objective**: Validate entire Innovation Submission journey via subcutaneous test
- **Actions**:
  1. Create `Journey1_InnovationSubmissionTests.cs` test class
  2. Implement test fixture with helper methods (Register, Activate, Login, CreateInnovation, SubmitInnovation)
  3. Write primary journey test: `Journey1_IdeaGeneratorSubmitsInnovation_PublishedSuccessfully()` (7-step orchestration)
  4. Write error path tests:
     - `Journey1_SubmitIncompleteInnovation_Returns400WithValidationErrors()`
     - `Journey1_NonOwnerCannotSubmitInnovation_Returns403()`
     - `Journey1_SubmitAlreadyPublished_Returns409()`
  5. Verify all 4 tests pass
- **Deliverable**: Journey1Tests class with 4 passing subcutaneous tests
- **Success Criteria**: Complete journey test orchestrates 7 API calls successfully, error paths return correct HTTP status codes

**T014**: Build Journey 2 Complete Test Suite
- **Objective**: Validate entire Innovation Discovery & Bid Submission journey via subcutaneous test
- **Actions**:
  1. Create `Journey2_BiddingTests.cs` test class
  2. Seed published innovation in test setup
  3. Write primary journey test: `Journey2_ManufacturingActorSubmitsBid_BidRecorded()` (6-step orchestration)
  4. Write error path tests:
     - `Journey2_IdeaGeneratorCannotBid_Returns403()`
     - `Journey2_DuplicateBid_Returns409()`
  5. Verify all 3 tests pass
- **Deliverable**: Journey2Tests class with 3 passing subcutaneous tests
- **Success Criteria**: Complete journey discovers innovation, submits bid, verifies owner sees bid

**T015**: Validate Complete Test Suite
- **Objective**: Achieve zero failures (94/97 tests passing; 3 Phase 1 domain tests intentionally skipped)
- **Actions**:
  1. Run full test suite: `dotnet test --verbosity normal`
  2. Generate test coverage report
  3. Verify all subcutaneous tests pass
  4. Verify all integration tests pass
  5. Document 3 Phase 1 domain tests as expected skips (not failures)
- **Deliverable**: Test execution report showing 94/97 passing (3 Phase 1 tests skipped)
- **Success Criteria**: Zero failures, test suite executes in <30 seconds (actual: 21.8s)

---

### Phase 5: Documentation & Verification (2 days, ~6 hours)

**T016**: Complete OpenAPI Documentation
- **Objective**: Ensure all 8 new endpoints fully documented with XML comments
- **Actions**:
  1. Review each new endpoint for XML documentation completeness
  2. Add missing `<summary>`, `<remarks>`, `<param>`, `<response>` tags
  3. Add validation error examples in `<example>` sections
  4. Build project and verify zero warnings: `dotnet build`
  5. Launch Swagger UI and verify documentation displays correctly
- **Deliverable**: Zero build warnings, complete Swagger UI documentation
- **Success Criteria**: All endpoints visible in Swagger with comprehensive descriptions

**T017**: Update Specification Traceability
- **Objective**: Document implementation traceability from spec to code
- **Actions**:
  1. Create implementation matrix mapping user stories to endpoints to tests
  2. Update spec.md with implementation status annotations
  3. Document any deviations or technical debt
  4. Update README.md with Phase 0.6 completion status
- **Deliverable**: Traceability matrix and updated documentation
- **Success Criteria**: Each user story traced to implemented endpoints and passing tests

---

## Dependencies & Constraints

### External Dependencies
- ✅ Phase 0 (Authentication) complete
- ✅ Phase 0.5 (Domain Model) complete
- ✅ EntityBase infrastructure exists
- ✅ Address value object exists
- ✅ Test infrastructure established (WebApplicationFactory, in-memory database)

### Technical Constraints
- Must maintain backward compatibility with existing 6 endpoints
- Must use Minimal APIs pattern (no controllers)
- Must follow Vertical Slice architecture (feature folders)
- Must use manual validation (no FluentValidation library)
- Must use BCrypt for password hashing (existing pattern)
- Database provider: In-memory for tests, LocalDB/SQLite for dev, Azure SQL Database (SQL Server engine) for prod

### Timeline Constraints
- Week 1: Fix failing tests (T001-T004)
- Week 2: Innovation CRUD (T005-T009)
- Week 3: Bid submission (T010-T012)
- Week 4: Journey tests + docs (T013-T017)
- Target: 3-4 weeks total (40-50 hours effort)

---

## Risks & Mitigations

### Risk 1: Database Context Lifecycle Complexity (HIGH)
**Description**: In-memory database scoping issues may be more complex than anticipated, blocking all integration tests.

**Mitigation**:
- Allocate 3 full days for T001-T004 (diagnostic + fix)
- Create minimal reproduction test case first
- Consult EF Core documentation on in-memory provider scoping
- Consider alternative: Use SQLite in-memory mode if EF Core in-memory provider too limited

**Contingency**: If database context issues persist beyond 3 days, switch to SQLite in-memory mode as temporary solution

---

### Risk 2: Scope Creep from Journey 3 (Partner Selection) (MEDIUM)
**Description**: Temptation to implement Journey 3 (Partner Selection) alongside Journey 1 and 2, inflating Phase 0.6 timeline.

**Mitigation**:
- Journey 3 explicitly out of scope for Phase 0.6
- Partner selection is complex operation (irreversibility, sufficient bids threshold, multi-actor coordination)
- Defer to Phase 0.7 or Phase 1
- Focus on proving subcutaneous testing pattern works for J1 and J2 first

**Contingency**: If ahead of schedule after T015, reassess whether to add Journey 3 or proceed to frontend work

---

### Risk 3: Test Execution Time Exceeds 30 Seconds (LOW)
**Description**: 67 tests with database setup/teardown per test may exceed acceptable CI/CD execution time.

**Mitigation**:
- Target <30 seconds for full test suite
- Use test parallelization (xUnit collections)
- Optimize database seeding (lazy initialization, shared seed data where safe)
- Profile slow tests and optimize

**Contingency**: If test suite >1 minute, introduce test categories (Unit, Integration, Journey) and run separately in CI/CD pipeline

---

## Definition of Done

### Code Quality
- ✅ All 67 tests passing (100% pass rate, excluding 3 Phase 1 domain tests)
- ✅ Zero build warnings
- ✅ All new endpoints have XML documentation
- ✅ All new endpoints have integration tests (min 1 happy path, 2 error paths)
- ✅ Code follows existing Vertical Slice architecture pattern
- ✅ Manual validation used (no FluentValidation dependency)

### Test Coverage
- ✅ Journey 1 (Innovation Submission): 4 subcutaneous tests passing
- ✅ Journey 2 (Discovery & Bidding): 3 subcutaneous tests passing
- ✅ All integration tests pass
- ✅ Test suite executes in <30 seconds

### API Completeness
- ✅ 8 new endpoints implemented (Innovation CRUD, Bid management, Industries list)
- ✅ Total 14 endpoints operational
- ✅ OpenAPI documentation complete in Swagger UI
- ✅ All endpoints follow existing authentication patterns

### Documentation
- ✅ spec.md complete with all user stories
- ✅ Implementation traceability matrix created
- ✅ README.md updated with Phase 0.6 status
- ✅ Swagger UI displays all endpoints with examples

### Validation
- ✅ Backend complete and validated via subcutaneous tests
- ✅ No UI work started (frontend remains a separate deliverable)
- ✅ All critical user journeys testable without UI
- ✅ API ready for frontend integration

---

## Success Criteria

Phase 0.6 is **COMPLETE** when:
1. ✅ **94/97 tests passing** (0 failures; 3 Phase 1 domain tests intentionally skipped with `[Fact(Skip)]`)
2. ✅ **8 new journey + integration tests per endpoint** (4 Journey 1, 3 Journey 2, plus per-endpoint integration suites)
3. ✅ **8 new API endpoints implemented** (all documented and tested)
4. ✅ **Zero build warnings**
5. ✅ **Backend validated as complete** before any frontend work begins
6. ✅ **Specification traceability** from user stories to code to tests

**Next Phase**: Phase 0.7 (Frontend) or Phase 1 (Domain Evolution) can proceed with confidence that the backend API surface is complete and working.
