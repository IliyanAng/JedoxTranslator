namespace JedoxTranslator.Core.Models;

public class Translation
{
    public int Id { get; set; }
    //We make the SID required as we need a source text for each translation
    public required string SID { get; set; }
    public required string LangId { get; set; }
    public required string TranslatedText { get; set; }
    public SourceText SourceText { get; set; } = null!;
}
