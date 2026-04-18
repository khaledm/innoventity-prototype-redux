# Conventional Commits Quick Reference Guide

**Authority**: Constitution Principle 8
**Specification**: [Conventional Commits 1.0.0](https://conventionalcommits.org)
**Status**: REQUIRED for all commits (as of April 18, 2026)

---

## Format Template

```
<type>(<optional scope>): <description>

[optional body]

[optional footer(s)]
```

---

## Required Types

| Type | Use When | SemVer Impact | Example |
|------|----------|---------------|---------|
| `feat` | Adding new feature | MINOR | `feat(auth): add OAuth2 login flow` |
| `fix` | Fixing a bug | PATCH | `fix(validation): prevent null reference in email validator` |
| `docs` | Documentation only | - | `docs(readme): update deployment instructions` |
| `style` | Formatting, whitespace, no logic change | - | `style(api): fix indentation in controller` |
| `refactor` | Code restructure, no behavior change | - | `refactor(services): extract helper methods` |
| `perf` | Performance improvement | PATCH | `perf(queries): add index to bid lookups` |
| `test` | Adding or fixing tests | - | `test(registration): add unit tests for validation` |
| `chore` | Build, tooling, dependencies | - | `chore: update Angular to 19.2.0` |

---

## Scope Guidelines

**Common Scopes** (project-specific):
- `auth` - Authentication/authorization
- `api` - Backend API changes
- `ui` - Frontend user interface
- `infrastructure` - Terraform, Azure resources
- `ci` - GitHub Actions workflows
- `registration` - Actor registration feature
- `bids` - Bidding system
- `incubator` - Virtual incubator feature
- `tests` - Test infrastructure

**Rules**:
- ✅ Use scope for localized changes: `feat(auth): add JWT refresh`
- ✅ Omit scope for cross-cutting: `chore: update all dependencies`
- ❌ Don't use vague scopes: `feat(stuff):`, `fix(things):`

---

## Subject Line Rules

1. **Imperative mood**: `add`, `fix`, `update` (NOT `added`, `fixed`, `updated`)
2. **Lowercase first letter**: `add validation` (NOT `Add validation`)
3. **No trailing period**: `fix bug` (NOT `fix bug.`)
4. **Max 50 characters**: Aim for clarity and brevity
5. **Complete the sentence**: "If applied, this commit will _[your subject]_"

### Good Examples ✅
- `add user authentication middleware`
- `fix race condition in token refresh`
- `remove deprecated API endpoints`
- `update dependency versions`

### Bad Examples ❌
- `Added user authentication middleware` (past tense)
- `Fix Race Condition In Token Refresh` (capitalized)
- `remove deprecated API endpoints.` (trailing period)
- `stuff` (not descriptive)

---

## Breaking Changes

**When to mark as breaking**:
- API endpoint changes (URL, method, response shape)
- Database schema changes requiring migration
- Configuration format changes
- Removed or renamed public interfaces
- Behavior changes that could break existing code

**How to mark**:
1. Append `!` after type/scope: `feat(api)!:`
2. Add `BREAKING CHANGE:` footer with migration details

**Example**:
```
feat(api)!: migrate authentication to JWT-only

Replace session-based auth with JWT tokens for better scalability
and stateless API design.

BREAKING CHANGE: Session cookies no longer supported. Clients must
update to use JWT tokens from /api/auth/login endpoint. Include token
in Authorization header as "Bearer <token>".

Migration Guide: Update API client to store and send JWT tokens.
See docs/migration/session-to-jwt.md for detailed steps.

Ref: #5001
```

---

## Body Guidelines

**Include body when**:
- Subject line alone doesn't explain "why"
- Change involves important context
- Multiple related changes in one commit
- Documenting thought process for future readers

**Body format**:
- Leave one blank line after subject
- Wrap lines at 72 characters
- Use bullet points for multiple items
- Focus on "why" and "context", not "what" (code shows "what")

**Example**:
```
refactor(services): extract bid validation logic

Previous implementation had validation logic scattered across
controller, service, and model layers. This consolidation:
- Improves testability (single source of truth)
- Reduces duplication (DRY principle)
- Enables reuse in batch processing scenarios

No functional changes; behavior remains identical.
```

---

## Footer Guidelines

**Common footers**:
- `Ref: #123` - References issue/PR (informational)
- `Closes: #456` - Closes issue (GitHub auto-closes)
- `Fixes: #789` - Fixes issue (same as Closes)
- `BREAKING CHANGE: ...` - Documents breaking changes
- `Reviewed-by: Name` - Code review attribution
- `Co-authored-by: Name <email>` - Pair programming credit

**Multiple footers allowed**:
```
fix(auth): resolve token expiration edge case

Closes: #3421
Ref: #3400
Co-authored-by: Jane Doe <jane@example.com>
```

---

## Complete Examples

### Simple Feature
```
feat(registration): add email verification step

Implements two-factor verification for new user registrations.
Sends verification code via email using SendGrid API.

Ref: #2100
```

### Bug Fix with Context
```
fix(bids): prevent duplicate bid submissions

Race condition allowed users to submit multiple bids within the same
100ms window. Added optimistic locking (row version) to bid entity
and retry logic in service layer.

Closes: #3891
```

### Documentation Update
```
docs(infrastructure): document Terraform module structure

Add README files to each infrastructure module explaining:
- Module purpose and responsibilities
- Required/optional variables
- Output values
- Usage examples
```

### Refactoring
```
refactor(api): migrate to minimal API endpoints

Replace controller-based endpoints with .NET 8 minimal APIs for:
- Reduced boilerplate (~40% less code)
- Better performance (lower memory allocation)
- Simpler dependency injection

No breaking changes to API contract. All tests passing.
```

### Breaking Change
```
feat(api)!: change bid submission endpoint contract

Modify POST /api/bids request body to include partnerType field
for better validation and clearer intent.

BREAKING CHANGE: POST /api/bids now requires `partnerType` field
in request body. Valid values: "RD", "Manufacturing", "Sales", "Investment".

Before: { "innovationId": 123, "amount": 50000 }
After: { "innovationId": 123, "partnerType": "RD", "amount": 50000 }

Migration: Update all API clients to include partnerType field.

Ref: #4200
```

### Chore (Dependencies)
```
chore(deps): update Angular to 19.2.0

Update Angular core packages and CLI to latest stable version.
No breaking changes in upgrade path. All tests passing.
```

---

## Anti-Patterns to Avoid

❌ **Vague messages**:
```
fix: bug fix
chore: updates
feat: new stuff
```

❌ **Past tense**:
```
fixed: login bug
added: new feature
```

❌ **Capitalized subject**:
```
Feat(Auth): Add Login
Fix(UI): Broken Button
```

❌ **Missing scope on localized change**:
```
feat: add validation
fix: broken query
```
Should be:
```
feat(registration): add email validation
fix(bids): correct SQL query pagination
```

❌ **Breaking change without marker**:
```
feat(api): change endpoint URL
```
Should be:
```
feat(api)!: change endpoint URL

BREAKING CHANGE: Endpoint moved from /api/v1/bids to /api/v2/bids
```

---

## AI Agent Compliance

When AI agents (GitHub Copilot, ChatGPT, etc.) generate commit messages:

1. **Validate format** before accepting
2. **Check type is appropriate** (feat vs fix vs refactor)
3. **Ensure scope is included** for localized changes
4. **Verify imperative mood** and lowercase
5. **Add body if needed** to explain "why"
6. **Mark breaking changes** explicitly

**Non-compliant AI-generated messages should be rejected and regenerated.**

---

## Validation Checklist

Before committing, verify:

- [ ] Type is one of: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `chore`
- [ ] Scope included if change is localized to a module/feature
- [ ] Subject line uses imperative mood (add, fix, update)
- [ ] Subject line is lowercase (except proper nouns)
- [ ] Subject line has no trailing period
- [ ] Subject line is ≤50 characters
- [ ] Breaking changes marked with `!` and `BREAKING CHANGE:` footer
- [ ] Body explains "why" if subject isn't sufficient
- [ ] Body lines wrapped at 72 characters
- [ ] Footers include issue references where applicable

---

## Integration with Git Hooks

This project uses `.specify/extensions.yml` to configure `speckit.git.commit` hooks that auto-commit after each Spec-Kit command.

**All auto-generated commits MUST follow Conventional Commits format.**

Hooks trigger:
- After `/speckit.constitution` updates
- After `/speckit.specify` creates specs
- After `/speckit.plan` generates plans
- After `/speckit.tasks` creates task lists
- And other Spec-Kit lifecycle events

---

## References

- **Constitution**: `.specify/memory/constitution.md` (Principle 8)
- **Specification**: https://conventionalcommits.org
- **SemVer**: https://semver.org
- **Git Hooks**: `.specify/extensions.yml`

---

**Last Updated**: April 18, 2026
**Authority**: Project Constitution v1.1.0
