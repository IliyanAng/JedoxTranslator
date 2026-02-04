namespace JedoxTranslator.Core.Dtos;

public class TextDetailDto
{
    public required string SID { get; set; }
    public required string Text { get; set; }
    public List<TranslationDto> Translations { get; set; } = new();
}

public class TranslationDto
{
    public required string LangId { get; set; }
    public required string Text { get; set; }
}
