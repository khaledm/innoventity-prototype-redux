# ARCHITECTURE GUARDRAILS FOR v1.0 DEVELOPMENT
## Keeping v2.0 Multi-Tenancy Options Open

**Document Version**: 1.0  
**Created**: February 8, 2026  
**Purpose**: Guide v1.0 development to avoid blocking v2.0 features (Closed/Hybrid modes, multi-tenancy)  
**Principle**: *"Don't build multi-tenancy now, but don't make it impossible later"*

---

## WHY THIS MATTERS

v1.0 focuses exclusively on **Open Innovation** mode (single-tenant, public discovery).

v2.0 will add:
- **Closed Innovation Mode**: Organization-scoped innovations with internal actors only
- **Hybrid Innovation Mode**: Selective external collaboration with organization control
- **Multi-Tenancy**: Organization context for data isolation
- **Government/Technology Park Actors**: 6th actor type

**Risk**: If v1.0 scatters single-tenant assumptions throughout the codebase, v2.0 will require massive refactoring instead of incremental feature addition.

**Goal**: Write clean, boundary-respecting v1.0 code that makes v2.0 additions straightforward.

---

## ARCHITECTURAL GUARDRAILS

### ✅ DO: Repository Pattern with Clean Abstractions

**v1.0 Pattern**:
```csharp
public interface IInnovationRepository
{
    Task<Innovation> GetByIdAsync(Guid id);
    Task<IEnumerable<Innovation>> GetPublishedAsync(int page, int pageSize);
    Task SaveAsync(Innovation innovation);
}

// v1.0 implementation: Simple queries
public class InnovationRepository : IInnovationRepository
{
    public async Task<Innovation> GetByIdAsync(Guid id)
    {
        return await _context.Innovations
            .FirstOrDefaultAsync(i => i.Id == id);
    }
}
```

**v2.0 Evolution** (non-destructive):
```csharp
// v2.0: Add organization context to repository methods
public interface IInnovationRepository
{
    Task<Innovation> GetByIdAsync(Guid id, Guid? organizationId = null);
    // organizationId = null means Open Innovation (public)
    // organizationId set means Closed/Hybrid (scoped)
}

// v2.0 implementation: Add WHERE clause
public async Task<Innovation> GetByIdAsync(Guid id, Guid? organizationId = null)
{
    var query = _context.Innovations.Where(i => i.Id == id);
    
    if (organizationId.HasValue)
        query = query.Where(i => i.OrganizationId == organizationId.Value);
        
    return await query.FirstOrDefaultAsync();
}
```

**Why This Works**: Repository abstraction hides query complexity. Callers don't change when organization context is added.

---

### ✅ DO: Policy-Based Authorization (Not Hard-Coded Checks)

**v1.0 Pattern**:
```csharp
// Define authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InnovationOwner", policy =>
        policy.Requirements.Add(new InnovationOwnerRequirement()));
        
    options.AddPolicy("IdeaGeneratorRole", policy =>
        policy.RequireRole("IdeaGenerator"));
});

// Use in endpoints
app.MapGet("/api/innovations/{id}", 
    [Authorize(Policy = "InnovationOwner")] 
    async (Guid id, IInnovationRepository repo) => 
{
    return await repo.GetByIdAsync(id);
});

// Handler checks ownership
public class InnovationOwnerHandler : AuthorizationHandler<InnovationOwnerRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        InnovationOwnerRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // v1.0: Check if user owns the innovation
        // v2.0: Add organization membership check here
        
        if (/* ownership check passes */)
            context.Succeed(requirement);
            
        return Task.CompletedTask;
    }
}
```

**v2.0 Evolution** (non-destructive):
```csharp
// v2.0: Add organization membership requirement
options.AddPolicy("OrganizationMember", policy =>
    policy.Requirements.Add(new OrganizationMemberRequirement()));

// v2.0: Update handler to check organization context
protected override Task HandleRequirementAsync(...)
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var orgId = context.User.FindFirst("OrganizationId")?.Value;
    
    // Check ownership AND organization membership
    if (/* user owns innovation */ && /* user in organization */)
        context.Succeed(requirement);
}
```

