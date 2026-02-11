# Technical Implementation Plan: Phase 0.5 Domain Model Refactoring

**Feature Branch**: `002-domain-enhancements`
**Specification**: [spec.md](spec.md)
**Reference Analysis**: [DDD Architectural Analysis](../../.specify/analysis/ddd-architectural-analysis.md)
**Created**: February 10, 2026
**Estimated Effort**: 7.5 hours (2h + 3h + 2.5h)

---

## Executive Summary

Phase 0.5 refactors the domain model foundation by adopting proven DDD patterns from the legacy system. This phase focuses exclusively on **infrastructure improvements** that enable future domain composition patterns (Phase 1+).

**Scope**: 4 requirements (R8.4.1, R1.4, R1.5, R9.1)
**Breaking Changes**: Yes (Actor schema, API contracts)
**Migration Strategy**: Automated with data preservation
**Risk Level**: MEDIUM (breaking changes mitigated by comprehensive tests)

---

## Current State Analysis

### Existing Implementation (001-platform-core)

**Domain Entities** (Phase 0-5 complete):
```
src/Innoventity.API/Domain/Entities/
├── Actor.cs (plain POCO, FullName, ContactAddress string)
├── ActorType.cs (enum: IdeaGenerator, Manufacturing, SalesMarketing, Engineering)
├── AccountStatus.cs (enum: PendingActivation, Active, Suspended)
├── Innovation.cs (plain POCO, flat 24 properties)
├── InnovationStatus.cs (enum: Draft, Published, PartnersSelected)
├── ResearchCategory.cs (enum: Management, Engineering, NaturalScience)
└── Industry.cs (plain POCO, IndustryId string PK)
```

**Test Coverage**:
- 32 tests passing (10 unit + 22 integration)
- Test distribution:
  - Actor entity tests: 4 unit tests
  - Registration tests: 6 integration tests
  - Login tests: 5 integration tests
  - Activation tests: 3 integration tests
  - Refresh token tests: 2 integration tests
  - Get innovation tests: 4 integration tests
  - E2E journey tests: 2 integration tests
  - Health check tests: 2 integration tests
  - Root endpoint tests: 1 integration test
  - Seed data tests: 3 integration tests

**Identified Gaps** (from DDD analysis):
1. ❌ No EntityBase<TId> - entities lack proper equality semantics
2. ❌ Actor.FullName (string) - prevents proper name handling
3. ❌ Actor.ContactAddress (string) - prevents structured queries
4. ❌ Actor.PasswordSalt missing - security compliance gap
5. ❌ Anemic entities - no domain behavior methods

**Phase 0.5 addresses gaps #1-#4. Gap #5 deferred to Phase 1.**

---

## Target State Architecture

### New Domain Structure

```
src/Innoventity.API/Domain/
├── Common/                              [NEW]
│   ├── EntityBase.cs                    [NEW] Abstract base with equality
│   ├── EntityOfGuid.cs                  [NEW] Guid specialization
│   └── EntityOfInt32.cs                 [NEW] Int32 specialization
├── Entities/
│   ├── Actor.cs                         [REFACTOR] Inherit EntityOfGuid
│   ├── Address.cs                       [NEW] Value object (owned entity)
│   ├── ActorType.cs                     [UNCHANGED]
│   ├── AccountStatus.cs                 [UNCHANGED]
│   ├── Innovation.cs                    [UPDATE] Inherit EntityOfGuid
│   ├── InnovationStatus.cs              [UNCHANGED]
│   ├── ResearchCategory.cs              [UNCHANGED]
│   └── Industry.cs                      [UPDATE] Inherit EntityOfInt32 OR EntityBase<string>
└── ValueObjects/                        [NEW - for future Phase 1 VOs]
```

### Actor Entity Transformation

**Before (current)**:
```csharp
public class Actor
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string ContactAddress { get; set; } = string.Empty;

    [Required]
    [MaxLength(60)]
    public string PasswordHash { get; set; } = string.Empty;

    // ... other properties
}
```

**After (Phase 0.5)**:
```csharp
public class Actor : EntityOfGuid  // Inherit from EntityBase<Guid>
{
    // Id property inherited from EntityBase<Guid>

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    public Address? ContactAddress { get; set; }  // Owned entity (nullable)

    [MaxLength(20)]
    public string? Phone { get; set; }  // Separate from address

    [Required]
    [MaxLength(60)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(44)]  // Base64 encoded 32-byte salt = 44 chars
    public string PasswordSalt { get; set; } = string.Empty;

    // ... other properties unchanged

    // Computed property for display (not stored)
    public string DisplayName => $"{FirstName} {LastName}";
    public string SortableName => $"{LastName}, {FirstName}";
}
```

### Address Value Object (Owned Entity)

```csharp
namespace Innoventity.API.Domain.Entities;

/// <summary>
/// Address value object - owned by Actor entity (R1.5)
/// No separate Id - embedded in Actor table as Actor_ContactAddress_*
/// </summary>
public class Address
{
    [Required]
    [MaxLength(100)]
    public string Address1 { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Address2 { get; set; }

    [Required]
    [MaxLength(50)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PostCode { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "US", "GB", "DE")
    /// </summary>
    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = string.Empty;
}
```

### EntityBase<TId> Infrastructure

```csharp
using System.Runtime.CompilerServices;

namespace Innoventity.API.Domain.Common;

/// <summary>
/// Base class for all domain entities providing identity-based equality semantics (R9.1)
/// Implements DDD Entity pattern from Evans "Domain-Driven Design" Chapter 5
/// </summary>
/// <typeparam name="TId">Type of the entity identifier (Guid, int, string, etc.)</typeparam>
public abstract class EntityBase<TId> : IEquatable<EntityBase<TId>>
{
    /// <summary>
    /// Unique identifier for the entity
    /// Protected setter ensures Id is set only during entity construction or by ORM
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Determines if entity is transient (not yet persisted to database)
    /// Transient entities have Id equal to default(TId)
    /// </summary>
    public bool IsTransient()
    {
        return EqualityComparer<TId>.Default.Equals(Id, default!);
    }

    /// <summary>
    /// Identity-based equality: Two entities are equal if they have the same Id
    /// Transient entities are equal only if they are the same object reference
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        // Transient entities are never equal (except by reference above)
        if (IsTransient() || other.IsTransient())
            return false;

        // Both entities have Ids - compare by Id only
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public bool Equals(EntityBase<TId>? other)
    {
        return Equals((object?)other);
    }

    /// <summary>
    /// Stable hash code for use in collections (HashSet, Dictionary)
    /// Transient entities use reference-based hash (RuntimeHelpers.GetHashCode)
    /// Persisted entities use Id-based hash
    /// </summary>
    public override int GetHashCode()
    {
        // For transient entities, use runtime reference hash (stable for lifetime)
        if (IsTransient())
            return RuntimeHelpers.GetHashCode(this);

        // For persisted entities, use Id hash (stable across lookups)
        return Id!.GetHashCode();
    }

    public static bool operator ==(EntityBase<TId>? left, EntityBase<TId>? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    public static bool operator !=(EntityBase<TId>? left, EntityBase<TId>? right)
    {
        return !(left == right);
    }
}
```

