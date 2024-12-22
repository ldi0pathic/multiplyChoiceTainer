using BL;
using DAL.Model;

namespace multiplyChoiceTainer;

public partial class Form1 : Form
{
    private readonly List<(TextBox AnswerTextBox, CheckBox IsCorrectCheckBox)> _answerFields = new();
    private readonly Panel _dynamicPanel;
    private readonly QuestionService _questionService;

    private int currentY;

    public Form1(QuestionService questionService)
    {
        _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
        InitializeComponent();
        _dynamicPanel = new Panel { Dock = DockStyle.Fill }; // Dock an das gesamte Fenster
        Controls.Add(_dynamicPanel);
        StartPage();
    }

    private void StartPage()
    {
        _dynamicPanel.Controls.Clear();

        var newQuestion = new Button
        {
            Text = "Neue Fragen Hinzufügen",
            Size = new Size(150, 30),
            Location = new Point(
                (ClientSize.Width - 150) / 2,
                (ClientSize.Height - 30) / 2
            ),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right // An allen Rändern verankern
        };
        newQuestion.Click += (sender, e) => CreateQuestionPage();
        _dynamicPanel.Controls.Add(newQuestion);

        var startQuiz = new Button
        {
            Text = "Start",
            Size = new Size(50, 30),
            Location = new Point(
                (ClientSize.Width - 50) / 2,
                (ClientSize.Height + 35) / 2
            ),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        startQuiz.Click += (sender, e) => QuizPage();
        _dynamicPanel.Controls.Add(startQuiz);
    }

    private void QuizPage()
    {
        _dynamicPanel.Controls.Clear();
    }

    private void CreateQuestionPage()
    {
        _dynamicPanel.Controls.Clear();

        currentY = 10;

        var lblQuestion = new Label
        {
            Location = new Point(12, currentY),
            Text = "Frage:",
            Width = 100,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        currentY += lblQuestion.Height + 10;

        var txtQuestion = new TextBox
        {
            Location = new Point(30, currentY),
            Multiline = true,
            Height = 60,
            ScrollBars = ScrollBars.Vertical,
            Width = _dynamicPanel.Width - 60,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right // Verankerung für horizontale Anpassung
        };
        currentY += txtQuestion.Height + 10;


        var lblPoints = new Label
        {
            Location = new Point(12, currentY),
            Text = "Punkte:",
            Width = 50,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        var txtPoints = new TextBox
        {
            Location = new Point(70, currentY),
            Width = 100,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        var btnAddAnswerField = new Button
        {
            Location = new Point(_dynamicPanel.Width - 60, currentY),
            Text = "+",
            Width = 30,
            Anchor = AnchorStyles.Top | AnchorStyles.Right // Verankerung für rechts
        };
        btnAddAnswerField.Click += BtnAddAnswerField_Click;
        currentY += btnAddAnswerField.Height + 10;

        var btnAddQuestion = new Button
        {
            Text = "Sichern",
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right // Verankerung für den unteren rechten Rand
        };
        btnAddQuestion.Location = new Point(_dynamicPanel.Width - btnAddQuestion.Width - 12, _dynamicPanel.Height - btnAddQuestion.Height - 12);
        btnAddQuestion.Click += async (sender, e) => await SaveQuestionAsync(txtQuestion, txtPoints);


        _dynamicPanel.Controls.Add(lblQuestion);
        _dynamicPanel.Controls.Add(txtQuestion);
        _dynamicPanel.Controls.Add(lblPoints);
        _dynamicPanel.Controls.Add(txtPoints);
        _dynamicPanel.Controls.Add(btnAddAnswerField);
        _dynamicPanel.Controls.Add(btnAddQuestion);
    }

    private void BtnAddAnswerField_Click(object sender, EventArgs e)
    {
        // TextBox für die Antwort erstellen
        var answerTextBox = new TextBox
        {
            Location = new Point(60, currentY), // x=30, y=aktuelle Y-Position
            Width = _dynamicPanel.Width - 90, // Breite über das komplette Panel mit Rand
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right // Verankerung für horizontale Anpassung
        };

        // Checkbox für die richtige Antwort erstellen
        var isCorrectAnswerCheckBox = new CheckBox
        {
            Location = new Point(30, currentY), // Etwas unterhalb der TextBox
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };

        _dynamicPanel.Controls.Add(answerTextBox);
        _dynamicPanel.Controls.Add(isCorrectAnswerCheckBox);

        _answerFields.Add((answerTextBox, isCorrectAnswerCheckBox));

        currentY += answerTextBox.Height + 15; // Position anpassen
    }

    private async Task SaveQuestionAsync(TextBox txtQuestion, TextBox txtPoints)
    {
        var question = new Question
        {
            QuestionText = txtQuestion.Text,
            Points = int.TryParse(txtPoints.Text, out var points) ? points : 0,
            QuestionType = QuestionType.Auswahl, // Hier kannst du den Typ anpassen
            CreatedAt = DateTime.Now
        };

        var answers = new List<Answer>();
        foreach (var (answerTextBox, isCorrectCheckBox) in _answerFields)
        {
            var answer = new Answer
            {
                AnswerText = answerTextBox.Text,
                IsCorrect = isCorrectCheckBox.Checked,
                CreatedAt = DateTime.Now
            };
            answers.Add(answer);
        }

        var result = await _questionService.SaveQuestionAsync(question, answers);

        if (result.IsSuccess)
            StartPage();
        else
            MessageBox.Show(result.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}