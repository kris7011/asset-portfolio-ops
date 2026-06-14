# Asset Portfolio Ops

Asset Portfolio Ops is a cloud-first full-stack demo platform for managing investment assets, customer portfolios, purchase requests, inventory data and audit events.

The project demonstrates a modern full-stack architecture using .NET, React, TypeScript, SQL persistence, Azure Cosmos DB-ready event storage, automated tests and CI/CD pipeline structure.

The current sample domain uses fine wine as the primary asset type, but the model is intentionally generic and can support other collectible or investment assets.

## Purpose

The goal of this project is to demonstrate how a business-oriented full-stack platform can be built with a clear separation between:

* Relational business data
* Event and audit data
* Backend API logic
* Frontend dashboard UI
* Automated testing
* CI/CD infrastructure

## Tech stack

### Backend

* .NET 10
* ASP.NET Core Web API
* C#
* Entity Framework Core
* SQLite for local SQL persistence
* Azure Cosmos DB-ready audit event store
* Swagger/OpenAPI
* xUnit tests

### Frontend

* React
* TypeScript
* Vite
* CSS
* API client abstraction

### Infrastructure

* Azure DevOps pipeline YAML
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
│   └── architecture.md
├── infra/
│   └── azure/
│       └── azure-pipelines.yml
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

For local development, the project uses SQLite through Entity Framework Core.

## Azure Cosmos DB event store

Audit events are handled through an `IAuditEventStore` abstraction.

The project contains two implementations:

* `InMemoryAuditEventStore` for local development
* `CosmosAuditEventStore` for Azure Cosmos DB

Cosmos DB is disabled by default in local development, but the implementation is included and can be enabled through configuration.

This keeps the project easy to run locally while still demonstrating a cloud-first NoSQL event storage design.

## Why SQL and NoSQL are both used

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

The API runs on:

```text
http://localhost:5107
```

Swagger is available at:

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

The frontend runs on:

```text
http://localhost:5173
```

## Build and test

### Backend

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

The repository includes an Azure DevOps pipeline in:

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

## Configuration

Cosmos DB is disabled by default in local development:

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

## What this project demonstrates

This project demonstrates:

* Full-stack development with .NET and React
* API design
* SQL persistence with Entity Framework Core
* NoSQL/event storage with Azure Cosmos DB
* Clean separation between domain logic and infrastructure
* Testable service design
* TypeScript API client patterns
* Local-first development
* Cloud-ready configuration
* CI/CD pipeline structure

## Future improvements

Potential next steps:

* Azure Bicep infrastructure
* Azure Function worker for integration events
* Cosmos DB Change Feed
* GraphQL endpoint for portfolio queries
* Docker Compose setup
* Portfolio risk indicators
* Integration status dashboard
* Frontend tests
* Authentication and authorization