**Why This Works**: Policy handlers encapsulate authorization logic. New checks added in one place.

---

### ✅ DO: Actor Type Extensibility via Inheritance

**v1.0 Pattern**:
```csharp
public abstract class InnovationActor
{
    public Guid Id { get; protected init; }
    public string Email { get; protected init; } = string.Empty;
    public ActorType Type { get; protected init; }
    public DateTime CreatedAt { get; protected init; }
    
    // Common actor behavior
    public abstract bool CanBidOn(Innovation innovation);
}

public class IdeaGenerator : InnovationActor
{
    public IdeaGenerator() => Type = ActorType.IdeaGenerator;
    
    public override bool CanBidOn(Innovation innovation) => false; // Authors don't bid
}

public class ManufacturingCompany : InnovationActor
{
    public ManufacturingCompany() => Type = ActorType.Manufacturing;
    
    public ProductionCapacity Capacity { get; set; }
    
    public override bool CanBidOn(Innovation innovation) => 
        innovation.RequiresManufacturing;
}

public enum ActorType
{
    IdeaGenerator,
    RDOrganization,
    Manufacturing,
    SalesMarketing,
    Investor
    // v2.0: Add Government, TechnologyPark (just add to enum)
}
```

**v2.0 Evolution** (non-destructive):
```csharp
// v2.0: Add new actor types
public enum ActorType
{
    IdeaGenerator,
    RDOrganization,
    Manufacturing,
    SalesMarketing,
    Investor,
    Government,        // NEW
    TechnologyPark     // NEW
}

public class GovernmentActor : InnovationActor
{
    public GovernmentActor() => Type = ActorType.Government;
    
    public PolicyArea PolicyFocus { get; set; }
    
    public override bool CanBidOn(Innovation innovation) => 
        innovation.IsEligibleForGovernmentSupport;
}
```

**Why This Works**: Polymorphism + enum. New actor types extend base class, no refactoring of existing types.

---

### ✅ DO: Feature-Specific Validation Rules

**v1.0 Pattern**:
```csharp
public class SubmitInnovationValidator : AbstractValidator<SubmitInnovationCommand>
{
    public SubmitInnovationValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200);
            
        RuleFor(x => x.ResearchCategory)
            .IsInEnum().WithMessage("Invalid research category")
            .NotEqual(ResearchCategory.Unknown);
            
        // v1.0: Open Innovation validation rules
        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(100).WithMessage("Innovation requires detailed description");
    }
}
```

**v2.0 Evolution** (non-destructive):
```csharp
// v2.0: Add mode-specific validators
public class SubmitClosedInnovationValidator : SubmitInnovationValidator
{
    public SubmitClosedInnovationValidator()
    {
        // Inherit base rules, add closed-specific
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("Closed innovations must belong to organization");
            
        RuleFor(x => x.InternalProjectCode)
            .NotEmpty().WithMessage("Internal tracking code required");
    }
}

// Use validator based on innovation mode
var validator = command.Mode == InnovationMode.Closed
    ? new SubmitClosedInnovationValidator()
    : new SubmitInnovationValidator();
```

**Why This Works**: Validator inheritance. Base rules shared, mode-specific rules added in subclasses.

---

### ❌ DON'T: Hard-Code Single-Tenant Assumptions Everywhere

**Anti-Pattern (v1.0 mistake)**:
```csharp
// BAD: Direct database queries scattered throughout
public async Task<List<Innovation>> GetPublishedIdeas()
{
    // Hard-coded assumption: ALL innovations are public
    return await _context.Innovations
        .Where(i => i.IsPublished)
        .ToListAsync();
}

// BAD: Global static organization reference
public static class CurrentContext
{
    public static Guid OrganizationId => Guid.Empty; // "Open Innovation" org
}

// BAD: Hard-coded WHERE clauses everywhere
.Where(i => i.OrganizationId == null) // Assumes null = public
```

