# INNOVENTITY RE-ENGINEERING STRATEGY
## From Legacy MVC to Modern Spec-Driven Platform
## Reference-Based Reimplementation (NOT Data Migration)

**Document Version**: 2.0  
**Strategy Date**: February 8, 2026  
**Project Type**: Greenfield Reimplementation Using Legacy as Reference  
**Target Timeline**: 10 months (40 weeks) for v1.0 - Open Innovation  
**Approach**: Spec-First, Vertical Slice Architecture, No Data Migration

---

## EXECUTIVE SUMMARY

**Current State**: 10-year-old ASP.NET MVC application (dormant, no operational data) with strong domain model, CQRS patterns, and solid business logic.

**Target State**: Modern spec-driven platform using ASP.NET Core 8 + Angular 18, preserving domain integrity while modernizing delivery mechanisms.

**Critical Discovery**: 🔴 **Legacy system has NO operational data** - this is a **reference-based reimplementation**, NOT a data migration project.

**Strategy**: Architectural extraction from legacy as specification source. Fresh modern implementation with optimized schema.

**v1.0 Scope** (10 months): Open Innovation mode only. Closed/Hybrid deferred to v2.0.

**Key Insight**: The legacy system has **excellent domain modeling** (innovation actors, formal collaboration, virtual incubator). The architecture is sound. We're using it as a blueprint for modern reimplementation.

---

## 1. RE-ENGINEERING STRATEGY

### Core Principle

> **Extract domain specifications from legacy. Build fresh modern implementation. No data migration needed.**

### Project Type Clarification 🔴

**This is NOT a migration project.** The legacy MVC application is dormant with no operational data.

**This IS a reference-based reimplementation:**
- Legacy codebase = Domain knowledge source + Business rules documentation
- No data migration complexity (no dual-write, no staging, no cutover)
- Fresh database with modern schema from day one
- Freedom to fix legacy naming (IdeaAuthor → IdeaGenerator)
- Seed data approach for initial launch
- Legacy preserved as historical reference only

### What Changes ❌

| Legacy Component | Reason for Change |
|------------------|-------------------|
| **ASP.NET MVC Controllers** | Too much orchestration logic. Mix of concerns. |
| **Razor Views** | Server-side rendering incompatible with modern UX. No reusability. |
| **jQuery + Handlebars** | Outdated (jQuery 1.6.1). No component model. Security vulnerabilities. |
| **NHibernate 3.2** | 11 years old. No async support. Migrate to EF Core. |
| **MvcContrib CQRS** | Abandoned framework. Replace with Vertical Slices. |
| **Forms Authentication** | Replace with JWT + Identity Server or Auth0. |
| **AppHarbor Deployment** | Cloud-native deployment with containers. |
| **Manual Routing** | OpenAPI/Swagger-driven API design. |

### What Stays (Conceptually) ✅

| Domain Concept | Why It Stays | New Implementation |
|----------------|--------------|-------------------|
| **Innovation Actors** | Core business concept. Well-modeled. | Same entity hierarchy, EF Core |
| **Formal Bid Workflow** | Proven business process. | API endpoints + Angular workflow |
| **Virtual Incubator** | Unique value proposition. | Enhanced with real-time features |
| **Research-Based Innovation** | Platform differentiator. | Better categorization UX |
| **Industry Affiliation Matching** | Smart actor-innovation pairing. | Enhanced with search/filters |
| **Business Plan Development** | Critical for commercialization. | Better collaboration tools |
| **Partner Selection Rules** | Business invariants. | Domain validation layer |
| **IPR Protection (LimitedView)** | Legal/security requirement. | Role-based API access |

### What Gets Better 🚀

| Capability | Legacy | Modern |
|------------|--------|--------|
| **Innovation Discovery** | Basic listing | Advanced search, filters, recommendations |
| **Collaboration** | Email + async messaging | Real-time chat, video, document sharing |
| **Platform Modes** | Open only (Closed/Hybrid unclear) | Explicit multi-tenant with org workspace |
| **Mobile Experience** | Desktop-only | Responsive Angular SPA |
| **Performance** | Synchronous, potential N+1 queries | Async/await, optimized queries, caching |
| **Observability** | Basic logging | Structured logging, metrics, tracing |
| **API** | None (MVC views) | Full REST API, OpenAPI spec |
| **Testing** | Limited unit tests | Spec-driven E2E + integration tests |

### AI-Assisted Development Workflow

**Integration of AI tools with existing spec-first + test-first discipline**

#### Integration Philosophy

AI tools (GitHub Copilot, Copilot Chat) are **force multipliers**, not replacements for architectural thinking, domain modeling, or testing discipline. The existing spec-first + test-first workflow remains unchanged; AI accelerates the mechanical parts.

**Core Principle With AI**:
> Extract domain specifications from legacy. **Use AI to scaffold mechanical code**. Hand-write business logic. **AI-generate test stubs, human-enhance test quality**. No data migration needed.

#### When AI Adds Value

✅ **Scaffolding from Specs**: After writing YAML spec, use Copilot to generate:
- DTO classes from spec schema
- Validator stubs from spec businessRules  
- API endpoint signature from spec API section
- Test class scaffolds from spec testScenarios

✅ **Test Generation**: Use Copilot Chat to create:
- Unit test stubs for domain entities
- Integration test templates for API endpoints
- E2E test scaffolds from user journeys

✅ **Boilerplate Reduction**: Use Copilot to generate:
- Repository interface implementations
- AutoMapper profiles
- DTO transformations

#### When Human Judgment is Critical

❌ **Domain Model Design**: AI cannot understand Innoventity-specific business rules
- Innovation aggregate boundaries
- Actor hierarchy structure  
- Formal response bidding logic
- Research category taxonomy

❌ **Security & Authorization**: AI-generated security code requires manual review
- Authorization policy design
- Role-based access control rules
- IP protection logic (LimitedView pattern)

❌ **Architectural Pattern Selection**: Project already made explicit pattern decisions
- Vertical Slice Architecture (approved)
- Hub-and-Spoke (rejected)
- Micro-Frontends (rejected)
- Strategy Pattern for UI (rejected)

#### Updated Workflow (With AI)

1. **Spec-First** (Human): Write `specs/[feature]/[usecase].spec.yaml`
2. **Structure Generation** (Spec-Kit): Generate folder structure + file skeletons
3. **Mechanical Code** (AI): Use Copilot to fill endpoint signatures, DTOs, validators
4. **Business Logic** (Human): Implement domain entity methods, business rules
5. **Test Scaffolding** (AI): Generate test class stubs from spec scenarios
6. **Test Enhancement** (Human): Add edge cases, mutation testing, meaningful assertions
7. **Code Review** (Human): Verify AI code adheres to architectural principles
8. **Deploy**

#### AI Quality Gates

Every AI-generated artifact must pass:
- [ ] **Architectural Compliance**: Follows Vertical Slice Architecture
- [ ] **Spec Adherence**: Matches YAML specification
- [ ] **Security Review**: Same scrutiny as human code
- [ ] **Test Quality**: AI tests verify business logic (not just syntax)
- [ ] **Simplicity**: No unnecessary abstraction or clever code

