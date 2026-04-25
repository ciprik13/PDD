---
status: idea
created: 2026-04-25
last-updated: 2026-04-25
---

# Transactional email sending

## Why
Several user-facing flows assume the backend can send email:

- Password reset (the most common reason auth lacks teeth).
- Email confirmation on register.
- Critical-detection alerts (e.g., late blight at high severity → notify the grower's contact email).
- Daily / weekly summary digests.

PRs #1–#6 ship without any email path, which means register/login work but password reset is impossible. Acceptable short-term, blocking medium-term.

## Approach sketch
- New `IEmailSender` in `Application`, simple `SendAsync(to, subject, htmlBody, textBody, ct)` signature.
- Two implementations:
  - **`ConsoleEmailSender`** for dev — writes the email to logs. Identifies issues without an SMTP setup.
  - **Provider implementation** in `Infrastructure` — pick one (recommendation: **Postmark** for transactional, deliverability is excellent and pricing is fair). Alternatives: SendGrid, Amazon SES, Mailgun.
- Templates: Razor templates compiled at startup, stored under `src/AgriCure.Infrastructure/Email/Templates/`. Each template has an HTML and a plain-text variant.
- Background sending via Hangfire — caller enqueues, doesn't wait. Failed sends retry with exponential backoff.
- Configuration: `Email:Provider`, `Email:From`, `Email:ApiKey`, `Email:DefaultLocale`.

## Dependencies / blockers
- PR #3 (Hangfire) — needed for retry/backoff.
- PR #4 (auth) — first real consumer (password reset).
- Decision: which provider. Postmark is the default recommendation; revisit if pricing changes.

## Ready to start when…
- A user attempts password reset for the first time and hits a "feature not available" message — that's the trigger.
- OR detection-alert email is requested by the product.

## Notes
- For local dev, default to ConsoleEmailSender. For staging/prod, set the env var.
- Sender domain must have SPF + DKIM + DMARC configured before going live, or every email lands in spam.
- Bounce / complaint webhooks should be handled — adds entries to the user record so we don't keep emailing dead addresses.
