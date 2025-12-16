using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RAG.Core.Abstractions;
using RAG.Infrastructure.Data;
using Xunit;

namespace RAG.IntegrationTests.Infrastructure;

/// <summary>
/// Custom factory that enables API key authentication for testing.
/// Implements IAsyncLifetime to ensure proper cleanup of environment variables.
/// </summary>
public class AuthEnabledWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set the API key for testing via environment variable BEFORE configuration is built
        Environment.SetEnvironmentVariable("API_KEY", "test-api-key-for-integration-tests");
        Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-openai-key");
        
        // Also add it to configuration to ensure it's picked up
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiKey"] = "test-api-key-for-integration-tests",
                ["OpenAI:ApiKey"] = "test-openai-key"
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
    
    /// <summary>
    /// Initialize the factory (no-op).
    /// </summary>
    public Task InitializeAsync() => Task.CompletedTask;
    
    /// <summary>
    /// Clean up environment variables to prevent pollution of other tests.
    /// </summary>
    public new async Task DisposeAsync()
    {
        try
        {
            // Always clear API_KEY environment variable to prevent pollution
            // Do this first to ensure it happens even if base disposal fails
            Environment.SetEnvironmentVariable("API_KEY", null);
        }
        finally
        {
            await base.DisposeAsync();
        }
    }
}
