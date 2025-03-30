namespace TypoChecker.Models;

public class ResultItem
{
    public string Sentense { get; set; }
    public string WrongWords { get; set; }
    public string CorrectWords { get; set; }
    public string Possibility { get; set; }
    public string Message { get; set; }
}