### EntityOfGuid Specialization

```csharp
namespace Innoventity.API.Domain.Common;

/// <summary>
/// Base class for entities using Guid as primary key
/// Used by: Actor, Innovation, FormalResponse (Phase 1+), BusinessPlan (Phase 2+)
/// </summary>
public abstract class EntityOfGuid : EntityBase<Guid>
{
    // Inherits all equality semantics from EntityBase<Guid>
    // No additional implementation needed
}
```

### EntityOfInt32 Specialization

```csharp
namespace Innoventity.API.Domain.Common;

/// <summary>
/// Base class for entities using int as primary key
/// Used by: Industry (if converting from string PK), child entities in Phase 2+
/// </summary>
public abstract class EntityOfInt32 : EntityBase<int>
{
    // Inherits all equality semantics from EntityBase<int>
    // No additional implementation needed
}
```

---

## Database Migration Strategy

### Migration: AddEntityBaseAndRefactorActor

**Migration Name**: `20260210_AddEntityBaseAndRefactorActor`

#### Schema Changes

**ALTER TABLE [Actors]**:
```sql
-- Add new columns
ALTER TABLE [Actors] ADD [FirstName] nvarchar(50) NULL;
ALTER TABLE [Actors] ADD [LastName] nvarchar(50) NULL;
ALTER TABLE [Actors] ADD [PasswordSalt] nvarchar(44) NOT NULL DEFAULT '';
ALTER TABLE [Actors] ADD [Phone] nvarchar(20) NULL;

-- Add Address owned entity columns (nullable - address is optional)
ALTER TABLE [Actors] ADD [ContactAddress_Address1] nvarchar(100) NULL;
ALTER TABLE [Actors] ADD [ContactAddress_Address2] nvarchar(100) NULL;
ALTER TABLE [Actors] ADD [ContactAddress_City] nvarchar(50) NULL;
ALTER TABLE [Actors] ADD [ContactAddress_PostCode] nvarchar(20) NULL;
ALTER TABLE [Actors] ADD [ContactAddress_CountryCode] nchar(2) NULL;

-- Data migration (split FullName, generate PasswordSalt, migrate ContactAddress)
-- See Data Migration section below

-- Drop old column (after data migrated)
ALTER TABLE [Actors] DROP COLUMN [ContactAddress];
ALTER TABLE [Actors] DROP COLUMN [FullName];

-- Make FirstName/LastName required after data migration
ALTER TABLE [Actors] ALTER COLUMN [FirstName] nvarchar(50) NOT NULL;
ALTER TABLE [Actors] ALTER COLUMN [LastName] nvarchar(50) NOT NULL;
```

#### Data Migration Logic

**FullName Split Algorithm**:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Add columns (nullable initially)
    migrationBuilder.AddColumn<string>(
        name: "FirstName",
        table: "Actors",
        type: "nvarchar(50)",
        maxLength: 50,
        nullable: true);

    migrationBuilder.AddColumn<string>(
        name: "LastName",
        table: "Actors",
        type: "nvarchar(50)",
        maxLength: 50,
        nullable: true);

    migrationBuilder.AddColumn<string>(
        name: "PasswordSalt",
        table: "Actors",
        type: "nvarchar(44)",
        maxLength: 44,
        nullable: false,
        defaultValue: "");

    // ... add Address columns (all nullable)

    // Data migration SQL
    migrationBuilder.Sql(@"
        UPDATE [Actors]
        SET
            -- Split FullName on last space
            [FirstName] = CASE
                WHEN CHARINDEX(' ', REVERSE([FullName])) > 0
                THEN LEFT([FullName], LEN([FullName]) - CHARINDEX(' ', REVERSE([FullName])))
                ELSE [FullName]  -- No space found - use entire name as FirstName
            END,
            [LastName] = CASE
                WHEN CHARINDEX(' ', REVERSE([FullName])) > 0
                THEN RIGHT([FullName], CHARINDEX(' ', REVERSE([FullName])) - 1)
                ELSE ''  -- No space found - empty LastName (will be manually reviewed)
            END,
            -- Generate PasswordSalt (extract from BCrypt hash or generate new)
            -- BCrypt hash format: $2a$12$[22-char-salt][31-char-hash]
            -- For Phase 0.5, generate new random salt (existing hashes remain valid)
            [PasswordSalt] = CONVERT(nvarchar(44), HASHBYTES('SHA2_256', NEWID()), 1)
    ");

    // Make FirstName/LastName required after data migration
    migrationBuilder.AlterColumn<string>(
        name: "FirstName",
        table: "Actors",
        type: "nvarchar(50)",
        maxLength: 50,
        nullable: false);

    migrationBuilder.AlterColumn<string>(
        name: "LastName",
        table: "Actors",
        type: "nvarchar(50)",
        maxLength: 50,
        nullable: false);

    // Drop old columns
    migrationBuilder.DropColumn(
        name: "FullName",
        table: "Actors");

    migrationBuilder.DropColumn(
        name: "ContactAddress",
        table: "Actors");
}
```

**ContactAddress Migration**:
- Current `ContactAddress` is string - no structured data to migrate
- Leave Address columns NULL for existing actors
- Future registrations must provide structured address (or leave entire address NULL)

**PasswordSalt Migration**:
- BCrypt stores salt embedded in hash (first 29 chars: `$2a$12$SALTSALTSALTSALTSALT`)
- Generate new random salt for audit trail (existing PasswordHash remains valid)
- Future: Consider extracting embedded salt if audit trail requires exact match

#### EF Core Configuration Changes

**AppDbContext.cs updates**:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Configure Actor entity
    modelBuilder.Entity<Actor>(entity =>
    {
        // R1.3: Email must be unique per ActorType
        entity.HasIndex(a => new { a.Email, a.ActorType })
              .IsUnique()
              .HasDatabaseName("IX_Actor_Email_ActorType");

        // FirstName/LastName (replacing FullName)
        entity.Property(a => a.FirstName)
              .IsRequired()
              .HasMaxLength(50);

        entity.Property(a => a.LastName)
              .IsRequired()
              .HasMaxLength(50);

        // Password security
        entity.Property(a => a.PasswordHash)
              .IsRequired()
              .HasMaxLength(60); // BCrypt hash length

        entity.Property(a => a.PasswordSalt)
              .IsRequired()
              .HasMaxLength(44); // Base64 encoded 32-byte salt

        // Phone (optional, separate from address)
        entity.Property(a => a.Phone)
              .HasMaxLength(20);

        // Configure Address as owned entity (R1.5)
        entity.OwnsOne(a => a.ContactAddress, address =>
        {
            address.Property(ad => ad.Address1)
                   .HasColumnName("ContactAddress_Address1")
                   .IsRequired()
                   .HasMaxLength(100);

            address.Property(ad => ad.Address2)
                   .HasColumnName("ContactAddress_Address2")
                   .HasMaxLength(100);

            address.Property(ad => ad.City)
                   .HasColumnName("ContactAddress_City")
                   .IsRequired()
                   .HasMaxLength(50);

            address.Property(ad => ad.PostCode)
                   .HasColumnName("ContactAddress_PostCode")
                   .IsRequired()
                   .HasMaxLength(20);

            address.Property(ad => ad.CountryCode)
                   .HasColumnName("ContactAddress_CountryCode")
                   .IsRequired()
                   .HasMaxLength(2)
                   .IsFixedLength();
        });

        // Other properties unchanged...
    });

    // Configure Innovation entity - add EntityBase inheritance
    modelBuilder.Entity<Innovation>(entity =>
    {
        // Existing configuration remains
        // No breaking changes to Innovation schema in Phase 0.5
    });

    // Configure Industry entity - add EntityBase inheritance
    modelBuilder.Entity<Industry>(entity =>
    {
        // Existing configuration remains
        // IndustryId remains string PK (not changed to int in Phase 0.5)
    });
}
```

