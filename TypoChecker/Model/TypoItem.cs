namespace TypoChecker.Models;
public class TypoItem : ICheckItem
{
    public string Sentense { get; set; }
    public string WrongWords { get; set; }
    public string CorrectWords { get; set; }
    public string CorrectSentense { get; set; }
    public string Message { get; set; }
}
