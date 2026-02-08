# Welcome Email - Idea Generator

**Subject**: Welcome to Innoventity - Your Innovation Journey Starts Here  
**Trigger**: POST /auth/activate (after successful account activation)  
**Actor Type**: IdeaGenerator

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
      
      <p>Your account has now been activated. We're thrilled to welcome you as an <strong>Idea Generator</strong> to the Innoventity platform!</p>
      
      <p>Innoventity connects research-based innovations with industry experts, manufacturers, sales & marketing professionals, and investors to turn groundbreaking ideas into successful businesses.</p>
      
      <div class="info-box">
        <strong>📌 Phase 0 - Get Started</strong><br>
        During this initial phase, you can:
        <ul style="margin: 8px 0;">
          <li>Explore the platform and its features</li>
          <li>View existing innovations to understand how ideas are presented</li>
          <li>Review collaboration opportunities</li>
        </ul>
      </div>
      
      <div class="info-box">
        <strong>🚀 Coming in Phase 1+</strong><br>
        <ul style="margin: 8px 0;">
          <li>Submit your research-based innovations</li>
          <li>Receive partnership proposals from R&D companies, manufacturers, sales/marketing experts, and investors</li>
          <li>Collaborate through our virtual incubator workspace</li>
          <li>Develop comprehensive business plans with your selected partners</li>
        </ul>
      </div>
      
      <p>Ready to explore? Log in to your account and discover how Innoventity can help bring your innovations to market:</p>
      
      <div style="text-align: center;">
        <a href="{{BASE_URL}}/login" class="cta-button" target="_blank" rel="noopener">
          Log In to Innoventity
        </a>
      </div>
      
      <p>If you have any questions or need assistance, our support team is here to help. Don't hesitate to reach out!</p>
      
      <p>
        We wish you great success on your innovation journey!<br><br>
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

Your account has now been activated. We're thrilled to welcome you as an Idea Generator to the Innoventity platform!

Innoventity connects research-based innovations with industry experts, manufacturers, sales & marketing professionals, and investors to turn groundbreaking ideas into successful businesses.

PHASE 0 - GET STARTED
-------------------------------------------
During this initial phase, you can:
• Explore the platform and its features
• View existing innovations to understand how ideas are presented
• Review collaboration opportunities

COMING IN PHASE 1+
-------------------------------------------
• Submit your research-based innovations
• Receive partnership proposals from R&D companies, manufacturers, sales/marketing experts, and investors
• Collaborate through our virtual incubator workspace
• Develop comprehensive business plans with your selected partners

Ready to explore? Log in to your account and discover how Innoventity can help bring your innovations to market:

{{BASE_URL}}/login

If you have any questions or need assistance, our support team is here to help. Don't hesitate to reach out!

We wish you great success on your innovation journey!

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
| `{{ACTOR_NAME}}` | Full name from registration | Dr. Sarah Johnson |
| `{{BASE_URL}}` | Application base URL | https://innoventity.azurewebsites.net |

---

## Implementation Notes

### Business Rules
- **Trigger**: Sent immediately after successful account activation (within 5 seconds)
- **Actor Type Check**: Only sent when `Actor.ActorType == ActorType.IdeaGenerator`
- **One-time**: Only sent once per account (track in `EmailSent` audit log)

### Phase 0 Scope Clarifications
- **View innovations**: Idea Generators can browse `GET /innovations/{id}` (spec.md R2.1)
- **NO submission yet**: Innovation posting deferred to Phase 1+ (not in current OpenAPI contract)
- **NO bid response yet**: Collaboration features deferred to Phase 1+

### Error Scenarios
- **Email send failure**: Retry up to 3 times with exponential backoff (5s, 25s, 125s)
- **Bounce/invalid email**: Update `Actor.AccountStatus` to `Suspended`, log in audit table

---

## Adapted from Legacy

**Original file**: `Welcome Email upon Activation - IdeaAuthor.txt` (Note: IdeaAuthor renamed to IdeaGenerator)  
**Changes**:
- ✅ Removed £20 submission fee reference (legacy: "pay £20 for submitting an idea")
- ✅ Simplified 3-step process (legacy: "pay → post → monitor → negotiate") to Phase 0 scope
- ✅ Removed video upload tool mention (only exists in legacy MVC app, not Phase 0 feature)
- ✅ Updated "IdeaAuthor" terminology to "IdeaGenerator" (aligned with data-model.md enum `ActorType.IdeaGenerator`)
- ✅ Added Phase 0 vs Phase 1+ distinction (legacy assumed all features immediately available)
- ✅ Fixed domain: `www.innoventity.com` → `{{BASE_URL}}`
- ✅ Converted plain text to responsive HTML + plain text fallback
- ✅ Modern professional tone (kept legacy's encouraging "wish you great success" closing)