---

## API Contract Changes (Breaking)

### Registration Endpoint

**Before**:
```http
POST /auth/register
Content-Type: application/json

{
  "email": "jane@example.com",
  "fullName": "Jane Smith",
  "contactAddress": "123 Main St, London, UK",
  "actorType": "IdeaGenerator",
  "password": "SecurePass123!"
}
```

**After**:
```http
POST /auth/register
Content-Type: application/json

{
  "email": "jane@example.com",
  "firstName": "Jane",
  "lastName": "Smith",
  "contactAddress": {
    "address1": "123 Main St",
    "address2": "Apt 4B",
    "city": "London",
    "postCode": "SW1A 1AA",
    "countryCode": "GB"
  },
  "phone": "+44 20 7123 4567",
  "actorType": "IdeaGenerator",
  "password": "SecurePass123!"
}
```

**Validation Rules**:
- `firstName`: Required, min 2 chars, max 50 chars, letters/spaces/hyphens only
- `lastName`: Required, min 2 chars, max 50 chars, letters/spaces/hyphens only
- `contactAddress`: Optional (entire object nullable)
  - If ANY address field provided, ALL required fields must be present (Address1, City, PostCode, CountryCode)
  - CountryCode: Exactly 2 uppercase letters (ISO 3166-1 alpha-2)
- `phone`: Optional, max 20 chars

### Login Response

**Before**:
```json
{
  "message": "Login successful",
  "actor": {
    "id": "...",
    "email": "jane@example.com",
    "fullName": "Jane Smith",
    "actorType": "IdeaGenerator"
  },
  "accessToken": "...",
  "refreshToken": "..."
}
```

**After**:
```json
{
  "message": "Login successful",
  "actor": {
    "id": "...",
    "email": "jane@example.com",
    "firstName": "Jane",
    "lastName": "Smith",
    "displayName": "Jane Smith",
    "actorType": "IdeaGenerator"
  },
  "accessToken": "...",
  "refreshToken": "..."
}
```

### GetInnovation Response (Owner nested object)

**Before**:
```json
{
  "id": "...",
  "title": "...",
  "owner": {
    "id": "...",
    "fullName": "Dr. Sarah Chen",
    "actorType": "IdeaGenerator"
  }
}
```

**After**:
```json
{
  "id": "...",
  "title": "...",
  "owner": {
    "id": "...",
    "firstName": "Sarah",
    "lastName": "Chen",
    "displayName": "Dr. Sarah Chen",
    "actorType": "IdeaGenerator"
  }
}
```

---

## Implementation Phases

### Phase A: EntityBase Infrastructure (2 hours)

#### A1: Create EntityBase<TId> (30 min)

**File**: `src/Innoventity.API/Domain/Common/EntityBase.cs`

**Implementation**:
1. Create `Domain/Common` directory
2. Implement EntityBase<TId> abstract class (see Target State Architecture above)
3. Key methods:
   - `IsTransient()` - check if Id == default(TId)
   - `Equals(object obj)` - identity-based equality
   - `GetHashCode()` - stable hash for collections
   - `operator ==` and `operator !=`

**Testing**:
- Create `tests/Innoventity.API.Tests/Unit/Domain/Common/EntityBaseTests.cs`
- Test cases:
  1. ✅ Two entities with same Id are equal
  2. ✅ Two entities with different Id are not equal
  3. ✅ Transient entities (no Id) are equal only by reference
  4. ✅ GetHashCode stable for persisted entities (can add to HashSet)
  5. ✅ GetHashCode stable for transient entities (same hash until GC)
  6. ✅ IsTransient() returns true for default Id
  7. ✅ Equality operators work correctly

**Estimated**: 30 minutes

---

#### A2: Create Specialization Classes (15 min)

**Files**:
- `src/Innoventity.API/Domain/Common/EntityOfGuid.cs`
- `src/Innoventity.API/Domain/Common/EntityOfInt32.cs`

