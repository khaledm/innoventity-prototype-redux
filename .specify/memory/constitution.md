<!--
═══════════════════════════════════════════════════════════════════════════════
CONSTITUTION SYNC IMPACT REPORT
═══════════════════════════════════════════════════════════════════════════════

VERSION: 1.1.0 (CONVENTIONAL COMMITS PRINCIPLE)
REPORT DATE: 2026-04-18
CHANGE TYPE: Minor Version Increment - New Principle Added

─────────────────────────────────────────────────────────────────────────────
VERSION HISTORY
─────────────────────────────────────────────────────────────────────────────
Previous Version: 1.0.0
New Version: 1.1.0
Bump Type: MINOR (New principle added)
Rationale: Formalize Conventional Commits specification as foundational
           principle for all git commit messages to ensure machine-parseable,
           semantic, and consistent version control history.

─────────────────────────────────────────────────────────────────────────────
CONSTITUTIONAL CHANGES
─────────────────────────────────────────────────────────────────────────────

NEW PRINCIPLES:
  8. Commit Messages Are Documentation
     - All git commits follow Conventional Commits 1.0.0 Specification
     - Semantic, machine-parseable commit history
     - Imperative mood, lowercase subject, no trailing period
     - Breaking changes explicitly marked with !
     - Aligns with existing speckit.git.commit hooks

MODIFIED SECTIONS:
  - Section 6 (Development Process): Added "Commit Message Format" subsection
    with reference to Principle 8 and quick-reference template

IMPACT ON EXISTING WORKFLOWS:
  ✅ speckit.git.commit hooks: Now have constitutional backing for format
  ✅ Feature development: Commit messages must follow spec from this point
  ✅ Code review checklist: Now includes commit message validation
  ✅ AI agents: Must generate Conventional Commits formatted messages

─────────────────────────────────────────────────────────────────────────────
TEMPLATE CONSISTENCY VALIDATION
─────────────────────────────────────────────────────────────────────────────

✅ .specify/extensions.yml
   - Contains speckit.git.commit hooks (before/after all commands)
   - Hooks now have constitutional principle backing them
   - No template changes required (hooks remain unchanged)

✅ plan-template.md, spec-template.md, tasks-template.md
   - No direct git commit dependencies
   - No updates required

✅ Memory files and agent instructions
   - AI agents must now follow Principle 8 when suggesting commits
   - Future git operations must validate against Conventional Commits spec

─────────────────────────────────────────────────────────────────────────────
VALIDATION RESULTS
─────────────────────────────────────────────────────────────────────────────

✅ No conflicts with existing principles
✅ Aligns with Principle 4 (Specification Drives Implementation)
✅ Supports Principle 6 (AI Augments, Humans Decide) - AI generates commits,
   humans validate format
✅ Version increment follows semantic versioning (MINOR: new capability)
✅ Effective date updated to amendment date
✅ Sign-off section updated with amendment authority

─────────────────────────────────────────────────────────────────────────────
COMMIT MESSAGE FOR THIS CHANGE
─────────────────────────────────────────────────────────────────────────────

docs(constitution): add principle 8 for conventional commits

Establish Conventional Commits 1.0.0 Specification as non-negotiable
principle for all git commit messages. Ensures semantic, machine-parseable
version control history.

Key requirements:
- Format: <type>(<scope>): <description>
- Types: feat, fix, docs, style, refactor, perf, test, chore
- Breaking changes marked with ! and BREAKING CHANGE: footer
- Imperative mood, lowercase, no trailing period
- Aligns with existing speckit.git.commit hooks

BREAKING CHANGE: All future commits must follow Conventional Commits spec.
Non-compliant messages should be rejected in code review.

Ref: https://conventionalcommits.org

═══════════════════════════════════════════════════════════════════════════════

═══════════════════════════════════════════════════════════════════════════════
CONSTITUTION SYNC IMPACT REPORT (ARCHIVED)
═══════════════════════════════════════════════════════════════════════════════

VERSION: 1.0.0 (INITIAL RATIFICATION)
REPORT DATE: 2026-02-08
CHANGE TYPE: Initial Constitution Establishment

─────────────────────────────────────────────────────────────────────────────
VERSION HISTORY
─────────────────────────────────────────────────────────────────────────────
Previous Version: None (Initial Version)
New Version: 1.0.0
Bump Type: MAJOR (Initial establishment of governance framework)
Rationale: First official constitution establishing foundational principles,
           constraints, and governance for the Innoventity platform v1.0
           development project.