**Why This Fails**: 
- Changing queries from single-tenant to multi-tenant requires finding every query in codebase
- Static organization context is global mutable state (disaster for testing)
- Hard-coded nulls have no semantic meaning (null = public? or missing data?)

**Correct Approach**:
```csharp
// GOOD: Repository method with semantic meaning
public async Task<List<Innovation>> GetPublicInnovationsAsync()
{
    // Implementation detail hidden
    return await _context.Innovations
        .Where(i => i.IsPublished && i.Visibility == InnovationVisibility.Public)
        .ToListAsync();
}

// v2.0: Add new method, don't modify existing
public async Task<List<Innovation>> GetOrganizationInnovationsAsync(Guid orgId)
{
    return await _context.Innovations
        .Where(i => i.OrganizationId == orgId)
        .ToListAsync();
}
```

---

### ❌ DON'T: Switch Statements on Actor Types in Business Logic

**Anti-Pattern (v1.0 mistake)**:
```csharp
// BAD: Switch on actor type (breaks Open/Closed Principle)
public decimal CalculateBidWeight(Bid bid)
{
    switch (bid.Actor.Type)
    {
        case ActorType.Manufacturing:
            return bid.ProductionCapacity * 0.3m;
        case ActorType.RDOrganization:
            return bid.ResearchExpertise * 0.4m;
        case ActorType.SalesMarketing:
            return bid.MarketReach * 0.2m;
        case ActorType.Investor:
            return bid.FundingAmount * 0.1m;
        default:
            throw new InvalidOperationException("Unknown actor type");
    }
}

// v2.0: Add Government actor → must modify switch everywhere
```

**Why This Fails**: Adding new actor type requires finding every switch statement. Violates Open/Closed Principle.

**Correct Approach**:
```csharp
// GOOD: Polymorphism (actor computes own weight)
public abstract class Bid
{
    public InnovationActor Actor { get; protected init; }
    public DateTime SubmittedAt { get; protected init; }
    
    public abstract decimal CalculateWeight();
}

public class ManufacturingBid : Bid
{
    public int ProductionCapacity { get; set; }
    
    public override decimal CalculateWeight() => ProductionCapacity * 0.3m;
}

public class RDBid : Bid
{
    public int ResearchExpertise { get; set; }
    
    public override decimal CalculateWeight() => ResearchExpertise * 0.4m;
}

// v2.0: Add new bid type, no changes to existing code
public class GovernmentBid : Bid
{
    public decimal PolicySupport { get; set; }
    
    public override decimal CalculateWeight() => PolicySupport * 0.25m;
}
```

---

### ❌ DON'T: Nest Organization Logic Deep in Domain Entities (Yet)

**Anti-Pattern (v1.0 mistake)**:
```csharp
// BAD: Adding organization properties to v1.0 entities "just in case"
public class Innovation
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    
    // DON'T add these in v1.0 if not using them
    public Guid? OrganizationId { get; set; }  // Unused in v1.0
    public Organization? Organization { get; set; }  // Navigation property unused
    public InnovationMode Mode { get; set; }  // Only Open mode exists in v1.0
}
```

**Why This Fails**: 
- Unused columns in database
- Confusing code (why is OrganizationId always null?)
- Forces migration when adding v2.0 (could have just added new columns then)

