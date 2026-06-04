# Data Ingestion Service

A .NET 8 Web API for ingesting customer transaction data in two modes:

- Real-time ingestion of individual transactions via JSON API.
- Batch ingestion of daily CSV files.

The service validates incoming data, rejects duplicates, stores accepted transactions in PostgreSQL, and exposes query and summary endpoints.

## Tech Stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- CsvHelper
- FluentValidation
- Docker / Docker Compose
- xUnit
- FluentAssertions

## Features

- Single transaction ingestion via JSON.
- Batch CSV ingestion via file upload.
- Row-level validation errors for CSV uploads.
- Deduplication for both real-time and batch ingestion.
- PostgreSQL persistence.
- Paginated and filterable customer transaction query.
- Aggregate ingestion statistics.
- Centralized and consistent error handling.
- Unit tests for important business logic.
- Docker Compose setup for API and database.

## Project Structure

```text
src/
  DataIngestionService.Api/
    Controllers/
      IngestionController.cs
      CustomersController.cs
      StatsController.cs

    Exceptions/
      DuplicateTransactionException.cs

    Mapping/
      TransactionCsvMapper.cs

    Middleware/
      ExceptionHandlingMiddleware.cs

    Models/
      Csv/
      Entities/
      Requests/
      Responses/

    Persistence/
      AppDbContext.cs

    Services/
      Abstractions/
      DeduplicationService.cs
      TransactionIngestionService.cs
      TransactionQueryService.cs
      StatsService.cs

    Validators/
      CreateTransactionRequestValidator.cs

tests/
  DataIngestionService.Tests/
    Helpers/
    Mapping/
    Middleware/
    Services/
    Validators/
```

## How to Run

### Prerequisites

- Docker
- Docker Compose

### Start the application

From the repository root, run:

```bash
docker-compose up --build
```

The API will be available at:

```text
http://localhost:8090/swagger
```

PostgreSQL is started as part of Docker Compose.

### Stop the application

```bash
docker-compose down
```

To also remove the database volume:

```bash
docker-compose down -v
```

## Running Tests

From the repository root, run:

```bash
dotnet test
```

The test suite covers:

- Transaction validation.
- Deduplication hash generation.
- Single transaction ingestion.
- Batch CSV mapping.
- Batch CSV ingestion.
- Duplicate detection.
- Customer transaction querying.
- Statistics aggregation.
- Centralized exception handling middleware.

## API Endpoints

## POST /ingest/transaction

Ingests a single transaction as JSON.

### Example request

```json
{
  "customerId": "CUST-001",
  "transactionDate": "2025-06-01T10:20:00Z",
  "amount": 120.5,
  "currency": "USD",
  "sourceChannel": "Web"
}
```

### Successful response

Status code: `201 Created`

```json
{
  "id": "2cb3f66a-6a34-4f99-8a4f-03228f1d0a63",
  "status": "Accepted"
}
```

### Duplicate response

Status code: `409 Conflict`

```json
{
  "status": 409,
  "title": "Duplicate transaction",
  "detail": "A transaction with the same customer, date, amount, currency and source channel already exists.",
  "errors": null
}
```

### Validation error response

Status code: `400 Bad Request`

```json
{
  "status": 400,
  "title": "Validation failed",
  "detail": "One or more validation errors occurred.",
  "errors": [
    {
      "field": "Amount",
      "message": "'Amount' must be greater than '0'."
    }
  ]
}
```

## POST /ingest/batch

Ingests transactions from a CSV file using `multipart/form-data`.

The endpoint returns a summary with accepted and rejected rows. Invalid rows are rejected with row-level error details.

### Expected CSV format

```csv
CustomerId,TransactionDate,Amount,Currency,SourceChannel
CUST-001,2025-06-01T10:20:00Z,120.50,USD,Web
CUST-002,2025-06-01T11:15:00Z,89.99,EUR,Mobile
```

### Example response

```json
{
  "totalRows": 5,
  "acceptedRows": 2,
  "rejectedRows": 3,
  "errors": [
    {
      "rowNumber": 4,
      "field": "Amount",
      "message": "'Amount' must be greater than '0'."
    },
    {
      "rowNumber": 5,
      "field": null,
      "message": "Duplicate transaction inside the uploaded file."
    },
    {
      "rowNumber": 6,
      "field": "TransactionDate",
      "message": "Transaction date is invalid."
    }
  ]
}
```

## GET /customers/{id}/transactions

Returns paginated transactions for a specific customer.

### Supported query parameters

