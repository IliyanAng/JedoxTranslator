using Ardalis.Result;
using JedoxTranslator.Core.Dtos;

namespace JedoxTranslator.Core.Services;

public interface ITranslationService
{
    Task<Result<List<string>>> GetAllSidsAsync();
    Task<Result<TextDetailDto>> GetBySidAsync(string sid);
    Task<Result<TextDetailDto>> CreateSourceTextAsync(TextDetailDto dto);
    Task<Result<TranslationDto>> UpdateTranslationAsync(string sid, string langId, string text);
    Task<Result<TextDetailDto>> UpdateSourceTextAsync(string sid, string text);
    Task<Result> DeleteTranslationAsync(string sid, string langId);
    Task<Result> DeleteBySidAsync(string sid);
    Task<Result<List<TextDetailDto>>> GetAllWithLanguageAsync(string langId);
}
