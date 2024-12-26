using System.Text.Json;
using BL.BoModel;
using DAL.Model;
using DAL.Model.Enum;
using Microsoft.Extensions.Logging;

namespace BL;

public class ImportExportService
{
    private readonly IGenericRepository<Answer> _answerRepository;
    private readonly ILogger<ImportExportService> _logger;
    private readonly IGenericRepository<Question> _questionRepository;
    private readonly QuestionService _questionService;

    public ImportExportService(ILoggerFactory loggerFactory, IGenericRepository<Question> questionRepository, IGenericRepository<Answer> answerRepository, QuestionService questionService)
    {
        _questionRepository = questionRepository;
        _answerRepository = answerRepository;
        _questionService = questionService;
        _logger = loggerFactory.CreateLogger<ImportExportService>();
    }

    public async Task ExportQuestionsToJsonAsync(string filePath)
    {
        try
        {
            var questions = await _questionRepository.GetAllAsync("IsDeleted = false");
            var answers = await _answerRepository.GetAllAsync();

            var exportData = questions.Select(question => new
            {
                question.QuestionText,
                question.Points,
                QuestionType = question.QuestionType.ToString(),
                question.Header1,
                question.Header2,
                Answers = answers.Where(a => a.QuestionId == question.Id)
                    .Select(answer => new
                    {
                        answer.AnswerText,
                        answer.IsCorrect,
                    })
            });

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Exportieren der Fragen in eine JSON-Datei.");
            throw;
        }
    }

    public async Task ImportQuestionsFromJsonAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Die JSON-Datei wurde nicht gefunden.", filePath);

            var json = await File.ReadAllTextAsync(filePath);

            var importData = JsonSerializer.Deserialize<List<ImportQuestionModel>>(json);
            if (importData == null)
                throw new InvalidOperationException("Die JSON-Datei enthält keine gültigen Daten.");

            foreach (var item in importData)
            {
                var question = new Question
                {
                    QuestionText = item.QuestionText,
                    Points = item.Points,
                    QuestionType = Enum.Parse<QuestionType>(item.QuestionType),
                    Header1 = item.Header1,
                    Header2 = item.Header2
                };

                var result = await _questionService.SaveQuestionAsync(question, item.Answers.Select(a => new Answer
                {
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect,
                }).ToList());

                if (result.IsFailure)
                    _logger.LogWarning("Ein Fehler ist beim Importieren der Frage aufgetreten: {Errors}", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Importieren der Fragen aus einer JSON-Datei.");
            throw;
        }
    }
}