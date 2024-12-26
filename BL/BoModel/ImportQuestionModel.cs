namespace BL.BoModel;

public class ImportQuestionModel
{
    public string QuestionText { get; set; }
    public int Points { get; set; }
    public string QuestionType { get; set; }
    public DateTime? LastAskedDate { get; set; }
    public int IncorrectAnswerCount { get; set; }
    public DateTime? LastIncorrectAnswerDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Header1 { get; set; }
    public string? Header2 { get; set; }
    public List<ImportAnswerModel> Answers { get; set; } = new();
}

public class ImportAnswerModel
{
    public string AnswerText { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime CreatedAt { get; set; }
}