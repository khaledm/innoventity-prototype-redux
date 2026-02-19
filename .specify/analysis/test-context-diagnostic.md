# Test Context Lifecycle Diagnostic Report

**Date**: February 15, 2026  
**Task**: T001 - Diagnose Database Context Lifecycle Issues  
**Analyst**: Implementation Agent

---

## Executive Summary

**Finding**: Two distinct root causes identified affecting 4 failing subcutaneous tests:

1. **Database Context Scoping Issue** (Phase0JourneyTests): Database name generated with `Guid.NewGuid()` inline, causing each database operation to potentially use different database instance
2. **JWT Token Recognition Issue** (GetInnovationTests): JWT token set on `DefaultRequestHeaders` not being recognized by authenticated endpoints

**Impact**: 4/60 tests failing (6.7% failure rate), blocking Journey 1-2 validation

---

## Baseline Test Results

**Test Suite Execution**: February 15, 2026 18:45:21  
**Total Tests**: 60  
**Passed**: 53 (88.3%)  
**Failed**: 7 (11.7%)  
**Duration**: 18.3 seconds

### Failing Tests Breakdown

| Test | Expected | Actual | Category |
|------|----------|--------|----------|
| `Phase0JourneyTests.Phase0Journey_RegisterActivateLoginViewInnovation_Success` | OK (200) | BadRequest (400) | Database Context |
| `GetInnovationTests.GetInnovation_WithValidId_ReturnsInnovationData` | OK (200) | Unauthorized (401) | JWT Token |
| `GetInnovationTests.GetInnovation_WithNonExistentId_Returns404` | NotFound (404) | Unauthorized (401) | JWT Token |
| `GetInnovationTests.GetInnovation_CrossActorAccess_Returns200` | OK (200) | Unauthorized (401) | JWT Token |
| `InnovationTests.Innovation_Should_RequireTitle` | ArgumentNullException | No exception | **Phase 1 Deferred** |
| `InnovationTests.Innovation_Should_RequireProductType` | ArgumentNullException | No exception | **Phase 1 Deferred** |
| `InnovationTests.Innovation_Should_RequireResearchBackground` | ArgumentNullException | No exception | **Phase 1 Deferred** |

**Phase 0.6 Scope**: First 4 failures (database context + JWT token) - CRITICAL  
**Phase 1 Scope**: Last 3 failures (domain validation) - DEFERRED

---

## Root Cause #1: Database Context Lifecycle (Phase0JourneyTests)

### Current Implementation

**File**: `tests/Innoventity.API.Tests/E2E/Journeys/Phase0JourneyTests.cs`

**Problem Code** (Lines 32-66):
```csharp
private WebApplicationFactory<Program> CreateFactory()
{
    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            // ... JWT configuration ...

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    // ❌ PROBLEM: New Guid generated inline
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });
}
```

**Why This Fails**:

1. **Line 64**: Database name uses `Guid.NewGuid()` **inline** in the lambda
2. **Problem**: Each time ConfigureServices lambda is invoked by the DI container, it may generate a **different GUID**
3. **Result**: Multiple database instances created instead of single shared instance
4. **Evidence**: Test seeds innovation in one scope (lines 115-156), then queries in another scope (line 165), but gets BadRequest suggesting innovation not found

### Evidence from Test Execution

**Step Analysis** (Phase0JourneyTests.Phase0Journey_RegisterActivateLoginViewInnovation_Success):

1. **Line 27**: Constructor creates factory with inline `Guid.NewGuid()` → Database "TestDb_ABC123"
2. **Line 29**: `SeedTestInnovation()` creates scope → May get database "TestDb_ABC123"
3. **Line 115**: Test method creates NEW scope to seed innovation → May get database "TestDb_XYZ789"  
4. **Line 165**: HTTP request to `/innovations/{id}` → WebApplicationFactory creates DbContext → May get database "TestDb_DEF456"
5. **Result**: Innovation seeded in "TestDb_XYZ789" but queried in "TestDb_DEF456" → **Not found** → **BadRequest**

**Developer's Clue**:
- Line 117 comment: *"Ensure industries exist (they may not have been seeded in this test's database instance)"*
- Developer already suspected database instance mismatch!

---

## Root Cause #2: JWT Token Recognition (GetInnovationTests)

### Current Implementation