─────────────────────────────────────────────────────────────────────────────
CONSTITUTIONAL CHANGES
─────────────────────────────────────────────────────────────────────────────

ESTABLISHED PRINCIPLES (8):
  1. User Experience First
     - Technology serves users, not the other way around
     - Design decisions start with user needs

  2. Quality is Non-Negotiable
     - Production-ready code at every phase
     - Tests validate real business rules
     - Security baked in, not bolted on

  3. Simplicity Over Cleverness
     - Choose obvious solutions over clever abstractions
     - Use frameworks as intended

  4. Specification Drives Implementation
     - Write specs before code
     - Specs define what "done" means
     - Spec-Kit workflow (SPECIFY → PLAN → TASKS → IMPLEMENT)

  5. Tests Must Prove They Work
     - Tests must be observed failing before implementation (red → green → refactor)
     - OR validated via characterization testing (deliberate breakage)
     - High coverage is necessary but not sufficient

  6. AI Augments, Humans Decide
     - AI generates boilerplate; humans design architecture
     - Critical business logic is human-implemented
     - All AI output is validated

  7. Architecture Must Support Evolution
     - v1.0 should not block v2.0 features
     - Don't build v2.0 features prematurely
     - Extensibility over flexibility

  8. Commit Messages Are Documentation
     - All commits follow Conventional Commits 1.0.0 Specification
     - Semantic, machine-parseable history
     - Imperative mood, scoped, breaking changes explicit

ESTABLISHED SECTIONS (10):
  ✅ Section 1: Project Vision
  ✅ Section 2: Project Scope (v1.0)
  ✅ Section 3: Core Principles (NON-NEGOTIABLE)
  ✅ Section 4: Constraints (Boundaries)
  ✅ Section 5: Quality Standards
  ✅ Section 6: Development Process
  ✅ Section 7: Success Criteria
  ✅ Section 8: Decision Authority
  ✅ Section 9: Amendment Process
  ✅ Section 10: Related Documents

MODIFIED PRINCIPLES: None (Initial version)
REMOVED SECTIONS: None (Initial version)
RENAMED SECTIONS: None (Initial version)

─────────────────────────────────────────────────────────────────────────────
TEMPLATE CONSISTENCY VALIDATION
─────────────────────────────────────────────────────────────────────────────

✅ plan-template.md
   - References "Constitution Check" section
   - Gates determined dynamically from constitution principles
   - No updates required (generic reference compatible)

✅ spec-template.md
   - No direct constitution dependencies
   - User story format aligns with Principle 1 (User Experience First)
   - No updates required

✅ tasks-template.md
   - No direct constitution dependencies
   - Task organization aligns with Principle 4 (Specification Drives Implementation)
   - No updates required

✅ checklist-template.md
   - Not reviewed (optional template)
   - Expected to align with Definition of Done (Section 6)

✅ agent-file-template.md
   - Not reviewed (optional template)
   - Expected to reference constitution for guidance

