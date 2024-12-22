using BL.Utils.ResultPattern;
using DAL.Model;
using Microsoft.Extensions.Logging;

namespace BL;

public class QuestionService
{
    private readonly IGenericRepository<Answer> _answerRepository;
    private readonly ILogger<QuestionService> _logger;
    private readonly IGenericRepository<Question> _questionRepository;

    public QuestionService(ILoggerFactory loggerFactory, IGenericRepository<Question> questionRepository, IGenericRepository<Answer> answerRepository)
    {
        _questionRepository = questionRepository;
        _answerRepository = answerRepository;
        _logger = loggerFactory.CreateLogger<QuestionService>();
    }

    public async Task<Result> SaveQuestionAsync(Question question, List<Answer> answers)
    {
        var validationResult = ValidateQuestion(question, answers);

        if (validationResult.IsFailiure)
            return validationResult;

        try
        {
            var key = await _questionRepository.InsertAsync(question);

            foreach (var answer in answers)
            {
                answer.QuestionId = key;
                await _answerRepository.InsertAsync(answer);
            }

            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message, e);
            return Result.Exception(e);
        }
    }

    private static Result ValidateQuestion(Question question, List<Answer> answers)
    {
        var msg = new List<string>();

        if (question.Points <= 0)
            msg.Add("Es muss mindestens ein Punkt möglich sein.");

        if (answers.Count < 2)
            msg.Add("Eine Frage muss mindestens zwei Antworten enthalten.");

        if (answers.All(a => a.IsCorrect != true))
            msg.Add("Mindestens eine Antwort muss korrekt sein.");

        return msg.Count != 0
            ? Result.Fail(msg)
            : Result.Success();
    }
}