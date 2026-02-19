# Account Activation Email

**Subject**: Activate Your Innoventity Account  
**Trigger**: POST /auth/register (immediately after successful registration)  
**Priority**: CRITICAL (blocking for user login)

---

## HTML Version

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <meta http-equiv="X-UA-Compatible" content="IE=edge">
  <title>Activate Your Innoventity Account</title>
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
    .alternative-link {
      font-size: 14px;
      color: #666;
      word-break: break-all;
    }
    .footer {
      padding: 24px;
      text-align: center;
      font-size: 14px;
      color: #666;
      background-color: #f5f5f5;
    }
    .expiration-notice {
      padding: 12px;
      background-color: #FFF3E0;
      border-left: 4px solid #FFC107;
      margin: 16px 0;
      font-size: 14px;
      color: #E65100;
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
      
      <p>Thank you for registering with Innoventity. We're excited to have you join our platform for research-based innovation collaboration.</p>
      
      <p>To complete your registration and activate your account, please click the button below:</p>
      
      <div style="text-align: center;">
        <a href="{{ACTIVATION_LINK}}" class="cta-button" target="_blank" rel="noopener">
          Activate Your Account
        </a>
      </div>
      
      <div class="expiration-notice">
        <strong>⏰ Important:</strong> This activation link will expire in 7 days. Please activate your account before then to avoid re-registration.
      </div>
      
      <p><strong>Button not working?</strong> Copy and paste this link into your browser:</p>
      <p class="alternative-link">{{ACTIVATION_LINK}}</p>
      
      <p>If you didn't register for an Innoventity account, please disregard this email. No action is required.</p>
      
      <p>We look forward to helping you turn research-based ideas into successful businesses!</p>
      
      <p>
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

ACTIVATE YOUR ACCOUNT

Dear {{ACTOR_NAME}},

Thank you for registering with Innoventity. We're excited to have you join our platform for research-based innovation collaboration.

To complete your registration and activate your account, please click this link:

{{ACTIVATION_LINK}}

⏰ IMPORTANT: This activation link will expire in 7 days. Please activate your account before then to avoid re-registration.

If you didn't register for an Innoventity account, please disregard this email. No action is required.

We look forward to helping you turn research-based ideas into successful businesses!

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
| `{{ACTOR_NAME}}` | Full name from registration | John Smith |
| `{{ACTIVATION_LINK}}` | Full activation URL with token | https://innoventity.azurewebsites.net/activate?token=abc123... |
| `{{BASE_URL}}` | Application base URL | https://innoventity.azurewebsites.net |
| `{{ACTIVATION_TOKEN}}` | UUID v4 token (36 chars) | 550e8400-e29b-41d4-a716-446655440000 |

---

## Implementation Notes

### Business Rules
- **Token validity**: 7 days (168 hours) from registration timestamp
- **Token format**: UUID v4 stored in `Actor.ActivationToken` (spec.md Section 3.4)
- **Rate limiting**: Maximum 3 activation attempts per hour per email (security)
- **Already activated**: If user clicks link after activation, redirect to login with message "Account already activated. Please login."

### Error Scenarios
- **Expired token**: HTTP 400 with message "Activation link expired. Please register again."
- **Invalid token**: HTTP 404 with message "Invalid activation link."
- **Already used**: HTTP 200 redirect to login (graceful, no error)

### Testing Checklist
- [ ] Email sent immediately after registration (within 5 seconds)
- [ ] Activation link format correct: `/activate?token={UUID}`
- [ ] Token stored in database with `CreatedAt` timestamp
- [ ] Clicking link updates `Actor.AccountStatus` to `Active`
- [ ] Clicking expired link shows appropriate error message
- [ ] Email renders correctly in Gmail, Outlook, Apple Mail
- [ ] Mobile responsive (tested at 320px, 375px, 414px widths)
- [ ] Plain text fallback renders correctly

### Security Considerations
- **Token entropy**: UUID v4 provides 122 bits of entropy (cryptographically secure)
- **HTTPS only**: Activation links must use HTTPS (no HTTP)
- **Rate limiting**: Prevent brute-force token guessing (max 10 failed attempts per IP per hour)
- **Email verification**: Confirms user owns the email address (prevents fake registrations)

---

## Adapted from Legacy

**Original file**: `Account Activation.txt`  
**Changes**:
- ✅ Updated domain: `www.innovemtuty.com` → `{{BASE_URL}}` placeholder
- ✅ Fixed typo: "innovemtuty" → "innoventity"
- ✅ Modern link format: `/activateaccount/{token}` → `/activate?token={{ACTIVATION_TOKEN}}`
- ✅ Added 7-day expiration notice (legacy didn't specify duration)
- ✅ Converted plain text to responsive HTML + plain text fallback
- ✅ Added button CTA (more prominent than plain link)
- ✅ Enhanced footer with support contact info
