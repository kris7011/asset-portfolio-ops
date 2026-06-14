# Asset Portfolio Ops

Asset Portfolio Ops is a cloud-first full-stack demo platform for managing investment assets, customer portfolios, purchase requests, inventory data and audit events.

The project demonstrates a modern full-stack architecture using .NET, React, TypeScript, SQL persistence, an Azure Cosmos DB-ready audit event store, automated tests and cloud deployment.

The current sample domain uses fine wine as the primary asset type, but the model is intentionally generic and can support other collectible or investment assets.

## Live demo

Frontend:

https://lemon-coast-018ec2603.7.azurestaticapps.net

Backend health check:

https://api-asset-portfolio-ops-dqbeesavemezebhs.denmarkeast-01.azurewebsites.net/health

The frontend is deployed to Azure Static Web Apps Free, and the backend API is deployed to Azure App Service Free F1. The project is intentionally configured as a low-cost portfolio demo.

## Dashboard preview

![Asset Portfolio Ops dashboard](docs/images/dashboard.png)

## Purpose

The goal of this project is to demonstrate how a business-oriented full-stack platform can be built with a clear separation between:

* Relational business data
* Event and audit data
* Backend API logic
* Frontend dashboard UI
* Automated testing
* CI/CD infrastructure
* Cloud-ready configuration

The project is intentionally built as a public portfolio project. It is not tied to one specific company, but it demonstrates patterns that are relevant for modern business systems, trading workflows, internal tools, inventory integrations and cloud-first software development.

## Tech stack

### Backend

* .NET 10
* ASP.NET Core Web API
* C#
* Entity Framework Core
* SQLite for local/demo SQL persistence
* Azure Cosmos DB-ready audit event store
* Swagger/OpenAPI
* xUnit tests

### Frontend

* React
* TypeScript
* Vite
* CSS
* TypeScript API client abstraction

### Cloud and DevOps

* Azure Static Web Apps
* Azure App Service
* GitHub Actions
* Azure DevOps pipeline YAML example
* Monorepo structure
* Git-based workflow

## Repository structure

```text
asset-portfolio-ops/
├── apps/
│   ├── api/              # .NET 10 ASP.NET Core API
│   └── web/              # React/TypeScript frontend
├── tests/
│   └── AssetPortfolioOps.Api.Tests/
├── docs/
│   ├── architecture.md
│   └── images/
│       └── dashboard.png
├── infra/
│   └── azure/
│       └── azure-pipelines.yml
├── .github/
│   └── workflows/
│       ├── api-app-service.yml
│       └── azure-static-web-apps-lemon-coast-018ec2603.yml
└── README.md
```

## Domain model

The core domain consists of:

* Customer
* Asset
* Holding
* Portfolio
* PurchaseRequest
* InventoryItem
* AuditEvent

The demo data currently uses investment-grade wine assets such as Bordeaux, Burgundy and Champagne.

The model is intentionally generic. A fine wine asset is just one example of a high-value investment asset. The same structure could be extended to other domains such as watches, art, whisky or similar collectible assets.

## Backend features

The API supports:

* Listing investment assets
* Viewing customer portfolio value
* Viewing inventory status
* Creating purchase requests
* Writing audit events
* Reading audit events
* Health check endpoint

Example endpoints:

```text
GET    /health
GET    /api/assets
GET    /api/assets/{id}
GET    /api/customers/{customerId}/portfolio
GET    /api/inventory
GET    /api/purchase-requests
POST   /api/purchase-requests
GET    /api/audit-events
```

## SQL persistence

The project uses SQL for structured business data where relationships matter.

Examples:

* A customer can have multiple holdings
* A holding references an asset
* A purchase request belongs to a customer and an asset
* Inventory items belong to assets

For local development and demo hosting, the project uses SQLite through Entity Framework Core.

The database is created automatically when the API starts.

## Azure Cosmos DB-ready event store

Audit events are handled through an `IAuditEventStore` abstraction.

