using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RAG.Api.Middleware;
using RAG.Core.Abstractions;
using RAG.Core.Services;
using RAG.Infrastructure.Clients;
using RAG.Infrastructure.Configuration;
using RAG.Infrastructure.Data;
using RAG.Infrastructure.Stores;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// VALIDATE REQUIRED ENVIRONMENT VARIABLES
// ============================================

// Validate OpenAI API Key
var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? builder.Configuration["OpenAI:ApiKey"];

if (string.IsNullOrWhiteSpace(openAiApiKey))
{
    throw new InvalidOperationException(
        "❌ OPENAI_API_KEY is required but not configured.\n" +
        "   Set it using one of these methods:\n" +
        "   1. Environment variable: export OPENAI_API_KEY=sk-your-key\n" +
        "   2. User secrets: dotnet user-secrets set \"OpenAI:ApiKey\" \"sk-your-key\"\n" +
        "   3. See .env.example for reference");
}

// Validate PostgreSQL connection
var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

if (string.IsNullOrEmpty(connectionString))
{
    var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
    var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "ragdb";
    var username = Environment.GetEnvironmentVariable("POSTGRES_USER");
    var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

    // In development, allow defaults; in production, require explicit configuration
    if (builder.Environment.IsProduction())
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "❌ PostgreSQL credentials are required in production.\n" +
                "   Set POSTGRES_CONNECTION_STRING or individual variables:\n" +
                "   - POSTGRES_USER\n" +
                "   - POSTGRES_PASSWORD\n" +
                "   - POSTGRES_DB (optional, default: ragdb)\n" +
                "   - POSTGRES_HOST (optional, default: localhost)\n" +
                "   - POSTGRES_PORT (optional, default: 5432)");
        }
        
        // Prevent insecure default credentials in production
        if (username.Equals("postgres", StringComparison.OrdinalIgnoreCase) && password == "postgres")
        {
            throw new InvalidOperationException(
                "❌ Default PostgreSQL credentials (postgres/postgres) are not allowed in production.\n" +
                "   Set secure credentials using POSTGRES_USER and POSTGRES_PASSWORD.");
        }
    }

    // Use defaults for development if not set
    username ??= "postgres";
    password ??= "postgres";

    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}

Console.WriteLine("✅ Environment configuration validated successfully");

// ============================================
// CONFIGURE SERVICES
// ============================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DbContext with pgvector support
builder.Services.AddDbContext<RagDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.UseVector()));

// Configure OpenAI options
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));

// Register HttpClient for OpenAI
builder.Services.AddHttpClient("OpenAI");

// Register infrastructure services
builder.Services.AddScoped<IVectorStore, PgVectorStore>();
builder.Services.AddScoped<IEmbeddingClient, OpenAiEmbeddingClient>();
builder.Services.AddScoped<IChatClient, OpenAiChatClient>();

// Register core services with logging
builder.Services.AddScoped<IngestionService>();
builder.Services.AddScoped<RagService>(sp =>
{
    var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
    var logger = sp.GetRequiredService<ILogger<RagService>>();
    return new RagService(
        sp.GetRequiredService<IEmbeddingClient>(),
        sp.GetRequiredService<IChatClient>(),
        sp.GetRequiredService<IVectorStore>(),
        options.Temperature,
        options.MaxTokens,
        logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Optional API Key Authentication
// Only enable if API_KEY environment variable or ApiKey config is set
var apiKey = Environment.GetEnvironmentVariable("API_KEY") 
    ?? builder.Configuration["ApiKey"];

if (!string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("🔐 API Key authentication is ENABLED");
    Console.WriteLine("   All requests must include X-API-KEY header");
    app.UseMiddleware<ApiKeyMiddleware>();
}
else
{
    Console.WriteLine("⚠️  API Key authentication is DISABLED");
    Console.WriteLine("   Set API_KEY environment variable to enable");
}

app.UseAuthorization();
app.MapControllers();

app.Run();

// Required for integration tests
public partial class Program { }
