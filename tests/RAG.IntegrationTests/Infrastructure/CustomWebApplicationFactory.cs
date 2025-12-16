using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RAG.Core.Abstractions;
using RAG.Infrastructure.Data;

namespace RAG.IntegrationTests.Infrastructure;

/// <summary>
/// Custom web application factory for integration testing.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set required environment variables for testing
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key-for-integration-tests");
        
        // Explicitly clear API_KEY to ensure authentication is disabled for these tests
        Environment.SetEnvironmentVariable("API_KEY", null);
        
        // Also ensure configuration doesn't have ApiKey set
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenAI:ApiKey"] = "test-key-for-integration-tests"
                // Explicitly don't set ApiKey to ensure auth is disabled
            });
        });
        
        builder.ConfigureServices(services =>
        {
            // Remove existing RagDbContext registration if it exists
            var descriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RagDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing (without pgvector)
            services.AddDbContext<RagDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryTestDb");
            });

            // Replace IVectorStore with stub implementation (doesn't require pgvector)
            services.RemoveAll<IVectorStore>();
            services.AddSingleton<IVectorStore, StubVectorStore>();

            // Replace IEmbeddingClient with dummy implementation
            services.RemoveAll<IEmbeddingClient>();
            services.AddSingleton<IEmbeddingClient, DummyEmbeddingClient>();

            // Replace IChatClient with dummy implementation
            services.RemoveAll<IChatClient>();
            services.AddSingleton<IChatClient, DummyChatClient>();
        });
    }
}
