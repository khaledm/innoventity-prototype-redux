# Welcome Email - R&D Organization

**Subject**: Welcome to Innoventity - Discover Research-Based Innovations  
**Trigger**: POST /auth/activate (after successful account activation)  
**Actor Type**: RD

---

## HTML Version

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <meta http-equiv="X-UA-Compatible" content="IE=edge">
  <title>Welcome to Innoventity</title>
  <style>
    body {
      margin: 0;
      padding: 0;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
      background-color: #f5f5f5;
    }
    .container {
      max-width: 600px;
      margin: 0 auto;
      background-color: #ffffff;
    }
    .header {
      background-color: #1976D2;
      padding: 24px;
      text-align: center;
    }
    .logo {
      max-width: 150px;
      height: auto;
    }
    .content {
      padding: 32px 24px;
      color: #212121;
    }
    .content h1 {
      font-size: 24px;
      margin: 0 0 16px 0;
      color: #1976D2;
    }
    .content p {
      font-size: 16px;
      line-height: 1.6;
      margin: 0 0 16px 0;
    }
    .cta-button {
      display: inline-block;
      padding: 14px 32px;
      margin: 24px 0;
      background-color: #FFC107;
      color: #212121;
      text-decoration: none;
      font-weight: 500;
      font-size: 16px;
      border-radius: 4px;
      text-align: center;
    }
    .cta-button:hover {
      background-color: #FFB300;
    }
    .info-box {
      padding: 16px;
      background-color: #E3F2FD;
      border-left: 4px solid #1976D2;
      margin: 16px 0;
    }
    .footer {
      padding: 24px;
      text-align: center;
      font-size: 14px;
      color: #666;
      background-color: #f5f5f5;
    }
  </style>
</head>
<body>
  <div class="container">
    <!-- Header with Logo -->
    <div class="header">
      <img src="{{BASE_URL}}/assets/logo-white.png" alt="Innoventity" class="logo">
    </div>

    <!-- Content -->
    <div class="content">
      <h1>Welcome to Innoventity!</h1>
      
      <p>Dear <strong>{{ACTOR_NAME}}</strong>,</p>
      
      <p>Your account has now been activated. We're pleased to welcome you as an <strong>R&D Organization</strong> to the Innoventity platform!</p>
      
      <p>Innoventity connects research-based innovations with domain experts like you, who can provide product development capabilities and technical expertise to transform concepts into market-ready prototypes.</p>
      
      <div class="info-box">
        <strong>📌 Phase 0 - Get Started</strong><br>
        During this initial phase, you can:
        <ul style="margin: 8px 0;">
          <li>Explore research-based innovations posted on the platform</li>
          <li>Review innovation details including research findings and technical specifications</li>
          <li>Understand the types of development opportunities available</li>
        </ul>
      </div>
      
      <div class="info-box">
        <strong>🚀 Coming in Phase 1+</strong><br>
        <ul style="margin: 8px 0;">
          <li>Submit partnership proposals (bids) for innovations matching your expertise</li>
          <li>Specify development estimates including timelines, costs, and resource requirements</li>
          <li>Collaborate with Idea Generators and other stakeholders through our virtual incubator</li>
          <li>Participate in business plan development and negotiations</li>
        </ul>
      </div>
      
      <p>Ready to discover innovation opportunities? Log in to your account and explore research-based ideas waiting for technical development:</p>
      
      <div style="text-align: center;">
        <a href="{{BASE_URL}}/login" class="cta-button" target="_blank" rel="noopener">
          Log In to Innoventity
        </a>
      </div>
      
      <p>If you have any questions or need assistance, our support team is here to help. Don't hesitate to reach out!</p>
      
      <p>
        We wish you great success in finding strategic development opportunities!<br><br>
        Best regards,<br>
        <strong>The Innoventity Team</strong>
      </p>
    </div>

    <!-- Footer -->
    <div class="footer">
      <p>Need help? Contact us at <a href="mailto:support@innoventity.example.com" style="color: #1976D2;">support@innoventity.example.com</a></p>
      <p>&copy; 2024 Innoventity. All rights reserved.</p>
    </div>
  </div>
</body>
</html>
```

---

## Plain Text Version

```
INNOVENTITY
===========================================

WELCOME TO INNOVENTITY!

Dear {{ACTOR_NAME}},

Your account has now been activated. We're pleased to welcome you as an R&D Organization to the Innoventity platform!

Innoventity connects research-based innovations with domain experts like you, who can provide product development capabilities and technical expertise to transform concepts into market-ready prototypes.

PHASE 0 - GET STARTED
-------------------------------------------
During this initial phase, you can:
• Explore research-based innovations posted on the platform
• Review innovation details including research findings and technical specifications
• Understand the types of development opportunities available

COMING IN PHASE 1+
-------------------------------------------
• Submit partnership proposals (bids) for innovations matching your expertise
• Specify development estimates including timelines, costs, and resource requirements
• Collaborate with Idea Generators and other stakeholders through our virtual incubator
• Participate in business plan development and negotiations

Ready to discover innovation opportunities? Log in to your account and explore research-based ideas waiting for technical development:

{{BASE_URL}}/login

If you have any questions or need assistance, our support team is here to help. Don't hesitate to reach out!

We wish you great success in finding strategic development opportunities!

Best regards,
The Innoventity Team

---
Need help? Contact us at support@innoventity.example.com
© 2024 Innoventity. All rights reserved.
```

---

## Template Variables

| Placeholder | Description | Example |
|-------------|-------------|---------|
| `{{ACTOR_NAME}}` | Organization name from registration | TechNova Research Labs |
| `{{BASE_URL}}` | Application base URL | https://innoventity.azurewebsites.net |

---

## Implementation Notes

### Business Rules
- **Trigger**: Sent immediately after successful account activation (within 5 seconds)
- **Actor Type Check**: Only sent when `Actor.ActorType == ActorType.RD`
- **One-time**: Only sent once per account

### Phase 0 Scope Clarifications
- **View innovations**: R&D organizations can browse `GET /innovations/{id}` (spec.md R2.1)
- **NO bidding yet**: Bid submission deferred to Phase 1+ (not in current OpenAPI contract)
- **NO development estimates yet**: Phase 1+ feature (legacy mentioned "development time, cost, resources, strategies")

---

## Adapted from Legacy

**Original file**: `Welcome Email upon Activation - R&D.txt`  
**Changes**:
- ✅ Removed £100 annual membership fee reference (legacy: "paying £100 as annual membership fee")
- ✅ Removed free vs paid membership tier distinction (legacy: "synopsis free, full details £100")
- ✅ Simplified bid response workflow (legacy: detailed clarification/join/reject options) to Phase 0 scope
- ✅ Removed development estimate instructions (legacy: "time, cost, resources, strategies") - Phase 1+ feature
- ✅ Removed "sole stakeholder" participation mode mention (Phase 1+ feature)
- ✅ Removed monitoring other companies feature (Phase 1+ feature)
- ✅ Removed business plan negotiation details (Phase 1+ feature)
- ✅ Updated domain: `www.innoventity.com` → `{{BASE_URL}}`
- ✅ Converted plain text to responsive HTML + plain text fallback
- ✅ Modern professional tone (kept legacy's encouraging closing)
