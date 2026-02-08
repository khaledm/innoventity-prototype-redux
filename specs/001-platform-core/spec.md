# Innoventity Platform - Functional Specification

**Feature**: Platform Core (v1.0)  
**Specification Type**: Functional Requirements  
**Phase**: SPECIFY  
**Status**: Approved (95.5/100 Constitutional Compliance)  
**Approval Date**: February 8, 2026

---

This specification defines **WHAT** the Innoventity platform does from a functional perspective — the user needs, workflows, business rules, and success criteria. It does **NOT** specify technical implementation details.

**Key Principles**:
- Functional requirements only (WHO, WHAT, WHY, HOW users interact)
- Business rules and invariants documented
- User journeys clearly articulated
- Success metrics defined
- Technical implementation deferred to PLAN phase

---

## 1. Project Context

### Problem Statement

Research-based innovation requires collaboration across multiple specialized domains — technical development (R&D), manufacturing/production, market strategy (sales/marketing), and financial backing (investment). However, **innovation originators (idea generators)** face significant barriers:

1. **Discovery Friction**: No structured way to find qualified partners across these domains
2. **Trust Gaps**: Lack of formal processes for partner evaluation and commitment
3. **Coordination Overhead**: No centralized workspace for collaboration once partners are selected
4. **IPR Protection**: Concerns about idea theft when sharing innovation details publicly
5. **Market Access**: Difficulty connecting research-based innovations with commercialization expertise

**Current State**: Innovators rely on personal networks, incubators (limited capacity), or generic collaboration platforms not designed for research commercialization workflows.

### Vision

**Innoventity** is an open innovation platform that enables research-based innovation originators to:
- Publish innovation opportunities to a curated ecosystem of specialized actors
- Receive formal partnership proposals (bids) from qualified organizations
- Select collaboration partners through a structured evaluation process
- Collaborate in a virtual incubator workspace with selected partners
- Develop joint business plans for commercialization

**Platform Value**:
- **For Idea Generators**: Access to specialized expertise + commercialization pathways
- **For R&D Organizations**: Access to novel research-based opportunities aligned with their technical capabilities
- **For Manufacturing Companies**: Early visibility into innovations requiring production/distribution capabilities
- **For Sales/Marketing Companies**: Opportunities to commercialize innovations by providing market strategy
- **For Investors**: Structured deal flow with multi-party validation (technical, market, production alignment)

### Opportunity

The platform creates a **multi-sided marketplace** for research-based innovation commercialization:
- 5 distinct actor types with complementary needs
- Formal bidding process reduces coordination costs
- Virtual incubator functions accelerate commercialization
- Industry affiliation enables discovery and matching
- Research category classification (Management/Engineering/Natural Science) provides metadata for filtering

**Success Looks Like**: An innovation originator can submit a research-based idea, receive qualified partnership proposals within weeks, select optimal partners, and enter a structured collaboration process — all within a single platform.

---

## 2. User Personas

### Persona 1: Idea Generator (Research-Based Innovation Originator)

**WHO**: Individuals or corporate entities with research-based innovations seeking commercialization partners.

**Core Needs**:
- Submit innovation opportunities with IPR protection
- Describe research background, product vision, and market potential
- Receive formal partnership proposals from qualified actors
- Evaluate and select partners across R&D, manufacturing, sales/marketing domains
- Collaborate with selected partners in structured workspace
- Maintain ownership and control throughout process

**Pain Points**:
- Lack of access to specialized commercialization expertise
- Concerns about idea theft when sharing innovation details
- Difficulty evaluating partner qualifications and commitment level
- No structured process for multi-party collaboration
- Uncertainty about commercialization pathways

**Success Criteria**:
- Innovation published and visible to target audience
- Received sufficient partnership proposals (≥1 from each required actor type)
- Selected optimal partners based on proposals
- Virtual incubator formed with selected team
- Business plan development underway

**Typical Information Provided**:
- Innovation title, description, research background
- IPR status (patent pending, copyright, trade secret)
- Product type and current development phase
- Target market, customer base, industry alignment
- Collaboration requirements (which actor types needed)

---

### Persona 2: R&D Organization (Research & Development Partner)

**WHO**: Research institutions, labs, universities, or R&D-focused companies providing technical development expertise.

**Entity Type**: `DomainExpert` in legacy system (renamed to `RDOrganization` in v1.0)

**Core Needs**:
- Discover research-based innovations aligned with technical expertise
- Filter opportunities by industry affiliation and research category
- Submit formal proposals outlining technical development approach
- Participate in virtual incubator if selected
- Contribute to business plan (technical roadmap, development costs)

**Pain Points**:
- Limited visibility into innovations requiring R&D expertise
- Difficulty demonstrating technical capabilities to innovators
- No structured process for partnership formation
- Uncertainty about commercialization commitment from other parties

**Success Criteria**:
- Found relevant innovation opportunities matching expertise
- Submitted competitive technical development proposal
- Selected as R&D partner
- Contributing to business plan and technical roadmap

**Typical Bid Content**:
- Technical development approach and methodology
- Relevant expertise/credentials/past projects
- Estimated development timeline and milestones
- Resource requirements and cost structure
- Location/geographic presence

---

### Persona 3: Manufacturing Company (Production & Distribution Partner)

**WHO**: Manufacturing organizations providing production capabilities, supply chain management, and distribution infrastructure.

**Core Needs**:
- Discover innovations requiring manufacturing/production expertise
- Filter by industry alignment and production capabilities
- Submit formal proposals outlining production approach
- Participate in virtual incubator if selected
- Contribute to business plan (cost structure, volume projections, distribution strategy)

**Pain Points**:
- Limited visibility into innovations ready for production
- Difficulty connecting with innovators needing manufacturing expertise
- No structured process for evaluating innovation feasibility for production
- Uncertainty about market demand and partner commitment

**Success Criteria**:
- Found innovations matching production capabilities
- Submitted competitive manufacturing/distribution proposal
- Selected as manufacturing partner
- Contributing to business plan (cost model, volume projections)

**Typical Bid Content**:
- Production capabilities and capacity
- Cost per unit estimates and volume breakpoints
- Quality standards and certifications
- Distribution infrastructure and reach
- Location/geographic presence

---

### Persona 4: Sales & Marketing Company (Commercialization Partner)

**WHO**: Organizations providing market strategy, customer acquisition, branding, and commercialization expertise.

**Core Needs**:
- Discover innovations with strong market potential
- Filter by target industry and market segments
- Submit formal proposals outlining commercialization strategy
- Participate in virtual incubator if selected
- Lead business plan market strategy sections

**Pain Points**:
- Limited access to innovations ready for market
- Difficulty demonstrating commercialization expertise to innovators
- No structured process for partnership formation
- Uncertainty about product readiness and partner commitment

**Success Criteria**:
- Found innovations with market opportunity matching expertise
- Submitted competitive commercialization strategy proposal
- Selected as sales/marketing partner
- Leading business plan market strategy and customer acquisition sections

**Typical Bid Content**:
- Market opportunity analysis
- Customer acquisition strategy
- Competitive positioning approach
- Revenue model recommendations
- Distribution channel expertise
- Location/geographic market presence

---

### Persona 5: Investor (Financial Backer)

**WHO**: Venture capital, angel investors, corporate venture arms, or financial institutions providing funding.

**Core Needs**:
- Discover innovations with strong commercialization potential
- Filter by industry, research category, and team composition
- Submit formal investment proposals outlining terms
- Participate in virtual incubator if selected (optional involvement)
- Contribute to business plan financial modeling

**Pain Points**:
- Fragmented deal flow with varying due diligence quality
- Difficulty assessing team completeness (technical, production, market capabilities)
- No structured visibility into partnership commitments
- Uncertainty about commercialization execution

**Success Criteria**:
- Found innovations with validated multi-party interest (R&D, manufacturing, sales committed)
- Submitted competitive investment proposal
- Selected as investor (if applicable)
- Contributing to business plan financial sections

**Typical Bid Content**:
- Investment amount and terms
- Valuation approach or proposed structure
- Due diligence requirements
- Post-investment support offered
- Geographic focus/restrictions

---

## 3. Core User Journeys

### Journey 1: Innovation Submission & Publication (Idea Generator)

**Goal**: Submit research-based innovation and make it discoverable to ecosystem actors.

**Steps**:

1. **Account Creation & Activation**
   - User registers as Idea Generator
   - Provides name, email, contact address
   - Receives activation email
   - Confirms account via activation token
   - Status changes from `PendingActivation` to `Active`

2. **Innovation Draft Creation**
   - User initiates new innovation submission
   - System generates unique `IdeaToken` for tracking
   - Innovation status: `Draft`

