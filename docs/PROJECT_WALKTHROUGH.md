# Asset Portfolio Ops: project walkthrough

A developer-level tour of what exists, how it works and how to explain it.
Only implemented behaviour is described; limitations are stated as such.

## 1. System overview

A full-stack demo for investment operations (sample domain: fine wine):
assets, customer portfolios, inventory, purchase requests with a status
workflow, portfolio risk indicators, audit events, **historical asset
valuations** and a **purchase preview** that shows the effect of a purchase
on portfolio concentration before anything is committed.

| Status | Capability |
|---|---|
| Implemented, tested | API and React dashboard for all of the above; EF Core migrations with an upgrade path; Cosmos DB audit store implementation (disabled by default) |
| Implemented, checked manually | The dashboard in a browser against the real API on SQLite (Edge, desktop and 390 px) |
| Demonstration data | All seed data, including the synthetic valuation series |
| Not implemented / not active | Authentication, live deployment (Azure resources decommissioned), real price import, holding history |

## 2. Architecture and structure

```text
apps/api            ASP.NET Core Minimal API (.NET 10)
  Domain/           entities (Asset, Holding, PurchaseRequest, AssetValuation, …)
  Data/             AppDbContext, seed data, ValuationSeedData, DatabaseInitializer, Migrations/
  Features/         one folder per feature: service interface + implementation
    Portfolios/     PortfolioService, PortfolioConcentration (pure)
    Valuations/     ValuationService
    PurchasePreviews/ PurchasePreviewService
    PurchaseRequests/, RiskIndicators/, Inventory/, Assets/, AuditEvents/
  Program.cs        DI registration and endpoint mapping
apps/web            React 19 + TypeScript + Vite
  src/App.tsx, ValuationHistory.tsx, PurchasePreview.tsx, api.ts, format.ts
tests/AssetPortfolioOps.Api.Tests   xUnit: service tests (EF InMemory) and API tests (SQLite)
```

Feature folders keep each use case together; services hold the business
rules and endpoints only translate HTTP.

## 3. Technologies

.NET 10, ASP.NET Core Minimal API, EF Core 10 with SQLite, Swashbuckle,
Azure Cosmos DB SDK (optional audit store), React 19, TypeScript, Vite,
Vitest with React Testing Library, xUnit, GitHub Actions.

## 4. Authentication and authorization

None. The API is open and CORS allows the configured frontend origin
(`http://localhost:5173` locally). This is a documented limitation; the
purchase preview being a read-only `GET` matters here because it cannot
change data even without authorization.

## 5. Persistence

SQLite via EF Core. Tables: Customers, Assets, Holdings, InventoryItems,
PurchaseRequests, AssetValuations (unique per asset and date, cascade on
asset delete). Seed data comes from `HasData`, so it is part of the
migrations. `DatabaseInitializer.Initialize` runs at startup:

1. If the database has an `Assets` table but no `__EFMigrationsHistory`
   (created by the earlier `EnsureCreated` bootstrap), it records
   `InitialCreate` as applied (a baseline).
2. `Database.Migrate()` applies the remaining migrations
   (`AddAssetValuations`).

Audit events are in memory by default or in Cosmos DB when enabled.

## 6. Core workflows

**Valuation history.** `GET /api/assets/{id}/valuations` →
`ValuationService.GetAssetHistoryAsync` filters by optional dates, orders
in memory (SQLite stores decimals as text) and computes first, latest,
change, change %, min and max. `GET …/portfolio/valuation-history` values
the customer's current holdings at each valuation date, carrying the last
price forward, and states that holding history is not recorded.

**Purchase preview.** `GET /api/customers/{id}/purchase-preview` →
`PurchasePreviewService.PreviewAsync`:

1. validates quantity (1–10,000) and the optional price (> 0);
2. reads the customer, the asset, holdings, inventory and pending requests
   with `AsNoTracking`;
3. computes `before` and `after` with `PortfolioConcentration.Calculate`
   (positions at market price; the price paid only affects the cost);
4. adds warnings: concentration rising/falling/staying at Medium or High,
   insufficient inventory, a price more than 10 % from market, pending
   requests not included.

Nothing is written. In the dashboard, **Create purchase request from this
preview** then posts the previewed values through the existing
`PurchaseRequestService` (validation, `Pending` status, audit event).

**Purchase request workflow** (unchanged): `Pending → Approved | Rejected`,
`Approved → Completed`; invalid transitions are rejected and every change
is audited.

## 7. Testing

| Suite | What | Count (local run, 2026-10-30) |
|---|---|---|
| `dotnet test` | service tests, API tests on SQLite, migration tests | 83 |
| `npm test` (apps/web) | component and API client tests | 14 |
| `npm run lint`, `tsc -b`, `npm run build` | static checks and production build | pass |

