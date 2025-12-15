using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using RAG.Infrastructure.Data.Entities;

namespace RAG.Infrastructure.Data;

/// <summary>
/// Database context for the RAG application.
/// </summary>
public class RagDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RagDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public RagDbContext(DbContextOptions<RagDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the document chunks.
    /// </summary>
    public DbSet<DocumentChunk> DocumentChunks { get; set; } = null!;

    /// <summary>
    /// Configures the database context options.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        
        // Enable pgvector extension
        optionsBuilder.UseNpgsql(o => o.UseVector());
    }

    /// <summary>
    /// Configures the entity mappings.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Configure DocumentChunk entity
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("document_chunks");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("Id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.DocumentId)
                .HasColumnName("DocumentId")
                .IsRequired();

            entity.Property(e => e.FileName)
                .HasColumnName("FileName")
                .IsRequired();

            entity.Property(e => e.ChunkIndex)
                .HasColumnName("ChunkIndex")
                .IsRequired();

            entity.Property(e => e.Content)
                .HasColumnName("Content")
                .IsRequired();

            entity.Property(e => e.Embedding)
                .HasColumnName("Embedding")
                .HasColumnType("vector(1536)")
                .IsRequired();

            entity.Property(e => e.CreatedAtUtc)
                .HasColumnName("CreatedAtUtc")
                .IsRequired();

            // Create indexes for efficient queries
            entity.HasIndex(e => e.DocumentId)
                .HasDatabaseName("IX_DocumentChunks_DocumentId");

            entity.HasIndex(e => e.FileName)
                .HasDatabaseName("IX_DocumentChunks_FileName");
        });
    }
}
