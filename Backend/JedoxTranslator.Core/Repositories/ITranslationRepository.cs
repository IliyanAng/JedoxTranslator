using Ardalis.Result;
using JedoxTranslator.Core.Models;

namespace JedoxTranslator.Core.Repositories;

public interface ITranslationRepository
{
    Task<Result<List<string>>> GetAllSidsAsync();
    Task<Result<SourceText>> GetBySidAsync(string sid);
    Task<Result<SourceText>> CreateSourceTextAsync(SourceText sourceText);
    Task<Result<Translation>> UpdateTranslationAsync(string sid, string langId, string translatedText);
    Task<Result<SourceText>> UpdateSourceTextAsync(string sid, string text);
    Task<Result> DeleteTranslationAsync(string sid, string langId);
    Task<Result> DeleteBySidAsync(string sid);
}
