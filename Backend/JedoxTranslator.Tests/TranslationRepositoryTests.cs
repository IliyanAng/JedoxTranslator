using Ardalis.Result;
using FluentAssertions;
using JedoxTranslator.Core.Data;
using JedoxTranslator.Core.Models;
using JedoxTranslator.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JedoxTranslator.Tests;

public class TranslationRepositoryTests
{
    private TranslationDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TranslationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TranslationDbContext(options);
        context.Database.EnsureCreated();
        
        // Manually seed data since EnsureCreated doesn't apply HasData
        if (!context.SourceTexts.Any())
        {
            context.SourceTexts.AddRange(
                new SourceText { SID = "welcome_message", Text = "Welcome to Jedox Translator" },
                new SourceText { SID = "goodbye_message", Text = "Goodbye" }
            );
            context.Translations.AddRange(
                new Translation { Id = 1, SID = "welcome_message", LangId = "de-DE", TranslatedText = "Willkommen bei Jedox Translator" },
                new Translation { Id = 2, SID = "goodbye_message", LangId = "de-DE", TranslatedText = "Auf Wiedersehen" }
            );
            context.SaveChanges();
        }
        
        return context;
    }

    [Fact]
    public async Task GetAllSidsAsync_ShouldReturnAllSids()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var result = await repository.GetAllSidsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("welcome_message");
        result.Value.Should().Contain("goodbye_message");
    }

    [Fact]
    public async Task GetBySidAsync_ExistingSid_ShouldReturnSourceText()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var result = await repository.GetBySidAsync("welcome_message");

        result.IsSuccess.Should().BeTrue();
        result.Value.SID.Should().Be("welcome_message");
        result.Value.Text.Should().Be("Welcome to Jedox Translator");
        result.Value.Translations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetBySidAsync_NonExistingSid_ShouldReturnNotFound()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var result = await repository.GetBySidAsync("nonexistent_sid");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Errors.Should().Contain("SID 'nonexistent_sid' not found");
    }

    [Fact]
    public async Task CreateSourceTextAsync_NewSid_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var sourceText = new SourceText
        {
            SID = "test.new.sid",
            Text = "Test text",
            Translations = new List<Translation>()
        };

        var result = await repository.CreateSourceTextAsync(sourceText);

        result.IsSuccess.Should().BeTrue();
        result.Value.SID.Should().Be("test.new.sid");
        result.Value.Text.Should().Be("Test text");

        var retrieved = await repository.GetBySidAsync("test.new.sid");
        retrieved.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateSourceTextAsync_DuplicateSid_ShouldReturnConflict()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var sourceText = new SourceText
        {
            SID = "welcome_message",
            Text = "Duplicate text",
            Translations = new List<Translation>()
        };

        var result = await repository.CreateSourceTextAsync(sourceText);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Errors.Should().Contain("SID 'welcome_message' already exists");
    }

    [Fact]
    public async Task CreateSourceTextAsync_WithTranslations_ShouldCreateAll()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var sourceText = new SourceText
        {
            SID = "test.with.translations",
            Text = "Test text",
            Translations = new List<Translation>
            {
                new() { SID = "test.with.translations", LangId = "de-DE", TranslatedText = "Test Text" },
                new() { SID = "test.with.translations", LangId = "fr-FR", TranslatedText = "Texte de test" }
            }
        };

        var result = await repository.CreateSourceTextAsync(sourceText);

        result.IsSuccess.Should().BeTrue();
        
        var retrieved = await repository.GetBySidAsync("test.with.translations");
        retrieved.Value.Translations.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateTranslationAsync_NewTranslation_ShouldCreate()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var result = await repository.UpdateTranslationAsync("welcome_message", "es-ES", "Bienvenido");

        result.IsSuccess.Should().BeTrue();
        result.Value.LangId.Should().Be("es-ES");
        result.Value.TranslatedText.Should().Be("Bienvenido");

        var sourceText = await repository.GetBySidAsync("welcome_message");
        sourceText.Value.Translations.Should().Contain(t => t.LangId == "es-ES");
    }

    [Fact]
    public async Task UpdateTranslationAsync_ExistingTranslation_ShouldUpdate()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var result = await repository.UpdateTranslationAsync("welcome_message", "de-DE", "Aktualisierter Text");

        result.IsSuccess.Should().BeTrue();
        result.Value.TranslatedText.Should().Be("Aktualisierter Text");

        var sourceText = await repository.GetBySidAsync("welcome_message");
        var translation = sourceText.Value.Translations.First(t => t.LangId == "de-DE");
        translation.TranslatedText.Should().Be("Aktualisierter Text");
    }

    [Fact]
    public async Task UpdateSourceTextAsync_ExistingSid_ShouldUpdate()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var result = await repository.UpdateSourceTextAsync("welcome_message", "Updated welcome message");

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("Updated welcome message");

        var retrieved = await repository.GetBySidAsync("welcome_message");
        retrieved.Value.Text.Should().Be("Updated welcome message");
    }

    [Fact]
    public async Task UpdateSourceTextAsync_NonExistingSid_ShouldReturnNotFound()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var result = await repository.UpdateSourceTextAsync("nonexistent_sid", "Some text");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task DeleteTranslationAsync_ExistingTranslation_ShouldDelete()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var result = await repository.DeleteTranslationAsync("welcome_message", "de-DE");

        result.IsSuccess.Should().BeTrue();

        var sourceText = await repository.GetBySidAsync("welcome_message");
        sourceText.Value.Translations.Should().NotContain(t => t.LangId == "de-DE");
    }

    [Fact]
    public async Task DeleteTranslationAsync_NonExistingTranslation_ShouldReturnNotFound()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var result = await repository.DeleteTranslationAsync("welcome_message", "nonexistent-lang");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task DeleteBySidAsync_ExistingSid_ShouldDeleteSourceTextAndTranslations()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var beforeDelete = await repository.GetBySidAsync("welcome_message");
        beforeDelete.Value.Translations.Should().NotBeEmpty();

        var result = await repository.DeleteBySidAsync("welcome_message");

        result.IsSuccess.Should().BeTrue();

        var afterDelete = await repository.GetBySidAsync("welcome_message");
        afterDelete.IsSuccess.Should().BeFalse();
        afterDelete.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task DeleteBySidAsync_NonExistingSid_ShouldReturnNotFound()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var result = await repository.DeleteBySidAsync("nonexistent_sid");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task CascadeDelete_DeletingSourceText_ShouldDeleteAllTranslations()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var translationCountBefore = context.Translations.Count(t => t.SID == "welcome_message");
        translationCountBefore.Should().BeGreaterThan(0);

        await repository.DeleteBySidAsync("welcome_message");

        var translationCountAfter = context.Translations.Count(t => t.SID == "welcome_message");
        translationCountAfter.Should().Be(0);
    }
}