3. **Idea Summary Completion**
   - Provide innovation title (required, non-default)
   - Describe product type
   - Document research background (required)
   - Declare IPR status:
     - Has IPR (patent, copyright, trade secret)
     - No IPR (with explanation)
   - Confirm right to use this idea (required checkbox)
   - Select research category: Management / Engineering / Natural Science

4. **Product Details Completion**
   - Describe proposed product
   - List key product advantages
   - Specify current development phase
   - Explain idea development process
   - Provide product keywords (for discoverability)
   - Provide advantage keywords

5. **Market Details Completion**
   - Describe target market
   - Define target customer base
   - Specify target customer type
   - Select target industries (≥1 required from master list)

6. **Collaboration Requirements**
   - Specify which actor types are needed:
     - R&D Organization (technical development)
     - Manufacturing Company (production)
     - Sales & Marketing Company (commercialization)
     - Investor (funding)

7. **Validation & Submission**
   - System validates:
     - Idea Summary complete
     - Product Details complete
     - Market Details complete
     - ≥1 target industry selected
     - IPR declaration complete
     - Right-to-use confirmed
   - User submits for publication
   - Submission timestamp recorded
   - Innovation becomes visible to ecosystem actors
   - Status changes to indicate accepting partnership proposals

**Success Outcome**: Innovation published and discoverable by actors matching industry affiliation filters.

**Error Scenarios & Recovery**:

1. **Activation Email Not Received** (Step 1)
   - **What happens**: User cannot complete account activation
   - **System response**: Provides "Resend activation email" option on login page
   - **Recovery path**: User requests new activation email, receives fresh link, completes activation

2. **Incomplete Section at Submission** (Step 7)
   - **What happens**: User attempts to submit with missing required fields
   - **System response**: Displays clear error message identifying incomplete section(s) and specific missing fields
   - **Recovery path**: User navigates to flagged section(s), completes missing information, resubmits
   - **Example**: "Cannot submit innovation: Idea Summary is incomplete. Missing: Research background, Right-to-use confirmation"

3. **No Target Industry Selected** (Step 5)
   - **What happens**: User completes Market Details but forgets to select industries
   - **System response**: Prevents submission, highlights industry selection field
   - **Recovery path**: User selects at least one target industry from master list, proceeds to submission

4. **Session Timeout During Draft Creation** (Any step)
   - **What happens**: User's session expires while filling out sections
   - **System response**: Auto-saves draft progress periodically, prompts re-authentication
   - **Recovery path**: User logs back in, draft with all entered data is preserved, user continues from where they left off

---

### Journey 2: Innovation Discovery & Bid Submission (All Non-Idea-Generator Actors)

**Goal**: Discover relevant innovations and submit formal partnership proposals.

**Steps**:

1. **Account Creation & Activation** (Same as Journey 1 for respective actor type)

2. **Industry Affiliation Setup**
   - Actor specifies industry expertise/focus areas
   - System uses this for innovation-to-actor matching

3. **Innovation Discovery**
   - Actor browses innovations filtered by:
     - Industry alignment (actor affiliation matches innovation target industry)
     - Research category (optional filter)
     - Innovation status (only published innovations accepting partnership proposals)
   - Actor views innovation details:
     - Idea Summary (research background, IPR status)
     - Product vision and advantages
     - Market opportunity and target customers
     - Collaboration requirements

4. **Bid Decision**
   - Actor evaluates opportunity fit with capabilities
   - Decides to submit formal partnership proposal

5. **Formal Bid Submission**
   - Actor provides:
     - Location/geographic presence (required)
     - Participation type (e.g., equity partner, service provider, investor terms)
     - Participation proposal (≥200 characters explaining approach/value)
   - System validates:
     - User is authenticated
     - User is correct actor type for this bid category (e.g., Manufacturer submitting manufacturing proposal)
     - User has not already submitted bid for this innovation
     - Innovation is accepting bids
   - Bid recorded with submission timestamp
   - Bid status initially marked as pending (awaiting selection)
   - System notifications:
     - Innovation owner notified of new bid
     - Real-time bid count update (if applicable)
   - **Error conditions**:
     - "You must be logged in to submit a bid"
     - "You have already submitted a bid for this innovation"
     - "This innovation is no longer accepting bids"
     - "Your actor type is not eligible for this bid category"

6. **Bid Management** (Optional)
   - Actor can view their submitted bids
   - Actor can edit/update bid proposal (if selection has not occurred)
   - Actor can withdraw bid (if selection has not occurred)

**Success Outcome**: Formal partnership proposal submitted and visible to innovation owner for evaluation.

**Error Scenarios & Recovery**:

1. **Duplicate Bid Attempt** (Step 5)
   - **What happens**: Actor attempts to submit a second bid for the same innovation
   - **System response**: "You have already submitted a bid for this innovation. You can view or edit your existing bid instead."
   - **Recovery path**: System redirects to existing bid, actor can edit and resubmit if needed

2. **Insufficient Proposal Length** (Step 5)
   - **What happens**: Actor enters less than 200 characters in participation proposal
   - **System response**: Character counter displays remaining characters needed, submit button disabled
   - **Recovery path**: Actor expands proposal text until minimum length met, submit button becomes enabled

3. **Innovation No Longer Accepting Bids** (Step 5)
   - **What happens**: Actor completes bid form but partners already selected by time of submission
   - **System response**: "This innovation is no longer accepting bids. Partners have been selected."
   - **Recovery path**: Actor is notified, bid is not saved, actor can browse other opportunities

4. **Unauthenticated Bid Attempt** (Step 5)
   - **What happens**: User session expires before bid submission
   - **System response**: "Your session has expired. Please log in again to submit your bid."
   - **Recovery path**: System preserves bid draft, user re-authenticates, bid form pre-populated with saved data, user resubmits

---

### Journey 3: Partner Evaluation & Selection (Idea Generator)

**Goal**: Evaluate received bids and select optimal collaboration partners.

**Steps**:

1. **Bid Monitoring**
   - Innovation owner receives notifications as bids arrive
   - Owner views bid dashboard showing:
     - Count of bids per actor type (R&D, Manufacturing, Sales/Marketing, Investor)
     - Bid details: who submitted, when, proposal summary

2. **Sufficient Bids Threshold**
   - System checks if innovation has received:
     - ≥1 Manufacturing bid
     - ≥1 Sales & Marketing bid
     - ≥1 R&D bid
   - When threshold met, innovation status indicates sufficient bids received
   - Owner notified that partner selection can begin

3. **Bid Evaluation**
   - Owner reviews each bid:
     - Actor profile and industry affiliation
     - Participation proposal details
     - Location/geographic alignment
     - Participation type/terms
   - Owner may communicate with bidders for clarification (messaging feature, implicit in legacy)

4. **Partner Selection**
   - Owner selects **exactly one partner** from each required actor type:
     - One R&D Organization
     - One Manufacturing Company
     - One Sales & Marketing Company
     - (Optionally) One Investor
   - System validates:
     - Owner is authenticated and owns innovation
     - Innovation has received sufficient bids
     - Exactly one bid selected from each required type
     - Selected bids have not already been committed to another selection
   - **This operation is IRREVERSIBLE**
   - System applies changes:
     - Selected bids marked as accepted with acceptance timestamp
     - Rejected bids remain unaccepted
     - Innovation status indicates partners have been selected
   - System sends notifications:
     - Selected partners notified of acceptance
     - Rejected bidders notified (optional/implicit)
   - **Error conditions**:
     - "You must own this innovation to select partners"
     - "Innovation has not received sufficient bids yet"
     - "You must select exactly one partner from each required category"
     - "Partner selection has already been completed for this innovation"

5. **Virtual Incubator Formation**
   - Innovation owner gains access to collaboration workspace
   - Selected partners gain access to collaboration workspace
   - Innovation details now visible to selected partners (full access)
   - Rejected bidders retain limited view only

**Success Outcome**: Virtual incubator team formed with committed partners (R&D, Manufacturing, Sales/Marketing, optionally Investor).

**Error Scenarios & Recovery**:

1. **Insufficient Bids Received** (Step 2)
   - **What happens**: Owner attempts selection before receiving at least one bid per required type
   - **System response**: "Cannot select partners yet: Missing bids from [R&D/Manufacturing/Sales]. Minimum one bid per category required."
   - **Recovery path**: System displays bid status dashboard, owner waits for more bids, system notifies when threshold met

2. **Missing Required Partner Type** (Step 4)
   - **What happens**: Owner attempts to finalize selection without picking from all required categories
   - **System response**: "You must select exactly one partner from each required category: Missing [category name(s)]"
   - **Recovery path**: System highlights missing categories, owner completes selections, resubmits