**Reference**: See [`AI_ASSISTED_DEVELOPMENT_PRINCIPLES.md`](AI_ASSISTED_DEVELOPMENT_PRINCIPLES.md) for comprehensive epistemic testing principles.

---

## 2. TARGET ARCHITECTURE

### High-Level System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     ANGULAR SPA (v18+)                      │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐ │
│  │ Innovation     │  │ Actor          │  │ Collaboration  │ │
│  │ Management     │  │ Discovery      │  │ Workspace      │ │
│  └────────────────┘  └────────────────┘  └────────────────┘ │
│  - Standalone Components  - Reactive Forms  - Typed Clients │
│  - NgRx for state        - Angular Material - Real-time     │
└──────────────────────▲──────────────────────────────────────┘
                       │ HTTPS + WebSocket (JSON)
┌──────────────────────┴──────────────────────────────────────┐
│              ASP.NET CORE 8 WEB API                         │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              VERTICAL SLICE ARCHITECTURE               │ │
│  │  /Innovation/Submit  /Actors/Match  /Bids/Evaluate     │ │
│  └────────────────────────────────────────────────────────┘ │
│  - Minimal APIs          - FluentValidation                 │
│  - MediatR (optional)    - Problem Details                  │
│  - JWT Auth              - OpenAPI/Swagger                  │
└──────────────────────▲──────────────────────────────────────┘
                       │
┌──────────────────────┴──────────────────────────────────────┐
│             DATA + INTEGRATION LAYER                        │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │ EF Core  │  │ Dapper   │  │ Azure SB │  │ Blob     │     │
│  │ (Writes) │  │ (Reads)  │  │ (Events) │  │ Storage  │     │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘     │
│                 SQL Server / PostgreSQL                     │
└─────────────────────────────────────────────────────────────┘
```

### Vertical Slice Structure

Each feature is a self-contained slice:

```
Features/
├── Innovations/
│   ├── Submit/
│   │   ├── SubmitInnovation.cs        # Endpoint + Handler
│   │   ├── SubmitInnovation.Spec.yaml # Spec-Kit specification
│   │   ├── SubmitInnovation.Tests.cs  # Integration tests
│   │   └── SubmitInnovationValidator.cs
│   ├── GetById/
│   │   ├── GetInnovationById.cs
│   │   └── GetInnovationById.Spec.yaml
│   └── Search/
│       ├── SearchInnovations.cs
│       └── SearchInnovations.Spec.yaml
├── Bids/
│   ├── SubmitFormalResponse/
│   ├── EvaluateBids/
│   └── SelectPartners/
└── Actors/
    ├── Register/
    ├── MatchByIndustry/
    └── GetProfile/
```

### Technology Stack

#### Backend (.NET 8)
- **Framework**: ASP.NET Core 8 (Minimal APIs)
- **Database**: 
  - **Writes**: EF Core 8
  - **Reads**: Dapper for optimized queries
- **Validation**: FluentValidation
- **Messaging**: Azure Service Bus (keep existing)
- **Auth**: ASP.NET Core Identity with JWT
- **Real-time**: SignalR for collaboration workspace
- **Logging**: Serilog + OpenTelemetry
- **API Docs**: Scalar + ASP.NET Core OpenAPI (replaces Swashbuckle)

#### Frontend (Angular 18+)
- **Framework**: Angular 18 (Standalone Components)
- **UI Library**: Angular Material
- **State Management**: NgRx (for complex state)
- **Forms**: Reactive Forms with typed validation
- **HTTP**: Typed API clients (generated from OpenAPI)
- **Real-time**: SignalR client
- **Testing**: Jest + Testing Library + Playwright

#### Infrastructure
- **Containerization**: Docker + Docker Compose
- **CI/CD**: GitHub Actions
- **Deployment**: Azure App Service or AKS
- **Database**: Azure SQL
- **Storage**: Azure Blob Storage
- **Monitoring**: Application Insights

---

## 3. MIGRATION MINDSET: OLD → NEW

### Conceptual Mapping Table

| Legacy Pattern | Modern Equivalent | Notes |
|----------------|-------------------|-------|
| **MVC Controller** | Vertical Slice Endpoint | One file per feature |
| **Controller Action** | Minimal API Endpoint | `app.MapPost("/innovations", ...)` |
| **Razor View** | Angular Component | `.component.ts` + `.component.html` |
| **ViewModel** | DTO / API Contract | Shared via OpenAPI spec |
| **jQuery AJAX** | Angular HttpClient | Typed, observable-based |
| **Handlebars Template** | Angular Template | Built-in reactivity |
| **MvcContrib Command** | MediatR Request | Or direct in endpoint |
| **Command Handler** | Vertical Slice Handler | Inline or extracted |
| **Repository Pattern** | EF Core DbContext | Or thin repository layer |
| **NHibernate Session** | EF Core DbContext | Unit of Work built-in |
| **Forms Auth Cookie** | JWT Bearer Token | Stateless |
| **[Authorize] Attribute** | `[Authorize]` | With policy-based auth |
| **ValidationRule<T>** | FluentValidation | Modern, testable |
| **Global.asax** | Program.cs + Middleware | Simplified startup |

---

## 4. SPEC-KIT SPECIFICATION STRUCTURE

### Specification Categories for Innoventity

#### 1. System Context Spec (`specs/system-context.spec.yaml`)

```yaml
system:
  name: Innoventity
  description: Global Open, Hybrid, and Closed Innovation Platform
  type: Virtual Incubator
  
  platformModes:
    - Open: Public innovation discovery and participation
    - Closed: Organization-internal innovation workflows
    - Hybrid: Selective external collaboration
  
  innovationCategories:
    - Management: Management research-based innovations
    - Engineering: Engineering research-based innovations
    - NaturalScience: Natural science research-based innovations
  
  architecture:
    style: Vertical Slice
    frontend: Angular SPA
    backend: ASP.NET Core API
    database: SQL Server
    messaging: Azure Service Bus
    
  authentication:
    type: JWT
    identityProvider: ASP.NET Core Identity
    
  authorization:
    model: Role + Policy Based
    roles:
      - IdeaGenerator
      - RDOrganization
      - ManufacturingCompany
      - SalesMarketingCompany
      - Investor
      - OrganizationAdmin
      - SystemAdmin
      
  environments:
    - development
    - staging
    - production
