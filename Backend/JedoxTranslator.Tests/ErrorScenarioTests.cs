using Ardalis.Result;
using FluentAssertions;
using JedoxTranslator.Core.Data;
using JedoxTranslator.Core.Dtos;
using JedoxTranslator.Core.Models;
using JedoxTranslator.Core.Repositories;
using JedoxTranslator.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace JedoxTranslator.Tests;

public class ErrorScenarioTests
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
    public async Task CreateSourceText_DuplicateSid_ShouldReturnConflictError()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "duplicate.sid",
            Text = "First creation"
        };
        var firstCreate = await service.CreateSourceTextAsync(dto);
        firstCreate.IsSuccess.Should().BeTrue();

        var duplicateDto = new TextDetailDto
        {
            SID = "duplicate.sid",
            Text = "Second creation attempt"
        };
        var secondCreate = await service.CreateSourceTextAsync(duplicateDto);

        secondCreate.IsSuccess.Should().BeFalse();
        secondCreate.Status.Should().Be(ResultStatus.Conflict);
        secondCreate.Errors.Should().Contain(e => e.Contains("already exists"));
    }

    [Fact]
    public async Task GetBySid_AfterDeletion_ShouldReturnNotFound()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "will.be.deleted",
            Text = "Temporary"
        };
        await service.CreateSourceTextAsync(dto);

        await service.DeleteBySidAsync("will.be.deleted");

        var result = await service.GetBySidAsync("will.be.deleted");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task UpdateTranslation_ForNonExistentSid_ShouldCreateTranslationAnyway()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);

        var result = await repository.UpdateTranslationAsync("nonexistent.sid", "de-DE", "Text");

        result.IsSuccess.Should().BeTrue();

        var translation = context.Translations.FirstOrDefault(t => t.SID == "nonexistent.sid");
        translation.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteBySid_MultipleTimes_SecondShouldReturnNotFound()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "delete.twice",
            Text = "Will be deleted"
        };
        await service.CreateSourceTextAsync(dto);

        var firstDelete = await service.DeleteBySidAsync("delete.twice");
        firstDelete.IsSuccess.Should().BeTrue();

        var secondDelete = await service.DeleteBySidAsync("delete.twice");
        secondDelete.IsSuccess.Should().BeFalse();
        secondDelete.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task DeleteTranslation_MultipleTimes_SecondShouldReturnNotFound()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "delete.trans.twice",
            Text = "English",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "Deutsch" }
            }
        };
        await service.CreateSourceTextAsync(dto);

        var firstDelete = await service.DeleteTranslationAsync("delete.trans.twice", "de-DE");
        firstDelete.IsSuccess.Should().BeTrue();

        var secondDelete = await service.DeleteTranslationAsync("delete.trans.twice", "de-DE");
        secondDelete.IsSuccess.Should().BeFalse();
        secondDelete.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task UpdateSourceText_MultipleTimes_ShouldKeepLatestValue()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "multiple.updates",
            Text = "Version 1"
        };
        await service.CreateSourceTextAsync(dto);

        await service.UpdateSourceTextAsync("multiple.updates", "Version 2");
        await service.UpdateSourceTextAsync("multiple.updates", "Version 3");
        await service.UpdateSourceTextAsync("multiple.updates", "Version 4");

        var result = await service.GetBySidAsync("multiple.updates");
        result.Value.Text.Should().Be("Version 4");
    }

    [Fact]
    public async Task CreateSourceText_ThenDeleteTranslation_ThenGetBySid_ShouldShowRemainingTranslations()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "partial.delete",
            Text = "English",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "Deutsch" },
                new() { LangId = "fr-FR", Text = "Français" },
                new() { LangId = "es-ES", Text = "Español" }
            }
        };
        await service.CreateSourceTextAsync(dto);

        await service.DeleteTranslationAsync("partial.delete", "fr-FR");

        var result = await service.GetBySidAsync("partial.delete");
        result.Value.Translations.Should().HaveCount(2);
        result.Value.Translations.Should().Contain(t => t.LangId == "de-DE");
        result.Value.Translations.Should().Contain(t => t.LangId == "es-ES");
        result.Value.Translations.Should().NotContain(t => t.LangId == "fr-FR");
    }

    [Fact]
    public async Task GetAllWithLanguage_AfterDeletingSid_ShouldNotIncludeIt()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "will.disappear",
            Text = "English",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "Deutsch" }
            }
        };
        await service.CreateSourceTextAsync(dto);

        var beforeDelete = await service.GetAllWithLanguageAsync("de-DE");
        beforeDelete.Value.Should().Contain(t => t.SID == "will.disappear");

        await service.DeleteBySidAsync("will.disappear");

        var afterDelete = await service.GetAllWithLanguageAsync("de-DE");
        afterDelete.Value.Should().NotContain(t => t.SID == "will.disappear");
    }

    [Fact]
    public async Task UpdateTranslation_AfterDeletingSid_ShouldRecreateTranslation()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "recreate.test",
            Text = "Original",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "Original DE" }
            }
        };
        await service.CreateSourceTextAsync(dto);

        await service.DeleteBySidAsync("recreate.test");

        var updateResult = await repository.UpdateTranslationAsync("recreate.test", "fr-FR", "New French");

        updateResult.IsSuccess.Should().BeTrue();

        var translationExists = context.Translations.Any(t => t.SID == "recreate.test" && t.LangId == "fr-FR");
        translationExists.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSids_WithNoData_ShouldReturnEmptyList()
    {
        var options = new DbContextOptionsBuilder<TranslationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TranslationDbContext(options);
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var result = await service.GetAllSidsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTranslation_WithVeryLongText_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "long.translation",
            Text = "English"
        };
        await service.CreateSourceTextAsync(dto);

        var longText = new string('x', 10000);
        var result = await service.UpdateTranslationAsync("long.translation", "de-DE", longText);

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().HaveLength(10000);
    }

    [Fact]
    public async Task CreateAndRetrieve_PreservesExactText()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var exactText = "  Exact    spacing   \n\tand\ttabs\r\npreserved  ";
        var dto = new TextDetailDto
        {
            SID = "exact.preservation",
            Text = exactText,
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = exactText }
            }
        };

        await service.CreateSourceTextAsync(dto);

        var result = await service.GetBySidAsync("exact.preservation");
        result.Value.Text.Should().Be(exactText);
        result.Value.Translations.First().Text.Should().Be(exactText);
    }
}