**Correct Approach**:
```csharp
// GOOD: v1.0 entity without unused properties
public class Innovation
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public ResearchCategory ResearchCategory { get; set; }  // Simple enum
    public InnovationVisibility Visibility { get; set; }  // Public in v1.0
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }  // IdeaGenerator actor ID
}

// v2.0: Add organization via migration
ALTER TABLE Innovations ADD OrganizationId uniqueidentifier NULL;
ALTER TABLE Innovations ADD Mode int NOT NULL DEFAULT 0; -- Open mode

// v2.0: Update entity
public class Innovation
{
    // ... existing properties ...
    public Guid? OrganizationId { get; set; }  // NOW we add it
    public InnovationMode Mode { get; set; }
}
```

**YAGNI Principle**: You Aren't Gonna Need It. Don't add organization properties until v2.0 actually needs them.

---

## DATABASE SCHEMA STRATEGY

### ✅ v1.0 Schema (Simple, No Multi-Tenancy)

```sql
-- Innovations table (Open Innovation only)
CREATE TABLE Innovations (
    Id uniqueidentifier PRIMARY KEY,
    Title nvarchar(200) NOT NULL,
    Description nvarchar(max) NOT NULL,
    ResearchCategory int NOT NULL,  -- 0=Management, 1=Engineering, 2=NaturalScience
    Visibility int NOT NULL DEFAULT 0,  -- 0=Public
    CreatedAt datetime2 NOT NULL,
    CreatedBy uniqueidentifier NOT NULL,  -- FK to InnovationActors
    SubmittedAt datetime2 NULL,
    -- NO OrganizationId column in v1.0
    
    INDEX IX_Innovations_CreatedBy (CreatedBy),
    INDEX IX_Innovations_ResearchCategory (ResearchCategory)
);

-- Actors table (5 types in v1.0)
CREATE TABLE InnovationActors (
    Id uniqueidentifier PRIMARY KEY,
    Email nvarchar(255) NOT NULL UNIQUE,
    ActorType int NOT NULL,  -- 0=IdeaGen, 1=RD, 2=Mfg, 3=Sales, 4=Investor
    CreatedAt datetime2 NOT NULL,
    -- NO OrganizationId column in v1.0
    
    INDEX IX_InnovationActors_Email (Email),
    INDEX IX_InnovationActors_ActorType (ActorType)
);
```

### ✅ v2.0 Migration (Add Multi-Tenancy)

```sql
-- v2.0: Add Organizations table
CREATE TABLE Organizations (
    Id uniqueidentifier PRIMARY KEY,
    Name nvarchar(200) NOT NULL,
    CreatedAt datetime2 NOT NULL
);

-- v2.0: Add OrganizationId to existing tables
ALTER TABLE Innovations 
    ADD OrganizationId uniqueidentifier NULL,
    ADD Mode int NOT NULL DEFAULT 0;  -- 0=Open, 1=Closed, 2=Hybrid

ALTER TABLE InnovationActors
    ADD OrganizationId uniqueidentifier NULL;

-- v2.0: Add foreign keys
ALTER TABLE Innovations
    ADD CONSTRAINT FK_Innovations_Organizations
    FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id);

ALTER TABLE InnovationActors
    ADD CONSTRAINT FK_InnovationActors_Organizations
    FOREIGN KEY (OrganizationId) REFERENCES Organizations(Id);

-- v2.0: Add indexes for organization queries
CREATE INDEX IX_Innovations_OrganizationId ON Innovations(OrganizationId);
CREATE INDEX IX_InnovationActors_OrganizationId ON InnovationActors(OrganizationId);
```

**Why This Works**: 
- v1.0 schema is clean (no unused columns)
- v2.0 adds columns via standard EF Core migration
- Existing v1.0 data remains valid (OrganizationId NULL = Open Innovation)
- No data rewrite required

---

## CODE REVIEW CHECKLIST

Use this during code reviews to ensure v1.0 code keeps v2.0 options open:

### ✅ Repository & Data Access
- [ ] All database queries go through repository interfaces?
- [ ] No direct `_context.Innovations.Where(...)` in handlers/services?
- [ ] Repository methods have semantic names (`GetPublicInnovations`, not `GetAll`)?
- [ ] No hard-coded `WHERE OrganizationId IS NULL` in SQL?

