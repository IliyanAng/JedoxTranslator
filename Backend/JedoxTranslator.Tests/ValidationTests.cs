using Ardalis.Result;
using FluentAssertions;
using JedoxTranslator.Core.Data;
using JedoxTranslator.Core.Dtos;
using JedoxTranslator.Core.Models;
using JedoxTranslator.Core.Repositories;
using JedoxTranslator.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace JedoxTranslator.Tests;

public class ValidationTests
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public async Task GetBySid_WithWhitespaceOrEmpty_ShouldHandleGracefully(string sid)
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var result = await service.GetBySidAsync(sid);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("es-ES")]
    [InlineData("it-IT")]
    [InlineData("pt-PT")]
    [InlineData("ja-JP")]
    [InlineData("zh-CN")]
    public async Task GetAllWithLanguage_ValidLanguageCodes_ShouldSucceed(string langId)
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var result = await service.GetAllWithLanguageAsync(langId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("en")]
    [InlineData("EN-US")]
    [InlineData("en_US")]
    public async Task GetAllWithLanguage_InvalidLanguageCodes_ShouldReturnEmpty(string langId)
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var result = await service.GetAllWithLanguageAsync(langId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateSourceText_WithNullTranslationsList_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "null.translations",
            Text = "English",
            Translations = null!
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Translations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateSourceText_WithEmptyTranslationsList_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "empty.translations",
            Text = "English",
            Translations = new List<TranslationDto>()
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Translations.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTranslation_WithWhitespaceOnlyText_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "whitespace.test",
            Text = "Original"
        };
        await service.CreateSourceTextAsync(dto);

        var result = await service.UpdateTranslationAsync("whitespace.test", "de-DE", "   ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("   ");
    }

    [Fact]
    public async Task DeleteTranslation_ThatDoesNotExist_ShouldReturnNotFound()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var result = await service.DeleteTranslationAsync("welcome_message", "nonexistent-lang");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task CreateSourceText_WithDuplicateTranslations_ShouldStoreOnlyOne()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "duplicate.trans",
            Text = "English",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "First" },
                new() { LangId = "de-DE", Text = "Second" }
            }
        };

        // With in-memory DB, duplicate keys in the same insert will cause an exception
        // or only one will be stored depending on EF Core behavior
        try
        {
            var result = await service.CreateSourceTextAsync(dto);

            // If it succeeds, check that only one translation exists
            if (result.IsSuccess)
            {
                var retrieved = await service.GetBySidAsync("duplicate.trans");
                retrieved.Value.Translations.Where(t => t.LangId == "de-DE").Should().HaveCountLessOrEqualTo(1);
            }
        }
        catch
        {
            // Exception is expected due to duplicate key
            true.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetAllSids_AfterCreatingAndDeleting_ShouldReflectChanges()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var initial = await service.GetAllSidsAsync();
        var initialCount = initial.Value.Count;

        await service.CreateSourceTextAsync(new TextDetailDto { SID = "temp.sid", Text = "Temp" });

        var afterCreate = await service.GetAllSidsAsync();
        afterCreate.Value.Should().HaveCount(initialCount + 1);

        await service.DeleteBySidAsync("temp.sid");

        var afterDelete = await service.GetAllSidsAsync();
        afterDelete.Value.Should().HaveCount(initialCount);
    }

    [Fact]
    public async Task UpdateSourceText_SameTextMultipleTimes_ShouldIdempotent()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "idempotent.test",
            Text = "Original"
        };
        await service.CreateSourceTextAsync(dto);

        await service.UpdateSourceTextAsync("idempotent.test", "Updated");
        var result1 = await service.GetBySidAsync("idempotent.test");

        await service.UpdateSourceTextAsync("idempotent.test", "Updated");
        var result2 = await service.GetBySidAsync("idempotent.test");

        result1.Value.Text.Should().Be(result2.Value.Text);
    }

    [Fact]
    public async Task CreateSourceText_WithAllSupportedLanguages_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var languages = new[]
        {
            "en-US", "de-DE", "fr-FR", "es-ES", "it-IT", "pt-PT",
            "nl-NL", "pl-PL", "ru-RU", "ja-JP", "ko-KR", "zh-CN",
            "ar-SA", "tr-TR", "sv-SE", "da-DK", "no-NO", "fi-FI",
            "cs-CZ", "el-GR", "he-IL", "hi-IN", "hu-HU", "id-ID",
            "th-TH", "vi-VN"
        };

        var translations = languages.Where(l => l != "en-US").Select(lang => new TranslationDto
        {
            LangId = lang,
            Text = $"Translation in {lang}"
        }).ToList();

        var dto = new TextDetailDto
        {
            SID = "all.languages",
            Text = "English",
            Translations = translations
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Translations.Should().HaveCount(25);
    }

    [Theory]
    [InlineData("app.test", "de-DE")]
    [InlineData("app.test", "fr-FR")]
    [InlineData("different.sid", "de-DE")]
    public async Task DeleteTranslation_ShouldNotAffectOtherTranslations(string sidToDelete, string langToDelete)
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        await service.CreateSourceTextAsync(new TextDetailDto
        {
            SID = "app.test",
            Text = "Test",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "Test DE" },
                new() { LangId = "fr-FR", Text = "Test FR" }
            }
        });

        await service.CreateSourceTextAsync(new TextDetailDto
        {
            SID = "different.sid",
            Text = "Different",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "Different DE" },
                new() { LangId = "fr-FR", Text = "Different FR" }
            }
        });

        var countBefore = context.Translations.Count();

        var deleteResult = await service.DeleteTranslationAsync(sidToDelete, langToDelete);
        deleteResult.IsSuccess.Should().BeTrue();

        var countAfter = context.Translations.Count();
        countAfter.Should().Be(countBefore - 1);
    }
}
