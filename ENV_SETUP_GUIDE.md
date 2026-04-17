# Environment Variables Setup Guide

## ✅ Why .env Files?

**.env files are better than appsettings.json for secrets because:**
1. ✅ Single place for all secrets
2. ✅ Never committed to Git (in .gitignore)
3. ✅ Works across different environments
4. ✅ Industry standard (used by Node.js, Python, etc.)
5. ✅ Easy to share template (.env.example)
6. ✅ No JSON syntax errors

## 📁 File Structure

```
PetCare/
├── .env                    # Your actual secrets (gitignored)
├── .env.example            # Template to share (committed to Git)
├── .gitignore             # Blocks .env from being pushed
└── PetCare.API/
    ├── appsettings.json   # Public config (safe to commit)
    └── Program.cs         # Loads .env at startup
```

## 🔧 Setup Steps

### 1. Copy Template
```powershell
# Copy the example to create your real .env file
cd F:\PetCare
Copy-Item .env.example .env
```

### 2. Edit .env with Your Secrets
```powershell
notepad .env
```

Update with your actual credentials:
```env
# Database Connection
SUPABASE_CONNECTION_STRING=Host=your-host;Port=5432;Database=postgres;Username=postgres.xxx;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;Command Timeout=60

# JWT Configuration
JWT_KEY=your_super_secret_key_minimum_32_characters_long
JWT_ISSUER=PetCare.API
JWT_AUDIENCE=PetCare.Client
JWT_EXPIRES_MINUTES=60

# Google OAuth
GOOGLE_CLIENT_ID=your_google_oauth_client_id.apps.googleusercontent.com
```

### 3. Verify .gitignore
Check that `.env` is in `.gitignore`:
```gitignore
.env
.env.local
.env.*.local
```

### 4. Run Your App
```powershell
cd F:\PetCare\PetCare.API
dotnet run
```

The app automatically loads `.env` on startup!

## 🔒 Security Checklist

- [x] `.env` file created with real secrets
- [x] `.env` is in `.gitignore`
- [x] `.env.example` has no real secrets (safe template)
- [x] `appsettings.json` has no secrets (empty strings)
- [ ] Old Supabase password reset (DO THIS!)
- [ ] Git history cleaned of exposed credentials

## 🚀 How It Works

1. **On Startup:** `Program.cs` calls `DotNetEnv.Env.Load()` which reads `.env`
2. **Environment Variables:** All values from `.env` become environment variables
3. **Priority Order:** 
   - Environment variables (from .env) take highest priority
   - Falls back to appsettings.json if env var not found

## 📝 Adding New Secrets

### In .env (your actual values):
```env
NEW_API_KEY=sk_live_actual_secret_key_here
```

### In .env.example (template):
```env
NEW_API_KEY=your_api_key_here
```

### In Program.cs (read it):
```csharp
var apiKey = Environment.GetEnvironmentVariable("NEW_API_KEY") 
    ?? throw new InvalidOperationException("NEW_API_KEY not configured");
```

## 🌍 Production Deployment

For production, **don't use .env files**. Instead, set environment variables directly:

### Azure App Service:
1. Go to Configuration → Application settings
2. Add each variable: `SUPABASE_CONNECTION_STRING`, `JWT_KEY`, `GOOGLE_CLIENT_ID`, etc.

### Docker:
```dockerfile
ENV SUPABASE_CONNECTION_STRING="your-connection-string"
ENV JWT_KEY="your-jwt-key"
```

Or use docker-compose:
```yaml
environment:
  - SUPABASE_CONNECTION_STRING=your-connection-string
  - JWT_KEY=your-jwt-key
```

### Kubernetes:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: petcare-secrets
data:
  SUPABASE_CONNECTION_STRING: base64-encoded-value
  JWT_KEY: base64-encoded-value
```

## ❌ Never Do This:

```json
// ❌ BAD - Secrets in appsettings.json
{
  "ConnectionStrings": {
    "SupabaseConnection": "Host=...;Password=actual-password"
  }
}
```

```csharp
// ❌ BAD - Hardcoded secrets
var password = "iM8kTY9p7GhUkuHj";
```

```powershell
# ❌ BAD - Committing .env to Git
git add .env
git commit -m "Added config"  # NEVER!
```

## ✅ Do This Instead:

```env
# ✅ GOOD - Secrets in .env (gitignored)
SUPABASE_CONNECTION_STRING=Host=...;Password=actual-password
```

```csharp
// ✅ GOOD - Read from environment
var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
```

```powershell
# ✅ GOOD - Only commit the template
git add .env.example
git commit -m "Added config template"
```

## 🆘 Troubleshooting

### App can't find connection string?
1. Check `.env` file exists in project root (same folder as `.sln`)
2. Check variable names match exactly (case-sensitive)
3. Check no spaces around `=` sign
4. Restart the app after editing `.env`

### .env not loading?
```csharp
// Add this at the very start of Program.cs
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load();
    Console.WriteLine("✅ .env loaded successfully");
}
else
{
    Console.WriteLine($"⚠️ .env not found at {envPath}");
}
```

## 📚 Team Collaboration

When a new team member joins:

1. They clone the repo
2. Copy `.env.example` to `.env`
3. Ask team lead for actual secrets
4. Fill in `.env` with real values
5. Run the app

**Never share secrets via:**
- ❌ Email
- ❌ Slack/Teams messages
- ❌ Git commits
- ❌ Screenshots

**Share secrets via:**
- ✅ Password manager (1Password, LastPass, Bitwarden)
- ✅ Secure vault (Azure Key Vault, AWS Secrets Manager)
- ✅ Encrypted message (Signal, encrypted email)

## 🎯 Summary

| File | Purpose | Committed to Git? |
|------|---------|------------------|
| `.env` | Your actual secrets | ❌ No (gitignored) |
| `.env.example` | Template for team | ✅ Yes |
| `appsettings.json` | Public config | ✅ Yes |
| `.gitignore` | Blocks .env | ✅ Yes |

**The Golden Rule:** If it's a secret, it goes in `.env` and never Git!
