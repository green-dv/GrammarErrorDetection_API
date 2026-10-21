namespace GrammarErrorDetection.Models.Grammar.Texts;

public sealed class TextListItemDto : TextEntity
{
    public List<SentenceErrorDto> Sentences { get; set; } = [];
    public int ErrorCount => Sentences.Sum(sentence => sentence.ErrorCount);
}
