using BL;
using BL.Utils;
using DAL.Model;
using DAL.Model.Enum;

namespace multiplyChoiceTainer;

public partial class Form1 : Form
{
    private readonly List<(TextBox AnswerTextBox, CheckBox IsCorrectCheckBox)> _answerFields = new();
    private readonly Panel _dynamicPanel;
    private readonly ImportExportService _importExportService;
    private readonly QuestionService _questionService;
    private readonly Dictionary<string, QuestionType> _typeMapping;
    private ComboBox _cmbQuestionType;
    private int _currentQuestions;
    private int _currentY;
    private int _maxQuestions;
    private int _maxTime;

    private decimal _score;
    private DateTime _startTime;
    private decimal _totalPoints;

    public Form1(QuestionService questionService, ImportExportService importExportService)
    {
        _score = 0;
        _totalPoints = 0;
        _maxQuestions = 0;
        _currentQuestions = 0;
        _questionService = questionService ?? throw new ArgumentNullException(nameof(questionService));
        _importExportService = importExportService;
        _typeMapping = EnumExtensions.GetDescriptionMapping<QuestionType>();
        InitializeComponent();

        _dynamicPanel = new Panel { Dock = DockStyle.Fill };
        Controls.Add(_dynamicPanel);

        _ = StartPage();
    }