```

#### 2. Domain Model Spec (`specs/domain-model.spec.yaml`)

```yaml
# Core Aggregates
aggregates:
  
  Innovation:
    description: Research-based innovation idea submitted to platform
    id: Guid
    properties:
      ideaToken: Guid                    # Public identifier
      title: string (required, max: 200)
      researchType: InnovationCategory enum
      researchBackground: text (required)
      hasIPR: boolean
      iprExplanation: text
      submittedOn: datetime?
      createdOn: datetime
      status: InnovationStatus enum
    
    children:
      ideaSummary: IdeaSummary (1:1)
      product: Product (1:1)
      market: Market (1:1)
      businessPlan: BusinessPlan (1:0..1)
      collaborationRequirement: CollaborationRequirement (1:1)
      
    collections:
      formalResponses: FormalResponse[]
      comments: Comment[]
      communications: IdeaCommunication[]
      
    invariants:
      - "Must have research background before submission"
      - "Must confirm right to use innovation"
      - "Cannot modify after partner selection completed"
      
  InnovationActor:
    description: Participant in innovation ecosystem
    id: Guid
    baseType: abstract
    properties:
      firstName: string (required)
      lastName: string (required)
      emailAddress: email (required, unique)
      memberSince: datetime
      status: MemberStatus enum
      activationToken: Guid
      
    subtypes:
      - IdeaGenerator: Submits innovations
      - RDOrganization: Provides technical expertise
      - ManufacturingCompany: Production capabilities
      - SalesMarketingCompany: Market strategy
      - Investor: Financial backing
      
    interfaces:
      - IIndustryAffiliated: For actor matching

  FormalResponse:
    description: "Actor's proposal to participate in innovation commercialization"
    id: Guid
    baseType: abstract
    properties:
      postedBy: InnovationActor (required)
      forInnovation: Innovation (required)
      location: string
      participationType: string
      participationProposal: text
      postedOn: datetime
      accepted: boolean (default: false)
      acceptedOn: datetime?
      
    subtypes:
      - ManufacturingResponse: Production proposal
      - SalesMarketingResponse: Market strategy proposal
      - RDResponse: Technical development proposal
      - InvestorResponse: Investment terms

# Enumerations
enums:
  InnovationStatus:
    - Draft
    - SummaryComplete
    - ProductComplete
    - MarketComplete
    - Published
    - ReceivingBids
    - SufficientBids
    - PartnersSelected
    - BusinessPlanComplete
    - CommercializationReady
    
  InnovationCategory:
    - Management
    - Engineering
    - NaturalScience
    
  MemberStatus:
    - PendingActivation
    - Active
    - Suspended
    
  PlatformMode:
    - Open
    - Closed
    - Hybrid
```

#### 3. Use Case Specs (Vertical Slices)

##### Example: Submit Innovation (`specs/innovations/submit.spec.yaml`)

```yaml
useCase:
  name: SubmitInnovation
  description: Idea generator submits research-based innovation to platform
  type: command
  actor: IdeaGenerator
  platformMode: [Open, Closed, Hybrid]
  
  api:
    method: POST
    route: /api/innovations
    authenticated: true
    
  request:
    schema:
      title: string (required, minLength: 10, maxLength: 200)
      researchType: InnovationCategory enum (required)
      researchBackground: text (required, minLength: 100)
      hasIPR: boolean (required)
      iprExplanation: text (required if hasIPR = false)
      hasRightToUse: boolean (required, mustBe: true)
      productDescription: text (required)
      keyAdvantages: text (required)
      developmentPhase: string (required)
      targetMarket: string (required)
      targetIndustries: Guid[] (required, minLength: 1)
      requiresRD: boolean (default: false)
      requiresManufacturing: boolean (default: false)
      requiresSalesMarketing: boolean (default: false)
      requiresInvestment: boolean (default: false)
      
  response:
    success:
      status: 201 Created
      schema:
        innovationId: Guid
        ideaToken: Guid
        status: InnovationStatus
        createdOn: datetime
    error:
      status: 400 Bad Request
      schema: ProblemDetails
      
  businessRules:
    - "User must be authenticated as IdeaGenerator"
    - "Title must be unique for this user"
    - "Research background must be at least 100 characters"
    - "Must confirm right to use innovation (hasRightToUse = true)"
    - "If no IPR, must provide explanation"
    - "At least one collaboration type must be required"
    - "Target industries must exist in system"
    - "In Closed mode, innovation scoped to user's organization"
    
  domainEvents:
    - InnovationSubmitted:
        innovationId: Guid
        generatorId: Guid
        researchType: InnovationCategory
        timestamp: datetime
        
  sideEffects:
    - "Send email confirmation to idea generator"
    - "Notify matched actors via Azure Service Bus"
    - "Create initial IdeaCommunication records for potential actors"
    
  testScenarios:
    - name: "Valid submission creates innovation"
      given:
        - "User authenticated as IdeaGenerator"
        - "All required fields provided"
      when: "POST request to /api/innovations"
      then:
        - "Returns 201 Created"
        - "Innovation entity persisted"
        - "Email sent"
        - "Event published"
        
    - name: "Missing research background fails"
      given:
        - "User authenticated"
        - "Research background empty"
      when: "POST request"
      then:
        - "Returns 400 Bad Request"
        - "Validation error: 'Research background is required'"
```

##### Example: Submit Formal Response (`specs/bids/submit-response.spec.yaml`)

```yaml
useCase:
  name: SubmitFormalResponse
  description: Innovation actor submits formal bid to participate in commercialization
  type: command
  actor: [RDOrganization, ManufacturingCompany, SalesMarketingCompany, Investor]
  platformMode: [Open, Closed, Hybrid]
  
  api:
    method: POST
    route: /api/innovations/{innovationId}/bids
    authenticated: true
    
  request:
    pathParameters:
      innovationId: Guid (required)
    schema:
      responseType: ResponseType enum (required)
      location: string (required)
      participationType: string (required)
      participationProposal: text (required, minLength: 200)
      
  businessRules:
    - "Innovation must be in Published or ReceivingBids status"
    - "Actor can only submit one bid per innovation per type"
    - "Actor cannot bid on own innovation"
    - "Actor's industry affiliation must match innovation's target industries"
    - "Response type must match actor type"
    - "In Closed mode, actor must belong to innovation's organization"
    
  domainEvents:
    - FormalResponseSubmitted:
        responseId: Guid
        innovationId: Guid
        actorId: Guid
        responseType: ResponseType
        
  sideEffects:
    - "Notify idea generator via email"
    - "Publish event to Azure Service Bus"
    - "Update innovation status if sufficient bids received"
```

##### Example: Select Partners (`specs/partnerships/select-partners.spec.yaml`)

```yaml
useCase:
  name: SelectCollaborationPartners
  description: Idea generator selects partners from submitted bids
  type: command
  actor: IdeaGenerator
  platformMode: [Open, Closed, Hybrid]
  
  api:
    method: POST
    route: /api/innovations/{innovationId}/partnerships/select
    authenticated: true
    
  request:
    pathParameters:
      innovationId: Guid (required)
    schema:
      selectedManufacturingBidId: Guid (required if manufacturing required)
      selectedSalesMarketingBidId: Guid (required if sales/marketing required)
      selectedRDBidId: Guid (required if R&D required)
      selectedInvestmentBidId: Guid (optional)
      
  businessRules:
    - "User must own the innovation"
    - "Innovation must be in ReceivingBids or SufficientBids status"
    - "Must select one bid from EACH required actor type"
    - "Selected bids must not already be accepted"
    - "This operation is IRREVERSIBLE once completed"
    - "Updates innovation status to PartnersSelected"
    
  invariants:
    - "HasCollaborationPartnerSelectionCompleted() = true after success"
    - "Cannot repeat selection after completion"
    
  domainEvents:
    - PartnersSelected:
        innovationId: Guid
        selectedPartnerIds: Guid[]
    - VirtualIncubatorFormed:
        innovationId: Guid
        teamMembers: ActorReference[]
        
  sideEffects:
    - "Notify selected partners via email"
    - "Create virtual incubator workspace"
    - "Grant selected partners access to full innovation details"
