# GrenRanks

Track and field rankings app with:
- Backend: ASP.NET Core Web API + EF Core + PostgreSQL
- Frontend: React (Vite)
- Import: Hy-Tek CSV upload and history tracking

## Local setup

1. Backend
   - `cd backend/TrackRank.Api`
   - Set connection string in `appsettings.Development.json` (local only)
   - Apply migrations: `dotnet ef database update`
   - Run API: `dotnet run`

2. Frontend
   - `cd frontend/trackrank-web`
   - `npm install`
   - `npm run dev`

3. Tests
   - From repo root: `dotnet test backend/TrackRank.Api.Tests/TrackRank.Api.Tests.csproj`

## Web app URL query parameters

The React app syncs filters, pagination, and sort with the browser URL (shareable links, refresh, Back/Forward). Other query keys are preserved when updating.

**Results**

| Parameter | Meaning |
|-----------|---------|
| `resultsPage` | Results list page (1-based). Omitted when `1`. |
| `resultsAthleteId` | Filter by athlete id. Omitted when empty. |
| `resultsEventId` | Filter by event id. Omitted when empty. |
| `resultsYear` | Filter by year. Omitted when empty. |
| `resultsSourceType` | Filter by source type. Omitted when empty. |
| `resultsSortBy` | Sort field. Omitted when default `resultDate`. |
| `resultsSortDir` | Sort direction (`asc` / `desc`). Omitted when default `desc`. |

**Import history**

| Parameter | Meaning |
|-----------|---------|
| `importHistoryPage` | Hy-Tek import history page (1-based). Omitted when `1`. |

## Environment variables (recommended)

Set secrets via environment variables instead of committing them:

- `Security__AdminApiKey` (required outside Development/Testing for admin endpoints)
- `ConnectionStrings__DefaultConnection` (database connection string)

PowerShell example:

```powershell
$env:Security__AdminApiKey = "your-strong-random-admin-key"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=trackrank;Username=postgres;Password=YOUR_PASSWORD"
```

## Admin endpoint protection

These endpoints require `X-Admin-Key` in non-Development environments:

- `POST /api/seed`
- `POST /api/imports/hytek`

Request header:

```text
X-Admin-Key: <value of Security__AdminApiKey>
```

`GET /api/imports/history` remains accessible without admin key.

## Deployment checklist

1. Set production env vars:
   - `ASPNETCORE_ENVIRONMENT=Production`
   - `Security__AdminApiKey=<strong-key>`
   - `ConnectionStrings__DefaultConnection=<prod-connection>`
2. Apply migrations: `dotnet ef database update`
3. Run API and verify:
   - `POST /api/seed` returns `401` without key
   - `POST /api/imports/hytek` returns `401` without key
   - both succeed with `X-Admin-Key`
   - `GET /api/imports/history` still returns `200`