    private async Task StartPage()
    {
        Reset();
        _questionService.ResetAskedQuestions();

        // TextBox für die Eingabe der maximalen Fragenanzahl
        var questionCountLabel = CreateLabel("Maximale Fragen pro Runde:", 12, FontStyle.Regular, 12);

        questionCountLabel.Location = new Point(10, _currentY);
        _dynamicPanel.Controls.Add(questionCountLabel);
        _maxQuestions = await _questionService.GetQuestionCountAsync();

        var questionCountTextBox = new TextBox
        {
            Text = _maxQuestions.ToString(),
            Width = 50,
            Location = new Point(20 + questionCountLabel.Width, _currentY)
        };
        _dynamicPanel.Controls.Add(questionCountTextBox);
        _currentY += questionCountTextBox.Height + 10; // Update _currentY nach der TextBox

        //------

        var questionTimeLabel = CreateLabel("Maximale Zeit (min) pro Runde:", 12, FontStyle.Regular, 12);
        questionTimeLabel.Location = new Point(10, _currentY);
        _dynamicPanel.Controls.Add(questionTimeLabel);
        var questionTimeTextBox = new TextBox
        {
            Text = "15",
            Width = 50,
            Location = new Point(20 + questionTimeLabel.Width, _currentY)
        };
        _dynamicPanel.Controls.Add(questionTimeTextBox);
        _currentY += questionTimeTextBox.Height + 10; // Update _currentY nach der TextBox

        //------

        var newQuestionButton = CreateButton("Neue Fragen Hinzufügen", 150, 30, (ClientSize.Width - 150) / 2, _currentY);
        newQuestionButton.Click += (_, _) => CreateQuestionPage();
        _dynamicPanel.Controls.Add(newQuestionButton);


        _currentY += newQuestionButton.Height + 10; // Update _currentY nach dem Button

        var showFrequentMistakesButton = CreateButton("Häufige Fehler anzeigen", 150, 30, (ClientSize.Width - 150) / 2, _currentY);
        showFrequentMistakesButton.Click += async (_, _) => await ShowFrequentMistakesPageAsync();
        _dynamicPanel.Controls.Add(showFrequentMistakesButton);

        _currentY += showFrequentMistakesButton.Height + 10; // Update _currentY nach dem Button

        // Button für Export
        var exportButton = CreateButton("Exportieren", 150, 30, (ClientSize.Width - 150) / 2, _currentY);
        exportButton.Click += btnExport_Click; // Verwendet die vorhandene btnExport_Click-Methode
        _dynamicPanel.Controls.Add(exportButton);

        _currentY += exportButton.Height + 10; // Update _currentY nach dem Button

        // Button für Import
        var importButton = CreateButton("Importieren", 150, 30, (ClientSize.Width - 150) / 2, _currentY);
        importButton.Click += btnImport_Click; // Verwendet die vorhandene btnImport_Click-Methode
        _dynamicPanel.Controls.Add(importButton);

        _currentY += importButton.Height + 10; // Update _currentY nach dem Button

        var startQuizButton2 = CreateButton("Start Übung", 100, 30, (ClientSize.Width - 100) / 2, _currentY);
        startQuizButton2.Click += async (_, _) =>
        {
            if (!int.TryParse(questionCountTextBox.Text, out var maxQuestions) && maxQuestions > 0)
            {
                MessageBox.Show("Bitte geben Sie eine gültige Anzahl von Fragen ein (positive Zahl).", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(questionTimeTextBox.Text, out var maxTime) && maxTime > 1)
            {
                MessageBox.Show("Bitte geben Sie eine gültige Zeit ein (positive Zahl).", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _maxQuestions = maxQuestions;
            _maxTime = maxTime;
            _startTime = DateTime.Now;
            await QuizPageAsync(true);
        };
        _dynamicPanel.Controls.Add(startQuizButton2);
        _currentY += startQuizButton2.Height + 10; // Update _currentY nach dem Button

        var startQuizButton = CreateButton("Start Prüfung", 100, 30, (ClientSize.Width - 100) / 2, _currentY);
        startQuizButton.Click += async (_, _) =>
        {
            _maxQuestions = 40;
            _maxTime = 70;
            _startTime = DateTime.Now;
            await QuizPageAsync();
        };
        _dynamicPanel.Controls.Add(startQuizButton);
    }


    private async Task ShowFrequentMistakesPageAsync()
    {
        Reset();

        var frequentMistakesResult = await _questionService.GetMostIncorrectlyAnsweredQuestionsAsync();
        if (frequentMistakesResult.IsFailure || frequentMistakesResult.Value == null)
        {
            MessageBox.Show("Fehler beim Abrufen der häufigsten Fehler: " + frequentMistakesResult.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            await StartPage();
            return;
        }

        var frequentMistakes = frequentMistakesResult.Value;

        var titleLabel = CreateLabel("Häufige Fehler", 16, FontStyle.Bold);
        titleLabel.Location = new Point(10, _currentY);
        _dynamicPanel.Controls.Add(titleLabel);
        _currentY += titleLabel.Height + 20;

        var enumerable = frequentMistakes as Question[] ?? frequentMistakes.ToArray();
        if (enumerable.Length != 0)
        {
            foreach (var question in enumerable)
            {
                var questionLabel = CreateLabel($"Frage: {question.QuestionText} (Fehler: {question.IncorrectAnswerCount})", 12, FontStyle.Regular, 12);
                questionLabel.Width = _dynamicPanel.Width - 20;
                questionLabel.Location = new Point(10, _currentY);
                _dynamicPanel.Controls.Add(questionLabel);
                _currentY += questionLabel.Height + 10;

                // Löschen-Button für jede Frage hinzufügen
                var deleteButton = CreateButton("Löschen", 100, 30, _dynamicPanel.Width - 110, _currentY);
                deleteButton.Click += async (_, _) =>
                {
                    var result = await _questionService.DeleteQuestionAsync(question.Id);
                    if (result.IsSuccess)
                    {
                        MessageBox.Show("Frage wurde erfolgreich gelöscht.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await ShowFrequentMistakesPageAsync(); // Seite nach Löschen aktualisieren
                    }
                    else
                    {
                        MessageBox.Show("Fehler beim Löschen der Frage: " + result.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                _dynamicPanel.Controls.Add(deleteButton);
                _currentY += deleteButton.Height + 10;
            }
        }
        else
        {
            var noMistakesLabel = CreateLabel("Keine häufig falsch beantworteten Fragen vorhanden.", 12, FontStyle.Regular, 12);
            noMistakesLabel.Location = new Point(10, _currentY);
            _dynamicPanel.Controls.Add(noMistakesLabel);
        }
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

    private async Task QuizPageAsync(bool testMode = false)
    {
        Reset();

        var usedTime = DateTime.Now - _startTime;

        if (usedTime.TotalMinutes > _maxTime)
            if (MessageBox.Show($"Die Zeit ist abgelaufen\r\nDU hast {_score}/ {_totalPoints} Punkte ({_score / _totalPoints * 100}%) erreicht!\r\n {_currentQuestions}/{_maxQuestions} Fragen bearbeitet", "Fertig :)", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK)
            {
                await StartPage();
                return;
            }

        _currentQuestions++;
        var questionResult = testMode ? await _questionService.GetWeightedRandomQuestionAsync() : await _questionService.GetRandomQuestionAsync();

        if (questionResult.IsFailure || questionResult.Value == null)
        {
            if (_totalPoints < 1) _totalPoints = 1;

            if (MessageBox.Show($"Keine weiteren Fragen verfügbar\r\nDU hast {_score}/ {_totalPoints} Punkte ({_score / _totalPoints * 100}%) erreicht!", "Fertig :)", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK) await StartPage();
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
        var tLabel = CreateLabel($"Zeit: {usedTime.Minutes}/{_maxTime}", 10, FontStyle.Regular, 50);
        var fLabel = CreateLabel($"Frage: {_currentQuestions}/{_maxQuestions}", 10, FontStyle.Regular, tLabel.Width + 50);
        var alabel = CreateLabel($"Antworten: {(question.QuestionType != QuestionType.Kreuz ? answers.Count(c => c.IsCorrect) : answers.Count())}", 10, FontStyle.Regular, tLabel.Width + fLabel.Width + 50);
        questionHeaderPanel.Controls.Add(tLabel);
        questionHeaderPanel.Controls.Add(fLabel);
        questionHeaderPanel.Controls.Add(alabel);
        questionHeaderPanel.Controls.Add(CreateLabel($"Typ: {question.QuestionType.GetDescription()}", 10, FontStyle.Regular, questionHeaderPanel.Width - 210));
        questionHeaderPanel.Controls.Add(CreateLabel($"Punkte: {question.Points}", 10, FontStyle.Regular, questionHeaderPanel.Width - 100));

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

        // Erstellung eines dynamischen TableLayoutPanel
        var answerTable = new TableLayoutPanel
        {
            Location = new Point(10, _currentY),
            AutoSize = true,
            ColumnCount = question.QuestionType == QuestionType.Kreuz ? 3 : 2, // Eine Spalte für die Checkbox, eine für das Label
            RowCount = answers.Count() + (question.QuestionType == QuestionType.Kreuz ? 1 : 0),
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(5),
            ColumnStyles = { new ColumnStyle(SizeType.AutoSize), new ColumnStyle(SizeType.AutoSize) }
        };
        answerTable.CellPaint += (sender, e) => { e.Graphics.FillRectangle(e.Row % 2 == 1 ? Brushes.LightGray : Brushes.Transparent, e.CellBounds); };

        var minSize = 0;
        if (question.QuestionType == QuestionType.Kreuz)
        {
            answerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var h1 = CreateLabel(question.Header1 ?? "", 12, FontStyle.Regular);
            answerTable.Controls.Add(h1);
            minSize += h1.Width;
            var h2 = CreateLabel(question.Header2 ?? "", 12, FontStyle.Regular);
            answerTable.Controls.Add(h2);
            minSize += h2.Width;
            answerTable.Controls.Add(CreateLabel("", 12, FontStyle.Regular));
        }

        var minsize2 = 0;
        foreach (var answer in answers)
            if (question.QuestionType == QuestionType.Kreuz)
            {
                var firstCheckbox = new CheckBox
                {
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    BackColor = Color.Transparent,
                    Name = $"checkbox_{answer.Id}_first" // Eindeutiger Name für die erste Checkbox
                };

                var secondCheckbox = new CheckBox
                {
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    BackColor = Color.Transparent,
                    Name = $"checkbox_{answer.Id}_second" // Eindeutiger Name für die zweite Checkbox
                };

                // Hinzufügen der Eventhandler
                firstCheckbox.CheckedChanged += (_, _) =>
                {
                    if (firstCheckbox.Checked)
                        secondCheckbox.Checked = false;
                };

                secondCheckbox.CheckedChanged += (_, _) =>
                {
                    if (secondCheckbox.Checked)
                        firstCheckbox.Checked = false;
                };

                // Kreuz-Typ spezifische Einträge
                answerTable.Controls.Add(firstCheckbox);
                answerTable.Controls.Add(secondCheckbox);
                minsize2 = minsize2 > firstCheckbox.Width + secondCheckbox.Width + 10 ? minsize2 : firstCheckbox.Width + secondCheckbox.Width + 10;

                answerTable.Controls.Add(CreateLabel(answer.AnswerText, 12, FontStyle.Regular, 0, minsize2 + minSize));
            }
            else
            {
                var checkbox = new CheckBox
                {
                    AutoSize = true,
                    Padding = new Padding(5),
                    Anchor = AnchorStyles.Left,
                    BackColor = Color.Transparent,
                    Name = $"checkbox_{answer.Id}" // Setze den Namen basierend auf der Antwort-ID
                };
                minsize2 = minsize2 > checkbox.Width ? minsize2 : checkbox.Width;
                var label = CreateLabel(answer.AnswerText, 12, FontStyle.Regular, 0, minsize2 + minSize);
                label.Click += (_, _) => checkbox.Checked = !checkbox.Checked;

                answerTable.Controls.Add(checkbox);
                answerTable.Controls.Add(label);
            }

        _dynamicPanel.Controls.Add(answerTable);
        _currentY += answerTable.Height + 10;

        if (_currentQuestions < _maxQuestions)
        {
            var btnSubmit = new Button
            {
                Text = "Antworten Abgeben",
                Width = _dynamicPanel.Width - 120,
                Height = 30,
                Location = new Point(110, _dynamicPanel.Height - 40),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            btnSubmit.Click += async (_, _) =>
            {
                var totalCorrectAnswers = answers.Count(a => a.IsCorrect);
                var pointsPerAnswer = question.Points / (decimal)totalCorrectAnswers;

                decimal totalScore = 0;
                var hasIncorrectAnswers = false;
                var ok = 0;
                foreach (var answer in answers)
                    if (question.QuestionType == QuestionType.Kreuz)
                    {
                        var firstCheckbox = answerTable.Controls
                            .OfType<CheckBox>()
                            .FirstOrDefault(cb => cb.Name == $"checkbox_{answer.Id}_first");

                        var secondCheckbox = answerTable.Controls
                            .OfType<CheckBox>()
                            .FirstOrDefault(cb => cb.Name == $"checkbox_{answer.Id}_second");

                        if (firstCheckbox != null && secondCheckbox != null)
                        {
                            if (answer.IsCorrect)
                            {
                                if (firstCheckbox.Checked)
                                {
                                    totalScore += pointsPerAnswer;
                                    ok++;
                                }
                                else if (secondCheckbox.Checked)
                                {
                                    hasIncorrectAnswers = true;
                                    totalScore -= pointsPerAnswer;
                                    ok--;
                                }

                                if (testMode)
                                {
                                    firstCheckbox.BackColor = Color.LightGreen;
                                    secondCheckbox.BackColor = Color.IndianRed;
                                }
                            }
                            else
                            {
                                if (secondCheckbox.Checked)
                                {
                                    totalScore += pointsPerAnswer;
                                    ok++;
                                    if (testMode)
                                        secondCheckbox.BackColor = Color.LightGreen;
                                }
                                else if (firstCheckbox.Checked)
                                {
                                    hasIncorrectAnswers = true;
                                    totalScore -= pointsPerAnswer;
                                    ok--;
                                    if (testMode)
                                        firstCheckbox.BackColor = Color.IndianRed;
                                }

                                if (testMode)
                                {
                                    firstCheckbox.BackColor = Color.IndianRed;
                                    secondCheckbox.BackColor = Color.LightGreen;
                                }
                            }
                        }
                    }
                    else
                    {
                        var selectedAnswer = answerTable.Controls.OfType<CheckBox>()
                            .FirstOrDefault(cb => cb.Name == $"checkbox_{answer.Id}");

                        if (selectedAnswer != null)
                        {
                            if (testMode && answer.IsCorrect) selectedAnswer.BackColor = Color.LightGreen;
                            if (answer.IsCorrect && !selectedAnswer.Checked)
                            {
                                hasIncorrectAnswers = true;
                                ok--;
                            }
                            else if (!answer.IsCorrect && selectedAnswer.Checked)
                            {
                                hasIncorrectAnswers = true;
                                totalScore -= pointsPerAnswer;
                                ok--;
                            }
                            else if (answer.IsCorrect && selectedAnswer.Checked)
                            {
                                totalScore += pointsPerAnswer;
                                ok++;
                            }
                        }
                    }

                totalScore = Math.Min(Math.Max(0, totalScore), question.Points);
                if (totalCorrectAnswers == ok)
                {
                    totalScore = question.Points;
                    //richtig beantwortete fragen solölöen nicht nochmal gefragt werden
                    _questionService.AddAskedQuestions(question.Id);
                }

                _score += Math.Round(totalScore, 2);

                if (hasIncorrectAnswers) await _questionService.IncrementIncorrectAnswerCountAsync(question.Id);
                if (testMode)
                {
                    if (MessageBox.Show($"{totalScore}/ {question.Points} Punkte", "Weiter", MessageBoxButtons.OK, MessageBoxIcon.Question) == DialogResult.OK)
                        await QuizPageAsync(testMode);
                }
                else
                {
                    await QuizPageAsync(testMode);
                }
            };

            _dynamicPanel.Controls.Add(btnSubmit);
            _currentY += btnSubmit.Height + 10;
        }
        else
        {
            var btnSubmit = new Button
            {
                Text = "Endergebnis Anzeigen",
                Width = _dynamicPanel.Width - 120,
                Height = 30,
                Location = new Point(110, _dynamicPanel.Height - 40),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            btnSubmit.Click += async (_, _) =>
            {
                var totalCorrectAnswers = answers.Count(a => a.IsCorrect);
                var pointsPerAnswer = question.Points / (decimal)totalCorrectAnswers;

                decimal totalScore = 0;
                var hasIncorrectAnswers = false;
                var ok = 0;
                foreach (var answer in answers)
                    if (question.QuestionType == QuestionType.Kreuz)
                    {
                        var firstCheckbox = _dynamicPanel.Controls.OfType<CheckBox>()
                            .FirstOrDefault(cb => cb.Name == $"checkbox_{answer.Id}_first");

                        var secondCheckbox = _dynamicPanel.Controls.OfType<CheckBox>()
                            .FirstOrDefault(cb => cb.Name == $"checkbox_{answer.Id}_second");

                        if (firstCheckbox != null && secondCheckbox != null)
                        {
                            if (answer.IsCorrect)
                            {
                                if (firstCheckbox.Checked)
                                {
                                    totalScore += pointsPerAnswer;
                                    ok++;
                                }
                                else if (secondCheckbox.Checked)
                                {
                                    hasIncorrectAnswers = true;
                                    totalScore -= pointsPerAnswer;
                                }

                                if (testMode)
                                {
                                    firstCheckbox.BackColor = Color.IndianRed;
                                    secondCheckbox.BackColor = Color.LightGreen;
                                }
                            }
                            else
                            {
                                if (secondCheckbox.Checked)
                                {
                                    totalScore += pointsPerAnswer;
                                    ok++;
                                }
                                else if (firstCheckbox.Checked)
                                {
                                    hasIncorrectAnswers = true;
                                    totalScore -= pointsPerAnswer;
                                }
                            }
                        }
                    }
                    else
                    {
                        var selectedAnswer = _dynamicPanel.Controls.OfType<CheckBox>()
                            .FirstOrDefault(cb => cb.Name == $"checkbox_{answer.Id}");

                        if (selectedAnswer != null)
                        {
                            if (testMode && answer.IsCorrect) selectedAnswer.BackColor = Color.LightGreen;
                            if (answer.IsCorrect && !selectedAnswer.Checked)
                            {
                                hasIncorrectAnswers = true;
                            }
                            else if (!answer.IsCorrect && selectedAnswer.Checked)
                            {
                                hasIncorrectAnswers = true;
                                totalScore -= pointsPerAnswer;
                            }
                            else if (answer.IsCorrect && selectedAnswer.Checked)
                            {
                                totalScore += pointsPerAnswer;
                                ok++;
                            }
                        }
                        else if (answer.IsCorrect)
                        {
                            hasIncorrectAnswers = true;
                        }
                    }

                totalScore = Math.Min(Math.Max(0, totalScore), question.Points);
                if (totalCorrectAnswers == ok) totalScore = question.Points;

                _score += Math.Round(totalScore, 2);

                if (hasIncorrectAnswers) await _questionService.IncrementIncorrectAnswerCountAsync(question.Id);

                if (MessageBox.Show($"DU hast {_score}/ {_totalPoints} Punkte ({_score / _totalPoints * 100}%) erreicht!", "Fertig :)", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK) await StartPage();
            };

            _dynamicPanel.Controls.Add(btnSubmit);
            _currentY += btnSubmit.Height + 10;
        }
    }


    private Label CreateLabel(string text, int fontSize, FontStyle fontStyle, int x = 0, int checkboxwith = 0)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Location = new Point(x, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font.FontFamily, fontSize, fontStyle),
            BackColor = Color.Transparent,
            MaximumSize = new Size(_dynamicPanel.Width - checkboxwith, 0)
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

        var backButton = CreateButton("Zurück", 100, 30, 10, _dynamicPanel.Height - 40);
        backButton.Click += (_, _) => StartPage();
        _dynamicPanel.Controls.Add(backButton);
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
        _cmbQuestionType.SelectedIndexChanged += CmbQuestionType_SelectedIndexChanged; // Event hinzufügen

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

        // TextBoxen für Checkbox links und rechts (initial deaktiviert)
        var lblCheckboxLeft = new Label
        {
            Name = "lblCheckboxLeft",
            Location = new Point(350, _currentY),
            Text = "links:",
            Width = 40,
            Enabled = false
        };
        var txtCheckboxLeft = new TextBox
        {
            Name = "txtCheckboxLeft",
            Location = new Point(390, _currentY),
            Width = 100,
            Enabled = false
        };

        var lblCheckboxRight = new Label
        {
            Name = "lblCheckboxRight",
            Location = new Point(500, _currentY),
            Text = "rechts:",
            Width = 45,
            Enabled = false
        };
        var txtCheckboxRight = new TextBox
        {
            Name = "txtCheckboxRight",
            Location = new Point(545, _currentY),
            Width = 100,
            Enabled = false
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
        btnAddQuestion.Click += async (_, _) => await SaveQuestionAsync(txtQuestion, txtPoints, txtCheckboxLeft, txtCheckboxRight);


        _dynamicPanel.Controls.Add(lblQuestion);
        _dynamicPanel.Controls.Add(txtQuestion);
        _dynamicPanel.Controls.Add(lblQuestionType);
        _dynamicPanel.Controls.Add(_cmbQuestionType);
        _dynamicPanel.Controls.Add(lblPoints);
        _dynamicPanel.Controls.Add(txtPoints);
        _dynamicPanel.Controls.Add(btnAddAnswerField);
        _dynamicPanel.Controls.Add(btnAddQuestion);
        _dynamicPanel.Controls.Add(lblCheckboxLeft);
        _dynamicPanel.Controls.Add(txtCheckboxLeft);
        _dynamicPanel.Controls.Add(lblCheckboxRight);
        _dynamicPanel.Controls.Add(txtCheckboxRight);
    }

    private void CmbQuestionType_SelectedIndexChanged(object sender, EventArgs e)
    {
        var selectedType = (QuestionType)_cmbQuestionType.SelectedValue!;

        // Überprüfe, ob der ausgewählte Typ 'Kreuz' ist
        var isKreuz = selectedType == QuestionType.Kreuz;

        // Steuere die Sichtbarkeit und Aktivierung der TextBoxen für Checkboxen
        foreach (Control control in _dynamicPanel.Controls)
        {
            if (control is TextBox { Name: "txtCheckboxLeft" or "txtCheckboxRight" } txt) txt.Enabled = isKreuz; // Aktivieren/Deaktivieren der TextBoxen

            if (control is Label { Name: "lblCheckboxLeft" or "lblCheckboxRight" } lbl) lbl.Enabled = isKreuz; // Sichtbarkeit der Labels steuern
        }
    }


    private void BtnAddAnswerField_Click(object? sender, EventArgs e)
    {
        var answerTextBox = new TextBox
        {
            Location = new Point(60, _currentY),
            Width = _dynamicPanel.Width - 70,
            Multiline = true
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

    private async Task SaveQuestionAsync(TextBox txtQuestion, TextBox txtPoints, TextBox links, TextBox rechts)
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
            QuestionText = txtQuestion.Text.Trim().Replace("\r", " ").Replace("\n", "").Replace("  ", " "),
            QuestionType = (QuestionType)(_cmbQuestionType.SelectedValue ?? QuestionType.Auswahl),
            Points = points,
            Header1 = links.Text.Trim().Replace("\r", " ").Replace("\n", "").Replace("  ", " "),
            Header2 = rechts.Text.Trim().Replace("\r", " ").Replace("\n", "").Replace("  ", " ")
        };

        var answers = _answerFields.Select(field => new Answer
        {
            AnswerText = field.AnswerTextBox.Text.Trim().Replace("\r", " ").Replace("\n", "").Replace("  ", " "),
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

    private async void btnExport_Click(object sender, EventArgs e)
    {
        using var saveFileDialog = new SaveFileDialog();
        saveFileDialog.Filter = "JSON files (*.json)|*.json";
        saveFileDialog.Title = "Exportieren von Fragen und Antworten";

        if (saveFileDialog.ShowDialog() == DialogResult.OK)
        {
            await _importExportService.ExportQuestionsToJsonAsync(saveFileDialog.FileName);
            MessageBox.Show("Export abgeschlossen!", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private async void btnImport_Click(object sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "JSON files (*.json)|*.json";
        openFileDialog.Title = "Importieren von Fragen und Antworten";

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            await _importExportService.ImportQuestionsFromJsonAsync(openFileDialog.FileName);
            MessageBox.Show("Import abgeschlossen!", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}