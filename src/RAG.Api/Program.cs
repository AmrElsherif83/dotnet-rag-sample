using Microsoft.EntityFrameworkCore;
using RAG.Core.Abstractions;
using RAG.Core.Services;
using RAG.Infrastructure.Clients;
using RAG.Infrastructure.Configuration;
using RAG.Infrastructure.Data;
using RAG.Infrastructure.Stores;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build connection string from environment variables
var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
if (string.IsNullOrEmpty(connectionString))
{
    var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
    var database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "ragdb";
    var username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
    var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
    connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}

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

// Register core services
builder.Services.AddScoped<IngestionService>();
builder.Services.AddScoped<RagService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Required for integration tests
public partial class Program { }
