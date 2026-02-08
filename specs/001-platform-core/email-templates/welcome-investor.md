# Welcome Email - Investor

**Subject**: Welcome to Innoventity - Discover Investment Opportunities  
**Trigger**: POST /auth/activate (after successful account activation)  
**Actor Type**: Investor

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
      
      <p>Your account has now been activated. We're delighted to welcome you as an <strong>Investor</strong> to the Innoventity platform!</p>
      
      <p>Innoventity connects research-based innovations with investors like you, who can evaluate groundbreaking ideas and participate in turning them into successful businesses alongside R&D, manufacturing, and sales & marketing partners.</p>
      
      <div class="info-box">
        <strong>📌 Phase 0 - Get Started</strong><br>
        During this initial phase, you can:
        <ul style="margin: 8px 0;">
          <li>Explore research-based innovations posted on the platform</li>
          <li>Review innovation details and market opportunities</li>
          <li>Identify investment-worthy opportunities</li>
        </ul>
      </div>
      
      <div class="info-box">
        <strong>🚀 Coming in Phase 1+</strong><br>
        <ul style="margin: 8px 0;">
          <li>Submit investment proposals for innovations matching your portfolio strategy</li>
          <li>Request clarifications from Idea Generators, R&D, manufacturing, and sales & marketing partners</li>
          <li>Access comprehensive business plans with financial projections</li>
          <li>Participate in negotiations and partnership agreements</li>
        </ul>
      </div>
      
      <p>Ready to discover exciting investment opportunities? Log in to your account and explore research-based innovations waiting for strategic capital:</p>
      
      <div style="text-align: center;">
        <a href="{{BASE_URL}}/login" class="cta-button" target="_blank" rel="noopener">
          Log In to Innoventity
        </a>
      </div>
      
      <p>If you have any questions or need assistance, our support team is here to help.</p>
      
      <p>
        We wish you great success in finding promising investment opportunities!<br><br>
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

Your account has now been activated. We're delighted to welcome you as an Investor to the Innoventity platform!

Innoventity connects research-based innovations with investors like you, who can evaluate groundbreaking ideas and participate in turning them into successful businesses alongside R&D, manufacturing, and sales & marketing partners.

PHASE 0 - GET STARTED
-------------------------------------------
During this initial phase, you can:
• Explore research-based innovations posted on the platform
• Review innovation details and market opportunities
• Identify investment-worthy opportunities

COMING IN PHASE 1+
-------------------------------------------
• Submit investment proposals for innovations matching your portfolio strategy
• Request clarifications from Idea Generators, R&D, manufacturing, and sales & marketing partners
• Access comprehensive business plans with financial projections
• Participate in negotiations and partnership agreements

Ready to discover exciting investment opportunities? Log in to your account and explore research-based innovations waiting for strategic capital:

{{BASE_URL}}/login

If you have any questions or need assistance, our support team is here to help.

We wish you great success in finding promising investment opportunities!

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
| `{{ACTOR_NAME}}` | Individual or organization name | Apex Ventures Capital |
| `{{BASE_URL}}` | Application base URL | https://innoventity.azurewebsites.net |

---

## Implementation Notes

### Business Rules
- **Trigger**: Sent immediately after successful account activation (within 5 seconds)
- **Actor Type Check**: Only sent when `Actor.ActorType == ActorType.Investor`
- **One-time**: Only sent once per account

### Phase 0 Scope Clarifications
- **View innovations**: Investors can browse `GET /innovations/{id}` (spec.md R2.1)
- **NO investment proposals yet**: Bid submission deferred to Phase 1+ (not in current OpenAPI contract)
- **NO business plan access yet**: Phase 1+ feature (legacy mentioned "business plan with various scenarios")
- **NO clarification requests yet**: Phase 1+ feature (legacy mentioned asking clarifications from all actor types)

---

## Adapted from Legacy

**Original file**: `Welcome Email upon Activation - Investor.txt`  
**Changes**:
- ✅ Removed £100 annual membership fee reference (legacy: "paying £100 as annual membership fee")
- ✅ Removed free vs paid membership tier distinction (legacy: "synopsis free, full details £100")
- ✅ Fixed typo: "Sales nad marketing" → "Sales & Marketing" (legacy had spelling error)
- ✅ Removed business plan access mention (legacy: "business plan with various scenarios") - Phase 1+ feature
- ✅ Simplified bid response workflow (legacy: clarification/join/reject options) to Phase 0 scope
- ✅ Removed clarification request details (legacy: "asking from Idea generator, Sales and marketing companies, Product development companies and manufacturers") - Phase 1+ feature
- ✅ Removed negotiation details (legacy: "invited to for negotiations") - Phase 1+ feature
- ✅ Updated domain: `www.innoventity.com` → `{{BASE_URL}}`
- ✅ Converted plain text to responsive HTML + plain text fallback
- ✅ Modern professional tone (kept legacy's encouraging closing)
- ✅ Maintained concise format (legacy Investor email was shorter than other actor types - 25 lines vs 40+ lines for others)
