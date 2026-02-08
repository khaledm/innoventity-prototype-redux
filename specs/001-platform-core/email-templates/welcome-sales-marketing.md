# Welcome Email - Sales & Marketing Company

**Subject**: Welcome to Innoventity - Find Commercialization Opportunities  
**Trigger**: POST /auth/activate (after successful account activation)  
**Actor Type**: SalesMarketing

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
      
      <p>Your account has now been activated. We're excited to welcome you as a <strong>Sales & Marketing Partner</strong> to the Innoventity platform!</p>
      
      <p>Innoventity connects research-based innovations with sales & marketing professionals like you, who can identify market opportunities and drive successful commercialization strategies for breakthrough products.</p>
      
      <div class="info-box">
        <strong>📌 Phase 0 - Get Started</strong><br>
        During this initial phase, you can:
        <ul style="margin: 8px 0;">
          <li>Explore research-based innovations posted on the platform</li>
          <li>Review innovation details including technical features and target markets</li>
          <li>Identify commercialization opportunities matching your market expertise</li>
        </ul>
      </div>
      
      <div class="info-box">
        <strong>🚀 Coming in Phase 1+</strong><br>
        <ul style="margin: 8px 0;">
          <li>Submit partnership proposals (bids) for innovations with strong market potential</li>
          <li>Specify market estimates including size, locations, costs, and strategies</li>
          <li>Collaborate with Idea Generators, R&D, manufacturing, and investment partners</li>
          <li>Participate in business plan development and go-to-market strategy planning</li>
        </ul>
      </div>
      
      <p>Ready to discover commercialization opportunities? Log in to your account and explore innovations that could benefit from your market expertise:</p>
      
      <div style="text-align: center;">
        <a href="{{BASE_URL}}/login" class="cta-button" target="_blank" rel="noopener">
          Log In to Innoventity
        </a>
      </div>
      
      <p>If you have any questions or need assistance, our support team is here to help. Don't hesitate to reach out!</p>
      
      <p>
        We wish you great success in finding strategic market opportunities!<br><br>
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

Your account has now been activated. We're excited to welcome you as a Sales & Marketing Partner to the Innoventity platform!

Innoventity connects research-based innovations with sales & marketing professionals like you, who can identify market opportunities and drive successful commercialization strategies for breakthrough products.

PHASE 0 - GET STARTED
-------------------------------------------
During this initial phase, you can:
• Explore research-based innovations posted on the platform
• Review innovation details including technical features and target markets
• Identify commercialization opportunities matching your market expertise

COMING IN PHASE 1+
-------------------------------------------
• Submit partnership proposals (bids) for innovations with strong market potential
• Specify market estimates including size, locations, costs, and strategies
• Collaborate with Idea Generators, R&D, manufacturing, and investment partners
• Participate in business plan development and go-to-market strategy planning

Ready to discover commercialization opportunities? Log in to your account and explore innovations that could benefit from your market expertise:

{{BASE_URL}}/login

If you have any questions or need assistance, our support team is here to help. Don't hesitate to reach out!

We wish you great success in finding strategic market opportunities!

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
| `{{ACTOR_NAME}}` | Company name from registration | MarketPro Consulting Group |
| `{{BASE_URL}}` | Application base URL | https://innoventity.azurewebsites.net |

---

## Implementation Notes

### Business Rules
- **Trigger**: Sent immediately after successful account activation (within 5 seconds)
- **Actor Type Check**: Only sent when `Actor.ActorType == ActorType.SalesMarketing`
- **One-time**: Only sent once per account

### Phase 0 Scope Clarifications
- **View innovations**: Sales & Marketing companies can browse `GET /innovations/{id}` (spec.md R2.1)
- **NO bidding yet**: Bid submission deferred to Phase 1+ (not in current OpenAPI contract)
- **NO market estimates yet**: Phase 1+ feature (legacy mentioned "market size, locations, costs, strategies")

---

## Adapted from Legacy

**Original file**: `Welcome Email upon Activation - Sales&Mktg.txt`  
**Changes**:
- ✅ Removed £100 annual membership fee reference (legacy: "paying £100 as annual membership fee")
- ✅ Removed free vs paid membership tier distinction (legacy: "synopsis free, full details £100")
- ✅ Fixed typo: "plateform" → "platform" (legacy had spelling error)
- ✅ Simplified bid response workflow (legacy: detailed clarification/join/reject options) to Phase 0 scope
- ✅ Removed market estimate instructions (legacy: "market size, locations, Sales & Marketing costs, strategies") - Phase 1+ feature
- ✅ Removed participation mode options (ownership/fee/hybrid basis) - Phase 1+ feature
- ✅ Removed "sole stakeholder" participation mode mention (Phase 1+ feature)
- ✅ Removed monitoring other companies feature (Phase 1+ feature)
- ✅ Removed business plan negotiation details (Phase 1+ feature)
- ✅ Updated domain: `www.innoventity.com` → `{{BASE_URL}}`
- ✅ Converted plain text to responsive HTML + plain text fallback
- ✅ Modern professional tone (kept legacy's encouraging closing)