**Implementation**:
- Simple inheritance from EntityBase<TId>
- No additional logic required
- XML documentation explaining usage

**Testing**:
- Verify compilation
- Covered by EntityBase tests (inheritance)

**Estimated**: 15 minutes

---

#### A3: Update Actor to Inherit EntityOfGuid (30 min)

**File**: `src/Innoventity.API/Domain/Entities/Actor.cs`

**Changes**:
```csharp
// Before
public class Actor
{
    [Key]
    public Guid Id { get; set; }
    // ...
}

// After
public class Actor : EntityOfGuid
{
    // Id inherited from EntityOfGuid, remove [Key] attribute
    // ...
}
```

**Testing**:
- Update `ActorTests.cs`:
  1. ✅ Test equality works for actors (same Id = equal)
  2. ✅ Test actors can be added to HashSet<Actor>
  3. ✅ Test IsTransient() for new actors

**Estimated**: 30 minutes

---

#### A4: Update Innovation to Inherit EntityOfGuid (15 min)

**File**: `src/Innoventity.API/Domain/Entities/Innovation.cs`

**Changes**:
```csharp
// Before
public class Innovation
{
    [Key]
    public Guid Id { get; set; }
    // ...
}

// After
public class Innovation : EntityOfGuid
{
    // Id inherited from EntityOfGuid, remove [Key] attribute
    // ...
}
```

**Testing**:
- Verify GetInnovation tests still pass

**Estimated**: 15 minutes

---

#### A5: Update Industry to Inherit EntityBase<string> (15 min)

**File**: `src/Innoventity.API/Domain/Entities/Industry.cs`

**Decision**: Industry uses `string IndustryId` as PK - use `EntityBase<string>` (not EntityOfInt32)

**Changes**:
```csharp
// Before
public class Industry
{
    [Key]
    public string IndustryId { get; set; } = string.Empty;
    // ...
}

// After
public class Industry : EntityBase<string>
{
    // Rename Id property to match EntityBase
    // public string Id { get; protected set; } = string.Empty; (inherited)
    public string Name { get; set; } = string.Empty;
}
```

**Migration Note**: Rename IndustryId → Id in database
```sql
EXEC sp_rename 'Industries.IndustryId', 'Id', 'COLUMN';
```

**Testing**:
- Verify seed data tests still pass
- Verify GetInnovation tests (includes TargetIndustries)

**Estimated**: 15 minutes

---

#### A6: Run EntityBase Tests (15 min)

**Goal**: Verify all EntityBase functionality works

**Test Execution**:
```bash
dotnet test --filter "FullyQualifiedName~EntityBaseTests"
```

**Expected**: 7+ tests passing

**Estimated**: 15 minutes

---

**Phase A Total**: 2 hours

---

### Phase B: Actor Schema Refactoring (3 hours)

#### B1: Create Address Value Object (20 min)

**File**: `src/Innoventity.API/Domain/Entities/Address.cs`

**Implementation** (see Target State Architecture above):
- 5 properties: Address1, Address2, City, PostCode, CountryCode
- No Id property (owned entity)
- Validation attributes

**Testing**:
- Create `tests/Innoventity.API.Tests/Unit/Domain/Entities/AddressTests.cs`
- Test cases:
  1. ✅ Address can be created with required fields
  2. ✅ CountryCode must be 2 characters
  3. ✅ Address2 is optional

**Estimated**: 20 minutes

---

#### B2: Refactor Actor Entity (40 min)

**File**: `src/Innoventity.API/Domain/Entities/Actor.cs`

**Changes**:
1. Add `FirstName` property (replacing FullName)
2. Add `LastName` property
3. Add `PasswordSalt` property
4. Add `Phone` property (separate from address)
5. Change `ContactAddress` from string to `Address?` (owned entity)
6. Add computed properties: `DisplayName`, `SortableName`
7. Remove `[Key]` attribute on Id (inherited from EntityOfGuid)

**Implementation**:
```csharp
public class Actor : EntityOfGuid
{
    // Id inherited from EntityOfGuid

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;

    public Address? ContactAddress { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    public ActorType ActorType { get; set; }

    [Required]
    public AccountStatus AccountStatus { get; set; } = AccountStatus.PendingActivation;

    [MaxLength(64)]
    public string? ActivationToken { get; set; }

    [Required]
    [MaxLength(60)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(44)]
    public string PasswordSalt { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Computed properties (not stored in database)
    public string DisplayName => $"{FirstName} {LastName}";
    public string SortableName => $"{LastName}, {FirstName}";
}
```

**Testing**:
- Update existing ActorTests.cs (4 tests need FirstName/LastName)
- Add new validation tests for Address

**Estimated**: 40 minutes

---

#### B3: Update AppDbContext Configuration (30 min)

**File**: `src/Innoventity.API/Infrastructure/Persistence/AppDbContext.cs`

**Changes** (see Database Migration Strategy above):
1. Update Actor configuration:
   - FirstName/LastName properties
   - PasswordSalt property
   - Phone property
   - Address owned entity configuration
2. Update Industry configuration (if renaming IndustryId → Id)
3. Remove FullName, ContactAddress string configuration

**Testing**:
- Verify DbContext creates without errors
- Run `dotnet ef migrations add AddEntityBaseAndRefactorActor`

**Estimated**: 30 minutes

---

#### B4: Create Database Migration (45 min)

**Command**:
```bash
cd src/Innoventity.API
dotnet ef migrations add AddEntityBaseAndRefactorActor
```

**Manual edits to generated migration**:
1. Ensure columns added as nullable first
2. Add data migration SQL (FullName split, PasswordSalt generation)
3. Make FirstName/LastName NOT NULL after data migration
4. Drop FullName, ContactAddress columns last

**Review migration logic** (see Database Migration Strategy above)

**Testing**:
- Apply migration to test database
- Verify data split correctly
- Verify PasswordSalt generated
- Verify no data loss

**Estimated**: 45 minutes

---

#### B5: Update Seed Data (25 min)

**File**: `src/Innoventity.API/Infrastructure/Persistence/SeedData.cs`

**Changes**:
- Update all Actor creations to use FirstName/LastName
- Add PasswordSalt generation (random 32-byte)
- Update ContactAddress to structured Address object (or leave NULL)

