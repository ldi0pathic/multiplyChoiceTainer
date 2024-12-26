using BL.Utils.ResultPattern;
using DAL.Model;
using DAL.Model.Enum;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace BL;

public class QuestionService
{
    private readonly IGenericRepository<Answer> _answerRepository;

    private readonly HashSet<long> _askedQuestions = new();
    private readonly ILogger<QuestionService> _logger;
    private readonly IGenericRepository<Question> _questionRepository;
    private readonly Random _random;

    public QuestionService(ILoggerFactory loggerFactory, IGenericRepository<Question> questionRepository, IGenericRepository<Answer> answerRepository)
    {
        _questionRepository = questionRepository;
        _answerRepository = answerRepository;
        _random = new Random(DateTime.Now.Millisecond);
        _logger = loggerFactory.CreateLogger<QuestionService>();
    }

    public async Task<Result> SaveQuestionAsync(Question question, List<Answer> answers)
    {
        var validationResult = ValidateQuestion(question, answers);

        if (validationResult.IsFailure)
            return validationResult;

        try
        {
            question.CreatedAt = DateTime.Now;
            var key = await _questionRepository.InsertAsync(question);

            foreach (var answer in answers)
            {
                answer.QuestionId = key;
                answer.CreatedAt = DateTime.Now;
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

            case QuestionType.Kreuz:
                if (question.Header1.IsNullOrEmpty() || question.Header2.IsNullOrEmpty())
                    msg.Add("Eine K-Frage Benötigt Überschriften für die Auswahl");
                break;
        }

        return msg.Count != 0
            ? Result.Fail(msg)
            : Result.Success();
    }

    public async Task<Result> IncrementIncorrectAnswerCountAsync(long questionId)
    {
        try
        {
            var question = await _questionRepository.GetByIdAsync(questionId);

            if (question == null)
                return Result.Fail("Frage konnte nicht gefunden werden.");

            question.IncorrectAnswerCount++;
            question.LastIncorrectAnswerDate = DateTime.Now;

            await _questionRepository.UpdateAsync(question);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Erhöhen des Zählers für falsche Antworten für Frage mit Id {questionId}", questionId);
            return Result.Exception(ex);
        }
    }

    public void ResetAskedQuestions()
    {
        _askedQuestions.Clear();
    }

    public async Task<Result<Question>> GetWeightedRandomQuestionAsync()
    {
        try
        {
            var questions = await _questionRepository.GetAllAsync();

            // Filtere Fragen, die bereits gestellt wurden oder die als gelöscht markiert sind
            IEnumerable<Question> enumerable = questions
                .Where(q => !_askedQuestions.Contains(q.Id) && !q.IsDeleted)
                .ToList();

            if (!enumerable.Any())
                return Result<Question>.Fail("Keine neuen Fragen in der Datenbank verfügbar.");

            var weightedQuestions = enumerable.Select(q =>
            {
                var incorrectCount = q.IncorrectAnswerCount;
                var lastAsked = q.LastAskedDate ?? DateTime.MinValue;
                var timeSinceLastAsked = (DateTime.Now - lastAsked).TotalDays;

                var weight = (incorrectCount + 1) * (timeSinceLastAsked + 1);

                var lastIncorrectAnswerDate = q.LastIncorrectAnswerDate ?? DateTime.MinValue;
                var timeSinceLastIncorrectAnswer = (DateTime.Now - lastIncorrectAnswerDate).TotalDays * _random.NextDouble();

                var incorrectAnswerWeightFactor = Math.Max(0, 1 - timeSinceLastIncorrectAnswer / 30);

                weight *= incorrectAnswerWeightFactor;

                return new { Question = q, Weight = weight };
            }).OrderBy(_ => Guid.NewGuid()).ToList();

            var totalWeight = weightedQuestions.Sum(wq => wq.Weight);
            var randomValue = _random.NextDouble() * totalWeight;

            foreach (var weightedQuestion in weightedQuestions)
            {
                randomValue -= weightedQuestion.Weight;

                if (!(randomValue <= 0))
                    continue;

                var selectedQuestion = weightedQuestion.Question;

                selectedQuestion.LastAskedDate = DateTime.Now;
                await _questionRepository.UpdateAsync(selectedQuestion);

                _askedQuestions.Add(selectedQuestion.Id);

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

    public async Task<Result<IEnumerable<Question>>> GetMostIncorrectlyAnsweredQuestionsAsync()
    {
        try
        {
            const string whereClause = "IncorrectAnswerCount > 0 AND IsDeleted != true";
            var questions = await _questionRepository.GetAllAsync(whereClause);

            var sortedQuestions = questions
                .OrderByDescending(q => q.IncorrectAnswerCount)
                .ToList();

            return sortedQuestions.Count == 0
                ? Result<IEnumerable<Question>>.Fail("Es gibt keine häufig falsch beantworteten Fragen.")
                : Result<IEnumerable<Question>>.Success(sortedQuestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Abrufen der häufigsten Fehler");
            return Result<IEnumerable<Question>>.Exception(ex);
        }
    }

    public async Task<Result> DeleteQuestionAsync(long questionId)
    {
        try
        {
            // Frage aus der Datenbank abrufen
            var question = await _questionRepository.GetByIdAsync(questionId);

            if (question == null)
                return Result.Fail("Frage konnte nicht gefunden werden.");

            // Frage als gelöscht markieren
            question.IsDeleted = true;

            // Update in der Datenbank durchführen
            await _questionRepository.UpdateAsync(question);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Löschen der Frage mit Id {questionId}", questionId);
            return Result.Exception(ex);
        }
    }
}