**File**: `tests/Innoventity.API.Tests/Integration/Features/Innovations/GetInnovationTests.cs`

**Problem Code** (Lines 172-178):
```csharp
[Fact]
public async Task GetInnovation_WithValidId_ReturnsInnovationData()
{
    // Arrange
    var token = await GetAccessToken(); // Successfully gets token
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await _client.GetAsync($"/innovations/{_testInnovationId}");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode); // ❌ FAILS: Gets Unauthorized (401)
}
```

**GetAccessToken() Implementation** (Lines 141-162):
```csharp
private async Task<string> GetAccessToken()
{
    // ... login request ...
    
    using var scope = _factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<Infrastructure.Authentication.PasswordHasher>();

    var actor = await context.Actors.FindAsync(_testActorId);
    if (actor == null)
    {
        throw new InvalidOperationException($"Actor with ID {_testActorId} not found in database");
    }

    actor.PasswordHash = passwordHasher.HashPassword("Test123!@#");
    await context.SaveChangesAsync();

    var response = await _client.PostAsJsonAsync("/auth/login", loginRequest);
    if (!response.IsSuccessStatusCode)
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Login failed with status {response.StatusCode}: {errorContent}");
        // ❌ Login is succeeding (no exception thrown)
    }

    var loginResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
    return loginResponse.GetProperty("accessToken").GetString() ?? 
           throw new InvalidOperationException("Failed to get access token");
           // ✅ Token is successfully returned
}
```

**Why This Fails**:

### Hypothesis 1: JWT Configuration Mismatch
- **Factory JWT Config** (Lines 38-45): Uses test signing key, issuer, audience
- **Endpoint JWT Validation**: May be using **different** configuration (production appsettings.json?)
- **Result**: Token issued with test config but validated with production config → **Mismatch** → **Unauthorized**

### Hypothesis 2: Token Not Properly Attached to Request
- **Line 173**: Sets `_client.DefaultRequestHeaders.Authorization`
- **Potential Issue**: DefaultRequestHeaders may not be sent with all requests (e.g., if HttpClient is recreated internally)
- **Note**: Phase0JourneyTests uses **same pattern** (line 165) and also fails with Unauthorized for some requests

### Hypothesis 3: JWT Middleware Not Configured in Test Environment
- WebApplicationFactory may not be loading JWT authentication middleware properly
- Program.cs may have conditional middleware loading based on environment
- Test environment may need explicit middleware configuration

---

## Diagnostic Evidence

### Database Context Evidence

**Good Pattern Example** (GetInnovationTests, Lines 29-33):
```csharp
private WebApplicationFactory<Program> CreateFactory()
{
    // ✅ CORRECT: Capture database name in closure BEFORE factory creation
    var databaseName = $"TestDb_{Guid.NewGuid()}";

    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // ... remove existing DbContext ...

                services.AddDbContext<AppDbContext>(options =>
                {
                    // ✅ CORRECT: Use captured variable (same instance every time)
                    options.UseInMemoryDatabase(databaseName);
                });
            });
        });
}
```

**Why This Works**:
- Database name captured in **closure** before factory creation
- Same value used every time lambda is invoked
- All scopes share the **same database instance**

**Bad Pattern Example** (Phase0JourneyTests, Line 64):
```csharp
options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
// ❌ BAD: Guid.NewGuid() called INSIDE lambda
// May generate different GUID each invocation
```

### JWT Token Evidence

**Test Log Excerpt** (from test execution):
```
info: Microsoft.EntityFrameworkCore.Update[30100]
      Saved 2 entities to in-memory store.
[xUnit.net 00:00:02.33] Innoventity.API.Tests.Integration.Features.Innovations.GetInnovationTests.GetInnovation_WithValidId_ReturnsInnovationData [FAIL]
[xUnit.net 00:00:02.33]   Assert.Equal() Failure: Values differ
[xUnit.net 00:00:02.33]   Expected: OK
[xUnit.net 00:00:02.33]   Actual:   Unauthorized
```

**Key Observation**: EF Core logs show data being saved successfully ("Saved 2 entities"), confirming database works. But subsequent GET request returns Unauthorized, suggesting JWT validation failure, not database issue.

---

## Proposed Solutions

### Solution 1: Fix Database Context Lifecycle (Phase0JourneyTests)

**Change Required**: Capture database name in closure before factory creation

