# Account email configuration

Credentials must be stored in User Secrets or environment variables. Never commit them to `appsettings.json`.

## SMTP placeholders

```powershell
dotnet user-secrets set "AccountEmail:Host" "smtp.example.com"
dotnet user-secrets set "AccountEmail:Port" "587"
dotnet user-secrets set "AccountEmail:UseSsl" "true"
dotnet user-secrets set "AccountEmail:UserName" "SMTP_USERNAME"
dotnet user-secrets set "AccountEmail:Password" "SMTP_PASSWORD"
dotnet user-secrets set "AccountEmail:FromAddress" "no-reply@example.com"
dotnet user-secrets set "AccountEmail:FromName" "Skills Swap"
```

For local development only, `AccountEmail:EnableDevelopmentPreview=true` displays the Identity-generated link in the existing confirmation/reset page. This mode is selected only when the ASP.NET Core environment is Development; production always uses the SMTP implementation.

Real delivery remains **Not Verified** until valid SMTP configuration is supplied and a controlled test email is received.
