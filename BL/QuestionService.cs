using BL.Utils.ResultPattern;
using DAL.Model;
using DAL.Model.Enum;
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

        if (validationResult.IsFailure)
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

        switch (question.QuestionType)
        {
            case QuestionType.Auswahl:
                if (answers.Count(a => a.IsCorrect) != 1)
                    msg.Add("Eine A-Frage muss genau eine richtige Antwort haben.");
                break;

            case QuestionType.Pick:
                if (answers.Count(a => a.IsCorrect) < 2)
                    msg.Add("Eine P-Frage muss mindestens zwei richtige Antworten haben.");
                break;
        }

        return msg.Count != 0
            ? Result.Fail(msg)
            : Result.Success();
    }


    public async Task<Result<Question>> GetWeightedRandomQuestionAsync()
    {
        try
        {
            var questions = await _questionRepository.GetAllAsync();

            IEnumerable<Question> enumerable = questions.ToList();
            if (!enumerable.Any())
                return Result<Question>.Fail("Keine Fragen in der Datenbank verfügbar.");

            var weightedQuestions = enumerable.Select(q =>
            {
                var incorrectCount = q.IncorrectAnswerCount;
                var lastAsked = q.LastAskedDate ?? DateTime.MinValue;
                var timeSinceLastAsked = (DateTime.Now - lastAsked).TotalDays;

                var weight = (incorrectCount + 1) * (timeSinceLastAsked + 1);

                var lastIncorrectAnswerDate = q.LastIncorrectAnswerDate ?? DateTime.MinValue;
                var timeSinceLastIncorrectAnswer = (DateTime.Now - lastIncorrectAnswerDate).TotalDays;

                // Je länger die falsche Antwort her ist, desto weniger Einfluss hat sie
                var incorrectAnswerWeightFactor = Math.Max(0, 1 - timeSinceLastIncorrectAnswer / 30);

                weight *= incorrectAnswerWeightFactor;

                return new { Question = q, Weight = weight };
            }).ToList();

            // Zufällige Auswahl basierend auf Gewicht
            var totalWeight = weightedQuestions.Sum(wq => wq.Weight);
            var randomValue = new Random().NextDouble() * totalWeight;

            foreach (var weightedQuestion in weightedQuestions)
            {
                randomValue -= weightedQuestion.Weight;

                if (!(randomValue <= 0))
                    continue;

                var selectedQuestion = weightedQuestion.Question;

                selectedQuestion.LastAskedDate = DateTime.Now;
                await _questionRepository.UpdateAsync(selectedQuestion);

                return Result<Question>.Success(selectedQuestion);
            }

            return Result<Question>.Fail("Es konnte keine Frage ausgewählt werden.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen einer gewichteten zufälligen Frage");
            return Result<Question>.Exception(ex);
        }
    }

    public async Task<Result<IEnumerable<Answer>>> GetQuestionAnswersAsync(long questionId)
    {
        try
        {
            var answers = await _answerRepository.GetAllAsync($"QuestionId = {questionId}");

            var randomAnswers = answers.OrderBy(_ => Guid.NewGuid()).ToList();

            return randomAnswers.Count == 0
                ? Result<IEnumerable<Answer>>.Fail($"Keine Antworten für Frage mit Id {questionId} in der Datenbank verfügbar.")
                : Result<IEnumerable<Answer>>.Success(randomAnswers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen der Antwortmöglichkeiten der Frage mit id {questionId}", questionId);
            return Result<IEnumerable<Answer>>.Exception(ex);
        }
    }
}