```

#### 4. UI View Specs

##### Example: Innovation Submission Form (`specs/ui/innovations/submit-form.spec.yaml`)

```yaml
view:
  name: InnovationSubmitPage
  description: Multi-step form for submitting research-based innovation
  route: /innovations/new
  authentication: required
  authorizedRoles: [IdeaGenerator]
  
  layout: StepperLayout
  steps:
    - step: 1
      name: IdeaSummary
      title: "Innovation Summary"
      fields:
        - name: title
          type: text
          label: "Innovation Title"
          required: true
          minLength: 10
          maxLength: 200
          
        - name: researchType
          type: select
          label: "Research Category"
          required: true
          options:
            - value: Management
              label: "Management Research"
            - value: Engineering
              label: "Engineering Research"
            - value: NaturalScience
              label: "Natural Science Research"
              
        - name: researchBackground
          type: textarea
          label: "Research Background"
          required: true
          minLength: 100
          rows: 6
          
        - name: hasIPR
          type: checkbox
          label: "This innovation has intellectual property rights (IPR)"
          
        - name: iprExplanation
          type: textarea
          label: "IPR Explanation"
          visibleWhen: "hasIPR === false"
          required: true
          
        - name: hasRightToUse
          type: checkbox
          label: "I confirm I have the right to use this innovation"
          required: true
          
    - step: 2
      name: ProductDetails
      title: "Product Information"
      fields:
        - name: productDescription
          type: richtext
          label: "Product Description"
          required: true
          
        - name: keyAdvantages
          type: textarea
          label: "Key Product Advantages"
          required: true
          
    - step: 3
      name: MarketDetails
      title: "Target Market"
      fields:
        - name: targetMarket
          type: textarea
          label: "Target Market Description"
          required: true
          
        - name: targetIndustries
          type: multiselect
          label: "Target Industries"
          required: true
          dataSource: /api/industries
          searchable: true
          
    - step: 4
      name: CollaborationNeeds
      title: "Collaboration Requirements"
      fields:
        - name: requiresRD
          type: checkbox
          label: "R&D Organization"
        - name: requiresManufacturing
          type: checkbox
          label: "Manufacturing Company"
        - name: requiresSalesMarketing
          type: checkbox
          label: "Sales & Marketing Company"
        - name: requiresInvestment
          type: checkbox
          label: "Investor"
          
  actions:
    - name: saveDraft
      type: secondary
      label: "Save as Draft"
      apiCall:
        useCase: SaveInnovationDraft
        
    - name: submitInnovation
      type: primary
      label: "Submit Innovation"
      apiCall:
        useCase: SubmitInnovation
      onSuccess:
        redirect: "/innovations/{innovationId}/dashboard"
        notification: "Innovation submitted successfully!"
```

#### 5. Cross-Cutting Specs (`specs/cross-cutting.spec.yaml`)

```yaml
crossCutting:
  
  logging:
    framework: Serilog
    structured: true
    minimumLevel: Information
    sinks:
      - Console (development)
      - ApplicationInsights (production)
    enrichers:
      - UserId
      - CorrelationId
      - Environment
      
  validation:
    framework: FluentValidation
    mode: FailFast (false)
    errorFormat: ProblemDetails
    clientValidation: Generated TypeScript from specs
    
  errorHandling:
    format: RFC 7807 Problem Details
    includeStackTrace: false (production)
    logErrors: true
    errorCodes:
      - VALIDATION_ERROR: 400
      - UNAUTHORIZED: 401
      - FORBIDDEN: 403
      - NOT_FOUND: 404
      - BUSINESS_RULE_VIOLATION: 409
      - INTERNAL_ERROR: 500
      
  authentication:
    scheme: Bearer JWT
    issuer: Innoventity API
    audience: Innoventity SPA
    tokenLifetime: 60 minutes
    refreshTokenLifetime: 7 days
    requireHttps: true (production)
    
  authorization:
    model: Policy-Based
    policies:
      - IdeaGeneratorOnly: User must be IdeaGenerator
      - ActorOnly: User must be any innovation actor
      - OwnsInnovation: User owns the innovation resource
      - SelectedPartner: User is selected partner for innovation
      - OrganizationMember: User belongs to organization (Closed mode)
      
  api:
    versioning:
      strategy: URL versioning (/api/v1/...)
      currentVersion: 1
    cors:
      allowedOrigins: [https://app.innoventity.com]
      allowedMethods: [GET, POST, PUT, DELETE, PATCH]
      allowCredentials: true
    rateLimit:
      enabled: true
      requestsPerMinute: 100
      strategy: SlidingWindow
      
  observability:
    metrics:
      provider: OpenTelemetry
      exportTo: ApplicationInsights
      customMetrics:
        - innovations_submitted_total
        - bids_received_total
        - partnerships_formed_total
    tracing:
      provider: OpenTelemetry
      samplingRate: 0.1 (production)
    healthChecks:
      endpoints:
        - /health/live
        - /health/ready
      checks:
        - Database
        - AzureServiceBus
        - BlobStorage
```

---

## 5. EXAMPLE CODE GENERATION FROM SPECS

### Backend: Vertical Slice (Generated)

```csharp
// Features/Innovations/Submit/SubmitInnovation.cs
public class SubmitInnovation : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/innovations", HandleAsync)
            .WithName("SubmitInnovation")
            .WithTags("Innovations")
            .Produces<SubmitInnovationResponse>(StatusCodes.Status201Created)
            .ProducesProblemDetails()
            .RequireAuthorization("IdeaGeneratorOnly");
    }

    private static async Task<Results<Created<SubmitInnovationResponse>, ValidationProblem>> 
        HandleAsync(
            SubmitInnovationRequest request,
            IValidator<SubmitInnovationRequest> validator,
            InnovationDbContext db,
            IPublisher publisher,
            ClaimsPrincipal user,
            CancellationToken ct)
    {
        // 1. Validate
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        // 2. Get user
        var userId = user.GetUserId();
        var ideaGenerator = await db.IdeaGenerators.FindAsync(userId, ct);
        if (ideaGenerator is null)
            return TypedResults.Problem("Idea generator not found", statusCode: 404);

        // 3. Create innovation
        var innovation = Innovation.Create(
            title: request.Title,
            researchType: request.ResearchType,
            researchBackground: request.ResearchBackground,
            hasIPR: request.HasIPR,
            iprExplanation: request.IprExplanation,
            generator: ideaGenerator);

        innovation.SetProductDetails(
            description: request.ProductDescription,
            advantages: request.KeyAdvantages,
            phase: request.DevelopmentPhase);

        innovation.SetMarketDetails(
            targetMarket: request.TargetMarket,
            industries: await db.Industries
                .Where(i => request.TargetIndustryIds.Contains(i.Id))
                .ToListAsync(ct));

        innovation.SetCollaborationRequirements(
            requiresRD: request.RequiresRD,
            requiresManufacturing: request.RequiresManufacturing,
            requiresSalesMarketing: request.RequiresSalesMarketing,
            requiresInvestment: request.RequiresInvestment);

        // 4. Save
        db.Innovations.Add(innovation);
        await db.SaveChangesAsync(ct);

        // 5. Publish event
        await publisher.Publish(
            new InnovationSubmitted(
                InnovationId: innovation.Id,
                GeneratorId: ideaGenerator.Id,
                ResearchType: innovation.ResearchType,
                Timestamp: DateTime.UtcNow), 
            ct);

        // 6. Return response
        var response = new SubmitInnovationResponse(
            InnovationId: innovation.Id,
            IdeaToken: innovation.IdeaToken,
            Status: innovation.Status,
            CreatedOn: innovation.CreatedOn);

        return TypedResults.Created($"/api/innovations/{innovation.Id}", response);
    }
}

