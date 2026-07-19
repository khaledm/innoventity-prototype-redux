# Manual Testing Overview: 005-FormalResponse

**Quick Reference** — Use this to navigate the full runbook

## Document Locations

- **Full Runbook**: `MANUAL_TEST_RUNBOOK.md` (step-by-step test cases with payloads)
- **API Contracts**: `contracts/api-contracts.md` (endpoint specs, request/response schemas)
- **Feature Spec**: `spec.md` (functional requirements, success criteria)

---

## What to Test (9 Phases)

| Phase | Scenario | Tests | Duration |
|-------|----------|-------|----------|
| **1** | Manufacturing actor submits 3-year cost projection | Happy path, 201 response | 3 min |
| **2** | Sales/Marketing actor submits 3-year revenue projections | Happy path, 201 response | 3 min |
| **3** | R&D actor submits development costs (2 years + duration) | Happy path, 201 response | 3 min |
| **4** | Investor actor submits lightweight feedback | Happy path, 201 response | 2 min |
| **5** | Innovation owner views all responses with full data | GET endpoint, 4 response types | 5 min |
| **6** | 3-tier visibility: owner/submitter/third-party | Verify data redaction rules | 8 min |
| **7** | Validation & error cases (rationale, years, duplicates) | 7 error scenarios, 422/403/409/404/400 | 15 min |
| **8** | SelectPartners compatibility | Verify non-blocking breaking change | 5 min |
| **9** | End-to-end fresh workflow | Complete submission → selection cycle | 15 min |

**Total**: ~60 minutes for complete coverage

---

## Test Data Setup (Before Starting)

### Actors Needed (5 or more)

| Role | ActorType | For | Notes |
|------|-----------|-----|-------|
| Idea Owner | IdeaGenerator | Setup + verification | Must own the test innovation |
| Manufacturer | Manufacturing | US1 tests | Creates ManufacturingResponse |
| Sales Org | SalesMarketing | US2 tests | Creates SalesMarketingResponse |
| R&D Lab | RD | US3 tests | Creates ResearchDevelopmentResponse |
| Investor | Investor | US4 tests | Creates InvestorResponse |
| Third-Party | Any type | Visibility tests | For permission validation |

### Innovation Needed

- **Status**: Published (required for all submissions)
- **Owner**: The IdeaGenerator actor from above
- **Count**: Use 1 primary innovation for most tests; create additional ones for error case validation

---

## Key Validations to Check

### ✅ Data Integrity
- [ ] 3-year manufacturing projections with rationale stored correctly
- [ ] 3-year sales projections with rationale stored correctly
- [ ] 2-year R&D projections + duration stored correctly
- [ ] Investor feedback stored correctly
- [ ] All rationale fields persisted (min 20, max 500 chars)
- [ ] ResponseType discriminator saved in database

### ✅ Business Rules
- [ ] Actor can submit only their matching type (Manufacturing → ManufacturingResponse)
- [ ] One response per actor per innovation (409 Conflict on duplicate)
- [ ] Owner cannot submit response for own innovation (403)
- [ ] Only Published innovations accept responses (404 otherwise)
- [ ] Years must be 1–10, contiguous, no gaps

### ✅ Visibility Rules (Critical)
- **Owner**: Sees `participationProposal` + financial projections + rationale + `feedback`
- **Submitting actor**: Sees own full response; other responses show only public fields
- **Third-party**: All responses show only public fields (`responseType`, `actorId`, `location`, `participationType`, `status`, `submittedAt`)

### ✅ SelectPartners Integration
- [ ] Accepts `selectedResponseIds` (renamed from `selectedBidIds`)
- [ ] Marks 3 selected responses as `Accepted` with `acceptedAt` timestamp
- [ ] Sets innovation status to `PartnersSelected`
- [ ] 403 Forbidden when called twice (immutability)
- [ ] Investor responses NOT part of selection (remain Pending)

---

## Using Scalar for Testing

### 1. Open Scalar
- Navigate to `https://<your-app>.azurewebsites.net/scalar`
- You'll see all endpoints organized by resource

### 2. Set Bearer Token
- Top-right corner has auth/token input
- Paste JWT after each login/switch
- All subsequent requests use this token automatically

### 3. Test an Endpoint
- Click endpoint in left sidebar
- Click **"Try it out"**
- Fill in path parameters (e.g., `{innovationId}`)
- Paste JSON in request body
- Add query parameters if needed
- Click **"Send Request"**
- Review response status + body

### 4. Copy Values Between Tests
- After each submission test, copy the `responseId` from response
- Use these IDs in subsequent retrieval/selection tests
- Keep a notepad open to track: `$MFG_RESPONSE_ID`, `$SALES_RESPONSE_ID`, etc.

