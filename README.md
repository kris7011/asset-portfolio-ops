# Asset Portfolio Ops

Asset Portfolio Ops is a cloud-first full-stack demo platform for managing investment assets, customer portfolios, purchase requests, inventory data, portfolio risk indicators and audit events.

The project demonstrates a modern full-stack architecture using .NET, React, TypeScript, SQL persistence, an Azure Cosmos DB-ready audit event store, automated tests, GitHub Actions CI and Azure deployment workflows.

The current sample domain uses fine wine as the primary asset type, but the model is intentionally generic and can support other collectible or investment assets.

## Deployment status

There is currently **no live demo**.

The project was previously deployed to Azure Static Web Apps (Free) and Azure App Service (Free F1). Those Azure resources have since been decommissioned, so the old demo URLs no longer respond.

The application runs fully locally (see [Local development](#local-development)). The Azure deployment workflows are kept in the repository as manually triggered references for redeploying to new Azure resources (see [CI/CD](#cicd)).

## Dashboard preview

![Dashboard preview](docs/images/dashboard.png)

Valuation history and the purchase preview (local run, demonstration data):

![Valuation history of a portfolio, labelled as demonstration data](docs/images/valuation-history.png)

![Purchase preview: concentration before and after buying 2 Burgundy](docs/images/purchase-preview.png)

## Purpose

The purpose of this project is to demonstrate how a full-stack application can support operational investment workflows.

The application shows how a business can manage:

* Investment assets
* Customer portfolio value
* Inventory levels
* Portfolio risk indicators
* Purchase requests
* Purchase request status transitions
* Audit events for operational decisions
* Historical asset valuations (clearly labelled demonstration data)
* Previewing a purchase's effect on portfolio concentration before committing it

The project is designed as a public portfolio project, showing practical software engineering skills in a realistic business domain.

## Tech stack

### Backend

* .NET 10
* ASP.NET Core Minimal API
* Entity Framework Core
* SQLite demo persistence
* Azure Cosmos DB-ready audit event store
* xUnit tests
* Swagger/OpenAPI

### Frontend

* React
* TypeScript
* Vite
* CSS
* Environment-based API configuration

### Cloud and DevOps

* GitHub Actions CI (build and test on every push and pull request)
* Manually triggered Azure deployment workflows
* Azure Static Web Apps and Azure App Service (previously deployed)
* Azure DevOps pipeline example
* Cloud-first configuration

## Repository structure

```text
asset-portfolio-ops
├── apps
│   ├── api
│   │   ├── Data
│   │   ├── Domain
│   │   ├── Features
│   │   │   ├── Assets
│   │   │   ├── AuditEvents
│   │   │   ├── Inventory
│   │   │   ├── Portfolios
│   │   │   ├── PurchaseRequests
│   │   │   └── RiskIndicators
│   │   ├── Program.cs
│   │   └── AssetPortfolioOps.Api.csproj
│   │
│   └── web
│       ├── src
│       │   ├── App.tsx
│       │   ├── App.css
│       │   └── api.ts
│       └── package.json
│
├── docs
│   ├── architecture.md
│   └── images
│       └── dashboard.png
│
├── infra
│   └── azure
│       └── azure-pipelines.yml
│
├── tests
│   └── AssetPortfolioOps.Api.Tests
│
├── .github
│   └── workflows
│
├── global.json
├── README.md
└── AssetPortfolioOps.slnx
```

## Domain model

The backend contains a small investment operations domain.

### Assets

Represents an investment asset such as fine wine, whiskey, art, watches or other collectible assets.

Example asset data:

* Bordeaux Premier Cru 2016
* Burgundy Grand Cru 2019
* Vintage Champagne 2012

### Customers

Represents customers with investment portfolios.

### Holdings

Represents the quantity of a specific asset owned by a customer, including average purchase price.

### Inventory

Represents available stock and warehouse location.

### Purchase requests

Represents a customer purchase request for a specific asset.

A purchase request has a status workflow:

```text
Pending → Approved
Pending → Rejected
Approved → Completed
```

Invalid transitions are rejected by the backend.

For example:

```text
Pending → Completed
Rejected → Approved
Completed → Rejected
```

are not allowed.

### Asset valuations

The price of one unit of an asset on a date (`AssetValuation`). The seeded history is a **synthetic, deterministic demonstration series**: 24 month-end prices per asset (October 2024 to September 2026) from a fixed formula, rounded to 50 DKK, ending at the asset's current market price. Every row carries `IsDemonstrationData = true` and the source "Demonstration data (synthetic series, not market prices)", and the API and the dashboard show that label.

### Risk indicators

Represents portfolio and operational warnings based on current data.

The current implementation can detect:

* High portfolio concentration
* Low inventory
* Pending purchase request exposure

This moves the dashboard beyond displaying raw data and into decision support.

### Audit events

Represents important business actions such as:

* Purchase request created
* Purchase request approved
* Purchase request rejected
* Purchase request completed

Audit events are stored behind an interface, so the application can use an in-memory store locally or a Cosmos DB implementation in cloud-ready scenarios.

## Backend features

The backend API supports:

* Reading assets
* Reading inventory
* Reading a customer portfolio
* Reading portfolio risk indicators
* Creating purchase requests
* Approving purchase requests
* Rejecting purchase requests
* Completing purchase requests
* Validating purchase request status transitions
* Writing audit events for purchase request actions
* Reading audit events
* Reading an asset's valuation history, optionally for a date range, with first/latest/change/min/max
* Reading a portfolio's value history (current holdings valued at historical prices)
* Previewing a purchase: concentration before and after, cost, inventory and warnings, without saving anything
* Health check endpoint
* Swagger/OpenAPI in development

## API endpoints

### Health

```http
GET /health
```

Returns basic application health information.

### Assets

```http
GET /api/assets
GET /api/assets/{id}
```

### Asset valuations

```http
GET /api/assets/{id}/valuations?from=2026-01-01&to=2026-09-30
```

`from` and `to` are optional ISO dates. Returns the points in date order and `firstPrice`, `latestPrice`, `changeAmount`, `changePercent`, `minPrice`, `maxPrice`, `isDemonstrationData` and `sources`. `400` if `from` is after `to`, `404` for an unknown asset.

### Portfolio

```http
GET /api/customers/{customerId}/portfolio
GET /api/customers/{customerId}/portfolio/valuation-history
```

The first returns portfolio value, gain/loss and portfolio items for a customer. The second values the customer's **current** holdings at every historical valuation date (carrying the last known price forward). Past holding quantities are not recorded, so it shows how today's portfolio would have been valued, not its actual past value; the response says so in `basis`.

### Purchase preview

```http
GET /api/customers/{customerId}/purchase-preview?assetId={assetId}&quantity=2&requestedPrice=12300
```

Read-only. Returns `isPreview: true`, the concentration `before` and `after` the purchase (every position's share, the largest share and its level), the estimated cost (`requestedPrice`, or the market price when omitted), inventory availability, existing pending requests, and warnings (`ConcentrationIncrease`, `ConcentrationDecrease`, `ConcentrationRemains`, `InsufficientInventory`, `PriceDeviation` above 10 %, `PendingRequests`). It never creates a purchase request, changes holdings or inventory, or writes an audit event. `400` for a quantity outside 1–10,000 or a non-positive price, `404` for an unknown customer or asset.

### Risk indicators

```http
GET /api/customers/{customerId}/risk-indicators
```

Returns decision-support indicators for a customer portfolio.

Example indicator types:

```text
HighConcentration
LowInventory
PendingRequestExposure
NoRiskDetected
```

### Inventory

```http
GET /api/inventory
```

Returns available inventory units and warehouse location.

### Purchase requests

```http
GET /api/purchase-requests
POST /api/purchase-requests
PATCH /api/purchase-requests/{id}/approve
PATCH /api/purchase-requests/{id}/reject
PATCH /api/purchase-requests/{id}/complete
```

### Audit events

```http
GET /api/audit-events
```

Returns audit events in newest-first order.

## Purchase request workflow

The project includes a small business workflow for purchase requests.

A user can create a purchase request from the dashboard. The request starts as `Pending`.

From there, the request can be:

* Approved
* Rejected

An approved request can then be:

* Completed

Each status transition is validated in the backend and written as an audit event.

This demonstrates:

* Backend business rules
* API design
* State transition validation
* Frontend workflow actions
* Audit trail for business decisions
* Automated tests for workflow rules

## Portfolio risk indicators

The project includes a risk indicator service that evaluates customer portfolio data and operational data.

The dashboard currently shows:

Concentration is calculated in one place, `PortfolioConcentration` (`Features/Portfolios`), which both the risk indicator and the purchase preview use, so they always apply the same thresholds (60 % Medium, 70 % High).

### High concentration risk

Detects when one asset represents a high percentage of the total portfolio value.

Example:

```text
Bordeaux Premier Cru 2016 represents 73.7% of the total portfolio value.
```

### Low inventory warning

Detects when an asset in the customer portfolio has low available inventory.

Example:

```text
Burgundy Grand Cru 2019 has only 9 units available.
```

### Pending request exposure

Calculates the total value of pending purchase requests for the customer.

Example:

```text
3 pending purchase request(s) represent 36,900 DKK in potential new exposure.
```

### No risk detected fallback

If no significant indicators are found, the service returns a low-severity informational indicator.

This demonstrates:

* Business logic beyond CRUD
* Data interpretation
* Decision-support functionality
* Backend aggregation logic
* Frontend visualization of risk severity

## SQL persistence

The project uses Entity Framework Core for relational data.

The following entities are stored in the relational model:

* Assets
* Customers
* Holdings
* Inventory items
* Purchase requests
* Asset valuations

The schema is managed with **EF Core migrations** (`apps/api/Data/Migrations`), applied at startup by `DatabaseInitializer`. Earlier versions created the schema with `EnsureCreated`; such a database has the tables of the first migration but no migration history, so the initializer records `InitialCreate` as applied before running the newer migrations. Existing rows are kept (covered by `DatabaseInitializerTests`).

The API uses SQLite as a simple, zero-cost persistence option.

This is suitable for a portfolio demo, but not intended as a production database setup.

In a production version, this would typically be moved to Azure SQL Database or another managed relational database.

## Azure Cosmos DB-ready audit event store

The project includes an Azure Cosmos DB-ready implementation for audit events.

The audit event store is hidden behind this interface:

```csharp
public interface IAuditEventStore
{
    Task AddAsync(AuditEvent auditEvent);
    Task<IReadOnlyCollection<AuditEvent>> GetAllAsync();
}
```

This makes the audit event storage replaceable without changing the purchase request workflow.

Current implementations:

* `InMemoryAuditEventStore`
* `CosmosAuditEventStore`

Cosmos DB is disabled by default to keep the project free to run and simple to start locally.

It can be enabled through configuration:

```json
{
  "CosmosDb": {
    "Enabled": true,
    "Endpoint": "<cosmos-endpoint>",
    "Key": "<cosmos-key>",
    "DatabaseName": "asset-portfolio-ops",
    "AuditEventsContainerName": "audit-events"
  }
}
```

## Why SQL and NoSQL are both used

The project intentionally shows both relational and document-oriented thinking.

### SQL is used for structured operational data

SQL is a good fit for:

* Customers
* Assets
* Holdings
* Inventory
* Purchase requests

These entities have clear relationships and benefit from relational consistency.

### Cosmos DB is used for flexible audit events

Audit events are a good fit for document storage because they may contain different metadata depending on the action.

For example, a created event may include:

```json
{
  "customerId": "...",
  "assetId": "...",
  "quantity": "2",
  "requestedPrice": "12300"
}
```

A status transition event may include:

```json
{
  "previousStatus": "Pending",
  "newStatus": "Approved"
}
```

This makes Cosmos DB a practical fit for event and audit data.

## Frontend dashboard

The React dashboard shows:

* Portfolio value
* Portfolio gain/loss
* Number of assets
* Inventory units
* Portfolio holdings
* Inventory overview
* Risk indicators
* Risk severity badges
* Purchase requests
* Purchase request status badges
* Purchase request workflow actions
* Audit events
* Valuation history: the portfolio (current holdings) or any asset, with a hover readout and a "Demonstration data" label
* Purchase preview: before/after allocation bars, concentration level, cost, inventory and warnings

The dashboard can create demo purchase requests and move them through the supported workflow.

Previewing and committing are separate on purpose: **Preview purchase** only reads, and **Create purchase request from this preview** is a second, explicit action that submits exactly the previewed values. Changing the form after a preview disables the create button until the purchase is previewed again.

Supported frontend actions:

* Create demo purchase request
* Approve pending request
* Reject pending request
* Complete approved request

After each action, the dashboard reloads the latest data from the backend API.

## Request flow

A typical purchase request workflow looks like this:

```text
User clicks "Create demo purchase request"
        ↓
React frontend sends POST request
        ↓
ASP.NET Core API validates input
        ↓
Purchase request is saved
        ↓
Audit event is written
        ↓
Dashboard reloads data
        ↓
User clicks Approve, Reject or Complete
        ↓
Backend validates status transition
        ↓
Status is updated
        ↓
Audit event is written
        ↓
Dashboard reloads with updated status
```

A typical risk indicator flow looks like this:

```text
Dashboard loads customer data
        ↓
React frontend calls risk indicator endpoint
        ↓
ASP.NET Core API evaluates portfolio, inventory and purchase requests
        ↓
Risk indicators are returned
        ↓
Dashboard displays severity badges and messages
```

## Automated tests

The backend has two kinds of xUnit tests:

* **Service tests** exercise the business logic directly against the EF Core in-memory provider.
* **API integration tests** (`tests/AssetPortfolioOps.Api.Tests/Api`) host the real API with `WebApplicationFactory` against an in-memory **SQLite** database. They cover HTTP routing, model binding, status codes, error bodies, JSON serialization and the SQLite provider the app actually uses. Each test gets its own isolated database.

Current test coverage includes:

* Portfolio calculation
* Missing customer portfolio handling
* Purchase request creation
* Purchase request validation
* Audit event creation
* Audit event ordering
* Approving purchase requests
* Rejecting purchase requests
* Completing approved purchase requests
* Rejecting invalid status transitions
* Writing audit events during status changes
* Rejecting every invalid status transition without changing status or writing audit events
* Validation of price, requester, customer and asset on creation
* High concentration risk detection
* Low inventory warning detection
* Pending request exposure severity thresholds
* "No risk detected" fallback
* Missing customer handling for risk indicators
* HTTP status codes (201, 400 with error message, 404) and the full create → approve → complete workflow through the API
* Concentration thresholds and position merging (`PortfolioConcentration`)
* The demonstration valuation series (deterministic, labelled, ending at the market price)
* Valuation history: ordering, summary values, date filtering, invalid ranges, carrying prices forward, portfolio history ending at today's value
* Purchase preview: exact before/after shares (73.75 % → 78.93 % for 2 Bordeaux), rising, falling and unchanged concentration, inventory, price deviation and pending-request warnings, input validation, and that nothing is saved (also through HTTP: no request, no audit event, unchanged holdings and inventory)
* Migrations: a new database, an upgrade of an `EnsureCreated` database that keeps its data, and idempotency

Run tests:

```powershell
dotnet test --configuration Release
```

### Frontend tests

The React dashboard has component and API client tests with Vitest, React Testing Library and jsdom. They stub `fetch` with an in-memory fake API, so no backend is needed.

They cover:

* Rendering portfolio, inventory and risk indicators
* Showing only the actions allowed for each purchase request status
* Approving a request and refreshing the list without hiding the dashboard
* Showing the backend's validation message (`{ "error": "..." }`) when an action is rejected
* Preventing duplicate submissions while a purchase request is being created
* Showing an error when the dashboard cannot be loaded
* Valuation history labelled as demonstration data, and switching between the portfolio and an asset
* Purchase preview with a read-only GET, showing before/after and warnings without any POST
* Creating a purchase request only through the explicit action, with the previewed values, and never from a stale preview
* Showing the API's validation message for a preview

```powershell
cd apps/web
npm test
npm run lint
```

## Local development

### Prerequisites

* .NET 10 SDK
* Node.js
* npm

### Run backend locally

From the repository root:

```powershell
dotnet run --project apps/api --launch-profile http
```

The API listens on `http://localhost:5107`, applies the migrations to `asset-portfolio-ops.db` on startup and exposes Swagger in development. To try it on a separate database file, set `ConnectionStrings__DefaultConnection`, for example `Data Source=C:/temp/apo-demo.db`.

The frontend expects the API base URL to be configured through environment variables.

### Run frontend locally

From the repository root:

```powershell
cd apps/web
npm install
npm run dev
```

Example local frontend environment file:

```env
VITE_API_BASE_URL=http://localhost:5107
```

## Frontend environment variables

The frontend uses `VITE_API_BASE_URL` to decide which backend API to call.

### Local example

```env
VITE_API_BASE_URL=http://localhost:5107
```

### Production example

```env
VITE_API_BASE_URL=https://<your-api>.azurewebsites.net
```

This makes the same frontend code work both locally and in Azure.

## Build and test

From the repository root:

```powershell
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

Build the frontend:

```powershell
cd apps/web
npm run build
```

## CI/CD

Continuous integration and deployment are deliberately separated.

### Continuous integration

Workflow:

```text
.github/workflows/ci.yml
```

Runs on every push to `main` and on every pull request. It needs no secrets and never touches Azure.

* Backend job: restores, builds and tests the .NET solution (test results are uploaded as a workflow artifact)
* Frontend job: installs dependencies with `npm ci`, then runs ESLint, the Vitest test suite and the production build

### Backend deployment (manual)

Workflow:

```text
.github/workflows/api-app-service.yml
```

Triggered manually with `workflow_dispatch`. It builds, tests, publishes and deploys the API to Azure App Service using the `AZURE_WEBAPP_PUBLISH_PROFILE` secret.

### Frontend deployment (manual)

Workflow:

```text
.github/workflows/azure-static-web-apps-lemon-coast-018ec2603.yml
```

Triggered manually with `workflow_dispatch`. It takes the backend API base URL as an input, builds the React app and deploys it to Azure Static Web Apps.

Both deployment workflows previously ran automatically on every push (and the frontend workflow also created preview environments for pull requests). Because the original Azure resources have been decommissioned, they now only run on demand and require new Azure resources and secrets to succeed.

### Azure DevOps pipeline example

The repository also contains an Azure DevOps pipeline example:

```text
infra/azure/azure-pipelines.yml
```

This is included to demonstrate Azure DevOps CI/CD structure.

GitHub Actions is the primary CI/CD system for this repository.

## Configuration

Backend configuration is handled through standard ASP.NET Core configuration files and environment variables.

Example development configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=asset-portfolio-ops.db"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173"
    ]
  },
  "CosmosDb": {
    "Enabled": false,
    "Endpoint": "",
    "Key": "",
    "DatabaseName": "asset-portfolio-ops",
    "AuditEventsContainerName": "audit-events"
  }
}
```

`appsettings.Production.json` contains the CORS origin of the former Azure Static Web Apps frontend. Update it when redeploying to a new frontend URL.

## What this project demonstrates

This project demonstrates practical experience with:

* C# and .NET backend development
* ASP.NET Core Minimal API
* REST API design
* Entity Framework Core
* SQL-based relational modeling
* Business workflow implementation
* Status transition validation
* Portfolio risk indicator logic
* Decision-support functionality
* Audit event tracking
* Azure Cosmos DB-ready architecture
* React and TypeScript frontend development
* Frontend API integration
* Cloud-first configuration
* GitHub Actions CI separated from deployment
* Azure App Service deployment workflow
* Azure Static Web Apps deployment workflow
* Automated backend tests
* Clean separation between domain, data and feature logic

## How this maps to a modern software developer role

This project is relevant to roles involving:

* .NET backend development
* Full-stack development
* Cloud-first product development
* API design
* Azure deployment
* CI/CD pipelines
* Business-critical internal tools
* Operational dashboards
* Integration-heavy systems
* Auditability and traceability
* Data-driven decision support

It shows how a small product feature can be built end-to-end:

```text
Domain model
    ↓
Database persistence
    ↓
Business logic
    ↓
API endpoints
    ↓
Frontend integration
    ↓
Cloud deployment
    ↓
Automated tests
```

## Design decisions

### Minimal API

The backend uses ASP.NET Core Minimal API to keep the API simple and readable.

### Feature folders

The backend is organized by feature area rather than technical layer only.

Examples:

* Assets
* Inventory
* Portfolios
* PurchaseRequests
* RiskIndicators
* AuditEvents

This makes the project easier to navigate as it grows.

### Business rules in backend

Purchase request status transitions are validated in the backend.

The frontend only exposes available actions, but the backend remains the source of truth.

### Risk indicators as backend logic

Risk indicators are calculated in the backend because they are business rules, not only visual UI logic.

The frontend displays the result, while the backend owns the interpretation of portfolio and operational data.

### Interface-based audit event store

Audit storage is abstracted behind an interface, making it easy to switch between in-memory storage and Cosmos DB.

### Low-cost cloud demo

The project was deployed using free-tier Azure services to demonstrate architecture, deployment and working software without unnecessary cloud costs.

Those resources have been decommissioned. CI runs without any cloud dependency, and deployment is an explicit manual step.

## Limitations

This is a portfolio demo and not a production SaaS product.

Current limitations:

* No authentication or authorization
* SQLite is used for demo persistence
* No live deployment at the moment (Azure resources decommissioned)
* Cosmos DB is implemented but disabled by default
* No Azure SQL production database
* No pagination
* No end-to-end browser tests
* No user management
* No role-based access control
* No advanced observability setup
* Demo data is seeded; the valuation history is a synthetic demonstration series, not market data, and there is no price import
* Holding history is not recorded, so the portfolio value history values today's holdings at past prices
* The purchase preview does not include other pending requests in the after state (it lists them as a warning instead)
* Risk indicator thresholds are simple demo rules

## Future improvements

Potential future improvements include:

* More advanced portfolio risk models
* Importing real valuations from a price source, and recording holding history
* Filtering purchase requests by status
* Filtering assets by type or region
* Authentication and role-based access
* Azure SQL production persistence
* Enabling Cosmos DB audit event storage
* Application Insights
* Pagination and sorting
* More detailed audit event metadata
* Separate admin and customer views

## Status

The project is not currently deployed. It runs locally, and CI builds and tests it on every push and pull request.

Current implemented features:

* Backend API
* React dashboard
* SQL persistence
* Portfolio risk indicators
* Purchase request workflow
* Audit event tracking
* Historical asset valuations (demonstration data) and portfolio value history
* Read-only purchase preview with before/after concentration
* EF Core migrations with an upgrade path for existing databases
* Automated backend tests
* Frontend component tests
* GitHub Actions CI (build and test)
* Manually triggered deployment workflows for Azure Static Web Apps and Azure App Service
