using FluentAssertions;
using JedoxTranslator.Core.Data;
using JedoxTranslator.Core.Dtos;
using JedoxTranslator.Core.Models;
using JedoxTranslator.Core.Repositories;
using JedoxTranslator.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace JedoxTranslator.Tests;

public class EdgeCaseTests
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
    public async Task CreateSourceText_WithSpecialCharacters_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "special.chars.test",
            Text = "Text with special chars: <>&\"'@#$%^&*()"
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Contain("<>&\"'@#$%^&*()");
    }

    [Fact]
    public async Task CreateSourceText_WithUnicodeCharacters_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "unicode.test",
            Text = "Unicode: 你好世界 مرحبا بالعالم こんにちは世界 🎉🚀✨"
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Contain("你好世界");
        result.Value.Text.Should().Contain("مرحبا");
        result.Value.Text.Should().Contain("🎉");
    }

    [Fact]
    public async Task CreateSourceText_WithVeryLongText_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var longText = new string('x', 5000);
        var dto = new TextDetailDto
        {
            SID = "long.text.test",
            Text = longText
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().HaveLength(5000);
    }

    [Fact]
    public async Task UpdateTranslation_WithEmptyText_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var createDto = new TextDetailDto
        {
            SID = "empty.update.test",
            Text = "Original"
        };
        await service.CreateSourceTextAsync(createDto);

        var result = await service.UpdateTranslationAsync("empty.update.test", "de-DE", "");

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("");
    }

    [Fact]
    public async Task GetBySid_WithNoTranslations_ShouldReturnEmptyList()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "no.translations",
            Text = "Only English"
        };
        await service.CreateSourceTextAsync(dto);

        var result = await service.GetBySidAsync("no.translations");

        result.IsSuccess.Should().BeTrue();
        result.Value.Translations.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateSourceText_ToEmptyString_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "empty.source.test",
            Text = "Original text"
        };
        await service.CreateSourceTextAsync(dto);

        var result = await service.UpdateSourceTextAsync("empty.source.test", "");

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("");
    }

    [Fact]
    public async Task CreateMultipleSids_WithSimilarNames_ShouldAllSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var sids = new[]
        {
            "app.button.save",
            "app.button.saved",
            "app.button.saving",
            "app.button",
            "app"
        };

        foreach (var sid in sids)
        {
            var dto = new TextDetailDto { SID = sid, Text = $"Text for {sid}" };
            var result = await service.CreateSourceTextAsync(dto);
            result.IsSuccess.Should().BeTrue();
        }

        var allSids = await service.GetAllSidsAsync();
        allSids.Value.Should().Contain(sids);
    }

    [Fact]
    public async Task DeleteTranslation_LastTranslation_ShouldOnlyDeleteTranslationNotSid()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "single.translation",
            Text = "English",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "Deutsch" }
            }
        };
        await service.CreateSourceTextAsync(dto);

        var deleteResult = await service.DeleteTranslationAsync("single.translation", "de-DE");
        deleteResult.IsSuccess.Should().BeTrue();

        var sidStillExists = await service.GetBySidAsync("single.translation");
        sidStillExists.IsSuccess.Should().BeTrue();
        sidStillExists.Value.Text.Should().Be("English");
        sidStillExists.Value.Translations.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTranslation_SameTextMultipleTimes_ShouldOnlyStoreOne()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "duplicate.update",
            Text = "English"
        };
        await service.CreateSourceTextAsync(dto);

        await service.UpdateTranslationAsync("duplicate.update", "de-DE", "Same text");
        await service.UpdateTranslationAsync("duplicate.update", "de-DE", "Same text");
        await service.UpdateTranslationAsync("duplicate.update", "de-DE", "Same text");

        var result = await service.GetBySidAsync("duplicate.update");
        result.Value.Translations.Should().HaveCount(1);
        result.Value.Translations.First().Text.Should().Be("Same text");
    }

    [Fact]
    public async Task GetAllWithLanguage_EmptyDatabase_ShouldReturnEmptyList()
    {
        var options = new DbContextOptionsBuilder<TranslationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TranslationDbContext(options);
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var result = await service.GetAllWithLanguageAsync("en-US");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task SidWithDotsUnderscoresHyphens_ShouldWork()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var complexSids = new[]
        {
            "app.module.component.element",
            "app_module_component_element",
            "app-module-component-element",
            "app.module_component-element"
        };

        foreach (var sid in complexSids)
        {
            var dto = new TextDetailDto { SID = sid, Text = $"Text for {sid}" };
            var result = await service.CreateSourceTextAsync(dto);
            result.IsSuccess.Should().BeTrue($"SID '{sid}' should be valid");
        }

        var allSids = await service.GetAllSidsAsync();
        allSids.Value.Should().Contain(complexSids);
    }

    [Fact]
    public async Task UpdateTranslation_MultipleLanguagesForSameSid_ShouldMaintainIndependence()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var dto = new TextDetailDto
        {
            SID = "multi.lang.test",
            Text = "English"
        };
        await service.CreateSourceTextAsync(dto);

        await service.UpdateTranslationAsync("multi.lang.test", "de-DE", "Deutsch v1");
        await service.UpdateTranslationAsync("multi.lang.test", "fr-FR", "Français v1");

        await service.UpdateTranslationAsync("multi.lang.test", "de-DE", "Deutsch v2");

        var result = await service.GetBySidAsync("multi.lang.test");
        result.Value.Translations.First(t => t.LangId == "de-DE").Text.Should().Be("Deutsch v2");
        result.Value.Translations.First(t => t.LangId == "fr-FR").Text.Should().Be("Français v1");
    }

    [Fact]
    public async Task CreateSourceText_WithNewlines_ShouldPreserveFormatting()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var textWithNewlines = "Line 1\nLine 2\nLine 3\r\nLine 4";
        var dto = new TextDetailDto
        {
            SID = "newlines.test",
            Text = textWithNewlines
        };

        var result = await service.CreateSourceTextAsync(dto);

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be(textWithNewlines);
    }

    [Fact]
    public async Task GetAllWithLanguage_MixedContent_ShouldOnlyReturnLanguageSpecific()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        await service.CreateSourceTextAsync(new TextDetailDto
        {
            SID = "has.german",
            Text = "English",
            Translations = new List<TranslationDto> { new() { LangId = "de-DE", Text = "Deutsch" } }
        });

        await service.CreateSourceTextAsync(new TextDetailDto
        {
            SID = "has.french",
            Text = "English",
            Translations = new List<TranslationDto> { new() { LangId = "fr-FR", Text = "Français" } }
        });

        await service.CreateSourceTextAsync(new TextDetailDto
        {
            SID = "english.only",
            Text = "English only"
        });

        var germanResult = await service.GetAllWithLanguageAsync("de-DE");
        germanResult.Value.Should().ContainSingle(t => t.SID == "has.german");
        germanResult.Value.Should().NotContain(t => t.SID == "has.french");
        germanResult.Value.Should().NotContain(t => t.SID == "english.only");
    }
}