### ✅ Authorization
- [ ] Using policy-based authorization (not hard-coded role checks)?
- [ ] Authorization logic in handlers, not scattered in controllers?
- [ ] No `if (User.IsInRole("IdeaGenerator"))` checks everywhere?
- [ ] Policies can be extended without modifying existing endpoints?

### ✅ Actor Type Handling
- [ ] Actor types use polymorphism (abstract base class)?
- [ ] No switch statements on `ActorType` in business logic?
- [ ] Actor-specific behavior in actor classes, not centralized switch?
- [ ] Adding new actor type requires adding new class only?

### ✅ Domain Model
- [ ] No unused organization properties in v1.0 entities?
- [ ] No `OrganizationId` columns in database yet?
- [ ] No `InnovationMode` enum with only one value?
- [ ] Clean separation between Open Innovation logic and future modes?

### ✅ Validation
- [ ] Validators use FluentValidation (not scattered conditionals)?
- [ ] Validation rules grouped by feature/use case?
- [ ] Mode-specific validation can be added via inheritance?

### ✅ Configuration
- [ ] No global static `CurrentOrganization` or similar?
- [ ] No hard-coded organization IDs in code?
- [ ] Configuration uses Options pattern?

---

## RED FLAGS 🚩

**Stop and refactor if you see**:

```csharp
// 🚩 RED FLAG: Static organization context
public static Guid CurrentOrganization = Guid.Empty;

// 🚩 RED FLAG: Hard-coded organization assumptions
public async Task<Innovation> GetInnovation(Guid id)
{
    return await _context.Innovations
        .Where(i => i.OrganizationId == null)  // Assumes null = public
        .FirstAsync(i => i.Id == id);
}

// 🚩 RED FLAG: Switch on actor type in domain logic
public decimal Calculate(Bid bid)
{
    switch (bid.ActorType)
    {
        case ActorType.Manufacturing: return bid.Cost * 0.3m;
        // This will break when adding Government actor
    }
}

// 🚩 RED FLAG: Unused organization properties
public class Innovation
{
    public Guid? OrganizationId { get; set; }  // Always null in v1.0 - why is it here?
}

// 🚩 RED FLAG: Direct database queries in handlers
public async Task<Result> Handle(Command cmd)
{
    var innovations = await _context.Innovations
        .Where(i => i.IsPublished)  // No repository abstraction
        .ToListAsync();
}
```

---

## AI-ASSISTED DEVELOPMENT GUARDRAILS

### ✅ DO: Use AI for Mechanical Code

**Appropriate AI Use Cases**:

**DTOs Generation**:
```csharp
// Prompt: "Create SubmitInnovationRequest DTO from ProductIdea entity with properties: Title, ResearchBackground, FundingRequired"
public record SubmitInnovationRequest(
    string Title,
    string ResearchBackground,
    decimal FundingRequired
);
```

**Repository Scaffolding**:
```csharp
// Prompt: "Implement IInnovationRepository with EF Core DbContext"
public class InnovationRepository : IInnovationRepository
{
    private readonly AppDbContext _context;
    
    public async Task<Innovation?> GetByIdAsync(Guid id)
        => await _context.Innovations.FindAsync(id);
}
```

**Test Generation**:
```csharp
// Prompt: "Generate xUnit tests for InnovationValidator covering all FluentValidation rules"
[Fact]
public void Validate_TitleEmpty_ReturnsError()
{
    var validator = new InnovationValidator();
    var request = new SubmitInnovationRequest { Title = "" };
    
    var result = validator.Validate(request);
    
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == "Title");
}
```

**Boilerplate Code**:
```csharp
// Prompt: "Create AutoMapper profile for Innovation → InnovationViewModel"
public class InnovationMappingProfile : Profile
{
    public InnovationMappingProfile()
    {
        CreateMap<Innovation, InnovationViewModel>();
    }
}
```

