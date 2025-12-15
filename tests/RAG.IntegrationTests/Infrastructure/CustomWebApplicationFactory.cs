using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
        builder.ConfigureServices(services =>
        {
            // Remove existing RagDbContext registration if it exists
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RagDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<RagDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryTestDb");
            });

            // Replace IEmbeddingClient with dummy implementation
            services.RemoveAll<IEmbeddingClient>();
            services.AddSingleton<IEmbeddingClient, DummyEmbeddingClient>();

            // Replace IChatClient with dummy implementation
            services.RemoveAll<IChatClient>();
            services.AddSingleton<IChatClient, DummyChatClient>();

            // Build the service provider and ensure database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<RagDbContext>();
            
            // Ensure the database is created
            db.Database.EnsureCreated();
        });
    }
}
