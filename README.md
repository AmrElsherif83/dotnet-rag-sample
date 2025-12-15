# dotnet-rag-sample

A .NET 8 RAG (Retrieval-Augmented Generation) sample application using PostgreSQL with pgvector for vector storage.

## Prerequisites

- .NET 8.0 SDK
- Docker and Docker Compose (for PostgreSQL with pgvector)
- EF Core tools (for database migrations)

## Setup

### 1. Start PostgreSQL with pgvector

```bash
docker-compose up -d
```

This starts a PostgreSQL 16 instance with pgvector extension on port 5432 with the following default credentials:
- User: `postgres`
- Password: `postgres`
- Database: `ragdb`

### 2. Install EF Core Tools

If you don't have EF Core tools installed globally:

```bash
dotnet tool install --global dotnet-ef
```

### 3. Create and Apply Database Migrations

From the solution root directory:

```bash
# Create initial migration
dotnet ef migrations add InitialCreate --project src/RAG.Infrastructure --startup-project src/RAG.Api

# Apply migrations to the database
dotnet ef database update --project src/RAG.Infrastructure --startup-project src/RAG.Api
```

The migrations will:
- Create the `document_chunks` table
- Enable the pgvector extension
- Create indexes on `DocumentId` and `FileName` for efficient queries

### 4. Build and Run

```bash
# Build the solution
dotnet build

# Run the API
dotnet run --project src/RAG.Api
```

## Connection String

The default connection string format for PostgreSQL with pgvector:

```
Host=localhost;Port=5432;Database=ragdb;Username=postgres;Password=postgres
```

Configure this in your application's `appsettings.json` or through environment variables.

**Important:** When registering the DbContext in `Program.cs` or `Startup.cs`, make sure to enable pgvector support:

```csharp
builder.Services.AddDbContext<RagDbContext>(options =>
    options.UseNpgsql(connectionString, o => o.UseVector()));
```

## Architecture

- **RAG.Core**: Core abstractions, models, and business logic
- **RAG.Infrastructure**: Data access layer with PostgreSQL and pgvector implementation
- **RAG.Api**: ASP.NET Core Web API

## Features

- Document ingestion with text chunking
- Vector embeddings storage using pgvector
- Similarity search using cosine distance
- PostgreSQL-based persistence