3. **Bid No Longer Available** (Step 4)
   - **What happens**: Owner selects a bid that was withdrawn by the actor
   - **System response**: "One or more selected partners are no longer available. Please review and update your selections."
   - **Recovery path**: System identifies unavailable bid(s), owner selects alternative from same category, resubmits

4. **Accidental Selection Attempt** (Step 4)
   - **What happens**: Owner hesitates before finalizing irreversible selection
   - **System response**: Displays confirmation prompt: "Partner selection is PERMANENT and cannot be undone. Selected partners: [list]. Are you sure you want to proceed?"
   - **Recovery path**: Owner can cancel to review, or confirm to proceed with selection

---

### Journey 4: Virtual Incubator Collaboration (Multi-Party)

**Goal**: Collaborate on business plan development and commercialization strategy.

**Participants**: Idea Generator + Selected Partners (R&D, Manufacturing, Sales/Marketing, Investor if selected)

**Steps**:

1. **Workspace Access**
   - All team members access virtual incubator workspace
   - Workspace scoped to specific innovation
   - Access control: Only innovation owner and accepted partners
   - Team dashboard displays:
     - Innovation details (full disclosure)
     - Team member list with roles
     - Business plan section status (complete/in-progress/not-started)
     - Activity feed (recent updates)

2. **Business Plan Development**
   
   **Collaboration Mechanics**:
   - **Section Assignment**: Each business plan section has a designated lead (based on partner expertise)
   - **Editing Workflow**:
     1. Partner navigates to their assigned section(s)
     2. Partner edits content in structured form fields
     3. Partner marks section as "draft" (visible to team but not final)
     4. Partner marks section as "ready for review" (notifies team)
     5. Team members review, add comments/suggestions
     6. Lead revises based on feedback
     7. Innovation owner approves section (locks content)
   - **Visibility**: All team members can view all sections, but only assigned lead can edit
   - **Comments**: Team members can add threaded comments on any section for discussion
   - **Version History**: System tracks changes, allows viewing previous versions of each section
   
   **Business Plan Sections**:
   
   - **Financial Model** (led by investor or innovation owner if no investor):
     - Revenue projections (3-5 year forecast)
     - Cost structure (fixed and variable costs)
     - Break-even analysis
     - Funding requirements and timeline
   
   - **Ownership Structure** (collaborative, requires consensus):
     - Equity splits among partners
     - IP licensing terms
     - Revenue sharing model
     - Exit strategy considerations
   
   - **Market Strategy** (led by sales/marketing partner):
     - Customer acquisition plan
     - Competitive positioning
     - Pricing strategy
     - Distribution channels
     - Marketing budget and timeline
   
   - **Technical Roadmap** (led by R&D partner):
     - Development phases and milestones
     - Resource requirements (personnel, equipment, facilities)
     - Risk mitigation for technical challenges
     - Quality assurance approach
   
   - **Production Plan** (led by manufacturing partner):
     - Manufacturing approach (in-house, contract, hybrid)
     - Volume scaling strategy
     - Quality standards and certifications
     - Supply chain management
     - Cost per unit projections at various volumes
   
   - **Competitive Analysis** (collaborative):
     - Market landscape overview
     - Key competitors and their offerings
     - Differentiation strategy
     - Barriers to entry

3. **Management Team Formation**
   - Innovation owner initiates team structure definition
   - Partners propose key roles needed (CEO, CTO, CFO, CMO, COO)
   - Partners indicate their intended roles
   - Team discusses and agrees on organizational structure
   - System records final management team composition
   - **Workflow**:
     1. Owner creates role (title, responsibilities)
     2. Partners nominate candidates (self or external)
     3. Team discusses and votes on assignments
     4. Owner finalizes appointments

4. **Project Valuation**
   - Financial valuation calculated by system based on completed Financial Model inputs
   - Inputs provided by investor/owner:
     - Total development costs
     - Market size estimate
     - Competitive position assessment
     - Revenue model parameters
   - System generates valuation range
   - Team reviews and may adjust inputs for sensitivity analysis
   - Final valuation documented in business plan

5. **Business Plan Completion**
   - System tracks completion status of all sections
   - Innovation owner reviews all sections marked "ready"
   - Owner marks business plan as "Complete" when satisfied
   - Innovation status updated to indicate business plan finished
   - System generates exportable business plan document (PDF)
   - Team can share document with external stakeholders (investors, advisors)

**Success Outcome**: Comprehensive business plan developed, team aligned, ready for commercialization execution.

**Error Scenarios & Recovery**:

1. **Unauthorized Access Attempt**
   - **What happens**: Non-selected partner attempts to access workspace
   - **System response**: "Access denied. Only the innovation owner and selected partners can access the virtual incubator workspace."
   - **Recovery path**: User is redirected to innovation discovery page

2. **Conflicting Edits**
   - **What happens**: Two team members attempt to edit the same section simultaneously
   - **System response**: First save succeeds, second user sees "This section was just updated by [partner name]. Please review changes before saving yours."
   - **Recovery path**: System displays comparison view, user merges changes manually, saves updated version

3. **Incomplete Section at Completion**
   - **What happens**: Owner attempts to mark business plan complete with unfinished sections
   - **System response**: "Cannot complete business plan: [section name(s)] not marked as ready. Please complete all sections or mark as not required."
   - **Recovery path**: Owner contacts assigned partners to complete sections, or marks optional sections as "not required", then resubmits

4. **Deadlocked Ownership Structure Negotiation**
   - **What happens**: Team cannot agree on equity splits or revenue sharing
   - **System response**: Section remains in "draft" status, preventing business plan completion
   - **Recovery path**: 
     - Team uses comment threads to negotiate terms
     - Innovation owner has final authority to approve structure if consensus cannot be reached
     - Alternative: Team can mark this section as "to be determined" and complete rest of plan

---

### Journey 5: Actor Profile & Industry Affiliation Management (All Actors)

**Goal**: Maintain actor profile and industry expertise/focus areas for effective innovation matching.

**Steps**:

1. **Initial Profile Setup** (During registration)
   - Provide basic information (name, email, contact address)
   - Confirm account via activation token

2. **Industry Affiliation Management**
   - Actor selects industries where they have expertise/capacity
   - Multiple industries can be selected
   - Industries organized as hierarchical structure (Sector → Subsector)
   - Actor can add new affiliations over time
   - Actor can remove affiliations

3. **Profile Updates** (Ongoing)
   - Update contact information
   - Update industry affiliations as expertise evolves
   - Account status maintained (active or suspended)

4. **Impact on Discovery**
   - Industry affiliations filter which innovations actor sees
   - Example: Manufacturing company affiliated with "Healthcare" sector sees healthcare-related innovations
   - Ensures actors only see relevant opportunities

**Success Outcome**: Actor profile accurately reflects capabilities, enabling effective innovation-to-actor matching.

---

## 4. Business Rules

### Rule Group 1: Registration & Account Management

**R1.1 Account Activation**:
- All new users start with pending activation status
- Activation email sent to registered email address
- User must confirm via unique activation link
- Upon confirmation, status changes to active
- Only active users can submit innovations or bids
- **Error message**: "Please activate your account before submitting innovations or bids. Check your email for the activation link."

**Acceptance Tests**:
```gherkin
Scenario: New user must activate account before submission
  Given a user has registered as an Idea Generator
  And the user has not clicked the activation link
  When the user attempts to create an innovation draft
  Then the system displays "Please activate your account before submitting innovations or bids"
  And the user is redirected to account activation instructions

Scenario: Activated user can create innovations
  Given a user has registered and activated their account
  When the user navigates to "Create New Innovation"
  Then the system allows the user to create an innovation draft
```

**R1.2 Actor Type Immutability**:
- Actor type (Idea Generator, R&D, Manufacturing, Sales/Marketing, Investor) is selected during registration
- Actor type cannot be changed post-registration
- Users with multiple roles must create separate accounts
- **Error message**: "Actor type cannot be changed after registration. Please create a new account for a different role."

**Acceptance Tests**:
```gherkin
Scenario: Actor attempts to change their type after registration
  Given a user has registered as "Manufacturing"
  And the user has activated their account
  When the user attempts to change actor type to "R&D"
  Then the system displays "Actor type cannot be changed after registration"
  And the actor type remains "Manufacturing"

Scenario: User with multiple roles creates separate accounts
  Given a user is registered as "Manufacturing" with email "user@company.com"
  When the user registers a new account as "Investor" with email "user-investor@company.com"
  Then both accounts are created successfully
  And each account has independent login credentials
```

**R1.3 Email Uniqueness**:
- Each email address can be registered only once per actor type
- Email serves as username equivalent
- **Error message**: "This email address is already registered. Please use a different email or log in to your existing account."

