using BL;
using BL.Utils;
using DAL.Model;
using DAL.Model.Enum;

namespace multiplyChoiceTainer;

public partial class Form1 : Form
{
    private readonly List<(TextBox AnswerTextBox, CheckBox IsCorrectCheckBox)> _answerFields = new();
    private readonly Panel _dynamicPanel;
    private readonly QuestionService _questionService;
    private readonly Dictionary<string, QuestionType> _typeMapping;
    private ComboBox _cmbQuestionType;
    private int _currentQuestions;
    private int _currentY;
    private int _maxQuestions;

    private decimal _score;
    private decimal _totalPoints;

    public Form1(QuestionService questionService)
    {
        _score = 0;
        _totalPoints = 0;
        _maxQuestions = 30;
        _currentQuestions = 0;
        _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
        _typeMapping = EnumExtensions.GetDescriptionMapping<QuestionType>();
        InitializeComponent();

        _dynamicPanel = new Panel { Dock = DockStyle.Fill };
        Controls.Add(_dynamicPanel);

        StartPage();
    }

    private void StartPage()
    {
        Reset();

        // TextBox für die Eingabe der maximalen Fragenanzahl
        var questionCountLabel = CreateLabel("Maximale Fragen pro Runde:", 12);
        questionCountLabel.Location = new Point(10, _currentY);
        _dynamicPanel.Controls.Add(questionCountLabel);

        var questionCountTextBox = new TextBox
        {
            Text = _maxQuestions.ToString(),
            Width = 100,
            Location = new Point(200, _currentY)
        };
        _dynamicPanel.Controls.Add(questionCountTextBox);
        _currentY += questionCountTextBox.Height + 10;

        var newQuestionButton = CreateButton("Neue Fragen Hinzufügen", 150, 30, (ClientSize.Width - 150) / 2, (ClientSize.Height - 30) / 2);
        newQuestionButton.Click += (_, _) => CreateQuestionPage();
        _dynamicPanel.Controls.Add(newQuestionButton);

        var startQuizButton = CreateButton("Start", 50, 30, (ClientSize.Width - 50) / 2, (ClientSize.Height + 35) / 2);
        startQuizButton.Click += async (_, _) =>
        {
            if (int.TryParse(questionCountTextBox.Text, out var maxQuestions) && maxQuestions > 0)
            {
                _maxQuestions = maxQuestions;
                await QuizPageAsync();
            }
            else
            {
                MessageBox.Show("Bitte geben Sie eine gültige Anzahl von Fragen ein (positive Zahl).", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
        _dynamicPanel.Controls.Add(startQuizButton);
    }

    private Button CreateButton(string text, int width, int height, int x, int y)
    {
        return new Button
        {
            Text = text,
            Size = new Size(width, height),
            Location = new Point(x, y),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
    }

    private async Task QuizPageAsync()
    {
        Reset();
        _currentQuestions++;

        var questionResult = await _questionService.GetWeightedRandomQuestionAsync();

        if (questionResult.IsFailure || questionResult.Value == null)
        {
            MessageBox.Show("Keine Frage verfügbar: " + questionResult.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var question = questionResult.Value;
        _totalPoints += question.Points;
        var answersResult = await _questionService.GetQuestionAnswersAsync(question.Id);

        if (answersResult.IsFailure || answersResult.Value == null)
        {
            MessageBox.Show("Keine Antworten verfügbar: " + answersResult.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var answers = answersResult.Value;

        var questionHeaderPanel = CreatePanel(new Point(10, _currentY), _dynamicPanel.Width - 20, 30);
        questionHeaderPanel.Controls.Add(CreateLabel($"Typ: {question.QuestionType.GetDescription()}", questionHeaderPanel.Width - 200));
        questionHeaderPanel.Controls.Add(CreateLabel($"Punkte: {question.Points}", questionHeaderPanel.Width - 100));

        _dynamicPanel.Controls.Add(questionHeaderPanel);
        _currentY += questionHeaderPanel.Height + 10;

        var questionTextLabel = CreateLabel(question.QuestionText, 12, FontStyle.Bold);
        questionTextLabel.Location = new Point(10, _currentY);
        questionTextLabel.Width = _dynamicPanel.Width - 20;
        questionTextLabel.TextAlign = ContentAlignment.MiddleLeft;
        _dynamicPanel.Controls.Add(questionTextLabel);
        _currentY += questionTextLabel.Height + 10;

        var separator = CreateSeparator();
        _dynamicPanel.Controls.Add(separator);
        _currentY += separator.Height + 10;

        foreach (var answer in answers)
        {
            var answerCheckbox = new CheckBox
            {
                Text = answer.AnswerText,
                AutoSize = true,
                Location = new Point(10, _currentY),
                Padding = new Padding(5)
            };
            _dynamicPanel.Controls.Add(answerCheckbox);

            _currentY += answerCheckbox.Height + 5;
        }

        if (_currentQuestions < _maxQuestions)
        {
            var btnSubmit = new Button
            {
                Text = "Antworten Abgeben",
                Width = _dynamicPanel.Width - 20,
                Height = 40,
                Location = new Point(10, _dynamicPanel.Height - 50),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            btnSubmit.Click += async (_, _) =>
            {
                var totalCorrectAnswers = answers.Count(a => a.IsCorrect);
                var pointsPerAnswer = question.Points / (decimal)totalCorrectAnswers;

                decimal totalScore = 0;

                foreach (var answer in answers)
                {
                    var selectedAnswer = _dynamicPanel.Controls.OfType<CheckBox>()
                        .FirstOrDefault(cb => cb.Text == answer.AnswerText); // Suche die CheckBox mit dem entsprechenden Text

                    if (selectedAnswer != null)
                        switch (answer.IsCorrect)
                        {
                            case true when selectedAnswer.Checked:
                                totalScore += pointsPerAnswer;
                                break;
                            case false when selectedAnswer.Checked:
                                totalScore -= pointsPerAnswer;
                                break;
                        }
                }

                totalScore = Math.Max(0, totalScore);
                _score += totalScore;
                await QuizPageAsync();
            };

            _dynamicPanel.Controls.Add(btnSubmit);
            _currentY += btnSubmit.Height + 10;
        }
        else
        {
            var btnSubmit = new Button
            {
                Text = "Endergebnis Anzeigen",
                Width = _dynamicPanel.Width - 20,
                Height = 40,
                Location = new Point(10, _dynamicPanel.Height - 50),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            btnSubmit.Click += async (_, _) =>
            {
                var totalCorrectAnswers = answers.Count(a => a.IsCorrect);
                var pointsPerAnswer = question.Points / (decimal)totalCorrectAnswers;

                decimal totalScore = 0;

                foreach (var answer in answers)
                {
                    var selectedAnswer = _dynamicPanel.Controls.OfType<CheckBox>()
                        .FirstOrDefault(cb => cb.Text == answer.AnswerText); // Suche die CheckBox mit dem entsprechenden Text

                    if (selectedAnswer != null)
                        switch (answer.IsCorrect)
                        {
                            case true when selectedAnswer.Checked:
                                totalScore += pointsPerAnswer;
                                break;
                            case false when selectedAnswer.Checked:
                                totalScore -= pointsPerAnswer;
                                break;
                        }
                }

                totalScore = Math.Max(0, totalScore);
                _score += totalScore;

                MessageBox.Show($"DU hast {_score}/ {_totalPoints} Punkte erreicht!", "Fertig :)", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            _dynamicPanel.Controls.Add(btnSubmit);
            _currentY += btnSubmit.Height + 10;
        }
    }

    private static Label CreateLabel(string text, int x)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Location = new Point(x, 0),
            TextAlign = ContentAlignment.MiddleRight
        };
    }

    private Label CreateLabel(string text, int fontSize, FontStyle fontStyle)
    {
        return new Label
        {
            Text = text,
            Font = new Font(Font.FontFamily, fontSize, fontStyle),
            AutoSize = true
        };
    }

    private static Panel CreatePanel(Point location, int width, int height)
    {
        return new Panel
        {
            Location = location,
            Width = width,
            Height = height,
            Dock = DockStyle.Top
        };
    }

    private Label CreateSeparator()
    {
        return new Label
        {
            Height = 2,
            Width = _dynamicPanel.Width - 20,
            Location = new Point(10, _currentY),
            BorderStyle = BorderStyle.Fixed3D
        };
    }

    private void Reset()
    {
        _dynamicPanel.Controls.Clear();
        _answerFields.Clear();
        _currentY = 10;
    }

    private void CreateQuestionPage()
    {
        Reset();

        var lblQuestion = new Label
        {
            Location = new Point(12, _currentY),
            Text = "Frage:",
            Width = 100
        };
        _currentY += lblQuestion.Height + 10;

        var txtQuestion = new TextBox
        {
            Location = new Point(30, _currentY),
            Multiline = true,
            Height = 60,
            ScrollBars = ScrollBars.Vertical,
            Width = _dynamicPanel.Width - 60
        };
        _currentY += txtQuestion.Height + 10;

        var lblQuestionType = new Label
        {
            Location = new Point(12, _currentY),
            Text = "Typ:",
            Width = 30
        };

        _cmbQuestionType = new ComboBox
        {
            Location = new Point(55, _currentY),
            Width = 100,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        _cmbQuestionType.DataSource = new BindingSource { DataSource = _typeMapping };
        _cmbQuestionType.DisplayMember = "Key";
        _cmbQuestionType.ValueMember = "Value";

        var lblPoints = new Label
        {
            Location = new Point(175, _currentY),
            Text = "Punkte:",
            Width = 50
        };

        var txtPoints = new TextBox
        {
            Location = new Point(225, _currentY),
            Width = 100
        };

        var btnAddAnswerField = new Button
        {
            Location = new Point(_dynamicPanel.Width - 60, _currentY),
            Text = "+",
            Width = 30
        };
        btnAddAnswerField.Click += BtnAddAnswerField_Click;
        _currentY += btnAddAnswerField.Height + 10;

        var btnAddQuestion = new Button
        {
            Text = "Sichern",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        btnAddQuestion.Location = new Point(_dynamicPanel.Width - btnAddQuestion.Width - 12, _dynamicPanel.Height - btnAddQuestion.Height - 12);
        btnAddQuestion.Click += async (_, _) => await SaveQuestionAsync(txtQuestion, txtPoints);

        _dynamicPanel.Controls.Add(lblQuestion);
        _dynamicPanel.Controls.Add(txtQuestion);
        _dynamicPanel.Controls.Add(lblQuestionType);
        _dynamicPanel.Controls.Add(_cmbQuestionType);
        _dynamicPanel.Controls.Add(lblPoints);
        _dynamicPanel.Controls.Add(txtPoints);
        _dynamicPanel.Controls.Add(btnAddAnswerField);
        _dynamicPanel.Controls.Add(btnAddQuestion);
    }

    private void BtnAddAnswerField_Click(object? sender, EventArgs e)
    {
        var answerTextBox = new TextBox
        {
            Location = new Point(60, _currentY),
            Width = _dynamicPanel.Width - 70
        };

        var answerCheckBox = new CheckBox
        {
            Location = new Point(10, _currentY)
        };

        _answerFields.Add((answerTextBox, answerCheckBox));

        _dynamicPanel.Controls.Add(answerTextBox);
        _dynamicPanel.Controls.Add(answerCheckBox);

        _currentY += answerTextBox.Height + 10;
    }

    private async Task SaveQuestionAsync(TextBox txtQuestion, TextBox txtPoints)
    {
        var errorMessages = new List<string>();

        if (string.IsNullOrWhiteSpace(txtQuestion.Text)) errorMessages.Add("Bitte geben Sie eine Frage ein.");
        if (!int.TryParse(txtPoints.Text, out var points) || points <= 0) errorMessages.Add("Bitte geben Sie eine gültige Punktzahl größer als 0 ein.");

        foreach (var (answerTextBox, _) in _answerFields)
            if (string.IsNullOrWhiteSpace(answerTextBox.Text))
            {
                errorMessages.Add("Alle Antworttexte müssen ausgefüllt werden.");
                break;
            }


        if (errorMessages.Count != 0)
        {
            MessageBox.Show(string.Join(Environment.NewLine, errorMessages), "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var question = new Question
        {
            QuestionText = txtQuestion.Text,
            QuestionType = (QuestionType)(_cmbQuestionType.SelectedValue ?? QuestionType.Auswahl),
            Points = points
        };

        var answers = _answerFields.Select(field => new Answer
        {
            AnswerText = field.AnswerTextBox.Text,
            IsCorrect = field.IsCorrectCheckBox.Checked
        }).ToList();

        var result = await _questionService.SaveQuestionAsync(question, answers);

        if (result.IsFailure)
        {
            MessageBox.Show($"Fehler beim Hinzufügen der Frage und Antworten: {result.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            MessageBox.Show("Frage und Antworten erfolgreich gespeichert.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
            CreateQuestionPage();
        }
    }
}