The project contains two implementations:

* `InMemoryAuditEventStore` for local/demo development
* `CosmosAuditEventStore` for Azure Cosmos DB

Cosmos DB is disabled by default, but the implementation is included and can be enabled through configuration.

This keeps the project easy to run locally while still demonstrating a cloud-first NoSQL event storage design.

## Why SQL and NoSQL are both used

SQL and NoSQL are used for different responsibilities.

SQL is used for relational business data where consistency, relationships and structured querying are important.

Cosmos DB is used for audit/event data because events are append-only, flexible and well suited for document-based storage.

This separation makes the architecture more realistic than forcing all data into one database type.

## Frontend dashboard

The React dashboard shows:

* Portfolio value
* Portfolio gain/loss
* Asset count
* Inventory units
* Portfolio holdings
* Inventory status
* Purchase requests
* Audit events

The frontend includes a small TypeScript API client, so the React components do not call `fetch` directly or hardcode endpoint logic.

The dashboard also includes a demo action for creating a purchase request. When the user creates a purchase request, the backend validates the request, stores it in SQL and writes an audit event.

## Request flow

A simplified request flow looks like this:

```text
React dashboard
    ↓
POST /api/purchase-requests
    ↓
PurchaseRequestService
    ↓
SQL persistence through Entity Framework Core
    ↓
IAuditEventStore
    ↓
In-memory event store locally or Cosmos DB when enabled
```

This demonstrates a realistic internal operations flow where frontend actions are persisted and audited.

## Cloud deployment

The project is deployed as a low-cost Azure demo.

### Frontend

The frontend is deployed to Azure Static Web Apps Free:

```text
https://lemon-coast-018ec2603.7.azurestaticapps.net
```

The Static Web App is deployed through GitHub Actions.

### Backend

The backend API is deployed to Azure App Service Free F1:

```text
https://api-asset-portfolio-ops-dqbeesavemezebhs.denmarkeast-01.azurewebsites.net
```

Health check endpoint:

```text
https://api-asset-portfolio-ops-dqbeesavemezebhs.denmarkeast-01.azurewebsites.net/health
```

### Current cloud setup

The deployed demo currently uses:

* Azure Static Web Apps for the frontend
* Azure App Service for the backend API
* SQLite/demo persistence
* In-memory audit event store
* Cosmos DB integration disabled by default

This keeps the project cheap to run while still demonstrating cloud-ready architecture.

## Local development

### Prerequisites

* .NET 10 SDK
* Node.js
* npm

### Run backend

From the repository root:

```powershell
dotnet run --project apps/api
```

The API runs locally on:

```text
http://localhost:5107
```

Swagger is available locally at:

```text
http://localhost:5107/swagger
```

### Run frontend

In a second terminal:

```powershell
cd apps/web
npm install
npm run dev
```

The frontend runs locally on:

```text
http://localhost:5173
```

## Frontend environment variables

The frontend uses this environment variable:

```env
VITE_API_BASE_URL=http://localhost:5107
```

Example local environment file:

```text
apps/web/.env.example
```

Production environment file:

```text
apps/web/.env.production
```

The production file points the deployed frontend to the deployed Azure App Service API.

## Build and test

### Backend

From the repository root:

```powershell
dotnet build
dotnet test
```

### Frontend

```powershell
cd apps/web
npm run build
```

## CI/CD

The project contains both GitHub Actions workflows and an Azure DevOps pipeline example.

### GitHub Actions

The deployed demo uses GitHub Actions.

Frontend deployment workflow:

```text
.github/workflows/azure-static-web-apps-lemon-coast-018ec2603.yml
```

Backend deployment workflow:

```text
.github/workflows/api-app-service.yml
```

The frontend workflow deploys the React application to Azure Static Web Apps.

The backend workflow:

* Restores .NET dependencies
* Builds the solution
* Runs tests
* Publishes the API
* Deploys the API to Azure App Service