**Implementation** (modify CreateFactory()):
```csharp
<private WebApplicationFactory<Program> CreateFactory()
{
    // ✅ FIX: Capture unique database name BEFORE factory creation
    var databaseName = $"TestDb_Phase0Journey_{Guid.NewGuid()}";

    return new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SigningKey"] = "test-signing-key-minimum-32-characters-required-for-hs256",
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                    ["Jwt:AccessTokenExpirationMinutes"] = "60",
                    ["Jwt:RefreshTokenExpirationDays"] = "7"
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    // ✅ FIX: Use captured variable instead of inline Guid.NewGuid()
                    options.UseInMemoryDatabase(databaseName);
                });
            });
        });
}
```

**Expected Result**: All scopes (constructor seed, test method seed, HTTP request) use **same database instance**

---

### Solution 2: Fix JWT Token Recognition (GetInnovationTests)

**Option A: Per-Request Token Attachment** (Recommended)

**Rationale**: DefaultRequestHeaders can cause cross-test contamination if HttpClient is reused

**Implementation**: Create helper extension method
```csharp
// New file: tests/Innoventity.API.Tests/TestFixtures/HttpClientExtensions.cs
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

    public static async Task<HttpResponseMessage> PostWithAuthAsync(
        this HttpClient client, 
        string requestUri, 
        HttpContent content,
        string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, requestUri) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    // ... additional HTTP methods ...
}
```

**Usage** (replace line 178):
```csharp
// OLD:
_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
var response = await _client.GetAsync($"/innovations/{_testInnovationId}");

// NEW:
var response = await _client.GetWithAuthAsync($"/innovations/{_testInnovationId}", token);
```

**Option B: Verify JWT Middleware Configuration** (Investigation Required)

**Action**: Check Program.cs to ensure JWT authentication middleware is loaded in test environment

**Verification Command**:
```bash
# Check Program.cs for environment-specific JWT configuration
grep -n "UseAuthentication\|AddJwtBearer" src/Innoventity.API/Program.cs
```

---

## Next Steps (T002-T003)

### T002: Implement Database Context Fix
1. Modify `Phase0JourneyTests.CreateFactory()` to capture database name in closure
2. Run Phase0JourneyTests to verify: Expected HTTP 200 OK (not BadRequest)
3. Verify innovation data returned correctly

### T003: Implement JWT Token Attachment Fix
1. Create `HttpClientExtensions.cs` with per-request token attachment helpers
2. Update all 3 failing GetInnovationTests to use new helpers
3. Verify GetInnovationTests pass: Expected OK/NotFound (not Unauthorized)

### T004: Validate All Infrastructure Tests Pass
1. Run full test suite: `dotnet test --verbosity normal`
2. Expected: 57/60 passing (95% pass rate)
3. Remaining 3 failures: Phase 1 domain validation tests (DEFERRED)

---

## Technical References

### EF Core In-Memory Provider Documentation
- **URL**: https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy
- **Key Concept**: Database name uniquely identifies the in-memory database instance
- **Quote**: *"Each database is identified by its name. Multiple contexts that use the same database name will share the same in-memory database instance."*

### WebApplicationFactory Documentation
- **URL**: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- **Key Concept**: ConfigureServices lambda may be invoked multiple times by DI container
- **Best Practice**: Capture configuration values **before** factory creation, not inline

### xUnit Test Patterns
- **URL**: http://xunitpatterns.com/
- **Pattern**: Test Fixture Setup - shared context across test methods
- **Anti-pattern**: Fresh Fixture - creates new context per test (causes isolation but loses data)

---

## Conclusion

**Root Causes Identified**:
1. ✅ **Database Context**: Inline `Guid.NewGuid()` in lambda causes multiple database instances (Phase0JourneyTests)
2. ✅ **JWT Token**: Token attachment or validation configuration issue (GetInnovationTests - requires further investigation in T003)

**Confidence Level**: **HIGH** for database context issue, **MEDIUM** for JWT token issue (requires verification of JWT middleware configuration)

**Estimated Effort Remaining**:
- T002 (Database Context Fix): 4 hours (as estimated in tasks.md)
- T003 (JWT Token Fix): 2 hours (as estimated in tasks.md)
- T004 (Validation): 1 hour (as estimated in tasks.md)

**Ready for T002**: ✅ YES - Solution approach documented with code examples
