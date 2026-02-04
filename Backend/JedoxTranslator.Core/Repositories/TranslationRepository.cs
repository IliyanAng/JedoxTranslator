using Ardalis.Result;
using JedoxTranslator.Core.Data;
using JedoxTranslator.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace JedoxTranslator.Core.Repositories;

public class TranslationRepository(TranslationDbContext context) : ITranslationRepository
{
    public async Task<Result<List<string>>> GetAllSidsAsync()
    {
        var sids = await context.SourceTexts
            .Select(st => st.SID)
            .AsNoTracking()
            .ToListAsync();

        return Result.Success(sids);
    }

    public async Task<Result<SourceText>> GetBySidAsync(string sid)
    {
        var sourceText = await context.SourceTexts
            .Include(st => st.Translations)
            //TODO: Do we care about tracking here? It will improve performance if we set it to .AsNoTracking()
            .FirstOrDefaultAsync(st => st.SID == sid);

        if (sourceText == null)
        {
            return Result.NotFound($"SID '{sid}' not found");
        }

        return Result.Success(sourceText);
    }

    public async Task<Result<SourceText>> CreateSourceTextAsync(SourceText sourceText)
    {
        var existing = await context.SourceTexts
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.SID == sourceText.SID);

        //TODO: What should we do if we pass the same ID for a create method.
        //Should we consider and UPSERT so we merge create and update and make it easier to handle? 
        if (existing != null)
        {
            return Result.Conflict($"SID '{sourceText.SID}' already exists");
        }

        context.SourceTexts.Add(sourceText);
        await context.SaveChangesAsync();

        return Result.Success(sourceText);
    }

    public async Task<Result<Translation>> UpdateTranslationAsync(string sid, string langId, string translatedText)
    {
        var translation = context.Translations.FirstOrDefault(tr => tr.LangId == langId && tr.SID == sid);

        if (translation == null)
        {
            translation = new Translation
            {
                SID = sid,
                LangId = langId,
                TranslatedText = translatedText
            };
            context.Translations.Add(translation);
        }
        else
        {
            translation.TranslatedText = translatedText;
        }

        await context.SaveChangesAsync();

        return Result.Success(translation);
    }

    public async Task<Result<SourceText>> UpdateSourceTextAsync(string sid, string text)
    {
        var sourceText = await context.SourceTexts
            .FirstOrDefaultAsync(st => st.SID == sid);

        if (sourceText == null)
        {
            return Result.NotFound($"SID '{sid}' not found");
        }

        sourceText.Text = text;
        await context.SaveChangesAsync();

        return Result.Success(sourceText);
    }

    public async Task<Result> DeleteTranslationAsync(string sid, string langId)
    {
        var translation = await context.Translations
            .FirstOrDefaultAsync(t => t.SID == sid && t.LangId == langId);

        if (translation == null)
        {
            return Result.NotFound($"Translation for SID '{sid}' and language '{langId}' not found");
        }

        context.Translations.Remove(translation);
        await context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> DeleteBySidAsync(string sid)
    {
        var sourceText = await context.SourceTexts
            .Include(st => st.Translations)
            .FirstOrDefaultAsync(st => st.SID == sid);

        if (sourceText == null)
        {
            return Result.NotFound($"SID '{sid}' not found");
        }

        //Since we have the delete behavior set to cascade, the translation will be deleted along the source text
        context.SourceTexts.Remove(sourceText);
        await context.SaveChangesAsync();

        return Result.Success();
    }
}
