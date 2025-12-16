[![.NET CI](https://github.com/AmrElsherif83/dotnet-rag-sample/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/AmrElsherif83/dotnet-rag-sample/actions/workflows/dotnet-ci.yml)

# dotnet-rag-sample

A Retrieval-Augmented Generation (RAG) implementation in .NET 8, demonstrating how to build a document Q&A system using PostgreSQL with pgvector for vector storage and OpenAI for embeddings and chat completions.

## 🎯 What This Project Does

This project implements a complete RAG pipeline:

1. **Document Ingestion**: Upload text documents, chunk them intelligently, generate embeddings, and store in PostgreSQL with pgvector
2. **Semantic Search**: Query your documents using natural language - the system finds the most relevant chunks using vector similarity
3. **AI-Powered Answers**: Get accurate answers to questions based on your documents, with citations to source material

### Architecture

```
┌─────────────┐     ┌─────────────┐     ┌─────────────────┐
│   RAG.Api   │────▶│  RAG.Core   │◀────│ RAG.Infrastructure │
│  (Web API)  │     │ (Abstractions│     │  (Implementations) │
└─────────────┘     │  &amp; Services) │     └─────────────────┘
                    └─────────────┘              │
                                                 ▼
                                    ┌─────────────────────┐
                                    │ PostgreSQL + pgvector│
                                    └─────────────────────┘
```

## 📋 Prerequisites

Before you begin, ensure you have the following installed:

- **Docker & Docker Compose** - [Install Docker](https://docs.docker.com/get-docker/)
- **.NET 8 SDK** - [Download .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- **OpenAI API Key** - [Get API Key](https://platform.openai.com/api-keys)

Verify installations:
```bash
docker --version
dotnet --version
```

## 🚀 Getting Started

### 1. Clone the Repository
```bash
git clone https://github.com/AmrElsherif83/dotnet-rag-sample.git
cd dotnet-rag-sample
```

### 2. Configure Environment Variables

Copy the example environment file and add your OpenAI API key:
```bash
cp .env.example .env
```

Edit `.env` and set your values:
```bash
# Required
OPENAI_API_KEY=sk-your-api-key-here

# PostgreSQL (optional - defaults shown)
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=ragdb

# Or use a full connection string
# POSTGRES_CONNECTION_STRING=Host=localhost;Port=5432;Database=ragdb;Username=postgres;Password=postgres
```

### 3. Start PostgreSQL with pgvector
```bash
docker compose up -d
```

Verify the container is running:
```bash
docker compose ps
```

### 4. Apply Database Migrations
```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Apply migrations
dotnet ef database update --project src/RAG.Infrastructure --startup-project src/RAG.Api
```

### 5. Run the API
```bash
cd src/RAG.Api
dotnet run
```

The API will be available at:
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:5001
- **Swagger UI**: https://localhost:5001/swagger

## 🔧 Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `OPENAI_API_KEY` | Your OpenAI API key | Yes |
| `POSTGRES_USER` | PostgreSQL username | No (default: postgres) |
| `POSTGRES_PASSWORD` | PostgreSQL password | No (default: postgres) |
| `POSTGRES_DB` | PostgreSQL database name | No (default: ragdb) |
| `POSTGRES_CONNECTION_STRING` | Full connection string (overrides individual vars) | No |

## 📡 API Endpoints

### POST /api/ingest
Upload and ingest a text or markdown file for RAG processing.

**Request:**
- Content-Type: `multipart/form-data`
- Body: `file` (IFormFile) - A `.txt` or `.md` file

**Example using curl:**
```bash
curl -X POST https://localhost:5001/api/ingest \
  -F "file=@samples/sample-document-1.txt"
```

**Success Response (200 OK):**
```json
{
  "fileName": "sample-document-1.txt",
  "chunksCreated": 5,
  "success": true,
  "errorMessage": null
}
```

**Error Responses:**
- `400 Bad Request` - No file provided or unsupported file type
- `502 Bad Gateway` - Embedding service unavailable

---

### POST /api/ask
Ask a question against the ingested documents.

**Request:**
- Content-Type: `application/json`
- Body:
```json
{
  "question": "What is the return policy?",
  "topK": 5,
  "fileNameFilter": null,
  "documentIdFilter": null
}
```

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `question` | string | Yes | - | The question to ask |
| `topK` | int | No | 5 | Number of relevant chunks to retrieve |
| `fileNameFilter` | string | No | null | Filter by file name (future use) |
| `documentIdFilter` | string | No | null | Filter by document ID (future use) |

**Example using curl:**
```bash
curl -X POST https://localhost:5001/api/ask \
  -H "Content-Type: application/json" \
  -d '{
    "question": "What is the return policy?",
    "topK": 3
  }'
```

**Success Response (200 OK):**
```json
{
  "answer": "All products come with a 30-day money-back guarantee. If you're not satisfied with your purchase, contact our support team within 30 days for a full refund. Digital products are non-refundable once the license key has been activated.",
  "citations": [
    "sample-document-1.txt: All products come with a 30-day money-back guarantee..."
  ]
}
```

**Error Responses:**
- `400 Bad Request` - Question is empty or topK <= 0
- `502 Bad Gateway` - Embedding or chat service unavailable

## 🔐 API Key Authentication (Optional)

The API supports optional API key authentication. When enabled, all requests must include a valid `X-API-KEY` header.

### Enabling Authentication

Set the `API_KEY` environment variable:

```bash
# Linux/macOS
export API_KEY=your-secret-api-key-here

# Windows PowerShell
$env:API_KEY="your-secret-api-key-here"

# Or add to .env file
API_KEY=your-secret-api-key-here
```

### Making Authenticated Requests

Include the `X-API-KEY` header in all requests:

```bash
# Ingest with API key
curl -X POST https://localhost:5001/api/ingest \
  -H "X-API-KEY: your-secret-api-key-here" \
  -F "file=@samples/sample-document-1.txt"

# Ask with API key
curl -X POST https://localhost:5001/api/ask \
  -H "X-API-KEY: your-secret-api-key-here" \
  -H "Content-Type: application/json" \
  -d '{"question": "What is the return policy?", "topK": 3}'
```

### Error Responses

| Status | Description |
|--------|-------------|
| `401 Unauthorized` | API key is missing or invalid |

**Missing API Key Response:**
```json
{
  "error": "Unauthorized",
  "message": "API Key is missing. Include X-API-KEY header in your request."
}
```

**Invalid API Key Response:**
```json
{
  "error": "Unauthorized",
  "message": "Invalid API Key."
}
```

### Notes

- Swagger UI (`/swagger`) is accessible without authentication for development convenience
- If `API_KEY` is not set, authentication is disabled and all requests are allowed
- Use a strong, randomly generated key in production
- Never commit your API key to source control

## 🧪 Running Tests

### Run All Tests
```bash
dotnet test
```

### Run Unit Tests Only
```bash
dotnet test tests/RAG.UnitTests
```

### Run Integration Tests Only
```bash
dotnet test tests/RAG.IntegrationTests
```

### Run Tests with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Test Structure
- **RAG.UnitTests** - Unit tests for core services and chunking logic
  - `TextChunkerTests` - Text chunking algorithms
  - `IngestionServiceTests` - Document ingestion pipeline
  - `RagServiceTests` - RAG query service
- **RAG.IntegrationTests** - API integration tests
  - `ApiTests` - End-to-end HTTP tests

## 🔒 Security & Secrets

### ⚠️ Important: Never Commit Secrets!

This project uses environment variables for all sensitive configuration. **Never commit API keys, passwords, or connection strings to the repository.**

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `OPENAI_API_KEY` | Your OpenAI API key | Yes |
| `POSTGRES_USER` | PostgreSQL username | No (default: postgres) |
| `POSTGRES_PASSWORD` | PostgreSQL password | No (default: postgres) |
| `POSTGRES_DB` | PostgreSQL database name | No (default: ragdb) |
| `POSTGRES_CONNECTION_STRING` | Full connection string (overrides individual vars) | No |

### Setting Up Your Environment

1. Copy the example file:
   ```bash
   cp .env.example .env
   ```

2. Edit `.env` with your actual values (this file is gitignored)

3. For production deployments, use:
   - Azure Key Vault
   - AWS Secrets Manager
   - HashiCorp Vault
   - Kubernetes Secrets

### Security Best Practices

- ✅ Use `.env.example` as a template (contains placeholder values only)
- ✅ The `.env` file is in `.gitignore` and will never be committed
- ✅ Rotate API keys regularly
- ✅ Use least-privilege database credentials
- ❌ Never log sensitive values
- ❌ Never include real credentials in sample files
- ❌ Never commit `.env` or any file containing secrets

### Reporting Security Issues

If you discover a security vulnerability, please open a private security advisory rather than a public issue.

## 📁 Project Structure

```
dotnet-rag-sample/
├── src/
│   ├── RAG.Api/              # ASP.NET Core Web API
│   ├── RAG.Core/             # Domain models, abstractions, and services
│   └── RAG.Infrastructure/   # Data access and external service implementations
├── tests/
│   ├── RAG.UnitTests/        # Unit tests
│   └── RAG.IntegrationTests/ # API integration tests
├── samples/                  # Sample documents for testing
├── docker-compose.yml        # PostgreSQL with pgvector setup
└── .env.example             # Environment variable template
```

## ⚠️ Known Limitations

1. **Single embedding model**: Currently hardcoded to OpenAI's text-embedding-ada-002 (1536 dimensions).
2. **Basic chunking**: The text chunker is simple. Consider using more sophisticated chunking (semantic, recursive) for production.
3. **No streaming**: Chat responses are not streamed. Large responses may timeout.
4. **No document management**: Cannot list, update, or delete documents after ingestion.
5. **Limited file types**: Only accepts `.txt` and `.md` files. No support for PDF, DOCX, or other document formats.

## 🚧 Next Steps / Roadmap

- [x] Add API authentication (JWT/API Keys)
- [ ] Implement document management (list, delete)
- [ ] Add file upload support (PDF, DOCX parsing)
- [ ] Implement streaming responses
- [ ] Add Azure OpenAI support
- [ ] Create Docker container for the API
- [ ] Add health checks and monitoring
- [ ] Implement caching for embeddings
- [ ] Add unit and integration tests
- [ ] Support multiple embedding dimensions

## 📄 License

MIT License - See LICENSE file for details.

## 🤝 Contributing

Contributions are welcome! Please open an issue or submit a pull request.

---

Built with ❤️ using .NET 8, PostgreSQL, pgvector, and OpenAI