**Example**:
```csharp
// Before
var actor = new Actor
{
    Id = Guid.Parse("..."),
    Email = "sarah@example.com",
    FullName = "Dr. Sarah Chen",
    ContactAddress = "Stanford University, CA, USA",
    // ...
};

// After
var actor = new Actor
{
    Id = Guid.Parse("..."),
    Email = "sarah@example.com",
    FirstName = "Sarah",
    LastName = "Chen",
    ContactAddress = new Address
    {
        Address1 = "Stanford University",
        City = "Stanford",
        PostCode = "94305",
        CountryCode = "US"
    },
    PasswordSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
    // ...
};
```

**Testing**:
- Run seed data tests
- Verify 3 seed data tests pass

**Estimated**: 25 minutes

---

#### B6: Phase B Integration Test (20 min)

**Goal**: Verify database schema changes work end-to-end

**Steps**:
1. Drop test database
2. Run migrations
3. Run seed data
4. Query actors - verify FirstName/LastName/Address columns exist
5. Verify PasswordSalt column exists and populated

**Estimated**: 20 minutes

---

**Phase B Total**: 3 hours

---

### Phase C: API & Test Updates (2.5 hours)

#### C1: Update Registration Endpoint (40 min)

**File**: `src/Innoventity.API/Features/Authentication/Register.cs`

**Changes**:
1. Update request DTO:
   ```csharp
   // Before
   record RegisterRequest(
       string Email,
       string FullName,
       string ContactAddress,
       string ActorType,
       string Password);

   // After
   record RegisterRequest(
       string Email,
       string FirstName,
       string LastName,
       AddressDto? ContactAddress,  // Structured object
       string? Phone,
       string ActorType,
       string Password);

   record AddressDto(
       string Address1,
       string? Address2,
       string City,
       string PostCode,
       string CountryCode);
   ```

2. Add validation:
   ```csharp
   // FirstName validation
   if (string.IsNullOrWhiteSpace(request.FirstName) || request.FirstName.Length < 2)
       return Results.BadRequest(new { error = "First name must be at least 2 characters" });

   // LastName validation
   if (string.IsNullOrWhiteSpace(request.LastName) || request.LastName.Length < 2)
       return Results.BadRequest(new { error = "Last name must be at least 2 characters" });

   // Address validation (all-or-nothing)
   if (request.ContactAddress != null)
   {
       if (string.IsNullOrWhiteSpace(request.ContactAddress.Address1) ||
           string.IsNullOrWhiteSpace(request.ContactAddress.City) ||
           string.IsNullOrWhiteSpace(request.ContactAddress.PostCode) ||
           string.IsNullOrWhiteSpace(request.ContactAddress.CountryCode))
       {
           return Results.BadRequest(new { error = "If address provided, Address1, City, PostCode, and CountryCode are required" });
       }

       if (request.ContactAddress.CountryCode.Length != 2)
           return Results.BadRequest(new { error = "CountryCode must be 2 characters (ISO 3166-1 alpha-2)" });
   }
   ```

3. Generate PasswordSalt:
   ```csharp
   var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
   var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

   var actor = new Actor
   {
       Id = Guid.NewGuid(),
       Email = request.Email.ToLowerInvariant(),
       FirstName = request.FirstName.Trim(),
       LastName = request.LastName.Trim(),
       ContactAddress = request.ContactAddress != null ? new Address
       {
           Address1 = request.ContactAddress.Address1,
           Address2 = request.ContactAddress.Address2,
           City = request.ContactAddress.City,
           PostCode = request.ContactAddress.PostCode,
           CountryCode = request.ContactAddress.CountryCode.ToUpperInvariant()
       } : null,
       Phone = request.Phone,
       ActorType = actorType,
       PasswordHash = passwordHash,
       PasswordSalt = salt,
       ActivationToken = activationToken
   };
   ```

**Testing**:
- Update RegisterActorTests.cs (6 tests)
- Change request bodies to use firstName/lastName/contactAddress object

**Estimated**: 40 minutes

---

#### C2: Update Login Endpoint (20 min)

**File**: `src/Innoventity.API/Features/Authentication/Login.cs`

**Changes**:
1. Update response DTO:
   ```csharp
   // Before
   record ActorResponse(
       Guid Id,
       string Email,
       string FullName,
       string ActorType);

   // After
   record ActorResponse(
       Guid Id,
       string Email,
       string FirstName,
       string LastName,
       string DisplayName,  // Computed: FirstName + LastName
       string ActorType);
   ```

2. Map response:
   ```csharp
   var actorResponse = new ActorResponse(
       actor.Id,
       actor.Email,
       actor.FirstName,
       actor.LastName,
       actor.DisplayName,
       actor.ActorType.ToString());
   ```

**Testing**:
- Update LoginTests.cs (5 tests)
- Verify response includes firstName/lastName/displayName

**Estimated**: 20 minutes

---

#### C3: Update Refresh Token Endpoint (15 min)

**File**: `src/Innoventity.API/Features/Authentication/RefreshToken.cs`

**Changes**:
- Update ActorResponse DTO (same as Login)
- Map FirstName/LastName/DisplayName

**Testing**:
- Update RefreshTokenTests.cs (2 tests)

**Estimated**: 15 minutes

---

#### C4: Update GetInnovation Endpoint (20 min)

**File**: `src/Innoventity.API/Features/Innovations/GetInnovation.cs`

**Changes**:
1. Update OwnerDto:
   ```csharp
   // Before
   record OwnerDto(
       Guid Id,
       string FullName,
       string ActorType);

   // After
   record OwnerDto(
       Guid Id,
       string FirstName,
       string LastName,
       string DisplayName,
       string ActorType);
   ```

2. Map owner:
   ```csharp
   var ownerDto = new OwnerDto(
       owner.Id,
       owner.FirstName,
       owner.LastName,
       owner.DisplayName,
       owner.ActorType.ToString());
   ```

**Testing**:
- Update GetInnovationTests.cs (4 tests)
- Verify owner object includes firstName/lastName/displayName

**Estimated**: 20 minutes

---

#### C5: Update All Unit Tests (30 min)

**Files**:
- `tests/Innoventity.API.Tests/Unit/Domain/Entities/ActorTests.cs` (4 tests)

**Changes**:
- Replace `FullName` with `FirstName` and `LastName`
- Update assertions

