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

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) and Docker Compose
- [OpenAI API Key](https://platform.openai.com/api-keys)

## 🚀 Setup Steps

### 1. Clone the Repository

```bash
git clone https://github.com/AmrElsherif83/dotnet-rag-sample.git
cd dotnet-rag-sample
```

### 2. Configure Environment Variables

```bash
# Copy the example environment file
cp .env.example .env

# Edit .env and add your OpenAI API key
# OPENAI_API_KEY=sk-your-api-key-here
```

### 3. Start PostgreSQL with pgvector

```bash
docker-compose up -d
```

This starts PostgreSQL 16 with the pgvector extension on port 5432.

### 4. Run Database Migrations

```bash
# Install EF Core tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create and apply migrations
dotnet ef migrations add InitialCreate \
  --project src/RAG.Infrastructure \
  --startup-project src/RAG.Api

dotnet ef database update \
  --project src/RAG.Infrastructure \
  --startup-project src/RAG.Api
```

### 5. Run the API

```bash
cd src/RAG.Api
dotnet run
```

The API will be available at `https://localhost:5001` or `http://localhost:5000`.

Swagger UI: `https://localhost:5001/swagger`

## 🔧 Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `OPENAI_API_KEY` | Your OpenAI API key | Yes |
| `POSTGRES_USER` | PostgreSQL username | No (default: postgres) |
| `POSTGRES_PASSWORD` | PostgreSQL password | No (default: postgres) |
| `POSTGRES_DB` | PostgreSQL database name | No (default: ragdb) |

## 📝 How to Test with curl

### Ingest a Document

```bash
# Ingest sample document
curl -X POST https://localhost:5001/api/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "fileName": "company-policies.txt",
    "content": "Our company offers 20 days of paid vacation per year. Employees can carry over up to 5 unused days to the next year. Remote work is allowed 3 days per week."
  }'
```

Expected response:
```json
{
  "fileName": "company-policies.txt",
  "chunksCreated": 1,
  "success": true,
  "errorMessage": null
}
```

### Ask a Question

```bash
curl -X POST https://localhost:5001/api/ask \
  -H "Content-Type: application/json" \
  -d '{
    "question": "How many vacation days do employees get?",
    "topK": 3
  }'
```

Expected response:
```json
{
  "answer": "Employees receive 20 days of paid vacation per year, with the ability to carry over up to 5 unused days to the next year.",
  "citations": [
    "company-policies.txt: Our company offers 20 days of paid vacation per year..."
  ]
}
```

## 🎬 Demo Script (Interview/Presentation)

Use this script to demonstrate the RAG system:

```bash
# Step 1: Verify services are running
docker-compose ps
curl https://localhost:5001/health

# Step 2: Ingest sample documents
curl -X POST https://localhost:5001/api/ingest \
  -H "Content-Type: application/json" \
  -d @samples/sample-document-1.txt

curl -X POST https://localhost:5001/api/ingest \
  -H "Content-Type: application/json" \
  -d @samples/sample-document-2.txt

# Step 3: Ask questions about the documents
curl -X POST https://localhost:5001/api/ask \
  -H "Content-Type: application/json" \
  -d '{"question": "What is the return policy?", "topK": 3}'

curl -X POST https://localhost:5001/api/ask \
  -H "Content-Type: application/json" \
  -d '{"question": "How do I contact support?", "topK": 3}'

# Step 4: Show how it handles questions outside the knowledge base
curl -X POST https://localhost:5001/api/ask \
  -H "Content-Type: application/json" \
  -d '{"question": "What is the capital of France?", "topK": 3}'
```

## 🔒 Security & Publishing Notes

### ⚠️ Important Security Guidelines

1. **Never commit secrets**: The `.env` file is gitignored. Never commit API keys or passwords.
2. **Use environment variables**: All sensitive configuration should come from environment variables or secret managers.
3. **Sample data only**: The `samples/` folder contains only fake, generic documents for demonstration purposes.
4. **No PII**: Do not ingest documents containing personal identifiable information (PII) in demos.
5. **Production deployment**: Use Azure Key Vault, AWS Secrets Manager, or similar for production secrets.

### Publishing Checklist

- [ ] Remove any test API keys
- [ ] Verify `.env` is in `.gitignore`
- [ ] Check no sensitive data in sample documents
- [ ] Review commit history for accidentally committed secrets
- [ ] Use HTTPS in production
- [ ] Configure proper CORS policies
- [ ] Add rate limiting for production

## ⚠️ Known Limitations

1. **No authentication**: The API has no authentication/authorization. Add JWT or API keys for production.
2. **Single embedding model**: Currently hardcoded to OpenAI's text-embedding-ada-002 (1536 dimensions).
3. **Basic chunking**: The text chunker is simple. Consider using more sophisticated chunking (semantic, recursive) for production.
4. **No streaming**: Chat responses are not streamed. Large responses may timeout.
5. **No document management**: Cannot list, update, or delete documents after ingestion.
6. **No file upload**: Only accepts text content, not file uploads (PDF, DOCX, etc.).

## 🚧 Next Steps / Roadmap

- [ ] Add API authentication (JWT/API Keys)
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
