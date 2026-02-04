namespace JedoxTranslator.Core.Models;

public class SourceText
{
    public required string SID { get; set; }
    public required string Text { get; set; }
    public ICollection<Translation> Translations { get; set; } = new List<Translation>();
}
