using Dapper.Contrib.Extensions;

namespace DAL.Model;

public class Answer
{
    [Key] public long Id { get; set; }

    public long QuestionId { get; set; }
    public required string AnswerText { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime CreatedAt { get; set; }
}