### Azure DevOps pipeline example

The repository also includes an Azure DevOps pipeline YAML file:

```text
infra/azure/azure-pipelines.yml
```

The pipeline is designed to:

* Install .NET SDK
* Install Node.js
* Restore backend dependencies
* Build backend
* Run backend tests
* Install frontend dependencies
* Build frontend

This is included to demonstrate Azure DevOps CI/CD structure.

## Configuration

Cosmos DB is disabled by default:

```json
"CosmosDb": {
  "Enabled": false,
  "Endpoint": "",
  "Key": "",
  "DatabaseName": "asset-portfolio-ops",
  "AuditEventsContainerName": "audit-events"
}
```

When enabled, the API uses the Cosmos DB implementation of `IAuditEventStore`.

The local and demo setup can run without an Azure Cosmos DB account.

## What this project demonstrates

This project demonstrates:

* Full-stack development with .NET and React
* API design
* SQL persistence with Entity Framework Core
* NoSQL/event storage design with Azure Cosmos DB
* Clean separation between domain logic and infrastructure
* Testable service design
* TypeScript API client patterns
* Local-first development
* Cloud-ready configuration
* GitHub Actions deployment
* Azure App Service deployment
* Azure Static Web Apps deployment
* Azure DevOps pipeline structure
* Monorepo organization
* Business-oriented dashboard design

## How this maps to a modern software developer role

This project is designed to demonstrate experience with technologies and practices commonly used in modern product teams:

| Area                   | Demonstrated by                                                       |
| ---------------------- | --------------------------------------------------------------------- |
| .NET / C#              | ASP.NET Core API and service layer                                    |
| React / TypeScript     | Frontend operations dashboard                                         |
| SQL                    | Entity Framework Core with SQLite locally                             |
| NoSQL                  | Azure Cosmos DB-ready audit event store                               |
| API design             | REST endpoints for assets, portfolio, inventory and purchase requests |
| Cloud-first design     | Configuration-driven cloud deployment                                 |
| Azure                  | Static Web Apps and App Service deployment                            |
| CI/CD                  | GitHub Actions and Azure DevOps YAML                                  |
| Testing                | xUnit backend tests                                                   |
| Monorepo               | API, frontend, tests, docs and infrastructure in one repository       |
| Documentation          | Architecture documentation and README                                 |
| Business understanding | Portfolio, inventory, purchase request and audit workflows            |

## Design decisions

### SQL for core business data

The core business entities have clear relationships. For example, a purchase request belongs to a customer and an asset. This makes SQL a good fit.

### Cosmos DB for events

Audit events are stored separately because they represent a log of what happened. Events are append-only and can evolve over time, which makes document storage a good fit.

### Interface-based audit storage

The backend depends on `IAuditEventStore` instead of directly depending on Cosmos DB. This keeps the business logic independent of the storage implementation and makes the code easier to test.

### Local-first development

The project can run locally without Azure dependencies. This makes it easier for others to clone, review and run the project.

### Low-cost cloud demo

The deployed version uses Azure free-tier-friendly services where possible. This makes it suitable as a public portfolio project without requiring a production-scale cloud setup.

## Limitations

This is a portfolio demo, not a production SaaS system.

Current limitations include:

* No authentication or authorization
* SQLite/demo persistence instead of a production SQL database
* Cosmos DB integration is included but disabled by default
* No frontend test suite yet
* No role-based access control
* Limited validation and error handling
* No real external market pricing integration
* No production observability setup

## Future improvements

Potential next steps:

* Azure SQL Database integration
* Enable Azure Cosmos DB Free Tier for real audit event storage
* Azure Bicep infrastructure
* Azure Function worker for integration events
* Cosmos DB Change Feed
* GraphQL endpoint for portfolio queries
* Docker Compose setup
* Portfolio risk indicators
* Integration status dashboard
* Frontend tests
* Authentication and authorization
* Role-based access for internal users
* More realistic ERP and warehouse sync simulation
