using DAL.Model.Enum;
using Dapper.Contrib.Extensions;

namespace DAL.Model;

public class Question
{
    [Key] public long Id { get; set; }

    public required string QuestionText { get; set; }
    public int Points { get; set; }
    public QuestionType QuestionType { get; set; }
    public DateTime? LastAskedDate { get; set; }
    public int IncorrectAnswerCount { get; set; }
    public DateTime? LastIncorrectAnswerDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsDeleted { get; set; }

    // Neue Felder für die Überschriften von Kreuzfragen
    public string? Header1 { get; set; }
    public string? Header2 { get; set; }
}