- `page`
- `pageSize`
- `from`
- `to`
- `currency`
- `sourceChannel`

### Example request

```text
GET /customers/CUST-001/transactions?page=1&pageSize=20&currency=USD&sourceChannel=Web
```

### Example response

```json
{
  "items": [
    {
      "id": "2cb3f66a-6a34-4f99-8a4f-03228f1d0a63",
      "customerId": "CUST-001",
      "transactionDate": "2025-06-01T10:20:00Z",
      "amount": 120.5,
      "currency": "USD",
      "sourceChannel": "Web",
      "createdAtUtc": "2025-06-01T10:21:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

## GET /stats/summary

Returns aggregate statistics about ingested transactions.

### Example response

```json
{
  "totalTransactions": 3,
  "totalCustomers": 2,
  "totalAmountByCurrency": [
    {
      "currency": "EUR",
      "totalAmount": 89.99
    },
    {
      "currency": "USD",
      "totalAmount": 320.5
    }
  ],
  "transactionsBySourceChannel": [
    {
      "sourceChannel": "Web",
      "count": 2
    },
    {
      "sourceChannel": "Mobile",
      "count": 1
    }
  ],
  "latestTransactionDate": "2025-06-01T11:15:00Z"
}
```

## Validation Rules

Each transaction is validated using the following rules:

- `customerId` is required and must not exceed 100 characters.
- `transactionDate` is required and cannot be in the future.
- `amount` is required and must be greater than zero.
- `currency` is required and must be a 3-letter uppercase code.
- `sourceChannel` is required and must not exceed 50 characters.

Before validation and persistence, input values are normalized:

- Leading and trailing spaces are removed from string fields.
- Currency is converted to uppercase.
- Transaction dates are converted to UTC before storage.

## Deduplication Rule

A transaction is considered a duplicate when the following normalized fields are the same:

- Customer identifier
- Transaction date
- Amount
- Currency
- Source channel

The service generates a SHA-256 hash from these fields and stores it as `deduplicationHash`.

A unique index is created on `deduplicationHash` in PostgreSQL. This means duplicate protection is enforced both in application logic and at the database level.

Application-level duplicate checks provide meaningful `409 Conflict` responses. The database-level unique constraint protects against race conditions.

## Batch Processing

The batch endpoint is designed to handle large CSV files efficiently.

The implementation does not read the whole file into memory as one large collection. Instead, it uses streaming CSV reading and processes records one by one.

Valid rows are accumulated and inserted in chunks. Invalid rows are rejected and added to the response with row-level error details.

This approach is intended to support CSV files with up to 100K records while keeping memory usage reasonable.

## Error Handling

The API uses centralized exception handling middleware.

Error responses follow a consistent shape:

```json
{
  "status": 400,
  "title": "Validation failed",
  "detail": "One or more validation errors occurred.",
  "errors": [
    {
      "field": "Amount",
      "message": "'Amount' must be greater than '0'."
    }
  ]
}
```

Handled error types:

- Validation errors return `400 Bad Request`.
- Duplicate transactions return `409 Conflict`.
- Unexpected errors return `500 Internal Server Error`.

Unexpected exception details are logged internally but are not exposed to the client.

## Architecture Description

The service uses a simple layered architecture.

### Controllers

Controllers are responsible for HTTP concerns only:

- Receiving requests.
- Passing data to services.
- Returning HTTP responses.

They do not contain business logic.

### Services

Services contain application logic:

- Transaction ingestion.
- Batch CSV processing.
- Deduplication.
- Querying customer transactions.
- Producing summary statistics.

### Validators

FluentValidation is used for input validation. Validation rules are isolated from controllers and services.

### Persistence

Entity Framework Core is used for data access. PostgreSQL is used as the database.

The `transactions` table has indexes for common query fields and a unique index for the deduplication hash.

### Middleware

A centralized exception handling middleware converts known exceptions into consistent JSON error responses.

## Database Choice

PostgreSQL was chosen because the assignment requires the service and database to run together via Docker Compose, and because the service needs realistic behavior for:

- Persistence.
- Indexes.
- Unique constraints.
- Pagination.
- Filtering.
- Aggregate queries.

SQLite or an in-memory database would make setup simpler, but they would be less representative of a real ingestion service.

PostgreSQL provides production-like behavior while still keeping the local setup simple: one API container and one database container.

## Trade-offs Considered

### Simple layered architecture instead of Clean Architecture or CQRS

I used a straightforward controller-service-persistence structure.

A full Clean Architecture or CQRS setup would also work, but for this assignment it would add more ceremony than value. The current structure keeps the solution easy to review while still separating responsibilities clearly.

### Application-level and database-level deduplication

The service checks for duplicates before saving, which allows it to return a clear `409 Conflict` response.

The database also enforces uniqueness with a unique index on `deduplicationHash`. This protects the system from race conditions and concurrent duplicate inserts.

### Chunked inserts instead of a database-specific bulk copy API

The batch endpoint inserts valid rows in chunks.

For the assignment scale of up to 100K records, this keeps the implementation simple and understandable. For a higher-volume production system, I would consider PostgreSQL-specific bulk insert mechanisms.

### CSV rows parsed as strings first

CSV fields are initially parsed as strings instead of strongly typed properties.

This makes it possible to return meaningful row-level errors for invalid values such as `wrong-date` or `abc` amount, rather than failing the whole file at parsing time.

### Auto migration on startup

The application applies EF Core migrations on startup.

This is convenient for local Docker Compose usage and makes the assignment easier to run. In a production environment, database migrations would usually be handled by a deployment pipeline or a separate migration step.

## What I Would Do Differently With More Time

With more time, I would add:

- Integration tests with Testcontainers and real PostgreSQL.
- A dedicated ingestion history table.
- More detailed audit logging for batch uploads.
- Configurable batch size.
- Correlation IDs for request tracing.
- Structured logs for accepted and rejected records.
- Metrics for ingestion throughput and rejection rate.
- Better handling of very large files with background processing.
- PostgreSQL-specific bulk insert optimization.
- Authentication and authorization.
- More complete OpenAPI examples.
- More advanced CSV schema validation.
- Support for configurable source channel lists.
- Better duplicate reporting for large batch files.

## Local Development Notes

### Useful commands

Build the solution:

```bash
dotnet build
```

Run tests:

```bash
dotnet test
```

Start the full environment:

```bash
docker-compose up --build
```

Stop the environment:

```bash
docker-compose down
```

Reset the database volume:

```bash
docker-compose down -v
```

## Known Assumptions

- Currency is validated as a 3-letter uppercase code, but the service does not verify it against a complete ISO currency list.
- Source channel is required, but the service does not restrict it to a predefined enum.
- A duplicate is defined by the combination of customer identifier, transaction date, amount, currency, and source channel.
- CSV files are expected to contain a header row.
- The expected CSV columns are: `CustomerId`, `TransactionDate`, `Amount`, `Currency`, `SourceChannel`.

## AI Usage

I used ChatGPT and GitHub Copilot to help structure the solution, review architectural trade-offs, generate implementation drafts, and improve README wording.

### Which tools did you use and for what?

I used ChatGPT for:

- Planning the project structure.
- Discussing database choice.
- Improving the README structure and wording.
- Troubleshooting Docker and test issues.

I used GitHub Copilot for:

- Drafting initial endpoint and DTO designs.
- Reviewing validation and deduplication rules.
- Drafting unit test ideas.

### What did you accept as-is, modify, or write from scratch?

I accepted some general suggestions, such as:

- Using a simple layered architecture.
- Using PostgreSQL with Docker Compose.
- Using FluentValidation for request validation.
- Using a SHA-256 hash for deduplication.
- Returning consistent error responses.

I modified the generated code to fit the actual project and fixed implementation details during development.

Examples of changes I made:

- Adjusted the Dockerfile to work with the actual project structure.
- Changed the target framework to `.NET 8`.
- Updated the solution handling because the project used `.slnx`.
- Added PostgreSQL readiness handling in Docker Compose.
- Improved the order of normalization and validation.
- Updated tests when implementation details changed.
- Reviewed and adjusted generated code instead of copying it blindly.

### Did the AI get anything wrong? How did you catch it?

Yes. One issue was the order of normalization and validation.

The service initially validated the raw currency value before trimming and uppercasing it. For example, `" usd "` failed validation because it had 5 characters and was not uppercase. I caught this through a failing unit test and fixed the service so input is normalized before validation.

Another issue was the initial Dockerfile assumption that the project used a `.sln` file. The project actually used `.slnx`, and later the Dockerfile was simplified to restore and publish the API project directly.

I reviewed the generated code, ran tests, verified Docker Compose startup, and fixed issues based on actual build, test, and runtime feedback.

## Submission Notes

The repository contains multiple commits showing the development progression.

The application can be started with:

```bash
docker-compose up --build
```

The API and PostgreSQL database are both started by Docker Compose.