**Acceptance Tests**:
```gherkin
Scenario: Duplicate email registration attempt
  Given a user is registered with email "innovator@example.com" as "Idea Generator"
  When another user attempts to register with email "innovator@example.com" as "Idea Generator"
  Then the system displays "This email address is already registered"
  And registration is not completed

Scenario: Same email can be used for different actor types
  Given a user is registered with email "multi@example.com" as "Idea Generator"
  When the same person registers with email "multi@example.com" as "Investor"
  Then the system allows registration (separate account per actor type)
```

---

### Rule Group 2: Innovation Submission & Validation

**R2.1 Completeness Requirements**:
An innovation can only be submitted for publication when ALL sections are complete:
- **Idea Summary**:
  - Title must be provided (cannot be placeholder text)
  - Product type must be specified
  - Research category must be selected
  - Research background must be documented
  - IPR status must be declared (with explanation if no IPR)
  - Right-to-use must be confirmed
- **Product Details**:
  - Product description must be provided
  - Key advantages must be listed
  - Development phase must be specified
  - Development process must be described
  - Product keywords must be provided
  - Advantage keywords must be provided
- **Market Details**:
  - Target market description must be provided
  - Target customer base must be defined
  - Target customer type must be specified
  - At least one target industry must be selected
- **Error message**: "Cannot submit innovation: [specific section] is incomplete. Please fill in all required fields."

**R2.2 Ownership**:
- Innovation is owned by the Idea Generator who created it
- Only the owner can edit innovation details
- Only the owner can submit innovation for publication
- Only the owner can select partners
- Ownership is immutable
- **Error message**: "You do not have permission to modify this innovation. Only the owner can make changes."

**Acceptance Tests**:
```gherkin
Scenario: Non-owner attempts to edit innovation
  Given User A has created an innovation
  And User B is logged in as a different Idea Generator
  When User B attempts to edit User A's innovation
  Then the system displays "You do not have permission to modify this innovation"
  And no changes are saved

Scenario: Owner can edit their own innovation
  Given User A has created an innovation in draft status
  When User A edits the innovation title and saves
  Then the changes are saved successfully
  And User A sees the updated title
```

**R2.3 Innovation Identity**:
- Each innovation is assigned a unique identifier upon creation
- Identifier generated when draft is created
- Identifier used for tracking, referencing, and access control throughout innovation lifecycle

---

### Rule Group 3: Innovation Discovery & Visibility

**R3.1 Visibility by Status**:
- Draft innovations: Visible only to owner
- Published innovations (≥Submitted): Visible to all actors matching industry affiliation
- Innovations with selected partners: Full details visible only to owner and accepted partners
- Rejected bids: Bidders retain limited view (visible but cannot access collaboration workspace)

**R3.2 Industry-Based Matching**:
- Actors see innovations where:
  - Actor's industry affiliation overlaps with innovation's target industries
- Example: Manufacturing company affiliated with "Electronics" sees innovations targeting "Electronics" industry

**R3.3 Research Category Filtering**:
- Research category (Management / Engineering / Natural Science) is metadata only
- Used for filtering/discovery, does NOT affect workflow or permissions
- Actors can filter innovations by research category

---

### Rule Group 4: Bid Submission & Management

**R4.1 Bid Eligibility**:
- Only non-Idea-Generator actors can submit bids (R&D, Manufacturing, Sales/Marketing, Investor)
- Actor cannot bid on their own innovations
- Innovation must be published and accepting partnership proposals
- Actor cannot submit multiple bids for the same innovation
- **Error messages**: 
  - "Only R&D, Manufacturing, Sales/Marketing, and Investor actors can submit bids"
  - "You cannot bid on your own innovation"
  - "This innovation is not accepting bids"
  - "You have already submitted a bid for this innovation"

**Acceptance Tests**:
```gherkin
Scenario: Idea Generator attempts to bid on any innovation
  Given a user is logged in as "Idea Generator"
  And an innovation is published and accepting bids
  When the user attempts to submit a bid
  Then the system displays "Only R&D, Manufacturing, Sales/Marketing, and Investor actors can submit bids"
  And the bid form is not accessible

Scenario: Actor attempts to bid on their own innovation
  Given a user is logged in as "Idea Generator" and also has a "Manufacturing" account
  And the user has created an innovation with their Idea Generator account
  When the user logs in as "Manufacturing" and attempts to bid on their own innovation
  Then the system displays "You cannot bid on your own innovation"
  And the bid is not submitted

Scenario: Actor submits duplicate bid
  Given a Manufacturing actor has already submitted a bid on Innovation X
  When the actor attempts to submit another bid on Innovation X
  Then the system displays "You have already submitted a bid for this innovation"
  And the system offers option to "Edit existing bid"
```

**R4.2 Bid Content Requirements**:
- Location (geographic presence) is required
- Participation type is required
- Participation proposal is required (minimum 200 characters)
- Bid is timestamped when submitted
- **Error message**: "Bid incomplete: Location, participation type, and proposal (minimum 200 characters) are all required."

**R4.3 Bid Immutability Post-Selection**:
- Once a bid is accepted in partner selection, it cannot be withdrawn or edited
- Unaccepted bids can be edited or withdrawn by poster
- **Error message**: "This bid has been accepted and cannot be modified or withdrawn."

**Acceptance Tests**:
```gherkin
Scenario: Actor edits unaccepted bid
  Given a Manufacturing actor has submitted a bid on Innovation X
  And the innovation owner has not yet selected partners
  When the actor edits the participation proposal and saves
  Then the changes are saved successfully
  And the updated bid is visible to the innovation owner

Scenario: Actor attempts to withdraw accepted bid
  Given a Manufacturing actor's bid was selected by innovation owner
  When the actor attempts to withdraw the bid
  Then the system displays "This bid has been accepted and cannot be modified or withdrawn"
  And the bid remains committed
```

**R4.4 Bid Counts by Actor Type**:
- System tracks separate bid counts for each actor type:
  - Manufacturing bids
  - Sales/Marketing bids
  - R&D bids
  - Investor bids

---

### Rule Group 5: Partner Selection

**R5.1 Selection Authority**:
- Only the innovation owner (Idea Generator) can select partners
- System validates user authentication and ownership before allowing selection

**R5.2 Selection Requirements**:
- Must select **exactly one bid** from each required actor type:
  - One R&D Organization (if R&D collaboration is required)
  - One Manufacturing Company (if manufacturing collaboration is required)
  - One Sales/Marketing Company (if sales/marketing collaboration is required)
  - (Optionally) One Investor (if investment is required)
- Cannot select more than one bid per actor type
- Cannot proceed without selecting all required types
- **Error message**: "You must select exactly one partner from each required category: [list of missing categories]."

**Acceptance Tests**:
```gherkin
Scenario: Owner attempts selection with missing required category
  Given an innovation requires R&D, Manufacturing, and Sales/Marketing partners
  And the owner has selected one Manufacturing bid and one Sales/Marketing bid
  And the owner has not selected an R&D bid
  When the owner attempts to finalize partner selection
  Then the system displays "You must select exactly one partner from each required category: R&D Organization"
  And selection is not completed

Scenario: Owner attempts to select multiple bids from same category
  Given an innovation requires R&D, Manufacturing, and Sales/Marketing partners
  When the owner selects two Manufacturing bids
  Then the system prevents the second selection
  And displays "Only one partner per category can be selected"

Scenario: Owner completes valid selection
  Given an innovation requires R&D, Manufacturing, and Sales/Marketing partners
  And the owner has selected exactly one bid from each category
  When the owner finalizes selection
  Then all three selected partners are notified
  And the virtual incubator workspace is created
```

**R5.3 Selection Irreversibility**:
- Once partners are selected and committed, the selection **CANNOT be undone or changed**
- Innovation status changes to indicate partners have been selected
- No mechanism exists to de-select partners or re-run selection process

**Acceptance Tests**:
```gherkin
Scenario: Owner attempts to change partners after selection
  Given an innovation owner has selected partners
  And partner selection was confirmed
  When the owner attempts to access the partner selection interface
  Then the system displays "Partner selection has been completed and cannot be changed"
  And the selection interface is not accessible

Scenario: System confirms irreversibility before final selection
  Given an innovation owner has chosen one partner from each required category
  When the owner clicks "Finalize Partner Selection"
  Then the system displays confirmation: "Partner selection is PERMANENT and cannot be undone. Are you sure?"
  And the owner must explicitly confirm before selection is applied
```

**R5.4 Selection Validation**:
- Innovation must have received sufficient bids before selection
- Selected bids must exist and not already be committed
- Selected bids must match required actor types
- **Error messages**:
  - "Innovation has not received sufficient bids. Wait for at least one bid per required category."
  - "One or more selected partners are no longer available"
  - "Selected bids do not match required actor types"