**Why This Works**: Mechanical transformations don't carry architectural decisions.

---

### ❌ DON'T: Use AI for Architectural Decisions

**Prohibited AI Use Cases**:

**Domain Modeling** ❌:
```
# DON'T: "Design the Innovation aggregate"
# Why: Domain modeling requires deep understanding of business rules unique to Innoventity
# Human must decide: What belongs in aggregate? What are boundaries? What are invariants?
```

**Security Design** ❌:
```
# DON'T: "Create authorization policies for actor access to innovations"
# Why: Security is critical; AI doesn't understand project-specific authorization requirements
# Human must decide: Who can view what? When? Under what conditions?
```

**Architectural Patterns** ❌:
```
# DON'T: "Design database schema for multi-tenancy"
# Why: v2.0 architectural decision explicitly deferred to future
# Human must decide: Which v2.0 approach to enable? What constraints matter?
```

**Core Business Logic** ❌:
```
# DON'T: "Implement partner selection business rules"
# Why: Business rules are unique, complex, and critical to correctness
# Human must decide: One bid per type? Can't select multiple from same category? Why?
```

**Why This Fails**: AI doesn't understand project-specific architectural constraints and business context.

---

### ✅ DO: Verify AI Adherence to Vertical Slices

**AI Prompt Template (Constraining Architecture)**:
```
"Generate API endpoint for submitting innovation.

Requirements:
- Vertical Slice Architecture: endpoint + handler + validator in Features/Innovations/Submit/ 
- Use FluentValidation for input validation
- Return ProblemDetails for validation errors
- Follow Fail Fast principle (validate at boundary)
- No cross-slice dependencies

Use these patterns:
[paste example code showing vertical slice structure]"
```

**Code Review Check**:
- [ ] Does AI-generated code live in correct feature folder?
- [ ] Are there any cross-slice dependencies introduced?
- [ ] Does it follow project architectural patterns?
- [ ] Is it unnecessarily abstracted or over-engineered?

---

### ❌ DON'T: Accept AI-Generated Tests Without Epistemic Validation

**Anti-Pattern** (Test Generated After Implementation):
```csharp
// AI-generated test that passes but doesn't verify business logic
[Fact]
public void SubmitInnovation_ReturnsOk()
{
    var result = _controller.Submit(new SubmitInnovationRequest());
    Assert.IsType<OkResult>(result); // ← Meaningless assertion
}
```

**Why It's Wrong**:
- Test never observed failing
- Doesn't check business rules
- Might be tautological
- False confidence

**Correct Pattern** (Test-First with Human Validation):
```csharp
// STEP 1: Human writes test FIRST
[Fact]
public async Task SubmitInnovation_WithValidData_CreatesInnovationAndPublishesEvent()
{
    // Arrange: Valid innovation data
    var request = CreateValidInnovationRequest();
    
    // STEP 2: Run test → FAILS (red) ✓ Proves test works
    
    // Act: Submit
    var result = await _handler.HandleAsync(request);
    
    // Assert: Specific domain outcomes
    var innovation = await _db.Innovations.FindAsync(result.InnovationId);
    Assert.NotNull(innovation);
    Assert.Equal(request.Title, innovation.Title);
    _eventPublisher.Verify(
        x => x.Publish<InnovationSubmitted>(It.IsAny<InnovationSubmitted>()), 
        Times.Once
    );
    
    // STEP 3: Implement → Run test → PASSES (green) ✓ Causal link verified
}
```

---

### Testing Guardrails (Every Test Must Pass These)

Before merging any test (AI or human-written):

**Epistemic Validity**:
- [ ] Has test been **observed failing**? (Red phase or characterization)
- [ ] Can reviewer explain **what bug** this test would catch?
- [ ] If validation/business logic removed, would test fail?

