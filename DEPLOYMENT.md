# Deployment Guide (Render + Vercel + Neon)

This guide deploys:

- Backend API (`backend/TrackRank.Api`) to Render
- Frontend (`frontend/trackrank-web`) to Vercel
- PostgreSQL to Neon

## 1) Prerequisites

- Code pushed to GitHub
- A Neon account (free tier)
- A Render account
- A Vercel account

## 2) Create Production Database (Neon)

1. Create a new Neon project.
2. Copy the PostgreSQL connection string.
3. Keep it ready for Render:
   - `ConnectionStrings__DefaultConnection=<your-neon-connection-string>`

## 3) Deploy Backend on Render

1. In Render, create a **Web Service** from your GitHub repo.
2. Set service root to:
   - `backend/TrackRank.Api`
3. Build/start (if prompted):
   - Build command: `dotnet publish -c Release -o out`
   - Start command: `dotnet out/TrackRank.Api.dll`
4. Add environment variables:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `ConnectionStrings__DefaultConnection=<your-neon-connection-string>`
   - `Security__AdminApiKey=<your-strong-admin-key>`
   - `Cors__AllowedOrigins__0=https://<your-vercel-domain>`
5. Deploy and copy the backend URL, for example:
   - `https://your-api.onrender.com`

## 4) Deploy Frontend on Vercel

1. In Vercel, import the same GitHub repo.
2. Set project root to:
   - `frontend/trackrank-web`
3. Build settings:
   - Build command: `npm run build`
   - Output directory: `dist`
4. Add environment variable:
   - `VITE_API_BASE_URL=https://<your-render-backend-domain>`
5. Deploy and copy the frontend URL, for example:
   - `https://your-app.vercel.app`

## 5) Update CORS (if needed)

If your Vercel URL changes (preview/prod), add more allowed origins in Render env vars:

- `Cors__AllowedOrigins__0=https://<prod-domain>`
- `Cors__AllowedOrigins__1=https://<preview-domain>`

Redeploy backend after changes.

## 6) Run Database Migrations in Production

Run migrations against the Neon connection string:

```powershell
$env:ConnectionStrings__DefaultConnection="<your-neon-connection-string>"
dotnet ef database update --project backend/TrackRank.Api
```

If `dotnet ef` is not installed:

```powershell
dotnet tool install --global dotnet-ef
```

## 7) Smoke Test Checklist

1. Open frontend URL.
2. Confirm Rankings page loads publicly.
3. Use Admin login with `Security__AdminApiKey`.
4. Confirm Manual Entry and Results become accessible.
5. Create a result and verify it persists.
6. Export rankings PDF and verify file downloads.

## 8) Security Notes

- Do not commit secrets to `appsettings.json`.
- Prefer environment variables for production secrets.
- Rotate `Security__AdminApiKey` if it was ever exposed in plaintext.

