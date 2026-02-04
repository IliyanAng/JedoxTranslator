using JedoxTranslator.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace JedoxTranslator.Core.Data;

public class TranslationDbContext(DbContextOptions<TranslationDbContext> options) : DbContext(options)
{
    public DbSet<SourceText> SourceTexts { get; set; }
    public DbSet<Translation> Translations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SourceText>(entity =>
        {
            entity.HasKey(e => e.SID);
            //TODO: What is the expected length of an SID? Is 200 enough or should we change it? 
            entity.Property(e => e.SID).IsRequired().HasMaxLength(200);
            //TODO: What is the length of a text? Without adding any restrictions it we go up to the maximum allowed , which for posgresql is 1gb.
            //That wouldn't be very optimized and easily can break our DB. 
            entity.Property(e => e.Text).IsRequired();

            entity.HasMany(e => e.Translations)
                .WithOne(t => t.SourceText)
                .HasForeignKey(t => t.SID)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Translation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SID, e.LangId }).IsUnique();
            //TODO: What is the expected length of an SID? Is 200 enough or should we change it? 
            entity.Property(e => e.SID).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LangId).IsRequired().HasMaxLength(10);
            entity.Property(e => e.TranslatedText).IsRequired();
        });

        modelBuilder.Entity<SourceText>().HasData(
            new SourceText { SID = "welcome_message", Text = "Welcome to Jedox Translator" },
            new SourceText { SID = "goodbye_message", Text = "Goodbye" }
        );

        modelBuilder.Entity<Translation>().HasData(
            new Translation { Id = 1, SID = "welcome_message", LangId = "de-DE", TranslatedText = "Willkommen bei Jedox Translator" },
            new Translation { Id = 2, SID = "goodbye_message", LangId = "de-DE", TranslatedText = "Auf Wiedersehen" }
        );
    }
}
