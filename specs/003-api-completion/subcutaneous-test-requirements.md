# Subcutaneous Test Requirements & Patterns

**Feature**: 003-api-completion (Phase 0.6)
**Purpose**: Define comprehensive requirements for subcutaneous testing pattern implementation
**Reference**: Martin Fowler's "Testing Strategies in a Microservice Architecture" (Subcutaneous Testing)

---

## Subcutaneous Testing Definition

**Subcutaneous tests** are integration tests that operate "just under the skin" of the UI layer, exercising complete user journeys through API endpoints with real infrastructure (database, authentication) while avoiding the complexity and fragility of UI automation.

### Key Characteristics

1. **Real Infrastructure**: Uses actual database, authentication service, HTTP layer (not mocks)
2. **Complete Journeys**: Orchestrates 5-10 API calls in sequence to validate end-to-end flows
3. **Fast Feedback**: Executes in seconds (18-30s for full suite), not minutes like UI tests
4. **Backend Validation**: Proves backend API complete and working before frontend development starts
5. **Living Documentation**: Test code serves as executable specification of expected API behavior

### Why Subcutaneous Tests?

**Martin Fowler's Rationale**:
> "The essential problem is that tests can be hard to write if you have to do a lot of unusual work to get the test to interact with a system. Examples of this include the awkwardness of testing through a text user interface or a remote API. In such situations, it can be valuable to make a special test API that sits just under the surface of the system - a subcutaneous test."

**Benefits for Innoventity**:
- **Faster than UI testing**: No Playwright/Selenium setup, no browser launch overhead (~18s vs. 2-5 minutes)
- **More stable than UI tests**: No DOM selectors, no async rendering issues, no visual regressions
- **Backend-first validation**: Proves API completeness before expensive frontend work begins
- **Frontend team unblocked**: Complete, validated API available for integration
- **Bug discovery shifted left**: API bugs caught in integration tests, not during UI development (~2-3 week lead time savings)

---

## Current Infrastructure Issues

### Issue 1: Database Context Lifecycle (CRITICAL)

**Symptom**: Data seeded in test constructor not visible to test method execution.

**Root Cause** (Hypothesis):
- EF Core In-Memory provider creates separate database instances for constructor scope vs method scope
- `SeedTestInnovation()` executes in constructor with one DbContext instance
- `GetAccessToken()` method queries for actor using different DbContext instance
- WebApplicationFactory may be creating third DbContext instance for API endpoint
- Database naming collision or scope isolation issue

**Impact**:
- Phase0JourneyTests.Phase0Journey_RegisterActivateLoginViewInnovation_Success: Expected OK, Actual **BadRequest**
- GetInnovation endpoint cannot find seeded innovation (returns BadRequest instead of OK)

**Required Fix** (T001-T002):
```csharp
// BEFORE (broken - separate database instances)
public class Phase0JourneyTests
{
    private readonly AppDbContext _dbContext;

    public Phase0JourneyTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("TestDb") // ❌ Shared name but separate instances
            .Options;
        _dbContext = new AppDbContext(options);
        SeedTestInnovation(_dbContext); // Seeded but not visible later
    }

    [Fact]
    public async Task Test()
    {
        var token = await GetAccessToken(); // ❌ New DbContext, can't see seeded data
        var response = await _client.GetAsync($"/innovations/{_testInnovationId}");
        // ❌ Fails: Innovation not found in database
    }
}

// AFTER (fixed - single shared database instance)
public class Phase0JourneyTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _databaseName;

    public Phase0JourneyTests()
    {
        // ✅ Step 1: Unique database name per test instance
        _databaseName = $"TestDb_Phase0Journey_{Guid.NewGuid()}";

        // ✅ Step 2: Create DbContext with unique database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();

        // ✅ Step 3: Seed using same DbContext
        SeedTestData(_dbContext);

        // ✅ Step 4: Configure factory to use SAME database name
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove default DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    // ✅ Register with SAME database name
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

**Acceptance Criteria**:
- ✅ Data seeded in constructor is queryable in test methods
- ✅ WebApplicationFactory uses same database instance as test DbContext
- ✅ Phase0JourneyTests passes with HTTP 200 OK (not BadRequest)

---

### Issue 2: JWT Token Attachment (CRITICAL)

**Symptom**: Authenticated requests return Unauthorized instead of expected status codes.

**Root Cause** (Hypothesis):
- JWT token not properly attached to `HttpClient.DefaultRequestHeaders`
- OR token attached but format incorrect (missing "Bearer " prefix)
- OR JWT configuration mismatch between test and application (signing key, issuer, audience)

**Impact**:
- GetInnovationTests.GetInnovation_WithValidId_ReturnsInnovationData: Expected OK, Actual **Unauthorized**
- GetInnovationTests.GetInnovation_WithNonExistentId_Returns404: Expected NotFound, Actual **Unauthorized**
- GetInnovationTests.GetInnovation_CrossActorAccess_Returns200: Expected OK, Actual **Unauthorized**

**Required Fix** (T003):
```csharp
// BEFORE (broken - token not attached correctly)
[Fact]
public async Task GetInnovation_Test()
{
    var token = await GetAccessToken();
    // ❌ Token never attached to request
    var response = await _client.GetAsync($"/innovations/{innovationId}");
    // ❌ Returns 401 Unauthorized
}

