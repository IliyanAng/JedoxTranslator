using FluentAssertions;
using JedoxTranslator.Core.Data;
using JedoxTranslator.Core.Dtos;
using JedoxTranslator.Core.Models;
using JedoxTranslator.Core.Repositories;
using JedoxTranslator.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace JedoxTranslator.Tests;

public class IntegrationTests
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
    public async Task CompleteWorkflow_CreateReadUpdateDelete_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var createDto = new TextDetailDto
        {
            SID = "workflow.test",
            Text = "Workflow Test",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "Workflow Test DE" }
            }
        };

        var createResult = await service.CreateSourceTextAsync(createDto);
        createResult.IsSuccess.Should().BeTrue();

        var readResult = await service.GetBySidAsync("workflow.test");
        readResult.IsSuccess.Should().BeTrue();
        readResult.Value.Text.Should().Be("Workflow Test");
        readResult.Value.Translations.Should().HaveCount(1);

        var updateResult = await service.UpdateSourceTextAsync("workflow.test", "Updated Workflow Test");
        updateResult.IsSuccess.Should().BeTrue();

        var readAfterUpdate = await service.GetBySidAsync("workflow.test");
        readAfterUpdate.Value.Text.Should().Be("Updated Workflow Test");

        var addTranslationResult = await service.UpdateTranslationAsync("workflow.test", "fr-FR", "Test de flux de travail");
        addTranslationResult.IsSuccess.Should().BeTrue();

        var readWithNewTranslation = await service.GetBySidAsync("workflow.test");
        readWithNewTranslation.Value.Translations.Should().HaveCount(2);

        var deleteTranslationResult = await service.DeleteTranslationAsync("workflow.test", "de-DE");
        deleteTranslationResult.IsSuccess.Should().BeTrue();

        var readAfterDeleteTranslation = await service.GetBySidAsync("workflow.test");
        readAfterDeleteTranslation.Value.Translations.Should().HaveCount(1);
        readAfterDeleteTranslation.Value.Translations.Should().NotContain(t => t.LangId == "de-DE");

        var deleteResult = await service.DeleteBySidAsync("workflow.test");
        deleteResult.IsSuccess.Should().BeTrue();

        var readAfterDelete = await service.GetBySidAsync("workflow.test");
        readAfterDelete.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleServices_WorkingOnSameData_ShouldBeConsistent()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service1 = new TranslationService(repository, context);
        var service2 = new TranslationService(repository, context);

        var createDto = new TextDetailDto
        {
            SID = "shared.sid",
            Text = "Shared Text"
        };

        await service1.CreateSourceTextAsync(createDto);

        var result1 = await service1.GetBySidAsync("shared.sid");
        var result2 = await service2.GetBySidAsync("shared.sid");

        result1.Value.Text.Should().Be(result2.Value.Text);

        await service1.UpdateSourceTextAsync("shared.sid", "Updated by service 1");

        var afterUpdate = await service2.GetBySidAsync("shared.sid");
        afterUpdate.Value.Text.Should().Be("Updated by service 1");
    }

    [Fact]
    public async Task GetAllWithLanguage_AfterMultipleOperations_ShouldReflectChanges()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var initialGerman = await service.GetAllWithLanguageAsync("de-DE");
        var initialCount = initialGerman.Value.Count;

        var newSid = new TextDetailDto
        {
            SID = "new.german.text",
            Text = "New English",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "Neuer deutscher Text" }
            }
        };
        await service.CreateSourceTextAsync(newSid);

        var afterCreate = await service.GetAllWithLanguageAsync("de-DE");
        afterCreate.Value.Count.Should().Be(initialCount + 1);

        await service.DeleteTranslationAsync("new.german.text", "de-DE");

        var afterDeleteTranslation = await service.GetAllWithLanguageAsync("de-DE");
        afterDeleteTranslation.Value.Count.Should().Be(initialCount);
    }

    [Fact]
    public async Task BulkOperations_CreateMultipleSids_ShouldSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var sidsToCreate = Enumerable.Range(1, 10).Select(i => new TextDetailDto
        {
            SID = $"bulk.test.{i}",
            Text = $"Bulk test {i}",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = $"Bulk Test {i} DE" }
            }
        }).ToList();

        foreach (var dto in sidsToCreate)
        {
            var result = await service.CreateSourceTextAsync(dto);
            result.IsSuccess.Should().BeTrue();
        }

        var allSids = await service.GetAllSidsAsync();
        allSids.Value.Should().Contain(sid => sid.StartsWith("bulk.test."));
        allSids.Value.Count(sid => sid.StartsWith("bulk.test.")).Should().Be(10);

        var allGerman = await service.GetAllWithLanguageAsync("de-DE");
        allGerman.Value.Count(t => t.SID.StartsWith("bulk.test.")).Should().Be(10);
    }

    [Fact]
    public async Task UpdateSourceText_ShouldNotAffectTranslations()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var createDto = new TextDetailDto
        {
            SID = "isolated.test",
            Text = "Original English",
            Translations = new List<TranslationDto>
            {
                new() { LangId = "de-DE", Text = "Original Deutsch" },
                new() { LangId = "fr-FR", Text = "Original Français" }
            }
        };
        await service.CreateSourceTextAsync(createDto);

        await service.UpdateSourceTextAsync("isolated.test", "Updated English");

        var result = await service.GetBySidAsync("isolated.test");
        result.Value.Text.Should().Be("Updated English");
        result.Value.Translations.Should().HaveCount(2);
        result.Value.Translations.First(t => t.LangId == "de-DE").Text.Should().Be("Original Deutsch");
        result.Value.Translations.First(t => t.LangId == "fr-FR").Text.Should().Be("Original Français");
    }

    [Fact]
    public async Task ConcurrentTranslationUpdates_ShouldAllSucceed()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var createDto = new TextDetailDto
        {
            SID = "concurrent.test",
            Text = "Concurrent Test"
        };
        await service.CreateSourceTextAsync(createDto);

        var languages = new[] { "de-DE", "fr-FR", "es-ES", "it-IT", "pt-PT" };
        var tasks = languages.Select(lang =>
            service.UpdateTranslationAsync("concurrent.test", lang, $"Translation {lang}")
        ).ToList();

        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r => r.IsSuccess.Should().BeTrue());

        var final = await service.GetBySidAsync("concurrent.test");
        final.Value.Translations.Should().HaveCount(5);
    }

    [Fact]
    public async Task DeleteSid_WithManyTranslations_ShouldDeleteAll()
    {
        var context = GetInMemoryContext();
        var repository = new TranslationRepository(context);
        var service = new TranslationService(repository, context);

        var translations = Enumerable.Range(1, 20).Select(i => new TranslationDto
        {
            LangId = $"lang-{i:D2}",
            Text = $"Translation {i}"
        }).ToList();

        var createDto = new TextDetailDto
        {
            SID = "many.translations",
            Text = "Many Translations",
            Translations = translations
        };
        await service.CreateSourceTextAsync(createDto);

        var before = await service.GetBySidAsync("many.translations");
        before.Value.Translations.Should().HaveCount(20);

        var deleteResult = await service.DeleteBySidAsync("many.translations");
        deleteResult.IsSuccess.Should().BeTrue();

        var translationsInDb = context.Translations.Count(t => t.SID == "many.translations");
        translationsInDb.Should().Be(0);
    }
}
