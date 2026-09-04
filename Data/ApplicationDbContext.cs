using AI_Document_Intelligence.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AI_Document_Intelligence.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<ExtractedDocument> ExtractedDocuments =>
        Set<ExtractedDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");

            entity.HasKey(document => document.Id);

            entity.Property(document => document.FileName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(document => document.ContentType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(document => document.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(document => document.ExtractedText);
        });

        modelBuilder.Entity<ExtractedDocument>(entity =>
        {
            entity.ToTable("ExtractedDocuments");

            entity.HasKey(extractedDocument => extractedDocument.Id);

            entity.Property(extractedDocument => extractedDocument.DocumentType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(extractedDocument => extractedDocument.ExtractedData)
                .IsRequired();

            entity.HasOne(extractedDocument => extractedDocument.Document)
                .WithMany(document => document.ExtractedDocuments)
                .HasForeignKey(extractedDocument => extractedDocument.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