⚠️  Command templates (commands/*.md)
   - Directory not found or empty
   - No updates needed if commands reference constitution generically

✅ README.md
   - Minimal content (only project name)
   - Should eventually reference constitution for project overview
   - Not critical for initial ratification

─────────────────────────────────────────────────────────────────────────────
DEPENDENT ARTIFACTS STATUS
─────────────────────────────────────────────────────────────────────────────

STRATEGIC DOCUMENTS (Referenced in Section 10):
  ⚠️  ../docs/doc/RE-ENGINEERING_STRATEGY.md - Exists but not validated
  ⚠️  ../docs/doc/PHASE_1_TARGET_DEFINITION.md - Exists but not validated
  ⚠️  ../docs/doc/PROJECT_KNOWLEDGE_BASE.md - Referenced but not found
  ⚠️  ../docs/doc/ARCHITECTURE_GUARDRAILS.md - Exists but not validated
  ⚠️  ../docs/doc/AI_ASSISTED_DEVELOPMENT_PRINCIPLES.md - Exists but not validated
  ⚠️  ../docs/doc/ARCHITECTURAL_INVENTORY.md - Exists but not validated

  These documents should be reviewed for alignment with constitutional
  principles in a follow-up consistency audit.

LEGACY REFERENCE:
  📍 Legacy codebase location specified:
     c:\Users\mahmu\source\repos\innoventity-prototype-development\legacy-mvc\
  ✅ Constitution correctly scopes legacy as reference for functional
     requirements, not data migration source

─────────────────────────────────────────────────────────────────────────────
VALIDATION RESULTS
─────────────────────────────────────────────────────────────────────────────

✅ No bracket placeholders remaining (all fields populated)
✅ Version follows semantic versioning: 1.0.0
✅ Dates in ISO format: YYYY-MM-DD
✅ Principles are declarative and testable
✅ Rationale provided for non-negotiable items
✅ Clear boundaries between principles and implementation details
✅ Amendment process documented
✅ Decision authority clearly defined
✅ Success criteria measurable

─────────────────────────────────────────────────────────────────────────────
FOLLOW-UP TASKS
─────────────────────────────────────────────────────────────────────────────

None - Constitution is complete and ready for use.

RECOMMENDED NEXT STEPS (post-ratification):
  1. Conduct consistency audit of strategic documents (Section 10 references)
  2. Create PROJECT_KNOWLEDGE_BASE.md if needed for decision tracking
  3. Update README.md to reference constitution as project foundation
  4. Begin feature development following Spec-Kit workflow (Principle 4)

─────────────────────────────────────────────────────────────────────────────
COMMIT MESSAGE
─────────────────────────────────────────────────────────────────────────────

docs: establish constitution v1.0.0 (initial ratification)

Establish foundational governance framework for Innoventity platform v1.0
development. Defines 7 core principles, project scope, quality standards,
development process, and success criteria.

Key principles:
- User Experience First
- Quality is Non-Negotiable
- Simplicity Over Cleverness
- Specification Drives Implementation
- Tests Must Prove They Work
- AI Augments, Humans Decide
- Architecture Must Support Evolution

Ratified: 2026-02-08
Authority: Project Owner
Next Review: Post-Phase 1 retrospective

═══════════════════════════════════════════════════════════════════════════════
END OF SYNC IMPACT REPORT
═══════════════════════════════════════════════════════════════════════════════
-->

# INNOVENTITY PLATFORM CONSTITUTION

## Project Principles and Non-Negotiable Constraints

**Version**: 1.1.0
**Effective Date**: April 18, 2026 (Last Amended)
**Initial Ratification**: February 8, 2026
**Project**: Innoventity - Global Open Innovation Platform (v1.0)
**Project Type**: Solo Learning/Portfolio Project - Reference-Based Reimplementation
**Authority**: Project Owner

---

## PURPOSE OF THIS DOCUMENT

This constitution defines the **unchangeable principles** that govern all decisions in the Innoventity platform development. Think of it as the project's DNA—the core beliefs that cannot be compromised.

**What belongs here**: Principles, values, constraints, boundaries
**What doesn't belong here**: Technical implementation details, specific technologies, code patterns

When specifications, plans, or implementation decisions conflict with this constitution, **the constitution wins**.

---

## REFERENCE: LEGACY SYSTEM

### Where to Find Functional Requirements

**Legacy Codebase Location**: `c:\Users\mahmu\source\repos\innoventity-prototype-development\legacy-mvc\`

**What the legacy provides**:

- ✅ **Business domain knowledge**: Innovation actors, workflows, business rules
- ✅ **User journeys**: How users currently interact with the platform
- ✅ **Business logic**: Partner selection rules, validation rules, state transitions
- ✅ **Integration patterns**: Email notifications, document storage, event publishing
- ✅ **Data model**: Entities, relationships, value objects

**What the legacy does NOT provide**:

- ❌ **Operational data** (system is dormant - no data to migrate)
- ❌ **Modern architectural patterns** (it's legacy MVC, we're modernizing)
- ❌ **Quality standards** (we're raising the bar)

**During SPECIFY phase**: Examine legacy code to extract:

- User stories and use cases
- Business rules and validations
- Actor capabilities and workflows
- Success criteria and outcomes

**Key Files to Review**:

- `Models/ProductIdea.cs` - Innovation domain model
- `Models/IdeaAuthor.cs` and actor entities - User types and capabilities
- `Controllers/*` - User workflows and journeys
- `Services/*` - Business logic and rules
- `ViewModels/*` - UI requirements and data needs

---

## 1. PROJECT VISION

### What We're Building

An **open innovation platform** that connects researchers with commercialization partners to transform ideas into market-ready products.

### Who It's For

- **Idea Generators**: Researchers with innovations seeking partners
- **R&D Organizations**: Technical experts offering research capabilities
- **Manufacturing Companies**: Production specialists
- **Sales & Marketing Companies**: Market access providers
- **Investors**: Funding sources

### Success Looks Like

- Innovation submission rate increases 20%+
- Partner formation rate increases 30%+
- Time from idea to commercialization reduces significantly
- User satisfaction with partner matching is high

### What Makes This Different

- **Formal collaboration process**: Structured bidding, selection, and partnership
- **Virtual incubator**: Collaborative workspace for selected teams
- **Business plan development**: Guided framework for commercialization planning
- **Multi-actor orchestration**: Coordinates 5 different user types with different needs

---

## 2. PROJECT SCOPE (v1.0)

### What's IN Scope

**Platform Mode**:

- ✅ Open Innovation only (public discovery, anyone can participate)

**Actor Types**:

- ✅ Idea Generator (researcher/inventor)
- ✅ R&D Organization (technical partner)
- ✅ Manufacturing Company (production partner)
- ✅ Sales & Marketing Company (distribution partner)
- ✅ Investor (funding partner)

**Core Features**:

- ✅ Actor registration and profile management
- ✅ Innovation submission (multi-step process)
- ✅ Innovation discovery (search, browse, filter)
- ✅ Formal bidding (4 bid types: R&D, Manufacturing, Sales, Investment)
- ✅ Partner selection (one per bid type, irreversible decision)
- ✅ Virtual incubator (team collaboration workspace)
- ✅ Business plan creation (financial projections, competitive analysis, commercialization roadmap)
- ✅ Document management (upload, store, share)
- ✅ Real-time notifications (bid alerts, selection notifications)
- ✅ Email notifications (workflow updates)

### What's OUT of Scope (Deferred to v2.0)

**Platform Modes**:

- ❌ Closed Innovation (organization-internal only)
- ❌ Hybrid Innovation (selective external collaboration)

**Organizational Features**:

- ❌ Organization workspaces
- ❌ Organization member management
- ❌ Multi-tenancy (organization-scoped data)

**Additional Actor Types**:

- ❌ Government entities
- ❌ Technology parks

**Advanced Features**:

- ❌ Analytics dashboard (beyond basic metrics)
- ❌ Mobile applications
- ❌ Advanced recommendation engine
- ❌ Marketplace features (payments, contracts)

**Why this scope?**
v1.0 focuses on **learning modern tech stack** and **delivering core value** (Open Innovation workflow). Complex organizational features add 6+ months and significant architectural complexity better suited for v2.0 after validating core platform.

---

## 3. CORE PRINCIPLES (NON-NEGOTIABLE)

### Principle 1: User Experience First

**Statement**: Technology serves users, not the other way around.

**What this means**:

- Design decisions start with user needs, not technical preferences
- Simplicity beats feature bloat
- User journeys are smooth and intuitive
- Error messages are helpful, not cryptic
- Performance is a feature (fast responses, no janky UI)

**Questions to ask**:

- Does this make users' lives easier?
- Would a non-technical user understand this?
- Are we solving user problems or creating developer satisfaction?

---

### Principle 2: Quality is Non-Negotiable

**Statement**: We ship production-ready code at every phase, not "we'll fix it later."

**What this means**:

- Every feature is fully tested before deployment
- Tests validate real business rules, not just happy paths
- Security is baked in, not bolted on
- Performance is validated, not assumed
- Documentation exists before code review

**This is NOT negotiable because**:

- Technical debt compounds interest exponentially
- "Fix it later" usually means "never fix it"
- Users deserve quality from day one
- Portfolio demonstration requires production-ready code

**Red flags**:

- "We'll add tests in Phase X"
- "Security review can happen later"
- "This is just a prototype" (no, it's v1.0)

---

### Principle 3: Simplicity Over Cleverness

**Statement**: Choose obvious solutions over clever abstractions.

**What this means**:

- Use frameworks as intended, not creatively
- Duplication is acceptable if it keeps code simple
- Solve today's problem, not hypothetical future problems
- Explicit code beats implicit magic
- Boring code is good code

**Questions to ask**:

- Is there a simpler way?
- Would a new team member understand this?
- Are we showing off or solving problems?
- Would this survive a code review by a skeptical senior engineer?

---

### Principle 4: Specification Drives Implementation

**Statement**: We write specs before code, and specs define what "done" means.

**What this means**:

- Every feature starts with a specification (user needs → technical plan → tasks → code)
- Specifications are living documents (updated as we learn)
- Code is validated against specifications, not the reverse
- No "we'll document it later"

**Spec-Kit workflow**:

1. **SPECIFY**: What user problem are we solving? (functional requirements from legacy)
2. **PLAN**: How will we solve it technically? (architecture decisions)
3. **TASKS**: What work needs to happen? (granular, testable chunks)
4. **IMPLEMENT**: Write the code (AI-assisted, human-validated)

**This is NOT negotiable because**:

- Spec-first prevents "let's just start coding and see what happens"
- Specifications enable AI assistance (AI needs clear requirements)
- Documentation exists by definition (specs = documentation)

---

### Principle 5: Tests Must Prove They Work

**Statement**: Tests that never fail have no validity. [Reference: Mark Seemann, "AI-generated tests as ceremony"]

**What this means**:

- Every test must be observed failing before implementation (red → green → refactor)
- OR tests must be validated by deliberately breaking the system (characterization testing)
- Tests verify business rules, not just "returns 200 OK"
- High coverage is necessary but not sufficient (mutation testing proves test quality)

**Critical business areas** (human-written tests required):

- Partner selection (only one per type, irreversible)
- Authorization (who can access what)
- Business plan calculations (financial projections)
- State transitions (innovation workflow states)

**Questions to ask**:

- Have I seen this test fail?
- What bug would this catch?
- Would this test still pass if I removed the business logic?

**This is NOT negotiable because**:

- False confidence is worse than no tests
- AI-generated tests often pass without validating anything
- Test-driven development requires observing failure first

---

### Principle 6: AI Augments, Humans Decide

**Statement**: AI tools accelerate mechanical work; humans own thinking and judgment.

**What this means**:

- AI generates boilerplate (DTOs, scaffolding, test stubs)
- Humans design domain models, security, architecture
- Humans validate all AI output (no blind trust)
- Critical business logic is human-implemented

**AI's role**:

- ✅ Generate repetitive code (DTOs, validators, mappers)
- ✅ Scaffold test structures
- ✅ Suggest implementations

**Human's role**:

- ✅ Write specifications
- ✅ Design domain model
- ✅ Implement authorization logic
- ✅ Validate tests have epistemic value
- ✅ Review AI code for simplicity and correctness

**This is NOT negotiable because**:

- AI doesn't understand business context
- Security bugs from AI can be catastrophic
- Architectural decisions require experience and judgment
- Learning requires understanding, not just generating

---

### Principle 7: Architecture Must Support Evolution

**Statement**: v1.0 should not block v2.0 features, but shouldn't build them prematurely.

**What this means**:

- Design clean boundaries (v2.0 can add multi-tenancy without rewrite)
- Don't hardcode single-tenant assumptions everywhere
- But also don't build organization features in v1.0
- Extensibility > flexibility (easy to add, not easy to change)

**v1.0 → v2.0 evolution path**:

- v2.0 will add: Organizations, Closed/Hybrid modes, Government actors
- v1.0 must allow: Adding organization context to queries, adding new actor types, policy-based authorization extensions
- v1.0 must NOT: Require database rewrite, break existing APIs, force architectural overhaul

**Design for**:

- Repository abstractions (can add tenant filtering in v2.0)
- Policy-based authorization (can add organization policies in v2.0)
- Actor extensibility (inheritance allows new types)

**Avoid**:

- Global organization state
- Hardcoded "WHERE OrganizationId IS NULL"
- Actor-type switch statements in 50 files

---

### Principle 8: Commit Messages Are Documentation

**Statement**: Git history is a communication tool. Every commit message must be semantic, machine-parseable, and follow Conventional Commits 1.0.0 Specification.

**What this means**:

- All commits follow **Conventional Commits** format: `<type>(<scope>): <description>`
- Commit history enables automated changelog generation and semantic versioning
- Future developers (including your future self) understand the "why" behind changes
- Breaking changes are explicitly marked and documented

**Commit Message Format**:

```
<type>(<optional scope>): <description>

[optional body]

[optional footer(s)]
```

**Required Types** (use ONLY these):

- `feat`: A new feature (correlates with MINOR in SemVer)
- `fix`: A bug fix (correlates with PATCH in SemVer)
- `docs`: Documentation changes only
- `style`: Formatting, missing semi-colons, whitespace (no code logic change)
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `perf`: Code change that improves performance
- `test`: Adding or correcting tests
- `chore`: Updates to build tasks, package manager configs, tooling

**Scope Rules**:

- **MUST** include scope if change is localized (e.g., `feat(auth):`, `fix(ui):`, `test(registration):`)
- Use module/feature names as scopes: `auth`, `ui`, `api`, `infrastructure`, `ci`, `registration`, `bids`
- For cross-cutting changes, scope may be omitted (e.g., `chore: update dependencies`)

**Subject Line Rules**:

- Use **imperative mood** ("add", "fix", "update", NOT "added", "fixed", "updated")
- **Lowercase** first letter (e.g., "add validation", NOT "Add validation")
- **No trailing period**
- **50 characters or less** (aim for clarity and brevity)

**Breaking Changes**:

- Append `!` after type/scope: `feat(api)!: change authentication flow`
- **MUST** include `BREAKING CHANGE:` as first line of footer
- Explain what broke and migration path in footer

**Body (Optional)**:

- Explain the **"why"** behind the change (the "what" should be obvious from code)
- Use when the subject line alone isn't sufficient
- Wrap at 72 characters per line

**Footer (Optional)**:

- Reference issues/PRs: `Ref: #123` or `Closes: #456`
- Document breaking changes: `BREAKING CHANGE: removed legacy API`
- Multiple footers allowed

**Example - Feature Commit**:

```
feat(forms): add signal-based validation for CRUD inputs

Migrates the old RxJS validator streams to the new Angular Signal
Resource API for better performance and simpler mental model.

Ref: #2259
```

**Example - Bug Fix Commit**:

```
fix(auth): prevent token refresh race condition

Ensures refresh token endpoint is called only once when multiple
concurrent requests fail with 401. Previous implementation caused
multiple refresh attempts leading to token invalidation.

Closes: #3421
```

**Example - Breaking Change Commit**:

```
feat(api)!: migrate to minimal API endpoints

Replace controller-based endpoints with minimal APIs for better
performance and reduced boilerplate.

BREAKING CHANGE: All endpoints now use /api/v2/ prefix instead of /api/.
Clients must update base URL configuration.

Migration: Update API_BASE_URL to include /v2/ segment.

Ref: #5001
```

**This is NOT negotiable because**:

- Enables automated changelog and release note generation
- Supports semantic versioning automation
- Improves code review quality (clear intent in history)
- Aligns with existing `speckit.git.commit` hooks in `.specify/extensions.yml`
- Machine-parseable history enables tooling (e.g., generating release notes)
- Portfolio demonstration requires professional git practices

**Questions to ask before committing**:

- Does this follow Conventional Commits format?
- Is the type correct (feat vs fix vs refactor)?
- Is the scope appropriate and clear?
- Did I use imperative mood?
- If breaking change, did I mark it with `!` and add footer?
- Does the body explain "why" if needed?

**Red flags**:

- "Updated stuff"
- "Fixed bug" (which bug? what module?)
- "WIP" or "temp commit" (should be squashed before merge)
- Past tense: "Added feature" (should be "add feature")
- Missing scope on localized change
- Breaking change without `!` and footer

---

## 4. CONSTRAINTS (BOUNDARIES)

### Constraint 1: Solo Project Realities

**Context**: This is a one-person learning project, not a team effort.

**What this means**:

- Timeline is flexible (10 months target, learning takes priority)
- No committee approvals needed (project owner decides)
- Code review is self-review (document decisions for portfolio)
- Scope must be realistic for one person (hence v1.0 simplification)

**Implications**:

- Choose simple solutions (no time for complex infrastructure)
- Focus on learning, not scaling to millions of users
- Document decisions for interview discussions
- Test automation is critical (no QA team)

---

### Constraint 2: No Data Migration

**Critical Discovery**: Legacy system is dormant with no operational data.

**What this means**:

- This is reference-based reimplementation, NOT data migration
- No complex migration scripts needed
- Fresh database with modern schema
- Seed data for development/testing
- Freedom to optimize naming and structure

**Implications**:

- Simpler scope (no migration complexity)
- Modern schema from day one (fix legacy naming: IdeaAuthor → IdeaGenerator)
- No backward compatibility constraints
- Can use latest patterns without restriction

---

### Constraint 3: Learning Objectives Drive Decisions

**Context**: Primary goal is mastering modern .NET + Angular, secondary is portfolio piece.

**What this means**:

- Technology choices prioritize learning value
- Over-engineering for education is acceptable (within reason)
- Documentation includes "why" for interview prep
- Architecture demonstrates best practices, not just working code

**Skills to demonstrate**:

- Modern .NET (ASP.NET Core 8, EF Core 8, Minimal APIs)
- Modern Angular (v18, Standalone Components, Signals) - should use Microsoft Asp.NET Core with Angular template to create, develop the code.
- Vertical Slice Architecture
- Spec-first development
- Test automation (unit, mutation, integration, E2E, load)
- Azure cloud deployment
- CI/CD pipelines
- API design and documentation

---

### Constraint 4: Production-Ready Mindset

**Context**: v1.0 must be deployable to real users at launch, not "good enough for a prototype."

**What this means**:

- Security from day one (authentication, authorization, data protection)
- Performance validated (load testing, optimization)
- Monitoring and observability (logs, metrics, alerts)
- Error handling and resilience (graceful degradation)
- Documentation complete (API docs, deployment runbooks)

**This is NOT a prototype** because:

- Portfolio must demonstrate production skills
- "Built a prototype" is weak on resume vs "Launched production platform"
- Learning production engineering is the goal
- Employers want production-ready developers

---

## 5. QUALITY STANDARDS

### Testing Standards

**Baseline requirements**:

- Unit test coverage: >80% (business logic)
- Integration test coverage: 100% (API endpoints)
- E2E test coverage: All critical user journeys
- Mutation testing score: >70% (validates test quality)

**Critical paths requiring human-written tests**:

- Partner selection workflow
- Authorization and access control
- Business plan financial calculations
- Innovation state transitions

**Test validity requirements**:

- Tests observed failing before implementation (red phase)
- OR validated via characterization (deliberate breakage)
- Assertions test business rules, not just HTTP status
- Tests are isolated (no inter-test dependencies)

---

### Security Standards

**Authentication**:

- All API endpoints require JWT authentication (except public endpoints)
- Tokens expire (1 hour access, 7 days refresh)
- HTTPS only (TLS 1.2 minimum)

**Authorization**:

- Policy-based authorization (resource ownership, role-based)
- Authorization logic is human-reviewed and tested
- No authorization bypasses or temporary admin access

**Data Protection**:

- Passwords hashed (bcrypt)
- Sensitive data encrypted at rest
- PII never logged
- Secrets in Azure Key Vault (never in code) _(v2.0+; v1.0 uses App Service Configuration — see plan.md CHK093)_

---

### Performance Standards

**Response times**:

- API p95 response time: <200ms
- Page load time (first contentful paint): <2 seconds

**Scalability**:

- Load tested for 100 concurrent users
- Database queries optimized (appropriate indexes)
- No N+1 query problems
- Async/await throughout (no blocking calls)

---

### Code Quality Standards

**Maintainability**:

- Follow Microsoft C# conventions
- Follow Angular style guide
- Code formatting automated (EditorConfig)
- Methods <50 lines (extract helpers)
- No code smells (validated via SonarCloud)

**Readability**:

- Meaningful names (no abbreviations, no `x`, `temp`, `data`)
- Comments explain WHY, not WHAT
- No commented-out code (Git remembers)
- No magic numbers (use named constants)

---

## 6. DEVELOPMENT PROCESS

### Definition of Done

A feature is complete when:

- [ ] Specification approved (SPECIFY → PLAN → TASKS)
- [ ] Code implements specification requirements
- [ ] Tests written and observed failing (red phase)
- [ ] Implementation makes tests pass (green phase)
- [ ] Code refactored if needed (refactor phase)
- [ ] Unit, integration, E2E tests passing
- [ ] Code reviewed (self-review with documented rationale)
- [ ] Deployed to staging
- [ ] Smoke tests passing in staging
- [ ] API documented (OpenAPI/Scalar)
- [ ] Security reviewed (if authorization changes)
- [ ] Performance validated (no regressions)

---

### Code Review Checklist

Self-review must validate:

- ✅ Follows this constitution (principles, constraints)
- ✅ Specification requirements met
- ✅ Tests have epistemic validity (observed failing)
- ✅ Architecture supports v2.0 evolution
- ✅ Security considerations addressed
- ✅ Performance acceptable (no obvious optimizations missed)
- ✅ Code is simple (no clever abstractions)
- ✅ Documentation complete
- ✅ **Commit messages follow Conventional Commits format** (Principle 8)

---

### Commit Message Format

**Reference**: See Principle 8 for complete specification.

**Quick Template**:

```
<type>(<scope>): <description in imperative mood>

[Why this change? Context if needed.]

[Ref: #issue-number]
[BREAKING CHANGE: migration details if applicable]
```

**Common Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`

**Validation Checklist**:

- [ ] Type is valid and correct
- [ ] Scope included for localized changes
- [ ] Subject uses imperative mood, lowercase, no period, <50 chars
- [ ] Breaking changes marked with `!` and footer
- [ ] Body explains "why" if subject isn't sufficient
- [ ] References issues/PRs if applicable

**AI Agent Requirement**: When generating commit messages, AI agents **MUST** follow Principle 8. Non-compliant messages should be rejected.

---

## 7. SUCCESS CRITERIA

### v1.0 Launch Criteria (Technical)

Ready to deploy when:

- [ ] All features complete (actor registration → virtual incubator)
- [ ] Test coverage meets standards (>80% unit, 100% integration, critical E2E)
- [ ] Mutation testing score >70%
- [ ] Performance targets met (API <200ms, page load <2s)
- [ ] Security audit clean (no critical/high vulnerabilities)
- [ ] Load testing passed (100 concurrent users)
- [ ] 99.9% uptime in staging for 30 days
- [ ] Zero critical incidents in staging
- [ ] Documentation complete (API, deployment, runbooks)
- [ ] Monitoring and alerting operational

### v1.0 Success Criteria (Learning)

Project succeeds if:

- [ ] Mastered modern .NET + Angular stack
- [ ] Can explain architectural decisions in interviews
- [ ] Portfolio demonstrates production-ready skills
- [ ] Comfortable with spec-first development
- [ ] Experienced with AI-assisted development (with discipline)
- [ ] Practiced cloud deployment and operations
- [ ] Built something genuinely useful

---

## 8. DECISION AUTHORITY

### Who Decides What

**Project Owner (You)** decides:

- All scope decisions (what's in/out of v1.0)
- All technical stack choices
- All architectural patterns
- Timeline adjustments (flexibility for learning)
- When to proceed to next phase

**This Constitution** decides:

- Non-negotiable principles (quality, testing, simplicity)
- Boundaries (v1.0 scope, no multi-tenancy)
- Process (Spec-Kit workflow, test-first)

**Users** decide:

- Whether platform is valuable
- Whether UX is intuitive
- Whether features meet their needs

**Technical Reality** decides:

- What's feasible in timeline
- What's achievable solo
- What's maintainable long-term

---

## 9. AMENDMENT PROCESS

### How to Change This Constitution

**When amendments are appropriate**:

- Discovered constraint makes principle impractical
- Better approach emerges during implementation
- Learning objectives shift
- Scope adjustment needed

**Amendment process**:

1. Document proposed change with rationale
2. Analyze impact (what breaks? what changes?)
3. Update related documentation (strategy, target definition, guardrails)
4. Increment constitution version (semantic versioning)
5. Create Architecture Decision Record (ADR)
6. Update Sync Impact Report (prepend new report to this file)

**Version increment rules**:

- **MAJOR** (x.0.0): Backward incompatible governance/principle removals or redefinitions
- **MINOR** (0.x.0): New principle/section added or materially expanded guidance
- **PATCH** (0.0.x): Clarifications, wording, typo fixes, non-semantic refinements

**Recent amendments**:

- **v1.1.0** (April 18, 2026): Added Principle 8 (Commit Messages Are Documentation)
  Establishes Conventional Commits 1.0.0 Specification as non-negotiable standard

---

## 10. RELATED DOCUMENTS

### Where to Find More Details

**Strategic Documents**:

- `doc/RE-ENGINEERING_STRATEGY.md` - Overall implementation approach
- `doc/PHASE_1_TARGET_DEFINITION.md` - Detailed technical decisions
- `doc/PROJECT_KNOWLEDGE_BASE.md` - Decision history and rationale

**Tactical Documents**:

- `doc/ARCHITECTURE_GUARDRAILS.md` - v1.0 → v2.0 evolution guidelines
- `doc/AI_ASSISTED_DEVELOPMENT_PRINCIPLES.md` - Epistemic testing requirements
- `doc/ARCHITECTURAL_INVENTORY.md` - Legacy system analysis

**Legacy Reference**:

- `legacy-mvc/` - Source of functional requirements and business logic

**Template System**:

- `.specify/templates/spec-template.md` - Feature specification format
- `.specify/templates/plan-template.md` - Implementation plan format (includes Constitution Check)
- `.specify/templates/tasks-template.md` - Task breakdown format

---

## SIGN-OFF

**As Project Owner, I establish this constitution as the foundation for Innoventity v1.0 development.**

These principles are non-negotiable. When in doubt, refer back to this document.

**Initial Ratification**: February 8, 2026
**Last Amendment**: April 18, 2026 (v1.1.0 - Principle 8 added)
**Authority**: Project Owner
**Next Review**: Post-Phase 1 retrospective

---

**END OF CONSTITUTION**
