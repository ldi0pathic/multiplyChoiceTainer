namespace BL.BoModel;

public class ImportQuestionModel
{
    public string QuestionText { get; set; }
    public int Points { get; set; }
    public string QuestionType { get; set; }
    public string? Header1 { get; set; }
    public string? Header2 { get; set; }
    public List<ImportAnswerModel> Answers { get; set; } = [];
}

public class ImportAnswerModel
{
    public string AnswerText { get; set; }
    public bool? canReorder { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime CreatedAt { get; set; }
}