**Example**:
```csharp
// Before
var actor = new Actor
{
    Id = Guid.NewGuid(),
    FullName = "Test Actor",
    // ...
};
Assert.False(string.IsNullOrEmpty(actor.FullName));

// After
var actor = new Actor
{
    Id = Guid.NewGuid(),
    FirstName = "Test",
    LastName = "Actor",
    // ...
};
Assert.False(string.IsNullOrEmpty(actor.FirstName));
Assert.False(string.IsNullOrEmpty(actor.LastName));
Assert.Equal("Test Actor", actor.DisplayName);
```

**Estimated**: 30 minutes

---

#### C6: Update All Integration Tests (40 min)

**Files** (total 22 integration tests):
- RegisterActorTests.cs (6 tests)
- ActivateAccountTests.cs (3 tests)
- LoginTests.cs (5 tests)
- RefreshTokenTests.cs (2 tests)
- GetInnovationTests.cs (4 tests)
- Phase0JourneyTests.cs (2 tests)

**Changes**:
- Update request bodies: `fullName` → `firstName`, `lastName`
- Update response assertions: `FullName` → `firstName`, `lastName`, `displayName`
- Update seed data actor creations

**Estimated**: 40 minutes

---

#### C7: Run Full Test Suite (15 min)

**Command**:
```bash
dotnet test --verbosity normal
```

**Expected Results**:
- ✅ 42+ tests passing (10 new EntityBase tests + 32 updated tests)
- ✅ Zero test failures
- ✅ Build succeeds with zero warnings

**If failures**:
- Debug failing tests
- Fix issues
- Re-run until all pass

**Estimated**: 15 minutes

---

**Phase C Total**: 2.5 hours

---

## Testing Strategy

### Test Coverage Matrix

| Test Type | Current (Phase 0-5) | Phase 0.5 Target | Delta |
|-----------|---------------------|------------------|-------|
| **Unit Tests** | 10 | 17 | +7 |
| - Actor entity tests | 4 | 6 | +2 |
| - Innovation entity tests | 3 | 3 | 0 |
| - Industry entity tests | 3 | 3 | 0 |
| - EntityBase tests | 0 | 7 | +7 (NEW) |
| - Address tests | 0 | 3 | +3 (NEW) |
| **Integration Tests** | 22 | 25 | +3 |
| - Registration tests | 6 | 8 | +2 |
| - Login tests | 5 | 5 | 0 (updated) |
| - Activation tests | 3 | 3 | 0 (updated) |
| - Refresh token tests | 2 | 2 | 0 (updated) |
| - Get innovation tests | 4 | 4 | 0 (updated) |
| - E2E journey tests | 2 | 3 | +1 |
| **Total** | **32** | **42** | **+10** |

### New Test Files

**Unit Tests**:
1. `EntityBaseTests.cs` (7 tests):
   - Equality by Id for persisted entities
   - Reference equality for transient entities
   - GetHashCode stability for collections
   - IsTransient() method
   - Equality operators
   - Null handling

2. `AddressTests.cs` (3 tests):
   - Valid address creation
   - CountryCode validation (must be 2 chars)
   - Required field validation

**Integration Tests**:
3. Registration with structured address (2 new tests):
   - Valid structured address accepted
   - Partial address rejected (if City provided without Country)

4. E2E journey with name decomposition (1 new test):
   - Register → Activate → Login → GetInnovation (verify displayName in owner)

### Test Execution Order

**Phase A Tests** (EntityBase infrastructure):
```bash
dotnet test --filter "FullyQualifiedName~EntityBaseTests"
# Expected: 7 tests passing
```

**Phase B Tests** (Actor refactoring):
```bash
dotnet test --filter "FullyQualifiedName~ActorTests"
# Expected: 6 tests passing (4 updated + 2 new)

dotnet test --filter "FullyQualifiedName~AddressTests"
# Expected: 3 tests passing
```

**Phase C Tests** (API & integration):
```bash
dotnet test --filter "Category=Integration"
# Expected: 25 tests passing

dotnet test --verbosity normal
# Expected: 42 tests passing
```

### Breaking Test Scenarios

**Scenario 1: FullName property no longer exists**
- **Expected Failure**: Compilation errors in all tests using `FullName`
- **Fix**: Replace with `FirstName` and `LastName`
- **Affected Files**: 22+ test files

**Scenario 2: ContactAddress is now Address object**
- **Expected Failure**: Type mismatch (string vs Address)
- **Fix**: Create Address object or set to null
- **Affected Files**: RegisterActorTests.cs, GetInnovationTests.cs

**Scenario 3: Migration fails on existing data**
- **Expected Failure**: FullName split produces empty LastName
- **Fix**: Manual review of actors with single-word names
- **Mitigation**: Migration logs warning for manual review

---

## Rollback Strategy

### Scenario: Critical Bug Found After Deployment

**Rollback Steps**:

1. **Revert Git Commit**:
   ```bash
   git checkout 001-platform-core
   git branch -D 002-domain-enhancements
   ```

2. **Revert Database Migration**:
   ```bash
   dotnet ef database update [PreviousMigrationName]
   # This recreates FullName, ContactAddress columns
   # Drops FirstName, LastName, PasswordSalt, Address_* columns
   ```

3. **Data Restoration**:
   - **Problem**: FullName dropped - data lost!
   - **Solution**: Migration must preserve FullName data
   - **Implementation**:
     ```sql
     -- In Down() migration
     UPDATE [Actors]
     SET [FullName] = [FirstName] + ' ' + [LastName]
     WHERE [FullName] IS NULL;
     ```

4. **Verify Rollback**:
   ```bash
   dotnet test --verbosity normal
   # All 32 original tests should pass
   ```

### Rollback Complexity: **MEDIUM**

**Risks**:
- Data loss if migration doesn't preserve FullName
- ContactAddress data lost (was nullable, likely NULL for existing actors)
- PasswordSalt generated randomly - cannot restore exact value (acceptable - hash remains valid)

**Mitigation**:
- **CRITICAL**: Test rollback on staging environment before production deployment
- Backup database before migration
- Keep migration reversible (preserve data in Down() method)

---

## Risk Assessment

### High Risks

#### Risk 1: FullName Split Algorithm Incorrect

**Probability**: MEDIUM
**Impact**: HIGH (data corruption)

**Scenario**: Actor named "Madonna" (single word) has empty LastName after split