// Validator (generated from business rules)
public class SubmitInnovationValidator : AbstractValidator<SubmitInnovationRequest>
{
    public SubmitInnovationValidator(InnovationDbContext db)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MinimumLength(10).WithMessage("Title must be at least 10 characters")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.ResearchBackground)
            .NotEmpty().WithMessage("Research background is required")
            .MinimumLength(100).WithMessage("Research background must be at least 100 characters");

        RuleFor(x => x.HasRightToUse)
            .Equal(true).WithMessage("You must confirm you have the right to use this innovation");

        When(x => !x.HasIPR, () =>
        {
            RuleFor(x => x.IprExplanation)
                .NotEmpty().WithMessage("IPR explanation is required when innovation has no IPR");
        });

        RuleFor(x => x)
            .Must(x => x.RequiresRD || x.RequiresManufacturing || 
                       x.RequiresSalesMarketing || x.RequiresInvestment)
            .WithMessage("At least one collaboration type must be required");

        RuleFor(x => x.TargetIndustryIds)
            .NotEmpty().WithMessage("At least one target industry is required")
            .MustAsync(async (ids, ct) =>
            {
                var count = await db.Industries.CountAsync(i => ids.Contains(i.Id), ct);
                return count == ids.Count;
            })
            .WithMessage("Invalid industry IDs provided");
    }
}
```

### Frontend: Angular Component (Generated)

```typescript
// src/app/features/innovations/submit/innovation-submit.component.ts
import { Component, inject } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { InnovationsService } from '../innovations.service';
import { IndustriesService } from '@/shared/services/industries.service';
import { NotificationService } from '@/shared/services/notification.service';

@Component({
  selector: 'app-innovation-submit',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
  ],
  templateUrl: './innovation-submit.component.html'
})
export class InnovationSubmitComponent {
  private fb = inject(FormBuilder);
  private innovationsSvc = inject(InnovationsService);
  private industriesSvc = inject(IndustriesService);
  private router = inject(Router);
  private notification = inject(NotificationService);

  industries$ = this.industriesSvc.getAll();