**R5.5 Sufficient Bids Threshold**:
- System determines innovation has "sufficient bids" when:
  - ≥1 Manufacturing bid
  - ≥1 Sales/Marketing bid
  - ≥1 R&D bid
- Investor bid is optional (not required for threshold)

**Acceptance Tests**:
```gherkin
Scenario: Innovation reaches sufficient bids threshold
  Given an innovation requires R&D, Manufacturing, and Sales/Marketing partners
  And the innovation has received 1 Manufacturing bid, 1 Sales/Marketing bid, and 0 R&D bids
  When a new R&D bid is submitted
  Then the system marks the innovation as having "sufficient bids"
  And the innovation owner is notified "Your innovation has received sufficient bids. You can now select partners."

Scenario: Innovation below threshold cannot begin selection
  Given an innovation requires R&D, Manufacturing, and Sales/Marketing partners
  And the innovation has only 1 Manufacturing bid and 1 Sales/Marketing bid
  When the owner attempts to access partner selection
  Then the system displays "Cannot select partners yet: Missing bids from R&D. Minimum one bid per required category."
  And the selection interface is not accessible
```

---

### Rule Group 6: Collaboration Workspace Access

**R6.1 Virtual Incubator Access Control**:
- Innovation owner (Idea Generator) always has full access
- Selected partners have full access to collaboration workspace
- Rejected bidders have limited view only
- Non-bidders have discovery view only
- **Error message**: "Access denied. Only the innovation owner and selected partners can access the virtual incubator workspace."

**Acceptance Tests**:
```gherkin
Scenario: Selected partner accesses workspace
  Given an innovation has completed partner selection
  And Manufacturing Company A was selected as partner
  When Manufacturing Company A accesses the virtual incubator
  Then the workspace is displayed with all sections accessible

Scenario: Rejected bidder attempts workspace access
  Given an innovation has completed partner selection
  And Manufacturing Company B submitted a bid but was not selected
  When Manufacturing Company B attempts to access the virtual incubator
  Then the system displays "Access denied. Only the innovation owner and selected partners can access the virtual incubator workspace"
  And Manufacturing Company B sees only basic innovation information (limited view)

Scenario: Non-bidder attempts workspace access
  Given an innovation has completed partner selection
  And Actor X never submitted a bid
  When Actor X attempts to access the virtual incubator directly
  Then the system displays "Access denied"
  And Actor X is redirected to innovation discovery page
```

**R6.2 Post-Selection Visibility**:
- Before partner selection: Innovation details visible to all actors (industry-matched)
- After partner selection: Full innovation details and business plan visible only to owner and selected partners
- Rejected bidders can still view basic innovation information but cannot access workspace
- **Error message**: "You do not have permission to view full details. Only selected partners can access this information."

---

### Rule Group 7: Business Plan Development

**R7.1 Business Plan Optionality**:
- Business plan is **optional but recommended** for commercialization
- System does not enforce business plan completion as a blocker
- Innovation can proceed to commercialization without formal business plan in system

**R7.2 Business Plan Contents** (when created):
- Financial model (revenue projections, cost structure)
- Ownership structure (equity, IP licensing, revenue sharing)
- Competitive analysis (market landscape, differentiation)
- Management team composition
- Technical roadmap (from R&D partner)
- Production plan (from Manufacturing partner)
- Market strategy (from Sales/Marketing partner)

**R7.3 Project Valuation**:
- Financial valuation can be calculated by the system
- Inputs: Development costs, market size, competitive position, revenue model
- Output: Estimated project value
- Used for fundraising and planning purposes

---

### Rule Group 8: Authorization & Access Control

**R8.1 User Authentication Requirements**:
- All actions beyond viewing public innovation listings require authentication
- User session must be valid (not expired)
- User account must be in active status (not suspended)
- **Error message**: "Your session has expired. Please log in again."

**R8.2 Resource Ownership Authorization**:
- **Innovation Editing**: Only the innovation owner can edit innovation details
- **Partner Selection**: Only the innovation owner can select partners
- **Bid Submission**: Actor must be authenticated and of eligible actor type
- **Bid Editing**: Only the bid author can edit their own bids
- **Workspace Access**: Only innovation owner and selected partners can access virtual incubator
- **Error message**: "Access denied. You do not have permission to perform this action."

**R8.3 Data Protection Requirements**:
- **Passwords**: Must be securely hashed, never stored in plain text, never displayed to users
- **Personal Identifiable Information (PII)**: Email addresses, names, contact addresses protected
  - Only visible to user themselves and system administrators
  - Not shared publicly or with other actors without consent
- **Innovation IPR Details**: Sensitive intellectual property information only visible per R6.1 and R6.2 visibility rules
- **Business Plan Financials**: Financial data only accessible to innovation owner and selected partners
- **Bid Proposals**: Bid details only visible to innovation owner and bid author (not to competing bidders)

**Acceptance Tests**:
```gherkin
Scenario: Passwords never displayed
  Given a user has set their password during registration
  When the user views their profile settings
  Then the password field shows asterisks or is blank
  And the actual password is never displayed in any interface

Scenario: Competing bidders cannot view each other's proposals
  Given Manufacturing Company A and Manufacturing Company B have both bid on Innovation X
  When Manufacturing Company A views the innovation
  Then Company A sees only their own bid proposal
  And Company A does not see Company B's bid details

Scenario: Business plan financials restricted to selected partners
  Given an innovation has selected partners and a business plan in progress
  And Manufacturing Company C submitted a bid but was not selected
  When Manufacturing Company C attempts to view the business plan
  Then the system displays "Access denied. Only the innovation owner and selected partners can access this information"
  And no financial data is displayed
```

