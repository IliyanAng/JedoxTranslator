using Ardalis.Result;
using FluentAssertions;
using JedoxTranslator.Core.Data;
using JedoxTranslator.Core.Dtos;
using JedoxTranslator.Core.Models;
using JedoxTranslator.Core.Repositories;
using JedoxTranslator.Core.Services;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace JedoxTranslator.Tests;

public class TranslationServiceExtendedTests
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
    public async Task UpdateSourceTextAsync_ExistingSid_ShouldUpdateText()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        var updatedSourceText = new SourceText
        {
            SID = "test.sid",
            Text = "Updated English text",
            Translations = new List<Translation>
            {
                new() { Id = 1, SID = "test.sid", LangId = "de-DE", TranslatedText = "German text" }
            }
        };

        repository.UpdateSourceTextAsync("test.sid", "Updated English text")
            .Returns(Result<SourceText>.Success(updatedSourceText));

        var result = await service.UpdateSourceTextAsync("test.sid", "Updated English text");

        result.IsSuccess.Should().BeTrue();
        result.Value.SID.Should().Be("test.sid");
        result.Value.Text.Should().Be("Updated English text");
        result.Value.Translations.Should().HaveCount(1);
        await repository.Received(1).UpdateSourceTextAsync("test.sid", "Updated English text");
    }

    [Fact]
    public async Task UpdateSourceTextAsync_NonExistingSid_ShouldReturnNotFound()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        repository.UpdateSourceTextAsync("nonexistent.sid", "Some text")
            .Returns(Result<SourceText>.NotFound("SID 'nonexistent.sid' not found"));

        var result = await service.UpdateSourceTextAsync("nonexistent.sid", "Some text");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        await repository.Received(1).UpdateSourceTextAsync("nonexistent.sid", "Some text");
    }

    [Fact]
    public async Task DeleteTranslationAsync_ExistingTranslation_ShouldSucceed()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        repository.DeleteTranslationAsync("test.sid", "de-DE")
            .Returns(Result.Success());

        var result = await service.DeleteTranslationAsync("test.sid", "de-DE");

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).DeleteTranslationAsync("test.sid", "de-DE");
    }

    [Fact]
    public async Task DeleteTranslationAsync_NonExistingTranslation_ShouldReturnNotFound()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        repository.DeleteTranslationAsync("test.sid", "nonexistent-lang")
            .Returns(Result.NotFound("Translation for SID 'test.sid' and language 'nonexistent-lang' not found"));

        var result = await service.DeleteTranslationAsync("test.sid", "nonexistent-lang");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task GetAllWithLanguageAsync_EnglishLanguage_ShouldReturnAllSourceTexts()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var result = await service.GetAllWithLanguageAsync("en-US");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().Contain(t => t.SID == "welcome_message");
        result.Value.Should().Contain(t => t.SID == "goodbye_message");
        
        var welcome = result.Value.First(t => t.SID == "welcome_message");
        welcome.Text.Should().Be("Welcome to Jedox Translator");
    }

    [Fact]
    public async Task GetAllWithLanguageAsync_GermanLanguage_ShouldReturnGermanTranslations()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var result = await service.GetAllWithLanguageAsync("de-DE");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        
        var welcome = result.Value.FirstOrDefault(t => t.SID == "welcome_message");
        welcome.Should().NotBeNull();
        welcome!.Text.Should().Be("Willkommen bei Jedox Translator");
    }

    [Fact]
    public async Task GetAllWithLanguageAsync_LanguageWithNoTranslations_ShouldReturnEmpty()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var result = await service.GetAllWithLanguageAsync("ja-JP");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllWithLanguageAsync_PartialTranslations_ShouldOnlyReturnTranslated()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var newSource = new SourceText
        {
            SID = "partially.translated",
            Text = "English only",
            Translations = new List<Translation>()
        };
        context.SourceTexts.Add(newSource);
        await context.SaveChangesAsync();

        var result = await service.GetAllWithLanguageAsync("de-DE");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotContain(t => t.SID == "partially.translated");
    }

    [Fact]
    public async Task CreateSourceTextAsync_EmptyText_ShouldSucceed()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        var sourceText = new SourceText
        {
            SID = "empty.text",
            Text = "",
            Translations = new List<Translation>()
        };

        repository.CreateSourceTextAsync(Arg.Any<SourceText>())
            .Returns(Result<SourceText>.Success(sourceText));

        var dto = new TextDetailDto
        {
            SID = "empty.text",
            Text = ""
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("");
    }

    [Fact]
    public async Task CreateSourceTextAsync_LongSid_ShouldSucceed()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        var longSid = new string('a', 200);
        var sourceText = new SourceText
        {
            SID = longSid,
            Text = "Test",
            Translations = new List<Translation>()
        };

        repository.CreateSourceTextAsync(Arg.Any<SourceText>())
            .Returns(Result<SourceText>.Success(sourceText));

        var dto = new TextDetailDto
        {
            SID = longSid,
            Text = "Test"
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.SID.Should().HaveLength(200);
    }

    [Fact]
    public async Task UpdateTranslationAsync_CreateNewForExistingSid_ShouldSucceed()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        var newTranslation = new Translation
        {
            Id = 10,
            SID = "welcome_message",
            LangId = "es-ES",
            TranslatedText = "Bienvenido"
        };

        repository.UpdateTranslationAsync("welcome_message", "es-ES", "Bienvenido")
            .Returns(Result<Translation>.Success(newTranslation));

        var result = await service.UpdateTranslationAsync("welcome_message", "es-ES", "Bienvenido");

        result.IsSuccess.Should().BeTrue();
        result.Value.LangId.Should().Be("es-ES");
        result.Value.Text.Should().Be("Bienvenido");
    }

    [Fact]
    public async Task GetBySidAsync_WithMultipleTranslations_ShouldReturnAll()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        var sourceText = new SourceText
        {
            SID = "multi.lang",
            Text = "English",
            Translations = new List<Translation>
            {
                new() { Id = 1, SID = "multi.lang", LangId = "de-DE", TranslatedText = "Deutsch" },
                new() { Id = 2, SID = "multi.lang", LangId = "fr-FR", TranslatedText = "Français" },
                new() { Id = 3, SID = "multi.lang", LangId = "es-ES", TranslatedText = "Español" },
                new() { Id = 4, SID = "multi.lang", LangId = "it-IT", TranslatedText = "Italiano" }
            }
        };

        repository.GetBySidAsync("multi.lang")
            .Returns(Result<SourceText>.Success(sourceText));

        var result = await service.GetBySidAsync("multi.lang");

        result.IsSuccess.Should().BeTrue();
        result.Value.Translations.Should().HaveCount(4);
        result.Value.Translations.Select(t => t.LangId).Should().Contain(new[] { "de-DE", "fr-FR", "es-ES", "it-IT" });
    }
}