**Mitigation**:
1. Migration logs warning for single-word names
2. Manual review process for flagged actors
3. Default: SingleWord → FirstName="SingleWord", LastName="" (allows manual fix)
4. Post-migration validation query:
   ```sql
   SELECT Id, FirstName, LastName
   FROM Actors
   WHERE LastName = '' OR FirstName = '';
   ```

**Contingency**: Provide admin tool to manually correct names

---

#### Risk 2: Breaking Changes Break Production

**Probability**: LOW (comprehensive tests)
**Impact**: HIGH (API downtime)

**Scenario**: Client apps using old API contract (fullName) fail after deployment

**Mitigation**:
1. Version API endpoints (v1 vs v2) - NOT implemented in Phase 0.5
2. Deploy to staging first, test with client apps
3. Communication: Notify client developers of breaking changes 2 weeks before
4. Grace period: Support both old/new contracts temporarily (NOT implemented in Phase 0.5)

**Contingency**: Rollback deployment, restore database backup

---

### Medium Risks

#### Risk 3: Test Updates Incomplete

**Probability**: MEDIUM
**Impact**: MEDIUM (tests fail, block deployment)

**Scenario**: Missed test file still uses `FullName` - compilation error

**Mitigation**:
1. Global search: `grep -r "FullName" tests/` before completion
2. Compiler errors will catch all `FullName` references
3. Run full test suite after each phase

**Contingency**: Complete test updates, re-run suite

---

#### Risk 4: EF Core Owned Entity Configuration Error

**Probability**: LOW
**Impact**: MEDIUM (runtime error)

**Scenario**: Address columns not created with correct names (ContactAddress_Address1 vs Address1)

**Mitigation**:
1. Explicit column name configuration in AppDbContext
2. Review generated migration SQL before applying
3. Test migration on local database first

**Contingency**: Fix configuration, regenerate migration

---

### Low Risks

#### Risk 5: Performance Degradation

**Probability**: LOW
**Impact**: LOW (minimal performance change)

**Scenario**: Entity equality checks slightly slower (method call vs struct comparison)

**Mitigation**:
1. EntityBase.GetHashCode() optimized for collections
2. No database query changes (schema only)
3. Benchmark critical paths if concerned

**Contingency**: Profile performance, optimize if needed

---

## Success Criteria

### Functional Requirements

- ✅ **R8.4.1 (Password Salt)**: Actor.PasswordSalt column exists, NOT NULL, generated on registration
- ✅ **R1.4 (Name Decomposition)**: Actor.FirstName and Actor.LastName columns exist, NOT NULL, FullName dropped
- ✅ **R1.5 (Address Value Object)**: Actor.ContactAddress is Address owned entity, 5 columns (Address1, City, PostCode, CountryCode, Address2)
- ✅ **R9.1 (EntityBase)**: Actor, Innovation, Industry inherit from EntityBase<TId>, equality semantics work

### Technical Requirements

- ✅ **Build**: Zero compilation errors, zero warnings
- ✅ **Tests**: 42+ tests passing (10 new + 32 updated)
- ✅ **Migration**: Applies successfully, data preserved (FullName split)
- ✅ **Seed Data**: 3 seed data tests passing
- ✅ **API Contracts**: Registration/Login/GetInnovation work with new schema

### Quality Requirements

- ✅ **Code Coverage**: Maintain >80% coverage (EntityBase 100%, Address 100%)
- ✅ **Documentation**: XML comments on all new classes
- ✅ **Consistency**: All entities follow EntityBase pattern
- ✅ **Reversibility**: Migration has working Down() method

### Constitutional Score

- ✅ **Current**: 98/100 (missing Salt, flat names, string address)
- ✅ **Target**: 99/100 (all Phase 0.5 improvements)
- ✅ **Improvement**: +1 point (security + data quality)

---

## Dependencies

### External Dependencies

- ✅ **EF Core 8.0**: Owned entity feature (OwnsOne) - already installed
- ✅ **BCrypt.Net**: Password hashing - already installed
- ✅ **System.Security.Cryptography**: Salt generation (built-in)
- ✅ **xUnit + FluentAssertions**: Testing - already installed

### Internal Dependencies

- ✅ **Phase 0-5 Complete**: 32 tests passing, all endpoints working
- ✅ **Database Schema**: Actors, Innovations, Industries tables exist
- ✅ **Seed Data**: 3 actors seeded for testing

### Blocking Dependencies

**NONE** - Phase 0.5 is self-contained and ready to start

---

## Timeline

### Estimated Effort: 7.5 hours

**Phase A**: 2 hours (EntityBase infrastructure)
**Phase B**: 3 hours (Actor schema refactoring)
**Phase C**: 2.5 hours (API & test updates)

### Recommended Schedule (2-day sprint)

**Day 1** (4 hours):
- Morning: Phase A (EntityBase) - 2 hours
- Afternoon: Phase B parts 1-3 (Address, Actor refactor, AppDbContext) - 2 hours

**Day 2** (3.5 hours):
- Morning: Phase B parts 4-6 (Migration, seed data, integration test) - 1.5 hours
- Afternoon: Phase C (API & test updates) - 2.5 hours

**Buffer**: 0.5 hours for unexpected issues

---

## Constraints

### Hard Constraints

1. **NO Phase 1+ Features**: Do not implement Innovation composition (R3.5), FormalResponse (R3.6), BusinessPlan (R3.7), or Valuation Service (R10.1) in Phase 0.5
2. **Preserve Existing Functionality**: All Phase 0-5 endpoints must work after refactoring
3. **No Data Loss**: Migration must preserve all existing actor data
4. **Breaking Changes Acceptable**: API contract changes allowed (documented)
5. **TDD Discipline**: Tests FIRST, implementation SECOND

### Soft Constraints

1. **Performance**: No degradation >5% on existing endpoints
2. **Code Style**: Follow existing C# conventions, XML comments on public APIs
3. **Git Commits**: Atomic commits per phase (A, B, C)
4. **Documentation**: Update README with Phase 0.5 completion status

---

## Definition of Done

### Phase A Done

- ✅ EntityBase<TId> class created with 100% test coverage
- ✅ EntityOfGuid, EntityOfInt32 created
- ✅ Actor, Innovation, Industry inherit from appropriate base
- ✅ 7 EntityBase unit tests passing
- ✅ Code reviewed, committed to 002-domain-enhancements branch

### Phase B Done