**Business Logic Coverage**:
- [ ] Does test check **business rule** (not just HTTP status)?
- [ ] Are **edge cases** tested (empty, null, boundary values)?
- [ ] Are **negative cases** tested (not just happy path)?

**Assertion Quality**:
- [ ] Are assertions **specific** to domain behavior?
- [ ] Do assertions check **outcomes** (not implementation details)?
- [ ] Would test fail for **wrong reasons** (brittle)?

**Test Design**:
- [ ] Test name clearly describes **scenario**?
- [ ] Arrange/Act/Assert structure **clear**?
- [ ] No **magic numbers** or unclear test data?

**If ANY checkbox unchecked** → Request test rewrite before merge.

---

### Red Flags for AI Code 🚩

**Stop and refactor if you see**:

**🚩 Tests Generated After Implementation (No Red Phase)**:
```csharp
// Commit history shows:
// 1. Implement SelectPartnersValidator.cs
// 2. AI generates 15 tests for SelectPartnersValidator.cs
// 3. All tests pass immediately
// ❌ These tests have no epistemic validity
```

**🚩 Generic Assertions (Not Business-Specific)**:
```csharp
[Fact]
public void AllEndpoints_Return200()
{
    foreach (var endpoint in _endpoints)
        Assert.Equal(200, CallEndpoint(endpoint).StatusCode);
    // ❌ Doesn't verify business logic
}
```

**🚩 Cross-Slice Dependencies Introduced**:
```csharp
// In Features/Innovations/Submit/SubmitInnovation.cs
using Features.Bids.SelectPartners;  // 🚩 Cross-slice dependency!

public async Task Handle(Command cmd)
{
    var partnerSelector = new SelectPartnersHandler(...);  // 🚩 Tight coupling
}
```

**🚩 Overly Abstracted Code (AI "Enterprise" Patterns)**:
```csharp
// AI loves unnecessary abstraction
public interface IInnovationFactory { }
public class InnovationFactoryFactory { }  // 🚩 Factory for factory?
public abstract class AbstractInnovationBuilderStrategy { }  // 🚩 Overkill

// Prefer simple, direct code:
var innovation = new Innovation(title, background);
```

**🚩 Authorization Logic Generated by AI Without Review**:
```csharp
// AI-generated authorization policy (NEVER trust without human review)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewInnovation", policy =>
        policy.RequireAssertion(context => true));  // 🚩 Always allows access!
});
```

**🚩 High Coverage, Low Mutation Score**:
```bash
$ dotnet test --collect:"XPlat Code Coverage"
Coverage: 94%  ✓

$ dotnet stryker
Mutation Score: 23%  🚩 Tests don't catch actual bugs
```

---

### AI-Assisted Testing Workflow

**Correct Process** (Maintains Epistemic Rigor):

1. **Human writes spec** in `specs/[feature]/[usecase].spec.yaml`
2. **Spec-Kit generates** project structure + file skeletons
3. **AI scaffolds test** from spec testScenarios:
   ```
   Prompt: "Generate xUnit integration test from this spec:
   [paste spec testScenarios]
   Use WebApplicationFactory, assert all success criteria"
   ```
4. **STOP: Human reviews test critically**:
   - Does assertion match specification?
   - Can I break system in a way test would miss?
   - Are error messages checked (not just status codes)?
5. **Human improves test**: Add edge cases, specific assertions
6. **Run test → MUST FAIL** ✓ Proves test works
7. **Implement feature** (human for critical logic, AI-assisted for mechanical)
8. **Run test → MUST PASS** ✓ Causal link established
9. **Characterization check**: Deliberately break implementation, verify test catches it

---

### Prompt Engineering for Architectural Compliance

**Good Prompt** (Constrains AI to Architecture):
```
"In this Vertical Slice Architecture project using ASP.NET Core 8 Minimal APIs:

Generate endpoint for 'Get Innovation By ID' feature.

Constraints:
- File location: Features/Innovations/GetById/GetInnovationById.cs
- Use IInnovationRepository (no direct DbContext)
- Return ProblemDetails for 404
- Use Result<T> pattern for errors
- No cross-slice dependencies

Example pattern:
[paste existing vertical slice as example]"
```

