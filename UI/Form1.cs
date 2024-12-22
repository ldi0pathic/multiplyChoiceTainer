namespace multiplyChoiceTainer;

public partial class Form1 : Form
{
    private readonly List<(TextBox AnswerTextBox, CheckBox IsCorrectCheckBox)> _answerFields = [];
    private readonly Panel _dynamicPanel;

    private int currentY;

    public Form1()
    {
        InitializeComponent();
        _dynamicPanel = new Panel { Dock = DockStyle.Fill };
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
            )
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
            )
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
            Width = 100
        };
        currentY += lblQuestion.Height + 10;

        var txtQuestion = new TextBox
        {
            Location = new Point(30, currentY),
            Multiline = true,
            Height = 60,
            ScrollBars = ScrollBars.Vertical,
            Width = _dynamicPanel.Width - 60
        };
        currentY += txtQuestion.Height + 10;


        var lblPoints = new Label
        {
            Location = new Point(12, currentY),
            Text = "Punkte:",
            Width = 50
        };

        var txtPoints = new TextBox
        {
            Location = new Point(70, currentY),
            Width = 100
        };

        var btnAddAnswerField = new Button
        {
            Location = new Point(_dynamicPanel.Width - 60, currentY),
            Text = "+",
            Width = 30,
            
        };
        btnAddAnswerField.Click += BtnAddAnswerField_Click;
        currentY += btnAddAnswerField.Height + 10;


        var btnAddQuestion = new Button
        {
            Text = "Sichern"
        };

        btnAddQuestion.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnAddQuestion.Location = new Point(_dynamicPanel.Width - btnAddQuestion.Width - 12, _dynamicPanel.Height - btnAddQuestion.Height - 12);
        //   btnAddQuestion.Click += (sender, e) => AddQuestionToDatabase(txtQuestion, txtPoints);


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
            Width = _dynamicPanel.Width - 90 // Breite über das komplette Panel mit Rand
        };

        // Checkbox für die richtige Antwort erstellen
        var isCorrectAnswerCheckBox = new CheckBox
        {
            Location = new Point(30, currentY) // Etwas unterhalb der TextBox
        };


        _dynamicPanel.Controls.Add(answerTextBox);
        _dynamicPanel.Controls.Add(isCorrectAnswerCheckBox);

        currentY += answerTextBox.Height + 10;
    }


    /*
       private void AddQuestionToDatabase(TextBox txtQuestion, TextBox txtPoints, TextBox txtAnswer1, TextBox txtAnswer2, TextBox txtAnswer3)
       {
           string question = txtQuestion.Text;
           int points = int.TryParse(txtPoints.Text, out int result) ? result : 0;
           string answer1 = txtAnswer1.Text;
           string answer2 = txtAnswer2.Text;
           string answer3 = txtAnswer3.Text;

           // Verbindungszeichenfolge zur SQLite-Datenbank
           string connectionString = "Data Source=mydatabase.db";

           using (var connection = new SQLiteConnection(connectionString))
           {
               connection.Open();

               // SQL-Befehl zum Hinzufügen der Frage
               string sql = @"
                   INSERT INTO Questions (QuestionText, Points)
                   VALUES (@QuestionText, @Points);
                   SELECT last_insert_rowid();";

               var questionId = connection.ExecuteScalar<int>(sql, new { QuestionText = question, Points = points });

               // SQL-Befehl zum Hinzufügen der Antworten
               string answerSql = @"
                   INSERT INTO Answers (QuestionId, AnswerText, IsCorrect)
                   VALUES (@QuestionId, @AnswerText, @IsCorrect);";

               // Hier kannst du für jede Antwort entscheiden, ob sie richtig ist oder nicht
               connection.Execute(answerSql, new { QuestionId = questionId, AnswerText = answer1, IsCorrect = true });
               connection.Execute(answerSql, new { QuestionId = questionId, AnswerText = answer2, IsCorrect = false });
               connection.Execute(answerSql, new { QuestionId = questionId, AnswerText = answer3, IsCorrect = false });
           }

           // Erfolgreich
           MessageBox.Show("Question added successfully!");
       }
       */
}