- ✅ Address value object created with tests
- ✅ Actor entity refactored (FirstName, LastName, PasswordSalt, Address)
- ✅ AppDbContext updated with owned entity configuration
- ✅ Migration created and tested on local database
- ✅ Seed data updated for 3 actors
- ✅ 3 seed data tests passing
- ✅ Code reviewed, committed

### Phase C Done

- ✅ Registration endpoint updated (firstName/lastName/address object)
- ✅ Login/RefreshToken endpoints updated (response DTO)
- ✅ GetInnovation endpoint updated (owner DTO)
- ✅ All 32 existing tests updated and passing
- ✅ 10 new tests added (EntityBase, Address, integration)
- ✅ Full test suite passing (42+ tests)
- ✅ Build succeeds with zero warnings
- ✅ Code reviewed, committed

### Phase 0.5 Done

- ✅ All phases A, B, C complete
- ✅ 42+ tests passing
- ✅ Migration applied to test database successfully
- ✅ Documentation updated (README, CHANGELOG)
- ✅ Constitutional score improved to 99/100
- ✅ Pull request created: 002-domain-enhancements → 001-platform-core
- ✅ Ready to merge after review

---

## Next Steps After Phase 0.5

### Immediate (After Merge to 001-platform-core)

1. **Continue Phase 6-8 Work**: Implement Create Innovation, Edit Innovation, Delete Innovation features
2. **Phase 0.5 Retrospective**: Document lessons learned, update estimation accuracy

### Phase 1 Preparation (Future)

1. **Create 003-innovation-composition Branch**: Plan Phase 1 refactoring
2. **Design Innovation Composition Pattern**: IdeaSummary, Product, Market, CollaborationRequirement owned entities
3. **Design FormalResponse Hierarchy**: Strategy pattern with Manufacturing/Sales/R&D specializations
4. **Estimate Effort**: Likely 21 hours based on complexity analysis

### Long-Term (Phase 2+)

1. **BusinessPlan Aggregate**: Factory methods, protected collections, child entities
2. **Project Valuation Service**: NPV calculation domain service
3. **Industry Hierarchy**: 4-level classification (Industry → Supersector → Sector → Subsector)

---

## Appendix A: File Checklist

### Files to Create

- [ ] `src/Innoventity.API/Domain/Common/EntityBase.cs`
- [ ] `src/Innoventity.API/Domain/Common/EntityOfGuid.cs`
- [ ] `src/Innoventity.API/Domain/Common/EntityOfInt32.cs`
- [ ] `src/Innoventity.API/Domain/Entities/Address.cs`
- [ ] `tests/Innoventity.API.Tests/Unit/Domain/Common/EntityBaseTests.cs`
- [ ] `tests/Innoventity.API.Tests/Unit/Domain/Entities/AddressTests.cs`

### Files to Modify

- [ ] `src/Innoventity.API/Domain/Entities/Actor.cs`
- [ ] `src/Innoventity.API/Domain/Entities/Innovation.cs`
- [ ] `src/Innoventity.API/Domain/Entities/Industry.cs`
- [ ] `src/Innoventity.API/Infrastructure/Persistence/AppDbContext.cs`
- [ ] `src/Innoventity.API/Infrastructure/Persistence/SeedData.cs`
- [ ] `src/Innoventity.API/Features/Authentication/Register.cs`
- [ ] `src/Innoventity.API/Features/Authentication/Login.cs`
- [ ] `src/Innoventity.API/Features/Authentication/RefreshToken.cs`
- [ ] `src/Innoventity.API/Features/Innovations/GetInnovation.cs`
- [ ] `tests/Innoventity.API.Tests/Unit/Domain/Entities/ActorTests.cs`
- [ ] `tests/Innoventity.API.Tests/Integration/Features/Authentication/RegisterActorTests.cs`
- [ ] `tests/Innoventity.API.Tests/Integration/Features/Authentication/ActivateAccountTests.cs`
- [ ] `tests/Innoventity.API.Tests/Integration/Features/Authentication/LoginTests.cs`
- [ ] `tests/Innoventity.API.Tests/Integration/Features/Authentication/RefreshTokenTests.cs`
- [ ] `tests/Innoventity.API.Tests/Integration/Features/Innovations/GetInnovationTests.cs`
- [ ] `tests/Innoventity.API.Tests/E2E/Journeys/Phase0JourneyTests.cs`

### Migrations to Create

- [ ] `src/Innoventity.API/Migrations/[Timestamp]_AddEntityBaseAndRefactorActor.cs`

---

## Appendix B: Validation Checklist

### Pre-Implementation

- [ ] Read spec.md (Phase 0.5 requirements only)
- [ ] Read DDD architectural analysis
- [ ] Understand current Actor/Innovation/Industry entity structure
- [ ] Understand current test coverage (32 tests)
- [ ] Review EntityBase pattern from legacy analysis

### During Implementation

- [ ] Follow TDD: Write test FIRST, then implement
- [ ] Run tests after each file change
- [ ] Commit atomically (one logical change per commit)
- [ ] Review generated migration SQL
- [ ] Test migration on local database before committing

### Post-Implementation

- [ ] All 42+ tests passing
- [ ] Build succeeds with zero warnings
- [ ] Migration applied successfully
- [ ] Seed data working
- [ ] Code reviewed (self-review checklist below)
- [ ] Documentation updated (README, this plan.md)

### Self-Review Checklist

- [ ] All public APIs have XML documentation
- [ ] No commented-out code
- [ ] No magic numbers (use constants)
- [ ] Consistent naming conventions
- [ ] No copy-paste duplications
- [ ] Error handling consistent with existing patterns
- [ ] Security: PasswordSalt generated with cryptographic RNG
- [ ] Security: No sensitive data in logs/errors

---

## Appendix C: References

- [Expert DDD Architectural Analysis](../../.specify/analysis/ddd-architectural-analysis.md)
- [Legacy Domain Analysis](../../.specify/analysis/legacy-domain-analysis.md)
- [Feature Specification](spec.md)
- Eric Evans: *Domain-Driven Design* (2003) - Chapter 5 "Entities"
- Martin Fowler: *Refactoring* (2018) - Value Objects pattern
- EF Core Owned Entity Types: https://learn.microsoft.com/ef/core/modeling/owned-entities

---

**Plan Version**: 1.0
**Last Updated**: February 10, 2026
**Next Review**: After Phase 0.5 completion