  // Step 1: Idea Summary
  ideaSummaryForm = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(200)]],
    researchType: ['', Validators.required],
    researchBackground: ['', [Validators.required, Validators.minLength(100)]],
    hasIPR: [false],
    iprExplanation: [''],
    hasRightToUse: [false, Validators.requiredTrue],
  });

  // Step 2: Product Details
  productDetailsForm = this.fb.group({
    productDescription: ['', Validators.required],
    keyAdvantages: ['', Validators.required],
    developmentPhase: ['', Validators.required],
  });

  // Step 3: Market Details
  marketDetailsForm = this.fb.group({
    targetMarket: ['', Validators.required],
    targetIndustryIds: [[] as string[], [Validators.required, Validators.minLength(1)]],
  });

  // Step 4: Collaboration Requirements
  collaborationForm = this.fb.group({
    requiresRD: [false],
    requiresManufacturing: [false],
    requiresSalesMarketing: [false],
    requiresInvestment: [false],
  });

  isSubmitting = false;

  ngOnInit() {
    // Conditional validation for IPR explanation
    this.ideaSummaryForm.get('hasIPR')?.valueChanges.subscribe(hasIPR => {
      const iprExplanation = this.ideaSummaryForm.get('iprExplanation');
      if (!hasIPR) {
        iprExplanation?.setValidators([Validators.required]);
      } else {
        iprExplanation?.clearValidators();
      }
      iprExplanation?.updateValueAndValidity();
    });

    // Custom validator for collaboration requirements
    this.collaborationForm.setValidators(this.atLeastOneCollaborationRequired());
  }

  private atLeastOneCollaborationRequired() {
    return (form: any) => {
      const hasAtLeastOne = 
        form.value.requiresRD ||
        form.value.requiresManufacturing ||
        form.value.requiresSalesMarketing ||
        form.value.requiresInvestment;
      return hasAtLeastOne ? null : { atLeastOneRequired: true };
    };
  }

  async submitInnovation() {
    if (!this.isFormValid()) return;

    this.isSubmitting = true;

    const request = {
      ...this.ideaSummaryForm.value,
      ...this.productDetailsForm.value,
      ...this.marketDetailsForm.value,
      ...this.collaborationForm.value,
    };

    this.innovationsSvc.submit(request).subscribe({
      next: (response) => {
        this.notification.success('Innovation submitted successfully!');
        this.router.navigate(['/innovations', response.innovationId, 'dashboard']);
      },
      error: (error) => {
        this.notification.error('Failed to submit: ' + error.message);
        this.isSubmitting = false;
      },
    });
  }

  private isFormValid(): boolean {
    return (
      this.ideaSummaryForm.valid &&
      this.productDetailsForm.valid &&
      this.marketDetailsForm.valid &&
      this.collaborationForm.valid
    );
  }
}
```

---

## 6. PHASED IMPLEMENTATION APPROACH (v1.0 - Open Innovation)

**Total Timeline**: 10 months (40 weeks) for v1.0  
**Scope**: Open Innovation mode only (5 actor types)  
**Deferred to v2.0**: Closed/Hybrid modes, multi-tenancy, organizations, Government/Tech Park actors

### 🚀 PRODUCTION-READY DELIVERY PRINCIPLE

**Critical Requirement**: Starting from **Phase 1**, every phase must deliver **production-ready code**.

**What "Production-Ready" Means**:
- ✅ **All new features**: Fully tested (unit + integration + E2E)
- ✅ **All existing features**: Regression tested (no breaking changes)
- ✅ **Deployable**: Can be deployed to production environment at phase end
- ✅ **Documented**: API documentation, user guides updated
- ✅ **Monitored**: Observability (logs, metrics, alerts) configured
- ✅ **Performant**: Meets performance targets (p95 < 200ms)
- ✅ **Secure**: Security review passed, no critical vulnerabilities

**Continuous Delivery Approach**:
- Each phase = production-ready increment
- No "we'll test it later" or "we'll fix it in Phase 4"
- Quality gates enforced at phase boundaries
- Can go live at any phase end (business decision, not technical blocker)

**Testing Requirements Per Phase**:
1. **Unit Tests**: 80%+ coverage for business logic
2. **Integration Tests**: All API endpoints with WebApplicationFactory
3. **E2E Tests**: Critical user journeys with Playwright
4. **Regression Tests**: All previously implemented features still work
5. **Load Tests**: Performance validation for new endpoints

---

### Phase 0: Foundation (Weeks 1-4) ✅ **PLANNING COMPLETE**

**Goal**: Set up infrastructure and prove the stack works end-to-end.

**Deliverables**:
- ✅ ASP.NET Core 8 API skeleton
- ✅ Angular 18 SPA skeleton
- ✅ Fresh database schema (EF Core + SQL Server) - no migration from legacy
- ✅ Authentication (JWT + ASP.NET Identity)
- ✅ CI/CD pipeline (GitHub Actions)
- ✅ Docker Compose for local development
- ✅ OpenAPI documentation with Scalar UI
- ✅ One working vertical slice end-to-end:
  - Example: "Get Innovation by ID" (simple read)
  - Backend endpoint + EF Core query
  - Angular service + component
  - Integration test

**Success Criteria**: Can deploy a "Hello World" innovation platform to Azure with auth working.

**Note**: No data migration - fresh database with seed data approach.

---

### Phase 1: Core Innovation Flow (Weeks 5-12)

**Goal**: Implement the primary user journey - innovation submission and discovery.

**Vertical Slices to Implement**:
1. **Actor Registration**
   - Register as Idea Generator
   - Register as R&D Org / Manufacturing / Sales / Investor
   - Industry affiliation selection
   
2. **Innovation Submission**
   - Submit innovation (full multi-step form)
   - Save as draft
   - View own innovations (dashboard)
   
3. **Innovation Discovery**
   - Search innovations by industry/category
   - View innovation limited details (IP protection)
   - Filter by research type (Management/Engineering/NaturalScience)
   
4. **Actor Profile**
   - View own profile
   - Update industry affiliations

**Database**:
- Implement `Member` hierarchy (IdeaGenerator, RDOrganization, Manufacturer, SalesMarketing, Investor)
- Implement `ProductIdea` aggregate with modern naming
- Seed `Industry` and `Subsector` master data
- No legacy data migration needed

**Testing Requirements**:
- ✅ **Unit tests for all domain entities and validators**
  - Use GitHub Copilot to scaffold test classes: `"Generate xUnit tests for IdeaGenerator entity"`
  - **Human validates**: Tests written FIRST or deliberately broken to prove detection (characterization)
  - **Human enhances**: Add edge cases AI missed, mutation testing (target >70% score)
  - **Epistemic gate**: Can reviewer explain what bug each test would catch?
- ✅ **Integration tests for all 4 vertical slices**
  - Use Copilot Chat to generate WebApplicationFactory tests from API specs
  - **Human reviews**: Verify tests call actual business logic, not just HTTP mocks
  - **Meaningful assertions**: Not just status codes, check domain outcomes
- ✅ **E2E tests for complete user journey**: Register → Submit Innovation → Discover Innovation
  - Use Copilot to scaffold Playwright tests from user journey specs
  - **Human reviews**: Add business outcome assertions, not just UI element checks
- ✅ **Regression tests**: Phase 0 authentication still works
- ✅ **Load tests**: Innovation submission and search endpoints meet p95 < 200ms
- ✅ **AI-Generated Test Quality Gate**: All AI tests reviewed by human for:
  - Actual business logic coverage (not just syntax validation)
  - Tests observed failing FIRST (red phase in TDD)
  - Edge cases and error handling
  - Meaningful assertions (not just "returns 200 OK")

**Production Readiness Checklist**:
- ✅ All features deployed to staging and validated
- ✅ API documentation (Scalar UI) updated with new endpoints
- ✅ Database migrations tested and documented
- ✅ Monitoring configured (Application Insights tracking)
- ✅ Security review: Input validation, authorization checks
- ✅ Performance benchmarks established

**Success Criteria**: 
- ✅ Idea generators can submit innovations and actors can discover them
- ✅ **Production-ready**: System is deployable with Phase 1 features live
- ✅ All tests passing (unit, integration, E2E)
- ✅ No critical bugs or security vulnerabilities

---

### Phase 2: Collaboration & Bidding (Weeks 13-20)

**Goal**: Implement the formal response (bidding) and partner selection workflows.

**Vertical Slices to Implement**:
1. **Submit Formal Response**
   - Manufacturing bid submission (cost/volume/distribution)
   - Sales/Marketing bid submission (market strategy)
   - R&D bid submission (technical development)
   - Investment bid submission (funding proposal)
   
2. **Bid Management**
   - View bids on innovation (owner only)
   - View my submitted bids (actor)
   - Edit/withdraw bid
   
3. **Partner Selection**
   - Select collaboration partners (one from each required type)
   - Validate business rules (one Manufacturing, one Sales, one R&D)
   - Virtual incubator team formation
   - Notification to selected partners

**Real-time Features** (NEW):
- SignalR hub for bid notifications
- Real-time bid count updates

**Testing Requirements**:
- ✅ **Unit tests for bid validation logic and business rules**
  - Use Copilot to generate test stubs for BidValidator, PartnerSelectionValidator
  - **Human-critical**: Partner selection rules (one per type) are business-critical → human writes tests first
  - **Observe failure**: Tests must fail before implementing validation logic
- ✅ **Integration tests for all bid submission and partner selection endpoints**
  - AI scaffolds WebApplicationFactory tests from specs
  - **Human validates**: Authorization checks (only innovation owner can select partners)
  - **Specific assertions**: Check bid persisted correctly, notifications sent
- ✅ **E2E tests**: Submit bid → View bids → Select partners → Verify notifications
  - Copilot generates Playwright test scaffolds
  - **Human adds**: Business outcome checks (selected partners receive notifications)
- ✅ **Real-time tests**: SignalR connection and notification delivery
  - AI generates SignalR client test boilerplate
  - **Human validates**: Actual message delivery, not just connection success
- ✅ **Regression tests**: Phase 0 auth + Phase 1 innovation flow still working
- ✅ **Load tests**: Concurrent bid submissions, partner selection under load
- ✅ **AI Quality Gate**: Partner selection logic is **high-risk** → mandatory characterization testing

**Production Readiness Checklist**:
- ✅ All Phase 2 features deployed to staging
- ✅ Regression testing: All Phase 0 and Phase 1 features validated
- ✅ API documentation updated (bid endpoints)
- ✅ SignalR hub configuration documented
- ✅ Azure Service Bus notifications verified
- ✅ Security review: Bid authorization, partner selection validation
- ✅ Performance validated: Bid submission and selection meet targets

**Success Criteria**: 
- ✅ Full bidding and partner selection workflow operational
- ✅ **Production-ready**: Phases 0, 1, and 2 all deployable and tested
- ✅ All tests passing including regression suite
- ✅ Real-time features working reliably

---

### Phase 3: Virtual Incubator (Weeks 21-28)

**Goal**: Implement business planning and incubator operations.

**Vertical Slices to Implement**:
1. **Business Plan Development**
   - Create business plan
   - Financial projections and investment modeling
   - Ownership structure definition
   - Competitive analysis
   
2. **Management Team**
   - Add team members from selected partners
   - Define roles and responsibilities
   
3. **Project Valuation**
   - Calculate project valuation
   - View financial projections

**Enhanced Features** (NEW):
- Collaborative editing (real-time with SignalR)
- Document upload (Azure Blob Storage)
- Financial calculator tools
- Export business plan to PDF

**Testing Requirements**:
- ✅ Unit tests for business plan domain logic and financial calculations
- ✅ Integration tests for business plan CRUD and document upload
- ✅ E2E tests: Create business plan → Add financials → Upload documents → Export PDF
- ✅ Real-time tests: Collaborative editing with multiple users
- ✅ Regression tests: Phases 0, 1, and 2 features still functioning
- ✅ Load tests: Document upload, PDF generation under concurrent load

**Production Readiness Checklist**:
- ✅ All Phase 3 features deployed to staging
- ✅ Comprehensive regression testing: Phases 0, 1, 2, and 3
- ✅ API documentation updated (business plan endpoints)
- ✅ Azure Blob Storage configuration documented
- ✅ PDF generation service tested and optimized
- ✅ Security review: Document access control, financial data protection
- ✅ Performance validated: Collaborative editing latency acceptable

**Success Criteria**: 
- ✅ Virtual incubator teams can build business plans collaboratively
- ✅ **Production-ready**: Complete platform (Phases 0-3) deployable
- ✅ Full regression test suite passing
- ✅ Document storage and PDF generation reliable

---

### Phase 4: Final Optimization & Launch Preparation (Weeks 29-36)

**Goal**: Final performance optimization, UX polish, and production launch preparation.

**Note**: Platform is already production-ready from Phase 3. This phase focuses on optimization and enhancements for initial launch.

**Optimization Work**:
- Performance tuning (query optimization, caching strategy)
- Advanced search capabilities (filters, sorting)
- Mobile-responsive improvements
- Accessibility compliance (WCAG 2.1 AA)
- UI/UX polish based on user testing

**Additional Features**:
- Email notifications (refined templates for all workflows)
- In-app notifications panel
- User activity feeds
- Basic analytics dashboard (innovations, bids, partnerships)

**Final Launch Validation**:
- Comprehensive load testing (JMeter scripts for all critical paths)
- Full security audit and penetration testing
- Disaster recovery plan and failover testing
- Production runbooks and operational documentation
- Performance monitoring and alerting (Application Insights)
- Full regression test suite across all phases
- User acceptance testing (UAT) with beta users

**Testing Requirements**:
- ✅ Performance testing: All endpoints meet targets under load
- ✅ Security testing: Penetration testing passed, vulnerabilities addressed
- ✅ Comprehensive regression: All features from Phases 0-4 validated
- ✅ UAT: Real users complete critical journeys successfully
- ✅ Disaster recovery: Backup/restore procedures tested

**Success Criteria**: 
- ✅ Platform optimized and ready for public launch
- ✅ All Open Innovation workflows polished and performant
- ✅ Security audit passed with no critical issues
- ✅ Operations team trained with documented runbooks

---

### Phase 5: Production Launch & Stabilization (Weeks 37-40)

**Goal**: Public production launch and post-launch stabilization.

**Launch Activities**:
- Production deployment to live environment
- DNS cutover and traffic routing
- User onboarding and training materials
- Marketing and communication launch
- Real-time monitoring and on-call support

**Stabilization Activities**:
- Monitor system health and performance metrics
- Rapid response to production issues
- Bug fixes and quick wins based on real usage
- Gather user feedback for continuous improvement
- Performance optimization based on actual usage patterns

**Testing Requirements**:
- ✅ Production smoke tests: All critical paths working in live environment
- ✅ Monitoring validation: Alerts firing correctly, dashboards accurate
- ✅ Rollback procedures: Tested and documented
- ✅ Continuous regression: Automated tests running against production

**Success Criteria**: 
- ✅ Stable production platform with active users
- ✅ Users successfully completing full innovation workflows (submission → bidding → partnership → business plan)
- ✅ System uptime ≥ 99.9%
- ✅ Performance targets met under real user load
- ✅ Zero critical production incidents
- ✅ Positive user feedback and engagement metrics

---

### 🔮 v2.0 Future Roadmap (Post-Week 40)

**Deferred Features** (requires separate planning):
- **Closed Innovation Mode**: Organization-scoped innovations with multi-tenancy
- **Hybrid Innovation Mode**: Selective external collaboration with approval workflows
- **Organization Management**: Workspace management, member invitations, org admin roles
- **Government/Technology Park Actors**: Additional actor type with unique capabilities
- **Advanced Research Categorization**: Workflow differences by research type (if needed)
- **Multi-Language Support**: Internationalization (i18n)
- **Advanced Analytics**: Dashboards, reporting, insights
- **Mobile Native Apps**: iOS/Android applications

---

## 7. ARCHITECTURAL PATTERN REVIEW - CRITICAL LEARNINGS

### Patterns Evaluated and Rejected ❌

During architectural planning, several patterns were considered and **explicitly rejected** to avoid over-engineering:

#### 1. Hub-and-Spoke Architecture ❌ REJECTED
**Why considered**: Centralize communication through innovation hub  
**Why rejected**: 
- Innoventity is a **network topology** (actors collaborate directly via partnerships)
- NOT a hub-mediated system
- Would introduce unnecessary intermediary layer
- **Better approach**: Direct actor-to-innovation relationships with partnership entities

#### 2. Micro-Frontends ❌ REJECTED
**Why considered**: Isolate actor-specific UI modules  
**Why rejected**:
- Massive over-engineering for current scale
- Adds deployment complexity (independent versioning, module federation)
- Angular feature modules with lazy loading achieve same isolation
- **Better approach**: Angular standalone components + feature modules with clear boundaries

#### 3. Strategy Pattern for UI ❌ REJECTED
**Why considered**: Handle actor type variations in UI  
**Why rejected**:
- Pattern mismatch - Strategy Pattern is for runtime algorithm selection
- Actor types need different FORMS and VIEWS, not different algorithms
- **Better approach**: 
  - Polymorphic domain entities (already have: `IdeaGenerator`, `Manufacturer`, etc.)
  - Angular routing with actor-type guards
  - Type-specific components (natural Angular pattern)

### Patterns Approved and Scoped ✅

#### 1. Role-Based Access Control (RBAC) ✅ APPROVED
- ASP.NET Core Identity native support
- Policy-based authorization
- Actor type = role in v1.0
- Clear fit for Open Innovation mode

#### 2. Domain Events (Scoped) ✅ APPROVED WITH LIMITS
- **Use for**: Real-time collaboration workspace (SignalR)
- **Don't use for**: Everything (not creating event-sourced system)
- Scoped to where real-time value exists
- Most workflows remain synchronous request-response

#### 3. Shared/Core Modules ✅ APPROVED
- Angular shared module for common UI components
- Backend Core project for cross-cutting concerns
- Clear boundaries, no circular dependencies

### Key Architectural Principle Established

> **Simplicity over cleverness. Use patterns where they solve actual problems, not where they sound impressive.**

**Decision Method**: Every pattern must answer:
1. What specific problem does this solve?
2. What simpler alternative exists?
3. What maintenance burden does this add?
4. Does the benefit exceed the cost?

---

## 8. IMPLEMENTATION TOOLING & AUTOMATION

### Spec-Kit Integration

**Workflow**:
1. **Write spec** in YAML (use cases, UI, tests)
2. **Generate scaffolding**:
   ```bash
   spec-kit generate --spec innovations/submit.spec.yaml
   ```
3. **Generated artifacts**:
   - API endpoint skeleton
   - Request/Response DTOs
   - Validator
   - Angular service method
   - Angular component skeleton
   - Integration test template
4. **Fill in domain logic** (the part that's unique to Innoventity)
5. **Iterate**: Regenerate safely when specs evolve

### Database Strategy (No Data Migration)

**Approach**: Fresh modern schema with seed data (legacy is dormant with no operational data).

**Tools**:
- **EF Core Migrations**: For schema creation and evolution
- **Seed Data Scripts**: For master data (industries, subsectors, roles)
- **Test Data Generators**: For realistic development/demo data

**Process**:
1. **Week 1**: Design modern schema with optimized naming (IdeaAuthor → IdeaGenerator)
2. **Week 1-2**: Create EF Core entities and initial migration
3. **Week 2**: Seed master data (industries, research categories)
4. **Week 3**: Create test data generators for development
5. **Ongoing**: Schema evolution through EF Core migrations

**Benefits of Fresh Start**:
- ✅ Modern naming conventions from day one
- ✅ Optimized schema design (no legacy constraints)
- ✅ No migration complexity or data transformation
- ✅ No dual-write or synchronization patterns needed
- ✅ Legacy codebase remains as reference only

### Testing Strategy

**Levels**:
1. **Unit Tests**: Domain logic and validators (xUnit)
2. **Integration Tests**: API endpoints (WebApplicationFactory)
3. **E2E Tests**: Playwright for critical user journeys
4. **Load Tests**: JMeter for performance validation

**Automation**:
- Tests run on every PR (GitHub Actions)
- Coverage reports with quality gates (80%+ for business logic)
- Playwright tests run against staging environment
- Performance tests run weekly on staging

**Coverage Target**: 80%+ for business logic, 100% for critical paths.

---

## 9. RISK MITIGATION

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **Learning Curve** | High | Medium | Solo project with flexible timeline, focus on learning |
| **Performance Degradation** | Medium | High | Load testing early, optimize queries, caching strategy |
| **User Adoption Issues** | Low | Medium | Phased rollout, training, beta testers, feedback loops |
| **Auth/Security Gaps** | Low | Critical | Security audit, penetration testing, OWASP checklist |
| **Spec-Kit Learning Curve** | High | Low | Start with simple slices, build templates, documentation |
| **Closed/Hybrid Mode Unknowns** | High | High | **Stakeholder validation ASAP**, prototype early |
| **Angular Complexity** | Medium | Medium | Use standalone components, keep simple, incremental |
| **Deadline Pressure** | High | Medium | MVP-first, defer nice-to-haves, buffer time |
| **AI-Generated Tests Without Epistemic Validation** | High | Critical | Mandatory test review checklist, mutation testing (>70% score), human validates critical paths |
| **AI Code Violates Vertical Slice Architecture** | Medium | High | AI-specific code review checklist (verify slice boundaries), reject cross-slice dependencies, refine prompts |
| **Security Vulnerabilities in AI-Generated Authorization** | Low | Critical | NEVER accept AI authorization code without human review, penetration testing for authorization |

---

## 9. SUCCESS METRICS

### Technical Metrics
- **API Response Time**: p95 < 200ms
- **Page Load Time**: < 2 seconds
- **Test Coverage**: > 80%
- **Uptime**: 99.9%
- **Zero Data Loss**: 100% data migration accuracy

### Business Metrics
- **User Retention**: Match or exceed legacy
- **Innovation Submission Rate**: +20% (better UX)
- **Collaboration Formation Rate**: +30% (real-time features)
- **Mobile Usage**: +50% (responsive design)

### Developer Metrics
- **Spec-to-Code Ratio**: 70% code-generated from specs
- **Time to Add Feature**: 50% reduction vs. legacy
- **Bug Rate**: < 5% defects per release
- **Developer Satisfaction**: Measurably improved

---

## 10. IMMEDIATE NEXT STEPS

### Week 1 Actions ⚠️ **CRITICAL**

1. **Stakeholder Validation**
   - Confirm Closed and Hybrid innovation mode requirements
   - Clarify research category usage (Management/Engineering/Natural Science)
   - Validate target architecture with business

2. **Setup Development Environment**
   - Clone repository
   - Install .NET 8 SDK, Node 20+, Docker
   - Setup Azure subscription for development

3. **Spec-Kit Evaluation**
   - Review Spec-Kit documentation
   - Create first sample spec (Get Innovation by ID)
   - Test code generation capabilities

4. **Create Foundation Repository**
   - Initialize ASP.NET Core 8 API project
   - Initialize Angular 18 project
   - Setup Docker Compose
   - Configure CI/CD pipeline skeleton

5. **First Vertical Slice**
   - Choose: "Get Innovation by ID" (simple, low risk)
   - Write spec
   - Generate + implement
   - Deploy to dev environment
   - **Prove the stack works**

### Week 2-4: Build Momentum

- Add authentication (JWT + Identity)
- Implement 2-3 more simple slices (read operations)
- Setup database migration strategy
- Create Angular component library
- Write team documentation

---

## CONCLUSION

This is not a rewrite. This is an **architectural extraction**.

You're taking 10 years of domain knowledge embedded in a solid but aging codebase, and transferring it into a modern, spec-driven platform.

**Core Strategy**:
- ✅ Preserve domain integrity (the innovation actors, formal bids, virtual incubator)
- ✅ Modernize delivery (ASP.NET Core API + Angular SPA)
- ✅ Extract specifications (make knowledge durable with Spec-Kit)
- ✅ Migrate incrementally (vertical slices, fail fast, learn fast)

**What Makes This Successful**:
1. Your existing domain model is **sound**
2. You're using **proven modern tech** (not experimental)
3. Spec-Kit gives you **generative leverage**
4. Phased migration **reduces risk**
5. Clear success criteria **keep focus**

**First Proof Point**: One working vertical slice in 4 weeks. If that works, the rest is execution.

---

**Document Prepared By**: Architecture Team  
**Date**: February 8, 2026  
**Status**: ✅ Ready for Phase 0 Implementation  
**Related Documents**: 
- [ARCHITECTURAL_INVENTORY.md](ARCHITECTURAL_INVENTORY.md) - Legacy system analysis
- [PHASE_0_IMPLEMENTATION_GUIDE.md](PHASE_0_IMPLEMENTATION_GUIDE.md) - Coming next
- [SPEC_KIT_TEMPLATE_LIBRARY.md](SPEC_KIT_TEMPLATE_LIBRARY.md) - Coming next

---

## REVISION HISTORY

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-02-08 | Architecture Team | Initial strategy document created |