**R8.4 Account Security**:
- **Password Complexity Requirements** (CHK013):
  - Minimum 8 characters
  - At least 1 uppercase letter (A-Z)
  - At least 1 lowercase letter (a-z)
  - At least 1 digit (0-9)
  - At least 1 special character (!@#$%^&*()_+-=[]{}|;:,.<>?)
  - Maximum 128 characters (prevent DoS via bcrypt)
- **Password Hashing**: BCrypt with work factor 12 (Plan §Security Requirements)
- Account activation required before performing any actions
- Failed login attempts tracked; temporary lockout after 5 failures for 15 minutes
- **Error messages**:
  - "Password must be at least 8 characters and include uppercase, lowercase, digit, and special character."
  - "Too many failed login attempts. Account temporarily locked for 15 minutes."
  - "Invalid email or password."

**R8.5 Role-Based Access**:
- **Idea Generator** actors can:
  - Create and edit their own innovations
  - Select partners for their innovations
  - Access virtual incubator for their innovations
  - View all published innovations (industry-matched)
- **R&D, Manufacturing, Sales/Marketing, Investor** actors can:
  - Submit bids on innovations (not their own)
  - Edit/withdraw their unaccepted bids
  - Access virtual incubator for innovations where they are selected partners
  - View all published innovations (industry-matched)
- **All actors** can:
  - Update their own profile and industry affiliations
  - View their own activity history

**R8.6 Administrative Actions** (out of scope for v1.0 user functionality):
- System administrators can suspend accounts (status change to suspended)
- Suspended users cannot perform any actions until reactivated
- **User-facing message**: "Your account has been suspended. Please contact support for assistance."

---

## 5. Success Metrics

### User Satisfaction Metrics

**Innovation Submission Success**:
- Time from draft creation to publication <1 hour (measures UX friction)
- Idea Summary completion rate >90% (measures form clarity)
- Product details completion rate >90%
- Market details completion rate >90%

**Bid Submission Success**:
- Time from innovation discovery to bid submission <30 minutes (measures form simplicity)
- Bid submission completion rate >85% (measures form clarity)
- Bid edit/withdrawal rate <10% (measures confidence in submission)

**Partner Selection Success**:
- Time from sufficient bids to partner selection <7 days (measures decision friction)
- Partner selection completion rate >90% (measures sufficient bid quality)
- Percentage of innovations reaching partner selection >60% (measures ecosystem health)

---

### Business Metrics

**Ecosystem Health**:
- Active actors per actor type (target: balanced distribution, no single type <15% of total)
- Average bids per innovation by actor type (target: ≥3 bids per type)
- Time from publication to sufficient bids <14 days (measures ecosystem liquidity)
- Percentage of innovations receiving ≥1 bid per required type >70%

**Collaboration Outcomes**:
- Percentage of innovations reaching partner selection >60%
- Percentage of selected partnerships progressing to business plan >50%
- Average time from partner selection to business plan completion <30 days

**Engagement**:
- Monthly active actors (MAA) per actor type
- Innovations published per month
- Bids submitted per month
- Partner selections per month

---

### Technical Metrics

**Performance**:
- API response time (p95) <200ms for all operations
- Page load time (p95) <2s for all views
- Real-time notification delivery latency <1s
- Database query performance (p95) <100ms

**Reliability**:
- System uptime >99.5% (excluding planned maintenance)
- Error rate <0.1% of requests
- Data loss incidents: 0
- Security incidents: 0

**Quality**:
- Automated test coverage >80% of functionality
- Zero critical security vulnerabilities
- Code quality standards enforced

**Observability**:
- All operations instrumented with telemetry
- Structured logging for all business operations
- Monitoring dashboards for key metrics
- Alerts configured for error spikes and performance degradation

---

### Leading Indicators (Early Signals for Iteration)

**Purpose**: Track early signals of ecosystem health and user behavior to enable rapid iteration before lagging indicators (like partnership completion) reveal problems.

#### Onboarding Health (Week 1 signals)

**Account Activation Rate**:
- **Metric**: % of registered users who activate within 24 hours
- **Target**: >80%
- **Signal**: If <80%, activation email may be unclear or landing in spam
- **Action**: Review email content, test deliverability, add "resend" option prominence

**Profile Completion Rate**:
- **Metric**: % of activated users who complete industry affiliation setup within first session
- **Target**: >75%
- **Signal**: If <75%, industry selection UX may be confusing
- **Action**: Simplify industry taxonomy, add search/filter, provide examples

**Time to First Meaningful Action**:
- **Metric**: Median time from activation to first action (idea draft start OR innovation browse OR bid draft start)
- **Target**: <10 minutes
- **Signal**: If >10 minutes, users may be confused about next steps
- **Action**: Add onboarding tour, clarify call-to-action on dashboard

---

#### Innovation Submission Funnel (Week 1-2 signals)

**Draft Start Rate**:
- **Metric**: % of Idea Generator users who start an innovation draft within 7 days of activation
- **Target**: >60%
- **Signal**: If <60%, value proposition unclear or intimidation factor too high
- **Action**: Add "Get Started" guidance, show example innovations, reduce perceived complexity

**Section Abandonment Analysis**:
- **Metric**: Which section has highest abandonment rate (% who start section but don't complete it)
- **Target**: No section >30% abandonment
- **Signal**: High abandonment reveals confusing or overly demanding sections
- **Action**: Simplify problem section, add inline help, reduce required fields, provide templates

**Draft-to-Publication Conversion**:
- **Metric**: % of started drafts that get published within 7 days
- **Target**: >50% (note: some drafts intentionally saved for later)
- **Signal**: If <50%, submission process may have friction or unclear requirements
- **Action**: Add progress indicators, provide section-by-section guidance, enable "save and continue later"

**Validation Error Frequency**:
- **Metric**: Average number of validation errors per submission attempt
- **Target**: <2 errors per attempt
- **Signal**: If >2, validation messages unclear or fields not marked required upfront
- **Action**: Improve inline validation, mark required fields clearly, add pre-submission checklist

---

#### Bid Activity (Week 2-3 signals)

**Innovation View Rate**:
- **Metric**: % of published innovations that get viewed by ≥5 non-owner actors within 48 hours
- **Target**: >70%
- **Signal**: If <70%, discovery is broken or industry matching failing
- **Action**: Review matching algorithm, improve search/filter, add email notifications for new opportunities

**View-to-Bid Conversion**:
- **Metric**: % of actors who view an innovation and submit a bid within 7 days
- **Target**: >15% (note: many views are exploratory)
- **Signal**: If <15%, innovations may not be compelling or bid process has friction
- **Action**: Interview actors to understand barriers, improve innovation template to highlight value, simplify bid form

**First Bid Velocity**:
- **Metric**: Median time from innovation publication to first bid received
- **Target**: <3 days
- **Signal**: If >3 days, ecosystem liquidity is low or matching is poor
- **Action**: Recruit more actors, improve notifications, enhance innovation descriptions

**Bid Diversity**:
- **Metric**: % of innovations that receive bids from all required actor types within 14 days
- **Target**: >40% (early indicator for "sufficient bids threshold" metric)
- **Signal**: If <40%, specific actor types may be undersupplied or innovations not compelling to certain types
- **Action**: Targeted recruitment for scarce actor types, improve value proposition for those actors

---

#### Partner Selection Dynamics (Week 3-4 signals)

**Selection Hesitation Rate**:
- **Metric**: % of innovations at "sufficient bids" threshold for >14 days without selection
- **Target**: <20%
- **Signal**: If >20%, owners may be overwhelmed by choice, lack confidence, or find bids inadequate
- **Action**: Add partner comparison tools, provide selection guidance, interview owners about decision barriers

**Bid Quality Feedback**:
- **Metric**: % of innovation owners who rate received bids as "helpful for decision making" (post-selection survey)
- **Target**: >70%
- **Signal**: If <70%, bid template may not elicit useful information
- **Action**: Enhance bid template with structured questions, add examples of good proposals

**Post-Selection Engagement**:
- **Metric**: % of selected partners who access virtual incubator within 48 hours of selection notification
- **Target**: >85%
- **Signal**: If <85%, notification may be unclear or workspace access has friction
- **Action**: Improve notification clarity, add direct link to workspace, send reminder notifications

---

#### Collaboration Momentum (Week 4+ signals)

**Workspace First Action**:
- **Metric**: Median time from virtual incubator access to first action (view, edit, comment)
- **Target**: <24 hours
- **Signal**: If >24 hours, partners may not understand workspace or feel unclear about next steps
- **Action**: Add workspace onboarding tour, clarify partner responsibilities, provide collaboration kickoff guide

**Section Lead Engagement**:
- **Metric**: % of assigned business plan section leads who make at least one edit within 7 days
- **Target**: >80%
- **Signal**: If <80%, leads may feel unclear about expectations or lack time/resources
- **Action**: Clarify lead responsibilities, provide section templates, add deadline reminders

**Comment Activity**:
- **Metric**: Average comments per business plan section (indicates collaboration level)
- **Target**: ≥3 comments per section
- **Signal**: If <3, collaboration may be siloed or partners hesitant to provide feedback
- **Action**: Encourage review culture, add "request feedback" feature, provide comment examples

**Stalled Workspace Rate**:
- **Metric**: % of virtual incubators with zero activity in past 14 days
- **Target**: <15%
- **Signal**: If >15%, partnerships may be failing or partners losing interest
- **Action**: Add automated engagement prompts, provide progress tracking, interview stalled teams about barriers

---

### Critical User Journeys for E2E Testing

**Priority 0 (P0) - Must Test End-to-End**:

These journeys represent the core value proposition and must be validated with full end-to-end tests in every release:

1. ✅ **Journey 1: Innovation Submission & Publication** (Idea Generator)
   - **Why P0**: Core value creation flow - if users can't submit innovations, platform has no content
   - **E2E Test Coverage**: Registration → Activation → Login → Draft creation → All sections complete → Submit → Verify visible to other actors
   - **Critical validations**: Account activation, completeness validation, publication confirmation, discoverability

2. ✅ **Journey 2: Innovation Discovery & Bid Submission** (All Non-Idea-Generator Actors)
   - **Why P0**: Core value exchange - if actors can't find and bid on innovations, marketplace fails
   - **E2E Test Coverage**: Actor registration → Activation → Industry affiliation setup → Browse innovations (industry-matched) → View details → Submit bid → Verify owner notified
   - **Critical validations**: Industry matching, bid eligibility, duplicate prevention, notification delivery

3. ✅ **Journey 3: Partner Evaluation & Selection** (Idea Generator)
   - **Why P0**: Core marketplace transaction - irreversible partner selection is the key decision point
   - **E2E Test Coverage**: View received bids → Evaluate proposals → Select one per required category → Confirm irreversibility warning → Finalize selection → Verify partners notified → Verify workspace created
   - **Critical validations**: Sufficient bids threshold, selection requirements, irreversibility enforcement, workspace access control

**Priority 1 (P1) - Important but Not Blocking**:

These journeys are important for full platform value but can be tested with integration tests if E2E resources are constrained:

4. 🟡 **Journey 4: Virtual Incubator Collaboration** (Multi-Party)
   - **Why P1**: Valuable but not all users complete business plans immediately; core marketplace function (J1-J3) can succeed without this
   - **Test Strategy**: Focus integration tests on section editing, commenting, version history; E2E test only critical path (access → edit section → mark ready → owner approves)
   - **Rationale for P1**: Complex multi-party workflow, optional business plan completion, v1.0 can deliver value even if users take collaboration offline

5. 🟡 **Journey 5: Actor Profile & Industry Affiliation Management** (All Actors)
   - **Why P1**: Profile updates are important but not part of core value creation flow; initial setup covered in J1 and J2
   - **Test Strategy**: Integration tests for profile CRUD operations; E2E test only if regression risk high
   - **Rationale for P1**: Simple CRUD operations, low business logic complexity, not time-sensitive

**Testing Prioritization Guidance**:
- **Phase 0 (First Slice)**: Focus on J1 partial (registration + authentication + view single innovation)
- **Phase 1 (Full Innovation Submission)**: Complete E2E testing for J1
- **Phase 2 (Bidding)**: Add E2E testing for J2 and J3 (full marketplace cycle)
- **Phase 3 (Virtual Incubator)**: Add integration testing for J4, E2E smoke test only
- **Phase 4+**: J5 can remain integration-tested unless field feedback reveals issues

---

## 6. Phase 0 Specifics: First Slice (Week 1-2 Target)

### Minimal Viable Slice

**Goal**: Prove end-to-end functionality with simplest possible workflow.

**Scope**: 
1. **User Registration & Authentication**: User can register as Idea Generator, activate account, and log in
2. **View Single Innovation**: Authenticated user can retrieve and view one innovation by its unique identifier

**User Story**:
> As an authenticated Idea Generator,  
> I want to retrieve a specific innovation by its identifier,  
> So that I can view its details.

**Required Information Displayed**:
- Innovation unique identifier
- Innovation title
- Creation timestamp
- Submission timestamp (if submitted)
- Idea author information (identifier, full name)
- Idea summary:
  - Title
  - Product type
  - Research background
  - Research category (Management/Engineering/Natural Science)
  - IPR status (has IPR: yes/no)

**Business Rules for Phase 0**:
- User must be authenticated
- Innovation with specified identifier must exist
- No authorization checks yet (any authenticated user can view any innovation)
- System returns appropriate error if innovation does not exist
- System returns appropriate error if user not authenticated

**Testing Requirements**:
- ✅ Unit tests for validation logic
- ✅ Integration test: Retrieve innovation returns correct data
- ✅ Integration test: Non-existent innovation returns error
- ✅ Integration test: Unauthenticated request returns error
- ✅ End-to-end test: Complete user journey (Register → Activate → Login → View innovation)

**Success Criteria**:
- All tests pass (tests must fail first before implementation per epistemic testing principles)
- API functionality documented and discoverable
- Deployed to staging environment
- Smoke tested via interactive documentation
- System operations observable via monitoring
- User can successfully complete the registration-to-viewing workflow

**What This Proves**:
- User registration and activation workflow complete
- Authentication mechanism working
- Data persistence and retrieval working
- API documentation accessible
- Deployment pipeline functional
- Monitoring and observability operational
- Testing strategy validated (unit → integration → end-to-end)

---

## 7. System Constraints & Non-Functional Requirements

### Ephemeral Environment Mandate

**Constraint**: ALL environments (Development, Test, Production) MUST be ephemeral — created, destroyed, and recreated on demand without data loss risk.

**Requirements**:
1. **Infrastructure as Code**: Complete environment provisioned from version-controlled templates
2. **Reproducibility**: Any environment recreatable from scratch with identical behavior
3. **Destroyability**: Environment deletion must NOT cause unrecoverable data loss
4. **Environment Parity**: Dev/Test/Prod differ only in scale/SKU configuration, NOT architecture

**Rationale**: 
- Eliminates environment drift and "works on my machine" issues
- Enables fearless experimentation during development
- Reduces operational risk (can always rebuild from known-good state)
- Forces discipline around data persistence and state management
- Supports rapid provisioning for feature branches and testing

**Impact on Design**:
- No manual configuration allowed (except initial service principal setup)
- All secrets injected via automation, never stored in environment
- Database schema managed via migrations (code-first, not database-first)
- Seed data scripts for dev/test environments (fixed identifiers for reproducibility)
- Production data backed up to EXTERNAL storage (outside ephemeral environment)

---

### Component Validation Criteria

**Constraint**: Every system component MUST be evaluated against these four questions before implementation:

1. **Recreatability**: Can this component be safely recreated from code/configuration?
   - ✅ Accept: Stateless services, schema-based databases, configuration-driven resources
   - ❌ Reject: Manual setup steps, undocumented dependencies, environment-specific patches

2. **Blast Radius**: What breaks if this component is destroyed?
   - Must document: Downstream dependencies, recovery time, data loss risk
   - Must provide: Mitigation strategy, backup/restore procedure, validation tests

3. **Cost of Recreation**: What is the time and financial cost to rebuild?
   - Must quantify: Provisioning time, validation duration, monetary cost
   - Must optimize: Minimize recreation cost to enable frequent rebuilds

4. **Observability**: How do we prove correctness in short-lived environments?
   - Must provide: Health checks, validation tests, monitoring instrumentation
   - Must enable: Rapid verification that recreated component functions correctly

**Application Examples**:
- **App Service**: ✅ Recreatable (IaC), Blast Radius = 2-3 min downtime, Cost = $0.10-0.50, Health endpoint validates
- **SQL Database**: ⚠️ Schema recreatable (migrations), Blast Radius = data loss if not backed up, Cost = $0.20-2.00, Migration check validates
- **Application Insights**: ✅ Recreatable, Blast Radius = historical data loss (30-90d), Cost = $0, Telemetry query validates

---

### Testing Discipline & Epistemic Validation

**Constraint**: All testing MUST follow epistemic discipline to ensure meaningful validation.

**Principles**:

1. **Demonstrate Failure Before Success**
   - Tests MUST fail when feature not implemented (red phase in TDD)
   - Tests MUST fail when validation condition not met (prove assertion works)
   - Passing tests without observing failure = no confidence in test correctness

2. **No Tautological Assertions**
   - ❌ Prohibited: `assert output == input` (test validates nothing)
   - ❌ Prohibited: `assert terraform.output("name") == terraform.input("name")` (circular validation)
   - ✅ Required: External validation (query Azure API directly, test independent behavior)

3. **LLM-Generated Code Not Trusted by Default**
   - All generated code MUST include validation strategy
   - Passing tests alone are insufficient evidence of correctness
   - Manual verification checklist required for critical paths
   - Characterization tests required for complex behavior (capture baseline, detect drift)

4. **Idempotency Testing**
   - Infrastructure: Second terraform apply MUST show "No changes"
   - Database migrations: Re-running migrations MUST be safe (no duplicate records, schema conflicts)
   - Seed data: Multiple executions MUST produce identical state

**Application Examples**:
- **Infrastructure Test**: Create environment → Health check passes ✅ → Destroy DB secret → Health check fails ❌ → Proves health check detects missing config
- **API Test**: Submit innovation without title → Expect 400 ❌ → Temporarily remove validation → Expect success → Proves validation works
- **Migration Test**: Run migration → Check schema → Run migration again → Expect no errors → Proves idempotency

---

### Code Generation Principles

**Constraint**: All code generation (manual, AI-assisted, or template-based) MUST adhere to these principles.

**Mandatory Practices**:

1. **Minimize Lines of Code**
   - Prefer built-in framework features over custom implementations
   - Delete code more than adding code
   - Measure: Track LOC growth per feature, justify increases

2. **Prefer Composition Over Inheritance**
   - Use dependency injection and interfaces
   - Avoid deep inheritance hierarchies (max 2 levels)
   - Composable components enable easier testing and modification

3. **Follow SOLID, KISS, YAGNI Strictly**
   - **SOLID**: Single responsibility, interface segregation, dependency inversion
   - **KISS**: Simplest solution that meets requirements (no clever abstractions)
   - **YAGNI**: Implement only what's needed NOW (defer speculative features)

4. **Avoid Speculative Abstractions**
   - ❌ Don't create "framework for future extensibility" without concrete use case
   - ✅ Refactor to abstraction when SECOND use case emerges (not first)
   - Cost of wrong abstraction > cost of duplication

5. **Self-Evaluate for Overengineering**
   - Before committing: "Could this be simpler?"
   - Before creating abstraction: "Do I have 2+ concrete use cases?"
   - Before adding dependency: "Can I solve this with existing tools?"

**Application Examples**:
- **Terraform Modules**: 20-40 lines average (composition over duplication), environment-agnostic
- **API Endpoints**: Minimal APIs pattern (no unnecessary controllers/services for simple CRUD)
- **Secret Management**: App Service Configuration Phase 0 (simple), Key Vault Phase 1+ (when audit logging proves necessary)

---

### Secret Management Constraints

**Constraint**: Secrets MUST never be discoverable in source control, logs, or client-side code.

**Requirements**:

1. **Never Committed to Source Control**
   - Database connection strings, API keys, JWT signing keys excluded via .gitignore
   - Use secret scanning tools (git-secrets, GitHub secret scanning)
   - Rotate immediately if secret accidentally committed (assume compromised)

2. **Injected via Automation**
   - Development: User Secrets or environment variables (NOT appsettings.*.json in repo)
   - Production: CI/CD pipeline variables or external secret store (NOT environment-specific config files)
   - No hardcoded secrets in code or infrastructure templates

3. **Encrypted at Rest**
   - Development: User Secrets (stored outside project directory)
   - Production: Encrypted storage (App Service Configuration, Key Vault, etc.)
   - Never plaintext in environment variables or unencrypted files

4. **Masked in Logs**
   - Never log secret values (connection strings, tokens, passwords)
   - Use structured logging with [SensitiveData] attributes
   - Verify logs don't expose secrets during code review

5. **Rotation Strategy Required**
   - Document how to rotate each secret
   - Test rotation procedure in dev/test environment
   - Production rotation must NOT require code changes (config-driven)

**Application Examples**:
- **JWT Signing Key**: Generated cryptographically (`openssl rand -base64 32`), stored in User Secrets (dev) / App Service Configuration (prod)
- **SQL Password**: Set via Terraform variables (injected from CI/CD), never in .tf files
- **API Keys**: Stored in CI/CD secret vault, injected as build variables, never in appsettings.json

---

### Observability Requirements

**Constraint**: System behavior MUST be observable in short-lived ephemeral environments.

**Requirements**:

1. **Structured Logging**
   - All operations log structured events with correlation IDs
   - Logs centralized (survive environment destruction)
   - Retention: 30-90 days minimum (independent of environment lifetime)

2. **Health Checks**
   - Every service provides health endpoint
   - Health check validates external dependencies (database, secrets, monitoring)
   - Deployment fails if health check returns non-200 status

3. **Metrics & Alerts**
   - Track: Availability (uptime %), Performance (response time p95), Errors (5xx rate)
   - Alert on: High error rate, health check failures, unexpected environment recreation
   - Metrics retained longer than environment lifetime (historical trending)

4. **Validation Testing**
   - Automated post-deployment validation suite
   - Must complete in <5 minutes (fast feedback for ephemeral environments)
   - Validates: Health check passes, database migrations applied, test authentication flow succeeds

**Application Examples**:
- **App Service**: `/health` endpoint checks database connectivity + monitoring service availability
- **Database**: EF Core migration check queries `__EFMigrationsHistory` table
- **Monitoring**: Application Insights telemetry query confirms requests logged in last 5 minutes

---

### Data Persistence Strategy

**Constraint**: Ephemeral environments require explicit data lifecycle management.

**Requirements**:

1. **Schema as Code**
   - Database schema defined via migrations (code-first)
   - Migrations idempotent (safe to re-run)
   - Schema recreated automatically on environment provisioning

1. **Seed Data for Dev/Test**
   - Seed scripts with fixed identifiers (reproducible test data)
   - Idempotent seeding (check for existing data before insert)
   - No seed data in production (only real user-generated data)

1. **State Externalization**
   - Application state: Stateless (derive from database, not memory)
   - Session state: JWT tokens (client-side, no server-side sessions)
   - Configuration state: Injected via IaC (not stored in environment)

**Application Examples**:
- **Dev Database**: Seed script creates Actor with GUID `11111111-1111-1111-1111-111111111111` (always reproducible)
- **JWT Tokens**: Client-side storage (localStorage/sessionStorage), server validates without state

---

### Environment Lifecycle Requirements

**Constraint**: Environment creation and destruction MUST be automated, documented, and validated.

**Requirements**:

1. **Creation Workflow Documentation**
   - Step-by-step instructions (developer can follow without prior knowledge)
   - Estimated time to complete: <10 minutes
   - Prerequisites clearly listed (tools, credentials, secret values)
   - Validation checklist (how to verify successful creation)

2. **Destruction Workflow Documentation**
   - Destruction steps (automated preferred, manual acceptable)
   - Blast radius documentation (what breaks, expected downtime)
   - Validation that all resources cleaned up (no orphaned resources)

3. **Validation Testing**
   - Post-creation validation suite (proves environment functional)
   - Automated tests preferred, manual checklist acceptable for Phase 0
   - Must validate: Infrastructure provisioned, database schema applied, application deployed, health checks passing

4. **Cost Tracking**
   - Document monthly cost per environment type (dev/test/prod)
   - Document recreation cost (time + money)
   - Justify any cost increases (trade-off analysis required)

**Application Examples**:
- **Creation**: `terraform apply` → Database migrations → Seed data → Health check validation → Duration ~5-7 minutes
- **Destruction**: `terraform destroy` → Verify resources deleted → Duration ~3-5 minutes
- **Validation**: Health endpoint returns 200 + Database contains expected migrations + Telemetry flowing to monitoring

---

## 8. Out of Scope (Deferred to v2.0)

**Explicitly NOT included in v1.0** (architectural support may exist, but features disabled):

### Platform Modes
- ❌ **Closed Innovation**: Organization-scoped innovation, restricted actor participation
- ❌ **Hybrid Innovation**: Selective disclosure, mixed public/private visibility
- ✅ **Open Innovation ONLY** in v1.0

### Actor Types
- ❌ **Government Entities**: Policy enablers, regulators
- ❌ **Technology Parks**: Physical/virtual incubators, ecosystem hosts
- ✅ **Limited to 5 actor types** in v1.0: Idea Generator, R&D Organization, Manufacturing, Sales/Marketing, Investor

### Multi-Tenancy & Organizations
- ❌ **Organization Entities**: Corporate accounts, team management
- ❌ **Organization-Scoped Innovations**: Closed innovation mode
- ❌ **Organization Administrators**: Org-level user management
- ✅ **Individual accounts only** in v1.0

### Advanced Discovery Features
- ❌ **Advanced Search**: Full-text search, keyword matching, faceted filters
- ❌ **Recommendation Engine**: AI-powered innovation-to-actor matching
- ❌ **Saved Searches**: Persistent filters and notifications
- ✅ **Industry affiliation matching only** in v1.0

### Advanced Collaboration Features
- ❌ **Real-Time Messaging**: Chat between innovation owner and bidders
- ❌ **Document Management**: File upload/sharing in virtual incubator
- ❌ **Task Management**: Milestones, assignments, progress tracking
- ❌ **Calendar Integration**: Meeting scheduling, deadline tracking
- ✅ **Business plan data entry only** in v1.0

### Financial Features
- ❌ **Payment Processing**: Investment transactions, escrow services
- ❌ **Revenue Sharing Automation**: Automated royalty distributions
- ❌ **Financial Reporting**: Dashboards, analytics for investors
- ✅ **Business plan financial modeling only** in v1.0

### Advanced Analytics
- ❌ **Innovation Success Tracking**: Post-commercialization outcomes
- ❌ **Actor Performance Metrics**: Bid success rates, partnership outcomes
- ❌ **Platform Analytics Dashboard**: Ecosystem health, trends
- ✅ **Basic activity logging only** in v1.0

### Internationalization
- ❌ **Multi-Language Support**: Localized UI and content
- ❌ **Currency Support**: Multi-currency financial projections
- ❌ **Regional Compliance**: GDPR, region-specific regulations
- ✅ **English only, basic compliance** in v1.0

### External Integrations
- ❌ **CRM Integration**: Salesforce, HubSpot connectors
- ❌ **Calendar Integration**: Outlook, Google Calendar
- ❌ **Patent Database Integration**: Automated IPR verification
- ✅ **No external integrations** in v1.0 (except Azure Service Bus for internal messaging)

---

## Approval & Next Steps

### Specification Review Checklist

Before proceeding to PLAN phase, validate:
- ✅ All user personas clearly defined with needs and pain points
- ✅ Core user journeys documented from start to finish
- ✅ Business rules comprehensive and unambiguous
- ✅ Success metrics measurable and meaningful
- ✅ Phase 0 scope minimal and achievable
- ✅ Out-of-scope items explicitly listed to avoid scope creep
- ✅ **No technical implementation details** in this document (reserved for PLAN.md)

### Questions for Review

1. **User Personas**: Are the 5 actor types accurately represented? Any missing needs or pain points?
2. **User Journeys**: Are the 5 journeys complete and realistic? Any missing steps or edge cases?
3. **Business Rules**: Are all rules clear and testable? Any contradictions or ambiguities?
4. **Phase 0 Scope**: Is the first slice appropriately minimal? Too ambitious or too trivial?
5. **Success Metrics**: Are the metrics actionable and measurable? Any critical metrics missing?
6. **Out of Scope**: Any v2.0 features that should be in v1.0? Any v1.0 features that should be deferred?

---

**END OF SPECIFICATION**
