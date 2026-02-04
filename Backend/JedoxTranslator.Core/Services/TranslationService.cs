using Ardalis.Result;
using JedoxTranslator.Core.Data;
using JedoxTranslator.Core.Dtos;
using JedoxTranslator.Core.Models;
using JedoxTranslator.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JedoxTranslator.Core.Services;

public class TranslationService(ITranslationRepository repository, TranslationDbContext context) : ITranslationService
{
    public async Task<Result<List<string>>> GetAllSidsAsync()
    {
        return await repository.GetAllSidsAsync();
    }

    public async Task<Result<TextDetailDto>> GetBySidAsync(string sid)
    {
        var result = await repository.GetBySidAsync(sid);

        if (!result.IsSuccess)
        {
            return Result.NotFound($"SID '{sid}' not found");
        }

        var sourceText = result.Value;
        var dto = new TextDetailDto
        {
            SID = sourceText.SID,
            Text = sourceText.Text,
            Translations = sourceText.Translations.Select(t => new TranslationDto
            {
                LangId = t.LangId,
                Text = t.TranslatedText
            }).ToList()
        };

        return Result.Success(dto);
    }

    public async Task<Result<TextDetailDto>> CreateSourceTextAsync(TextDetailDto dto)
    {
        var sourceText = new SourceText
        {
            SID = dto.SID,
            Text = dto.Text,
            Translations = new List<Translation>()
        };

        if (dto.Translations != null)
        {
            foreach (var additionalDto in dto.Translations)
            {
                sourceText.Translations.Add(new Translation
                {
                    SID = dto.SID,
                    LangId = additionalDto.LangId,
                    TranslatedText = additionalDto.Text
                });
            }
        }

        var result = await repository.CreateSourceTextAsync(sourceText);

        if (!result.IsSuccess)
        {
            return Result.Conflict($"SID '{dto.SID}' already exists");
        }

        var createdSourceText = result.Value;
        var textDetailDto = new TextDetailDto
        {
            SID = createdSourceText.SID,
            Text = createdSourceText.Text,
            Translations = createdSourceText.Translations.Select(t => new TranslationDto
            {
                LangId = t.LangId,
                Text = t.TranslatedText
            }).ToList()
        };

        return Result.Success(textDetailDto);
    }

    public async Task<Result<TranslationDto>> UpdateTranslationAsync(string sid, string langId, string text)
    {
        var result = await repository.UpdateTranslationAsync(sid, langId, text);

        if (!result.IsSuccess)
        {
            return Result<TranslationDto>.NotFound($"SID '{sid}' not found");
        }

        var translation = result.Value;
        var dto = new TranslationDto
        {
            LangId = translation.LangId,
            Text = translation.TranslatedText
        };

        return Result.Success(dto);
    }

    public async Task<Result<TextDetailDto>> UpdateSourceTextAsync(string sid, string text)
    {
        var result = await repository.UpdateSourceTextAsync(sid, text);

        if (!result.IsSuccess)
        {
            return Result<TextDetailDto>.NotFound($"SID '{sid}' not found");
        }

        var sourceText = result.Value;
        var dto = new TextDetailDto
        {
            SID = sourceText.SID,
            Text = sourceText.Text,
            Translations = sourceText.Translations.Select(t => new TranslationDto
            {
                LangId = t.LangId,
                Text = t.TranslatedText
            }).ToList()
        };

        return Result.Success(dto);
    }

    public async Task<Result> DeleteTranslationAsync(string sid, string langId)
    {
        return await repository.DeleteTranslationAsync(sid, langId);
    }

    public async Task<Result> DeleteBySidAsync(string sid)
    {
        return await repository.DeleteBySidAsync(sid);
    }

    //TODO: This can be optimized with some caching and/or pagination, 
    //When our system grows larger this will stop being that performant
    public async Task<Result<List<TextDetailDto>>> GetAllWithLanguageAsync(string langId)
    {
        var allSourceTexts = await context.SourceTexts
            .Include(st => st.Translations)
            .ToListAsync();

        var result = new List<TextDetailDto>();

        foreach (var sourceText in allSourceTexts)
        {
            var translation = sourceText.Translations.FirstOrDefault(t => t.LangId == langId);

            if (langId == "en-US")
            {
                result.Add(new TextDetailDto
                {
                    SID = sourceText.SID,
                    Text = sourceText.Text
                });
            }
            else if (translation != null)
            {
                result.Add(new TextDetailDto
                {
                    SID = sourceText.SID,
                    Text = translation.TranslatedText
                });
            }
        }

        return Result.Success(result);
    }
}
