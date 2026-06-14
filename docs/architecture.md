# Architecture

## Purpose

Asset Portfolio Ops is a cloud-first full-stack demo platform for managing investment assets, customer portfolios, purchase requests, inventory data and audit events.

The project is built to demonstrate how a modern business application can combine a relational database, a NoSQL event store, API endpoints, automated tests and a frontend dashboard.

## Domain

The domain is based on investment assets and portfolio operations.

The current example data uses fine wine as the main asset type, but the model is intentionally generic. The same structure could be used for other collectible or investment assets such as watches, art, whisky or similar high-value products.

The core domain concepts are:

* Customer
* Asset
* Holding
* Portfolio
* PurchaseRequest
* InventoryItem
* AuditEvent

## Backend

The backend is built with .NET 10 and ASP.NET Core.

The API exposes endpoints for:

* Listing assets
* Viewing a customer's portfolio
* Viewing inventory data
* Creating purchase requests
* Reading audit events
* Checking application health

The backend uses a simple service-based structure where each feature has its own service interface and implementation. This keeps the business logic separate from the API endpoint definitions.

## SQL persistence

SQL is used for the core business data because those entities have clear relationships.

Examples:

* A customer can have multiple holdings
* A holding points to an asset
* A purchase request belongs to a customer and an asset
* Inventory items belong to assets

This makes SQL a good fit for data integrity, relationships and querying structured business data.

In local development, the project uses SQLite through Entity Framework Core. The same persistence approach can later be moved to Azure SQL by changing the database provider and connection string.

## Cosmos DB event store

Azure Cosmos DB is used for audit/event data.

Audit events are different from the core business data because they are append-only, event-based and often more flexible in structure.

Examples of audit events:

* Purchase request created
* Purchase request approved
* Inventory sync completed
* Portfolio snapshot created

The application uses an `IAuditEventStore` interface so the business logic does not depend directly on Cosmos DB. Locally, the application uses an in-memory audit event store by default. When Cosmos DB is enabled through configuration, the application uses the Cosmos DB implementation instead.

This makes the solution easy to run locally while still being cloud-ready.

## Why SQL and NoSQL are both used

SQL and NoSQL are used for different reasons.

SQL is used for relational business data where consistency and relationships are important.

Cosmos DB is used for audit and integration events where the data is event-based, append-only and better suited for document storage.

This separation makes the architecture more realistic than storing everything in one database type.

## Local development

The API can run locally without any external Azure dependencies.

By default:

* SQL data is stored in a local SQLite database
* Audit events are stored in memory
* Cosmos DB is disabled through configuration

This makes it simple for another developer or reviewer to clone the repository and run the project.

## Future improvements

Potential future improvements include:

* React/TypeScript frontend dashboard
* Azure DevOps CI/CD pipeline
* Azure Bicep infrastructure
* Cosmos DB Change Feed
* Azure Function worker for integration events
* GraphQL endpoint for portfolio queries
* Docker Compose for local development
* Portfolio risk indicators
* Integration status dashboard
