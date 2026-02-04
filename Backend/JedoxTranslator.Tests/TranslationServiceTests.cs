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

public class TranslationServiceTests
{
    private TranslationDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TranslationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TranslationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task CreateSourceTextAsync_ShouldCreateSourceText()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        var sourceText = new SourceText
        {
            SID = "test_sid",
            Text = "Test text",
            Translations = new List<Translation>()
        };

        repository.CreateSourceTextAsync(Arg.Any<SourceText>())
            .Returns(Result<SourceText>.Success(sourceText));

        var dto = new TextDetailDto
        {
            SID = "test_sid",
            Text = "Test text"
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.SID.Should().Be("test_sid");
        result.Value.Text.Should().Be("Test text");
        await repository.Received(1).CreateSourceTextAsync(Arg.Is<SourceText>(st => 
            st.SID == "test_sid" && st.Text == "Test text"));
    }

    [Fact]
    public async Task GetAllSidsAsync_ShouldReturnAllSids()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        var sids = new List<string> { "welcome_message", "goodbye_message" };
        repository.GetAllSidsAsync().Returns(Result<List<string>>.Success(sids));

        var result = await service.GetAllSidsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("welcome_message");
        result.Value.Should().Contain("goodbye_message");
        await repository.Received(1).GetAllSidsAsync();
    }

    [Fact]
    public async Task GetBySidAsync_ShouldReturnTextDetail()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        var sourceText = new SourceText
        {
            SID = "welcome_message",
            Text = "Welcome",
            Translations = new List<Translation>
            {
                new() { Id = 1, SID = "welcome_message", LangId = "de-DE", TranslatedText = "Willkommen" },
                new() { Id = 2, SID = "welcome_message", LangId = "fr-FR", TranslatedText = "Bienvenue" }
            }
        };

        repository.GetBySidAsync("welcome_message")
            .Returns(Result<SourceText>.Success(sourceText));

        var result = await service.GetBySidAsync("welcome_message");

        result.IsSuccess.Should().BeTrue();
        result.Value.SID.Should().Be("welcome_message");
        result.Value.Text.Should().Be("Welcome");
        result.Value.Translations.Should().HaveCount(2);
        result.Value.Translations.Should().Contain(t => t.LangId == "de-DE");
        result.Value.Translations.Should().Contain(t => t.LangId == "fr-FR");
        await repository.Received(1).GetBySidAsync("welcome_message");
    }

    [Fact]
    public async Task UpdateTranslationAsync_ShouldUpdateTranslation()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        var updatedTranslation = new Translation
        {
            Id = 1,
            SID = "welcome_message",
            LangId = "de-DE",
            TranslatedText = "Updated text"
        };

        repository.UpdateTranslationAsync("welcome_message", "de-DE", "Updated text")
            .Returns(Result<Translation>.Success(updatedTranslation));

        var result = await service.UpdateTranslationAsync("welcome_message", "de-DE", "Updated text");

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("Updated text");
        await repository.Received(1).UpdateTranslationAsync("welcome_message", "de-DE", "Updated text");
    }

    [Fact]
    public async Task UpdateTranslationAsync_ShouldReturnNotFoundWhenSidDoesNotExist()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        repository.UpdateTranslationAsync("nonexistent_sid", "de-DE", "Text")
            .Returns(Result<Translation>.NotFound("SID 'nonexistent_sid' not found"));

        var result = await service.UpdateTranslationAsync("nonexistent_sid", "de-DE", "Text");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        await repository.Received(1).UpdateTranslationAsync("nonexistent_sid", "de-DE", "Text");
    }

    [Fact]
    public async Task DeleteBySidAsync_ShouldDeleteSourceTextAndTranslations()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        repository.DeleteBySidAsync("welcome_message").Returns(Result.Success());

        var result = await service.DeleteBySidAsync("welcome_message");

        result.IsSuccess.Should().BeTrue();
        await repository.Received(1).DeleteBySidAsync("welcome_message");
    }

    [Fact]
    public async Task DeleteBySidAsync_ShouldReturnNotFoundWhenSidDoesNotExist()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        repository.DeleteBySidAsync("nonexistent_sid")
            .Returns(Result.NotFound("SID 'nonexistent_sid' not found"));

        var result = await service.DeleteBySidAsync("nonexistent_sid");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
        await repository.Received(1).DeleteBySidAsync("nonexistent_sid");
    }

    [Fact]
    public async Task GetAllWithLanguageAsync_ShouldReturnTranslationsWithFallback()
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
    public async Task GetAllWithLanguageAsync_ShouldFallbackToDefaultText()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var result = await service.GetAllWithLanguageAsync("fr-FR");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        var welcome = result.Value.FirstOrDefault(t => t.SID == "welcome_message");
        welcome.Should().NotBeNull();
        welcome!.Text.Should().Be("Welcome to Jedox Translator");
    }

    [Fact]
    public async Task CreateSourceTextAsync_WithAdditionalTranslations_ShouldCreateAll()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        var sourceText = new SourceText
        {
            SID = "test_sid",
            Text = "Test text",
            Translations = new List<Translation>
            {
                new() { Id = 1, SID = "test_sid", LangId = "de-DE", TranslatedText = "Test Text" },
                new() { Id = 2, SID = "test_sid", LangId = "fr-FR", TranslatedText = "Texte de test" }
            }
        };

        repository.CreateSourceTextAsync(Arg.Any<SourceText>())
            .Returns(Result<SourceText>.Success(sourceText));

        var dto = new TextDetailDto
        {
            SID = "test_sid",
            Text = "Test text",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "Test Text" },
                new() { LangId = "fr-FR", Text = "Texte de test" }
            }
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Translations.Should().HaveCount(2);
        await repository.Received(1).CreateSourceTextAsync(Arg.Is<SourceText>(st => 
            st.SID == "test_sid" && st.Translations.Count == 2));
    }

    [Fact]
    public async Task CreateSourceTextAsync_ShouldReturnConflictWhenSidExists()
    {
        var repository = Substitute.For<ITranslationRepository>();
        var context = GetInMemoryContext();
        var service = new TranslationService(repository, context);

        repository.CreateSourceTextAsync(Arg.Any<SourceText>())
            .Returns(Result<SourceText>.Conflict("SID 'test_sid' already exists"));

        var dto = new TextDetailDto
        {
            SID = "test_sid",
            Text = "Test text"
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Conflict);
        await repository.Received(1).CreateSourceTextAsync(Arg.Any<SourceText>());
    }
}
