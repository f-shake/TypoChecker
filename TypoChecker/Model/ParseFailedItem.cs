namespace TypoChecker.Models;

public class ParseFailedItem : ICheckItem
{
    public ParseFailedItem()
    {
    }

    public ParseFailedItem(string message)
    {
        Message = message;
    }

    public string Message { get; set; }
}
