using JedoxTranslator.Core.Dtos;
using JedoxTranslator.Core.Services;
using JedoxTranslator.Core.Utils;
using Microsoft.AspNetCore.Mvc;
using SerilogTimings.Extensions;

namespace JedoxTranslator.API.Endpoints;

public static class TranslationEndpoints
{
    public static WebApplication GetAllSids(this WebApplication webApp)
    {
        webApp.MapGet("api/v1/translations/sids", async (
            HttpContext context,
            Serilog.ILogger log,
            CancellationToken cancellationToken,
            ITranslationService translationService) =>
        {
            using var op = log.BeginOperation("Invoking {@MethodName}", nameof(GetAllSids));
            var result = await translationService.GetAllSidsAsync();
            op.Complete();
            return result.ToHttpTypedResult(log, true, nameof(GetAllSids));
        })
        .RequireAuthorization()
        .Produces<HttpResult<List<string>>>()
        .WithSummary("Get all SIDs")
        .WithDescription("Returns a list of all translation SIDs")
        .WithTags("Translations");

        return webApp;
    }

    public static WebApplication GetBySid(this WebApplication webApp)
    {
        webApp.MapGet("api/v1/translations/{sid}", async (
            HttpContext context,
            Serilog.ILogger log,
            CancellationToken cancellationToken,
            ITranslationService translationService,
            [FromRoute] string sid) =>
        {
            using var op = log.BeginOperation("Invoking {@MethodName} with params: {@SID}", nameof(GetBySid), sid);
            var result = await translationService.GetBySidAsync(sid);
            op.Complete();
            return result.ToHttpTypedResult(log, true, nameof(GetBySid), sid);
        })
        .RequireAuthorization()
        .Produces<HttpResult<TextDetailDto>>()
        .WithSummary("Get translations by SID")
        .WithDescription("Returns all translations for a specific SID")
        .WithTags("Translations");

        return webApp;
    }

    public static WebApplication CreateTranslation(this WebApplication webApp)
    {
        webApp.MapPost("api/v1/translations", async (
            HttpContext context,
            Serilog.ILogger log,
            CancellationToken cancellationToken,
            ITranslationService translationService,
            [FromBody] TextDetailDto dto) =>
        {
            using var op = log.BeginOperation("Invoking {@MethodName} with params: {@CreateTranslationDto}", nameof(CreateTranslation), dto);
            var result = await translationService.CreateSourceTextAsync(dto);
            op.Complete();
            return result.ToHttpTypedResult(log, true, nameof(CreateTranslation), dto);
        })
        .RequireAuthorization()
        .Produces<HttpResult<TextDetailDto>>()
        .WithSummary("Create a new translation")
        .WithDescription("Creates a new SID with default text and optional additional translations")
        .WithTags("Translations");

        return webApp;
    }

    public static WebApplication UpdateTranslation(this WebApplication webApp)
    {
        webApp.MapPut("api/v1/translations/{sid}/{langId}", async (
            HttpContext context,
            Serilog.ILogger log,
            CancellationToken cancellationToken,
            ITranslationService translationService,
            [FromRoute] string sid,
            [FromRoute] string langId,
            [FromBody] UpdateTranslationRequest request) =>
        {
            using var op = log.BeginOperation("Invoking {@MethodName} with params: {@SID}, {@LangId}, {@Text}", nameof(UpdateTranslation), sid, langId, request.Text);
            var result = await translationService.UpdateTranslationAsync(sid, langId, request.Text);
            op.Complete();
            return result.ToHttpTypedResult(log, true, nameof(UpdateTranslation), new { sid, langId, request.Text });
        })
        .RequireAuthorization()
        .Produces<HttpResult<TranslationDto>>()
        .WithSummary("Update a translation")
        .WithDescription("Updates or creates a translation for a specific SID and language")
        .WithTags("Translations");

        return webApp;
    }

    public static WebApplication UpdateSourceText(this WebApplication webApp)
    {
        webApp.MapPut("api/v1/translations/{sid}/source", async (
            HttpContext context,
            Serilog.ILogger log,
            CancellationToken cancellationToken,
            ITranslationService translationService,
            [FromRoute] string sid,
            [FromBody] UpdateTranslationRequest request) =>
        {
            using var op = log.BeginOperation("Invoking {@MethodName} with params: {@SID}, {@Text}", nameof(UpdateSourceText), sid, request.Text);
            var result = await translationService.UpdateSourceTextAsync(sid, request.Text);
            op.Complete();
            return result.ToHttpTypedResult(log, true, nameof(UpdateSourceText), new { sid, request.Text });
        })
        .RequireAuthorization()
        .Produces<HttpResult<TextDetailDto>>()
        .WithSummary("Update source text")
        .WithDescription("Updates the default English source text for a specific SID")
        .WithTags("Translations");

        return webApp;
    }

    public static WebApplication DeleteTranslation(this WebApplication webApp)
    {
        webApp.MapDelete("api/v1/translations/{sid}/{langId}", async (
            HttpContext context,
            Serilog.ILogger log,
            CancellationToken cancellationToken,
            ITranslationService translationService,
            [FromRoute] string sid,
            [FromRoute] string langId) =>
        {
            using var op = log.BeginOperation("Invoking {@MethodName} with params: {@SID}, {@LangId}", nameof(DeleteTranslation), sid, langId);
            var result = await translationService.DeleteTranslationAsync(sid, langId);
            op.Complete();
            return result.ToHttpTypedResult(log, true, nameof(DeleteTranslation), new { sid, langId });
        })
        .RequireAuthorization()
        .Produces<HttpResult<object>>()
        .WithSummary("Delete a translation")
        .WithDescription("Deletes a specific translation for a language")
        .WithTags("Translations");

        return webApp;
    }

    public static WebApplication DeleteBySid(this WebApplication webApp)
    {
        webApp.MapDelete("api/v1/translations/{sid}", async (
            HttpContext context,
            Serilog.ILogger log,
            CancellationToken cancellationToken,
            ITranslationService translationService,
            [FromRoute] string sid) =>
        {
            using var op = log.BeginOperation("Invoking {@MethodName} with params: {@SID}", nameof(DeleteBySid), sid);
            var result = await translationService.DeleteBySidAsync(sid);
            op.Complete();
            return result.ToHttpTypedResult(log, true, nameof(DeleteBySid), sid);
        })
        .RequireAuthorization()
        .Produces<HttpResult<object>>()
        .WithSummary("Delete a SID")
        .WithDescription("Deletes a SID and all its translations")
        .WithTags("Translations");

        return webApp;
    }

    public static WebApplication GetAllWithLanguage(this WebApplication webApp)
    {
        webApp.MapGet("api/v1/translations", async (
            HttpContext context,
            Serilog.ILogger log,
            CancellationToken cancellationToken,
            ITranslationService translationService,
            [FromQuery] string langId = "en-US") =>
        {
            using var op = log.BeginOperation("Invoking {@MethodName} with params: {@LangId}", nameof(GetAllWithLanguage), langId);
            var result = await translationService.GetAllWithLanguageAsync(langId);
            op.Complete();
            return result.ToHttpTypedResult(log, true, nameof(GetAllWithLanguage), langId);
        })
        .RequireAuthorization()
        .Produces<HttpResult<List<TextDetailDto>>>()
        .WithSummary("Get all translations for a language")
        .WithDescription("Returns all translations for a specific language with fallback to en-US")
        .WithTags("Translations");

        return webApp;
    }
}

public record UpdateTranslationRequest(string Text);
