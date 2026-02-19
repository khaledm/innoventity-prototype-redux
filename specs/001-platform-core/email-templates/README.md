# Email Templates - Phase 0

**Purpose**: Email notification templates for Innoventity Platform Phase 0  
**Format**: Dual format (HTML + Plain Text fallback)  
**Adapted From**: Legacy MVC app templates (`C:\Users\mahmu\source\repos\innoventity-prototype-development\doc\Email Templates\`)  
**Phase 0 Scope**: Account activation + Welcome emails only (bid notifications deferred to Phase 1+)

---

## Template Structure

### Standard Elements

**From Address**: `noreply@innoventity.example.com` (update to production domain)  
**Reply-To**: `support@innoventity.example.com`  
**Logo**: Innoventity logo (top, centered, 150px width max)  
**Footer**: Unsubscribe link + company address (Phase 1+)

### Placeholders

All templates use `{{PLACEHOLDER}}` syntax for dynamic content:

- `{{ACTOR_NAME}}` - Full name from registration
- `{{ACTOR_TYPE}}` - IdeaGenerator, RD, Manufacturing, SalesMarketing, Investor
- `{{ACTIVATION_TOKEN}}` - UUID v4 token for account activation
- `{{BASE_URL}}` - Application base URL (e.g., https://innoventity.azurewebsites.net)
- `{{ACTIVATION_LINK}}` - `{{BASE_URL}}/activate?token={{ACTIVATION_TOKEN}}`

### Tone & Voice

- **Professional**: Business communication, not casual
- **Encouraging**: "Welcome to Innoventity", "Wish you great success"
- **Instructional**: Clear next steps, actionable CTAs
- **Concise**: Avoid wall of text, use bullet points

---

## Phase 0 Templates

### 1. Account Activation Email
**File**: `activation-email.md`  
**Subject**: `Activate Your Innoventity Account`  
**Trigger**: POST /auth/register (immediately after successful registration)  
**Blocking**: Yes (user cannot login until activated)

### 2. Welcome Emails (Per Actor Type)
**Files**: `welcome-{actor-type}.md`  
**Subject**: `Welcome to Innoventity - Get Started as a {{ACTOR_TYPE}}`  
**Trigger**: POST /auth/activate (after successful account activation)  
**Blocking**: No (informational onboarding)

Actor-specific welcome emails:
- `welcome-idea-generator.md` - Innovation originators
- `welcome-rd-organization.md` - R&D companies
- `welcome-manufacturing.md` - Manufacturing partners
- `welcome-sales-marketing.md` - Sales & Marketing partners
- `welcome-investor.md` - Financial investors

---

## Phase 0 Adaptations from Legacy

### Removed Features
- ❌ Payment references (£20 submission fee, £100 membership)
- ❌ Free vs paid membership tiers
- ❌ Video upload tool mentions
- ❌ Document upload references
- ❌ Advanced search/filtering features
- ❌ Business plan reports/scenarios

### Simplified Workflow
- **Legacy**: 3-step process (register → pay → post idea → monitor → negotiate)
- **Phase 0**: Simplified (register → activate → view innovation)

### Updated Content
- ✅ Fixed typos: "plateform" → "platform", "nad" → "and"
- ✅ Domain: `www.innovemtuty.com` → `{{BASE_URL}}`
- ✅ Modern activation link: `/activateaccount/{token}` → `/activate?token={{ACTIVATION_TOKEN}}`
- ✅ Responsive HTML formatting (not just plain text)

---

## HTML Template Guidelines

### Required Meta Tags
```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <meta http-equiv="X-UA-Compatible" content="IE=edge">
  <title>{{EMAIL_SUBJECT}}</title>
</head>
```

### Responsive Design
- **Mobile-first**: 320px minimum width support
- **Max width**: 600px centered container
- **Font size**: 16px base (14px minimum for body text)
- **Touch targets**: 44x44px minimum for buttons/links
- **Contrast**: 4.5:1 minimum (WCAG 2.1 AA)

### Color Palette
- **Primary**: #1976D2 (Material Design Blue 700)
- **Text**: #212121 (Dark grey, not pure black)
- **Background**: #FFFFFF (White)
- **Accent**: #FFC107 (Material Design Amber 500 for CTAs)

### Call-to-Action Buttons
```html
<table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 20px 0;">
  <tr>
    <td style="border-radius: 4px; background: #1976D2;">
      <a href="{{ACTIVATION_LINK}}" target="_blank" 
         style="display: inline-block; padding: 12px 24px; font-size: 16px; 
                color: #ffffff; text-decoration: none; font-weight: 500;">
        Activate Your Account
      </a>
    </td>
  </tr>
</table>
```

### Plain Text Fallback
Always provide plain text version for email clients that don't support HTML:
- Remove HTML tags
- Convert links to plain URLs: `{{ACTIVATION_LINK}}`
- Use ASCII art for visual separation (e.g., `---`, `===`)

---

## Testing Requirements

### Email Client Compatibility
- Gmail (web, iOS, Android)
- Outlook (desktop, web, mobile)
- Apple Mail (macOS, iOS)
- Thunderbird (desktop)

### Validation Checklist
- [ ] Subject line <50 characters (mobile preview)
- [ ] Preview text provided (first 100 chars of body)
- [ ] All placeholders replaced with test data
- [ ] Links clickable and point to correct URLs
- [ ] Unsubscribe link present (Phase 1+)
- [ ] Logo loads correctly (CDN or embedded base64)
- [ ] Mobile preview at 320px width readable
- [ ] Spam score <5 (use MailTester or similar)

---

## Implementation Notes

### Email Service Provider
**Phase 0**: Azure Communication Services Email (recommended)  
**Alternative**: SendGrid, AWS SES, Mailgun

### Sending Logic
```csharp
// Example: After successful registration
var emailContent = _emailTemplateService.RenderTemplate(
    "activation-email.html",
    new {
        ActorName = registeredActor.FullName,
        ActivationToken = registeredActor.ActivationToken,
        BaseUrl = _configuration["App:BaseUrl"],
        ActivationLink = $"{_configuration["App:BaseUrl"]}/activate?token={registeredActor.ActivationToken}"
    }
);

await _emailService.SendAsync(
    to: registeredActor.Email,
    subject: "Activate Your Innoventity Account",
    htmlContent: emailContent,
    textContent: _emailTemplateService.RenderTemplate("activation-email.txt", templateData)
);
```

### Link Expiration
- **Activation token**: 7 days (168 hours)
- **Token format**: UUID v4 (36 characters with dashes)
- **Expiration check**: Server-side validation on `/activate` endpoint
- **Error message**: "Activation link has expired. Please register again."

---

## Future Enhancements (Phase 1+)

- [ ] Sufficient Bids Notification (when innovation reaches R5.5 threshold)
- [ ] Partner Selection Notification (when owner selects bids)
- [ ] Virtual Incubator Invitation (workspace access granted)
- [ ] Business Plan Progress Updates (milestones reached)
- [ ] Weekly Innovation Digest (personalized recommendations)
- [ ] Email preferences management (unsubscribe, frequency)
- [ ] Localization support (multi-language templates)