### 5. Debug Failed Tests
- Scalar shows full request + response
- Check status code first (422 vs 409 vs 403)
- Error response includes `errors` object with field names
- Compare error message against expected in runbook

---

## Common Test Patterns

### Pattern 1: Submission Testing
```
1. Auth: Switch to actor token (e.g., $MFG_TOKEN)
2. Endpoint: POST /innovations/{innovationId}/bids/[type]
3. Payload: Full projection data with all rationale fields
4. Expected: 201 Created + responseId
5. Capture: Copy responseId for later tests
```

### Pattern 2: Retrieval Testing
```
1. Auth: Switch to caller token (owner / submitter / third-party)
2. Endpoint: GET /innovations/{innovationId}/bids
3. Query: Add ?type=manufacturing (optional)
4. Expected: 200 OK + filtered responses
5. Verify: Check visibility rules (what data is included/omitted)
```

### Pattern 3: Error Testing
```
1. Auth: Appropriate actor token
2. Endpoint: POST /innovations/{innovationId}/bids/[type]
3. Payload: Invalid data (short rationale, wrong year, etc.)
4. Expected: 422/403/409/404/400 with specific error message
5. Verify: Error identifies the field or constraint violated
```

---

## Expected Response Times (Performance Check)

| Endpoint | Expected Time | Note |
|----------|---------------|------|
| POST /bids/manufacturing | <200ms | Validation + insert |
| POST /bids/sales | <200ms | Validation + insert |
| POST /bids/rd | <200ms | Validation + insert |
| POST /bids/investor | <150ms | No projection data |
| GET /bids | <200ms | For ≤50 responses |
| GET /bids?type=manufacturing | <200ms | Filtered query |
| POST /select-partners | <200ms | Update 3 responses |

---

## Success Criteria Summary

**Feature is working correctly when**:

1. ✅ All 4 response types can be submitted with their specific projection data
2. ✅ Rationale fields are enforced (20–500 chars) with field-specific error messages
3. ✅ Year contiguity validation works (rejects gaps, identifies missing year)
4. ✅ 3-tier visibility is correctly enforced (owner/submitter/other)
5. ✅ Type filtering works (`?type=manufacturing`, `sales`, `rd`, `investor`)
6. ✅ SelectPartners continues working with renamed `selectedResponseIds` field
7. ✅ Partner selection is immutable (403 on re-selection)
8. ✅ All existing tests still pass (zero regressions)

---

## Troubleshooting Quick Reference

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| 401 responses | Expired or invalid JWT | Re-authenticate, copy fresh token |
| 404 on valid innovation | Innovation not Published | Verify innovation status in database |
| 409 Conflict | Already submitted for this innovation | Use different actor or innovation |
| 422 validation errors | Check error message details | Verify rationale ≥20 chars, years contiguous 1–10 |
| Visibility showing full data to third-party | Bug in visibility filter | Verify GET /bids code; check actor tokens match expectations |
| SelectPartners failing | Wrong field name in request | Use `selectedResponseIds` not `selectedBidIds` |

---

## Additional Resources

- **Architecture Decision**: See `research.md` for TPH vs TPT, JSON column strategy, migration approach
- **Data Model**: See `data-model.md` for entity/value-object structure and database schema
- **Contracts**: See `contracts/api-contracts.md` for exact request/response schemas
- **Quality Checklist**: See `checklists/requirements.md` for spec validation checklist

---

## Sign-Off Template

Use this when you've completed testing:

```
Testing Date: _______________
Tester: _______________
Environment: Azure Dev
Build Version: _______________

Phase 1 (Manufacturing): ☐ PASS ☐ FAIL
Phase 2 (Sales): ☐ PASS ☐ FAIL
Phase 3 (R&D): ☐ PASS ☐ FAIL
Phase 4 (Investor): ☐ PASS ☐ FAIL
Phase 5 (View/Filter): ☐ PASS ☐ FAIL
Phase 6 (Visibility): ☐ PASS ☐ FAIL
Phase 7 (Validation): ☐ PASS ☐ FAIL
Phase 8 (SelectPartners): ☐ PASS ☐ FAIL
Phase 9 (E2E): ☐ PASS ☐ FAIL

Overall Result: ☐ READY FOR PRODUCTION ☐ ISSUES FOUND

Issues (if any):
1. ...
2. ...

Sign-off: _____________________________
```

---

## Next Steps

1. **Deploy** the latest code with the `005-formalresponse-hierarchy` feature to Azure Dev
2. **Run migrations** (`dotnet ef database update`)
3. **Seed test data** (actors + published innovation)
4. **Open Scalar** and authenticate with your test tokens
5. **Follow the runbook** in order (Phase 1 → 9)
6. **Document results** in the sign-off section
7. **Report any deviations** from expected behavior
