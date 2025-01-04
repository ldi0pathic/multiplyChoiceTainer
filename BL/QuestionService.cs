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

    public async Task<int> GetQuestionCountAsync()
    {
        const string whereClause = "IsDeleted != true";
        var questions = await _questionRepository.GetAllAsync(whereClause);

        return questions.Count();
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

        foreach (var answer in answers)
            if (answer.canReorder)
            {
                var spilt = answer.AnswerText.Split('/');
                if (spilt.Length <= 1)
                {
                    msg.Add("Antworten, die mit Reorder markiert sind, müssen ein '/' zum spliten enthalten!");
                    break;
                }
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

    public void AddAskedQuestions(long questinId)
    {
        _askedQuestions.Add(questinId);
    }

    public async Task<Result<Question>> GetRandomQuestionAsync()
    {
        const string whereClause = "IsDeleted != true";
        var questions = await _questionRepository.GetAllAsync(whereClause);

        if (!questions.Any()) return Result<Question>.Fail("Es konnte keine Frage ausgewählt werden.");
        var random = questions.Where(q => !_askedQuestions.Contains(q.Id) && !q.IsDeleted).OrderBy(_ => Guid.NewGuid()).ToList();

        if (random.Count == 0) return Result<Question>.Fail("Es konnte keine Frage ausgewählt werden.");

        var question = random.First();

        question.LastAskedDate = DateTime.Now;
        await _questionRepository.UpdateAsync(question);

        _askedQuestions.Add(question.Id);

        return Result<Question>.Success(question);
    }

    public async Task<Result<Question>> GetWeightedRandomQuestionAsync()
    {
        try
        {
            // Alle Fragen abrufen
            var questions = await _questionRepository.GetAllAsync();

            // Filtere gültige Fragen
            var validQuestions = questions
                .Where(q => !_askedQuestions.Contains(q.Id) && !q.IsDeleted)
                .ToList();

            if (!validQuestions.Any())
                return Result<Question>.Fail("Keine neuen Fragen in der Datenbank verfügbar.");

            // Gewichtung berechnen
            var weightedQuestions = validQuestions.Select(q =>
            {
                var incorrectCount = q.IncorrectAnswerCount + 1;
                var lastWrong = q.LastIncorrectAnswerDate ?? DateTime.MinValue;

                // Gewicht basiert auf Anzahl der falschen Antworten und Zeit seit dem letzten Fehler
                var weight = incorrectCount * (DateTime.MaxValue - lastWrong).TotalMinutes;

                return new { Question = q, Weight = weight };
            }).ToList();

            Question selectedQuestion;
            if (_random.Next(3) != 2)
                selectedQuestion = weightedQuestions
                    .OrderByDescending(q => q.Weight) // Behalte hohe Gewichtungen vorne
                    .ThenBy(_ => Guid.NewGuid()) // Zufälligkeit innerhalb derselben Gewichtung
                    .First().Question;
            else
                selectedQuestion = weightedQuestions
                    .OrderByDescending(q => Guid.NewGuid())
                    .First().Question;

            // Frage aktualisieren
            selectedQuestion.LastAskedDate = DateTime.UtcNow;
            await _questionRepository.UpdateAsync(selectedQuestion);

            return Result<Question>.Success(selectedQuestion);
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


            var randomAnswers = answers.OrderBy(_ => Guid.NewGuid()).Select(Reorder).ToList();

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

    private static Answer Reorder(Answer answer)
    {
        if (!answer.canReorder) return answer;

        var split = answer.AnswerText.Split('/');

        for (var i = 0; i < split.Length - 1; i++)
            split[i] = split[i].Trim();

        split = split.OrderBy(_ => Guid.NewGuid()).ToArray();
        answer.AnswerText = string.Join(" / ", split).Trim();

        return answer;
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