// AFTER (fixed - proper token attachment)
[Fact]
public async Task GetInnovation_Test()
{
    var token = await GetAccessToken();

    // ✅ Option 1: Per-request header (recommended for test isolation)
    var request = new HttpRequestMessage(HttpMethod.Get, $"/innovations/{innovationId}");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var response = await _client.SendAsync(request);

    // ✅ Option 2: Helper method
    var response = await _client.GetWithAuthAsync($"/innovations/{innovationId}", token);
}

// Helper method (add to test fixture)
public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> GetWithAuthAsync(
        this HttpClient client,
        string requestUri,
        string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }
}
```

**Acceptance Criteria**:
- ✅ All authenticated requests include `Authorization: Bearer {token}` header
- ✅ GetInnovationTests (3 tests) pass with expected status codes (not Unauthorized)
- ✅ JWT configuration consistent between test and application

---

## Subcutaneous Test Pattern Standards

### Pattern 1: Test Class Structure

**Requirements**:
1. One test class per complete user journey (Journey1Tests.cs, Journey2Tests.cs)
2. Test class implements `IDisposable` for proper cleanup
3. Constructor initializes database, factory, client with unique database name
4. Dispose method cleans up all resources

**Template**:
```csharp
public class Journey1_InnovationSubmissionTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _databaseName;

    public Journey1_InnovationSubmissionTests()
    {
        // Unique database per test instance
        _databaseName = $"TestDb_Journey1_{Guid.NewGuid()}";

        // Create DbContext
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();

        // Seed minimal reference data (industries, etc.)
        SeedReferenceData(_dbContext);

        // Configure factory
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

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

### Pattern 2: Helper Methods (Test Fixture)

**Requirements**:
1. Create reusable helper methods for common operations (Register, Login, CreateInnovation, etc.)
2. Helpers return strongly-typed response DTOs (not anonymous objects)
3. Helpers attach JWT tokens automatically for authenticated endpoints
4. Helpers validate response status codes with clear error messages

**Template**:
```csharp
public class TestFixture
{
    private readonly HttpClient _client;
    private readonly AppDbContext _dbContext;

    public TestFixture(HttpClient client, AppDbContext dbContext)
    {
        _client = client;
        _dbContext = dbContext;
    }

    public async Task<RegisterResponse> RegisterActor(
        ActorType actorType,
        string email,
        string password = "TestPassword123!")
    {
        var request = new RegisterRequest
        {
            Email = email,
            Password = password,
            ActorType = actorType.ToString(),
            FirstName = "Test",
            LastName = "User"
        };

        var response = await _client.PostAsJsonAsync("/auth/register", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Registration failed: {await response.Content.ReadAsStringAsync()}");

        var result = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        return result;
    }

    public async Task<LoginResponse> Login(string email, string password)
    {
        var request = new LoginRequest { Email = email, Password = password };
        var response = await _client.PostAsJsonAsync("/auth/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"Login failed: {await response.Content.ReadAsStringAsync()}");

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return result;
    }

    public async Task<CreateInnovationResponse> CreateInnovation(
        string token,
        CreateInnovationRequest innovationData)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/innovations")
        {
            Content = JsonContent.Create(innovationData)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Create innovation failed: {await response.Content.ReadAsStringAsync()}");

        var result = await response.Content.ReadFromJsonAsync<CreateInnovationResponse>();
        return result;
    }
}
```

---

### Pattern 3: Primary Journey Test Structure

**Requirements**:
1. One test method per complete journey flow (happy path)
2. Test name follows pattern: `Journey{N}_{ActorRole}_{Action}_{ExpectedOutcome}`
3. Minimum 5 API calls orchestrated in sequence
4. Assert HTTP status codes at each step
5. Assert database state changes after key operations
6. Assert cross-actor visibility where applicable

**Template**:
```csharp
[Fact]
public async Task Journey1_IdeaGeneratorSubmitsInnovation_PublishedSuccessfully()
{
    // Arrange: Create test fixture
    var fixture = new TestFixture(_client, _dbContext);

    // Step 1: Register Idea Generator
    var registration = await fixture.RegisterActor(
        ActorType.IdeaGenerator,
        "sarah@example.com");
    registration.Should().NotBeNull();
    registration.ActorId.Should().NotBeEmpty();

    // Step 2: Activate Account
    var activation = await fixture.ActivateAccount(registration.ActivationToken);
    activation.StatusCode.Should().Be(HttpStatusCode.OK);

    // Step 3: Login
    var login = await fixture.Login("sarah@example.com", "TestPassword123!");
    login.AccessToken.Should().NotBeNullOrEmpty();
    var token = login.AccessToken;

    // Step 4: Create Innovation Draft
    var innovationData = new CreateInnovationRequest
    {
        Title = "Quantum Battery Prototype",
        ProductType = "Energy Storage Device",
        ResearchCategory = "Engineering",
        ResearchBackground = "Lithium-air battery leveraging quantum tunneling for 10x energy density improvement",
        HasIPR = true,
        HasRightToUse = true,
        ProductDescription = "Next-generation battery technology for electric vehicles",
        TechnologyDescription = "Quantum tunneling mechanism enables electron transfer at unprecedented speeds",
        TargetBeneficiaries = "Electric vehicle manufacturers, grid-scale energy storage",
        RelevantMarketSize = 50_000_000_000m,
        PotentialMarketSize = 150_000_000_000m,
        TargetMarket = "Automotive OEMs, renewable energy providers",
        TargetCustomerBase = "B2B enterprise customers",
        TargetCustomerType = "B2B",
        TargetIndustryIds = new[] { "ELEC-001", "ENRG-001" },
        PartnersNeeded = new[] { "RD", "Manufacturing", "SalesMarketing" }
    };
    var draft = await fixture.CreateInnovation(token, innovationData);
    draft.InnovationId.Should().NotBeEmpty();
    draft.Status.Should().Be("Draft");
    var innovationId = draft.InnovationId;

    // Step 5: Update Innovation (optional - testing PUT)
    var updates = new UpdateInnovationRequest
    {
        Title = "Quantum Battery Prototype v2",
        ResearchBackground = "UPDATED: Lithium-air battery..."
    };
    var update = await fixture.UpdateInnovation(innovationId, token, updates);
    update.StatusCode.Should().Be(HttpStatusCode.OK);

    // Step 6: Submit for Publication
    var submit = await fixture.SubmitInnovation(innovationId, token);
    submit.StatusCode.Should().Be(HttpStatusCode.OK);
    submit.Data.Status.Should().Be("Published");
    submit.Data.SubmittedAt.Should().NotBeNull();

    // Step 7: Verify in Database
    var innovation = await _dbContext.Innovations.FindAsync(innovationId);
    innovation.Should().NotBeNull();
    innovation.Status.Should().Be(InnovationStatus.Published);
    innovation.SubmissionTimestamp.Should().NotBeNull();
    innovation.OwnerId.Should().Be(registration.ActorId);

    // Step 8: Verify Discoverable by Other Actors (cross-actor visibility)
    var manufacturingActor = await fixture.RegisterActor(
        ActorType.Manufacturing,
        "hans@manufacturer.de");
    await fixture.ActivateAccount(manufacturingActor.ActivationToken);
    var manufacturingLogin = await fixture.Login(
        "hans@manufacturer.de",
        "TestPassword123!");

    var discoveries = await fixture.ListInnovations(
        manufacturingLogin.AccessToken,
        industryId: "ELEC-001");
    discoveries.StatusCode.Should().Be(HttpStatusCode.OK);
    discoveries.Data.Items.Should().Contain(i => i.InnovationId == innovationId);
}
```

**Assertions Required**:
- ✅ HTTP status codes at each step (201, 200, etc.)
- ✅ Response body structure and content
- ✅ Database state changes (query after API call)
- ✅ Business rule enforcement (status transitions, visibility rules)

---

### Pattern 4: Error Path Test Structure

**Requirements**:
1. 2-3 error path tests per journey (validation errors, authorization failures)
2. Test name follows pattern: `Journey{N}_{ErrorCondition}_{ExpectedErrorCode}`
3. Shorter than primary journey test (3-5 API calls)
4. Assert specific error response structure (ProblemDetails format)
5. Verify business rules enforced correctly

**Template**:
```csharp
[Fact]
public async Task Journey1_SubmitIncompleteInnovation_Returns400WithValidationErrors()
{
    // Arrange
    var fixture = new TestFixture(_client, _dbContext);
    var actor = await fixture.RegisterActivateLogin(ActorType.IdeaGenerator);

    // Act: Create innovation with INCOMPLETE data (missing required fields)
    var incompleteData = new CreateInnovationRequest
    {
        Title = "Test Innovation",
        // Missing: ResearchCategory, ResearchBackground, etc.
    };
    var draft = await fixture.CreateInnovation(actor.AccessToken, incompleteData);
    var innovationId = draft.InnovationId;

    // Act: Attempt submission with incomplete data
    var submitRequest = new HttpRequestMessage(
        HttpMethod.Patch,
        $"/innovations/{innovationId}/submit");
    submitRequest.Headers.Authorization =
        new AuthenticationHeaderValue("Bearer", actor.AccessToken);
    var response = await _client.SendAsync(submitRequest);

    // Assert: 400 Bad Request with validation errors
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var problemDetails = await response.Content
        .ReadFromJsonAsync<ValidationProblemDetails>();
    problemDetails.Should().NotBeNull();
    problemDetails.Status.Should().Be(400);
    problemDetails.Errors.Should().ContainKey("ResearchBackground");
    problemDetails.Errors.Should().ContainKey("TargetIndustries");
}

[Fact]
public async Task Journey1_NonOwnerCannotSubmitInnovation_Returns403()
{
    // Arrange
    var fixture = new TestFixture(_client, _dbContext);
    var owner = await fixture.RegisterActivateLogin(ActorType.IdeaGenerator);
    var otherActor = await fixture.RegisterActivateLogin(ActorType.IdeaGenerator);

    var innovation = await fixture.CreateInnovation(
        owner.AccessToken,
        TestData.CompleteInnovation());
    var innovationId = innovation.InnovationId;

    // Act: Different actor attempts submission
    var submitRequest = new HttpRequestMessage(
        HttpMethod.Patch,
        $"/innovations/{innovationId}/submit");
    submitRequest.Headers.Authorization =
        new AuthenticationHeaderValue("Bearer", otherActor.AccessToken);
    var response = await _client.SendAsync(submitRequest);

    // Assert: 403 Forbidden
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

    var problemDetails = await response.Content
        .ReadFromJsonAsync<ProblemDetails>();
    problemDetails.Detail.Should().Contain("not the owner");
}
```

**Assertions Required**:
- ✅ Correct HTTP error status code (400, 401, 403, 404, 409)
- ✅ ProblemDetails structure present
- ✅ Error message descriptive and actionable
- ✅ Validation errors include field names and reasons

---

## Test Execution Requirements

### Performance Targets

**Full Test Suite**:
- Execution time: **<30 seconds** for 67 tests
- Individual test time: **<5 seconds** for journey tests
- Database setup/teardown: **<1 second** per test

**If performance degrades**:
- Profile slow tests using `dotnet test --logger "console;verbosity=detailed"`
- Optimize database seeding (lazy initialization, shared seed data patterns)
- Consider test parallelization (xUnit collections)
- Introduce test categories if needed (Unit, Integration, Journey)

### Test Independence

**Requirements**:
1. Each test MUST create its own test data
2. No shared state between tests
3. Tests MUST be runnable in any order
4. Tests SHOULD be runnable in parallel (when properly configured)

**Database Isolation**:
- Use unique database name per test class: `TestDb_{ClassName}_{Guid.NewGuid()}`
- Dispose database in test cleanup
- Seed only minimal reference data (industries, etc.)

### Continuous Integration

**CI Pipeline Requirements**:
1. Run full test suite on every commit to feature branch
2. Fail build if any test fails (100% pass rate required)
3. Report test execution time (alert if >30 seconds)
4. Optional: Generate code coverage report

**Build Configuration**:
```yaml
# .github/workflows/ci.yml
- name: Run Tests
  run: dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"

- name: Publish Test Results
  uses: EnricoMi/publish-unit-test-result-action@v1
  if: always()
  with:
    files: '**/test-results.trx'
```

---

## Success Criteria Summary

### Phase 0.6 Complete When:

**Infrastructure Fixed** (T001-T004):
- ✅ Database context lifecycle issue resolved
- ✅ JWT token attachment working correctly
- ✅ All 4 existing subcutaneous tests passing (Phase0JourneyTests, GetInnovationTests)

**Journey 1 Complete** (T005-T009, T013):
- ✅ POST /innovations implemented and tested
- ✅ PUT /innovations/{id} implemented and tested
- ✅ PATCH /innovations/{id}/submit implemented and tested
- ✅ GET /innovations implemented and tested
- ✅ GET /industries implemented and tested
- ✅ Journey1Tests with 4 tests passing (1 primary + 3 error paths)

**Journey 2 Complete** (T010-T012, T014):
- ✅ POST /innovations/{innovationId}/bids implemented and tested
- ✅ GET /innovations/{innovationId}/bids implemented and tested
- ✅ PUT /bids/{bidId} implemented and tested
- ✅ Journey2Tests with 3 tests passing (1 primary + 2 error paths)

**Documentation Complete** (T015-T017):
- ✅ 100% test pass rate (67/67 tests)
- ✅ All endpoints have comprehensive XML documentation
- ✅ Traceability matrix complete (user stories → endpoints → tests)
- ✅ README.md updated with Phase 0.6 completion

**Quality Gates**:
- ✅ Zero build warnings
- ✅ Test suite executes in <30 seconds
- ✅ All endpoints follow Vertical Slice architecture pattern
- ✅ Manual validation (no FluentValidation dependency)
- ✅ Backend API surface complete before frontend work begins

---

## Appendix: Subcutaneous Testing Resources

### Martin Fowler References

1. **Testing Strategies in a Microservice Architecture** (2014)
   - https://martinfowler.com/articles/microservice-testing/
   - Section: "Subcutaneous Testing"
   - Quote: "The essential problem is that tests can be hard to write if you have to do a lot of unusual work to get the test to interact with a system."

2. **Test Pyramid** (2012)
   - https://martinfowler.com/bliki/TestPyramid.html
   - Subcutaneous tests sit between unit tests and UI tests
   - Faster than UI tests, more comprehensive than unit tests

### Related Documentation

- **EF Core In-Memory Database**: https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy
- **WebApplicationFactory**: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- **xUnit Test Patterns**: http://xunitpatterns.com/
- **FluentAssertions**: https://fluentassertions.com/introduction

### Team Knowledge Transfer

**After Phase 0.6 completion, document**:
- Lessons learned from database context lifecycle fix
- Performance optimization techniques used
- Test fixture patterns that worked well
- Any deviations from original plan

**Share with team**:
- Subcutaneous test pattern as best practice for future features
- Test execution time benchmarks
- CI/CD integration approach