Key tests for this feature: exact before/after shares (73.75 % → 78.93 %
for 2 Bordeaux; 73.75 % → 62.76 % for 2 Burgundy), a None → Medium
increase, all warning types, validation, "changes nothing" both in the
service and through HTTP, the demonstration series' properties, the
portfolio history ending at the current value, and upgrading an
`EnsureCreated` database without losing rows. CI runs both suites.

## 8. Local development and deployment

```powershell
dotnet run --project apps/api --launch-profile http   # http://localhost:5107
cd apps/web; npm ci; npm run dev                        # http://localhost:5173
```

Everything runs locally without paid services. The Azure deployment
workflows are kept but manually triggered; the earlier resources were
decommissioned.

## 9. Backup and restore

The data is one SQLite file (`asset-portfolio-ops.db` in the API's content
root by default). Back it up by copying the file while the API is stopped
(or with `sqlite3 asset-portfolio-ops.db ".backup backup.db"`), and restore
by putting the copy back. A restored older file is upgraded by the
migrations on the next start. Documented, not automated.

## 10. Design decisions

* **GET for the preview.** The preview is a safe computation, so a `GET`
  states and enforces that it has no side effects; a `POST
  /purchase-previews` would also work but blurs the line with committing.
* **One concentration rule** shared by risk indicators and preview, so the
  dashboard never shows two different answers.
* **Positions valued at market price, cost at the requested price.** The
  portfolio's composition depends on what the holding is worth, not what
  was paid.
* **Synthetic, deterministic valuations** instead of random or scraped
  prices: reproducible tests and screenshots, honest labelling, no external
  dependency or licence issue. Alternative: a price import job — future work.
* **Migrations with a baseline** instead of continuing with `EnsureCreated`,
  which cannot evolve an existing database.

## 11. Known limitations

No authentication; demonstration data only; no holding history (the
portfolio history uses today's quantities); pending requests are listed,
not simulated, in the preview; SQLite for demo persistence; no live
deployment; no browser end-to-end tests.

## 12. Interview preparation: valuations and purchase preview

**Non-technical.** "Before a customer buys more of a wine, an advisor can
see how much of the customer's money would then sit in a single wine, and
whether that crosses our risk limits. Nothing is bought until the advisor
presses a separate button. A price chart shows how each wine's value has
developed, clearly marked as example data."

**Developer.** "The preview is a GET endpoint that reads holdings, inventory
and pending requests without tracking, and runs a pure
`PortfolioConcentration` calculation for the current and the hypothetical
portfolio. The same calculation drives the risk indicator. Valuations are a
new EF entity with a migration and a labelled, deterministic seed series;
the API returns history plus summary statistics."

**Senior / architectural.** "I separated preview from commit at every layer:
a safe HTTP method, a read-only service with no `SaveChanges` or audit
writes, and a UI that only lets you commit the exact values you previewed.
The concentration rule lives in one pure function so business rules cannot
drift between screens. I moved the schema to migrations with a baseline so
existing local databases upgrade instead of breaking, and I was explicit
that the price history is synthetic and that holding history does not
exist, rather than presenting a misleading 'historical portfolio value'."

**Questions and example answers**

1. *Why not reuse `POST /api/purchase-requests` with a "dry run" flag?*
   A flag on a write endpoint makes it easy to commit by mistake and mixes
   audit concerns into a read. A separate GET cannot have side effects.
2. *How do you prove the preview changes nothing?* A service test checks
   the change tracker and every table; an API test compares holdings and
   inventory responses before and after, and that no purchase request or
   audit event exists.
3. *Why is the portfolio history "current holdings at historical prices"?*
   Holdings have no history in this model. Showing it as actual past value
   would be wrong, so the response carries a `basis` text and the UI shows it.
4. *What happens to a developer's existing database?* `DatabaseInitializer`
   detects an `EnsureCreated` database, marks `InitialCreate` as applied and
   runs the newer migrations; a test upgrades such a database and checks
   that its purchase request survives.
5. *What would you change for production?* Authentication and per-advisor
   authorization, a real price feed with provenance, holding history,
   including pending requests in the simulation, and a server-side
   database such as Azure SQL.

**Hands-on exercise.** Start the API and the dashboard. Preview 2 ×
Burgundy for Emma and explain why the concentration falls from High to
Medium (75,000 of 119,500 = 62.76 %). Then preview 2 × Bordeaux and
compute 78.93 % by hand. Add a test that previews 9 Burgundy (the whole
inventory) and asserts no `InsufficientInventory` warning, and one with 10
that asserts it.

## 13. Ownership note

The valuation history, purchase preview, migrations and their tests were
implemented with AI assistance (Claude Code) on the owner's specification,
building on the owner's existing API, dashboard and tests. Verified by the
test runs above, a local run of the API and dashboard against SQLite with
browser checks, and CI.