**Bad Prompt** (No Constraints):
```
"Create an API endpoint to get innovation by ID"
// AI will generate whatever pattern it knows, likely violating architecture
```

---

### Integration with Spec-First Development

**How AI Fits** (Roles Clarified):

| Stage | Tool | Responsibility |
|-------|------|----------------|
| 1. Write Spec | **Human** | Define business rules, test scenarios, API contract |
| 2. Generate Structure | **Spec-Kit** | Create folders, file skeletons (template-based) |
| 3. Generate Content | **AI (Copilot)** | Fill DTOs, validator stubs, test scaffolds |
| 4. Validate Tests | **Human** | Review tests critically, add edge cases, ensure epistemic validity |
| 5. Implement Logic | **Human** (critical) / **AI** (mechanical) | Domain logic = human; infrastructure = AI-assisted |
| 6. Review | **Human** | Verify architecture compliance, test quality, business correctness |

**Key Distinction**:
- **Spec-Kit**: Template-based generation (deterministic, no AI)
- **AI (Copilot)**: Content generation (probabilistic, requires validation)
- **Human**: Critical thinking, business logic, architectural decisions

---

### Success Metrics for AI Integration

**Track These** (Assess AI Value):

✅ **Positive Indicators**:
- Time to implement vertical slice (target: 20% reduction with AI)
- Test coverage maintained at 80%+ with less manual effort
- Mutation testing score >70% (proves tests are meaningful)
- Zero architectural violations introduced by AI

🚩 **Red Flags** (Reassess AI Usage):
- AI code rejection rate >10% in code review
- Mutation score drops below 50% (weak tests)
- Cross-slice dependencies introduced
- Security issues in AI-generated authorization

**Goal**: AI accelerates mechanical work without compromising architecture or test quality.

---

## PORTFOLIO NARRATIVE

**When explaining this to interviewers**:

> "During architecture planning, I made a critical decision about v1.0 scope: focus exclusively on Open Innovation mode, deferring Closed/Hybrid modes and multi-tenancy to v2.0. This is good scope management, not technical debt.
> 
> However, I was careful not to write v1.0 code that would force a rewrite for v2.0. I created architectural guardrails ensuring clean abstractions:
> - Repository pattern hides query complexity
> - Policy-based authorization centralizes access control
> - Polymorphic actor types avoid switch statements
> - Clean separation of concerns
> 
> When v2.0 adds organization context, it's incremental: add properties via migration, extend repository methods with optional parameters, add new authorization policies. No fundamental refactoring needed. This demonstrates understanding the difference between deliberate scope management and painting yourself into a corner."

**Key Phrases**:
- "Strategic deferral, not technical debt"
- "Clean abstractions enable evolutionary architecture"
- "YAGNI balanced with future-proofing"
- "Non-destructive evolution from v1.0 to v2.0"

---

## RELATED DOCUMENTS

- [PHASE_1_TARGET_DEFINITION.md](PHASE_1_TARGET_DEFINITION.md) - What we're building in v1.0
- [RE-ENGINEERING_STRATEGY.md](RE-ENGINEERING_STRATEGY.md) - Overall implementation strategy
- [ARCHITECTURAL_INVENTORY.md](ARCHITECTURAL_INVENTORY.md) - Legacy reference showing multi-tenancy gaps
- [PROJECT_KNOWLEDGE_BASE.md](PROJECT_KNOWLEDGE_BASE.md) - Why we deferred Closed/Hybrid modes

---

**Document Status**: ✅ Active Guardrails (Review Before Every PR)  
**Last Updated**: February 8, 2026  
**Next Review**: After first vertical slice implementation (Week 1)
