# Contract: Innovations API (nested section shape)

Endpoint URLs and HTTP methods are unchanged from pre-006 (FR-007: "endpoint addresses and authorization rules are unchanged"). Only request/response **body shapes** change.

## POST /innovations (create)

**Request**: unchanged flat field names at the top level (client sends `title`, `productType`, `productDescription`, etc.) — `CreateInnovation.cs` accepts the existing flat request DTO and constructs the four owned entities server-side. No breaking change to the create request shape.

## GET /innovations/{id} (retrieve)

**Response** (breaking change — was flat, now nested):

```json
{
  "id": "guid",
  "ideaToken": "guid",
  "ownerId": "guid",
  "owner": { "id": "...", "firstName": "...", "lastName": "...", "displayName": "...", "email": "...", "actorType": "..." },
  "ideaSummary": {
    "title": "string",
    "productType": "string",
    "researchBackground": "string",
    "researchCategory": "Management|Engineering|NaturalScience",
    "iprStatus": "string"
  },
  "product": {
    "productDescription": "string",
    "technologyDescription": "string",
    "productAdvantages": "string",
    "developmentPhase": "string",
    "developmentProcess": "string",
    "targetBeneficiaries": "string",
    "productKeywords": "string",
    "advantageKeywords": "string"
  },
  "market": {
    "targetMarket": "string",
    "targetCustomerBase": "string",
    "targetCustomerType": "string",
    "relevantMarketSize": "decimal|null",
    "potentialMarketSize": "decimal|null"
  },
  "collaborationRequirement": {
    "partnersNeeded": "string|null"
  },
  "status": "Draft|Published",
  "createdAt": "ISO 8601",
  "submittedAt": "ISO 8601|null",
  "targetIndustries": [{ "industryId": "string", "name": "string" }]
}
```

## GET /innovations (list, paginated)

**Response** — each item nests only `ideaSummary` (summary view; other sections not included in list items):

```json
{
  "items": [{
    "innovationId": "guid",
    "ideaSummary": { "title": "string", "productType": "string", "researchCategory": "string" },
    "status": "Published",
    "submittedAt": "ISO 8601|null",
    "owner": { "actorId": "guid", "firstName": "string", "lastName": "string", "displayName": "string" },
    "targetIndustries": ["string"],
    "partnersNeeded": ["string"]
  }],
  "totalCount": 0,
  "page": 1,
  "pageSize": 20
}
```

Query params unchanged: `industryId`, `researchCategory`, `page`, `pageSize`.

## PUT /innovations/{id} (update)

**Request**: unchanged flat field names (partial update; omitted/null fields keep their current value).

**Response** (breaking change):

```json
{
  "innovationId": "guid",
  "ideaSummary": { "title": "string", "productType": "string", "researchCategory": "string" },
  "status": "Draft|Published",
  "modifiedAt": "ISO 8601",
  "ownerId": "guid"
}
```

## PATCH /innovations/{id}/submit (publish gate)

**Request**: no body.

**Response on success (200)**: unchanged — `{ "innovationId": "guid", "status": "Published", "submittedAt": "ISO 8601" }`.

**Response on validation failure (400)**: unchanged `ValidationProblem` shape — `errors` dictionary keyed by bare field name (`"Title"`, `"ResearchBackground"`, `"ProductDescription"`, `"TechnologyDescription"`, `"TargetBeneficiaries"`, `"RelevantMarketSize"`, `"PotentialMarketSize"`, `"TargetIndustries"`, `"PartnersNeeded"`), each with a string array of messages. **Note**: keys are not section-qualified (e.g. not `"ideaSummary.title"`) — FR-008 requires the section be *identifiable*, which the constitution-mapping table above satisfies implicitly (each key maps 1:1 to exactly one section), but a client cannot derive the section from the key string alone without that mapping table. Tracked as an open item, not re-litigated in this plan (see `tasks.md` for optional follow-up).

**Decision gate**: the 200-vs-400 branch is `!innovation.IsReadyForSubmission()`, not `validationErrors.Any()` — the two are required to always agree (enforced by `SubmitInnovation_PlaceholderTitleOnly_Returns400AndMatchesIsReadyForSubmission`).

Other behaviors unchanged: 401 (no/invalid auth), 403 (non-owner), 404 (not found), 409 (already published).
