// To play the quiz:
//
// 1. Run the script
// 2. Go through the questions; 10 random questions are selected for the quiz from a pool.
//
// This quiz is currently only configured to run on Tabular Editor 3. It is possible to configure it for Tabular Editor 2.
// Note: Some 'Read More' links are only configured to go to the 'Tabular Editor' docs. 
// Note: Questions may be updated over time.

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

// Hide the wait form in the C# Script dialog
ScriptHelper.WaitFormVisible = false;

// Define a class to hold the question data
class QuizQuestion
{
    public string Question { get; set; }
    public List<string> PossibleAnswers { get; set; }
    public string Context { get; set; }
    public string Answer { get; set; }
    public string Link { get; set; }
    public string QuestionType { get; set; }
    public int Points { get; set; }
}

class Quiz
{
    // Define the font and font size for the dialogs
    private static readonly System.Drawing.Font DialogFont = new System.Drawing.Font("Segoe UI Semibold", 11);

    // Get the questions from a remote git repo
    private static readonly HttpClient client = new HttpClient();
    private static List<QuizQuestion> QuizQuestions = new List<QuizQuestion>();
    private static Image _backgroundImage;

    async static Task LoadQuizQuestions()
    {
        string json = await client.GetStringAsync("https://raw.githubusercontent.com/data-goblin/TabularEditorQuiz/main/quizquestions.json");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Parse the questions from the JSON in the repo
        QuizQuestions = JsonSerializer.Deserialize<List<QuizQuestion>>(json, options);
    }

    private static int CurrentScore = 0;
    private static int CurrentCorrectAnswers = 0;

    // Define a method to shuffle the order of the questions
    private static List<QuizQuestion> ShuffleQuestions(List<QuizQuestion> questions)
    {
        Random rng = new Random();
        int n = questions.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            QuizQuestion value = questions[k];
            questions[k] = questions[n];
            questions[n] = value;
        }
        return questions.Take(10).ToList();
    }

    // Define a method to shuffle the order of the answers for each question
    private static QuizQuestion ShuffleAnswers(QuizQuestion question)
    {
        Random rng = new Random();
        int n = question.PossibleAnswers.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            string value = question.PossibleAnswers[k];
            question.PossibleAnswers[k] = question.PossibleAnswers[n];
            question.PossibleAnswers[n] = value;
        }
        question.PossibleAnswers.Add("All of the Above");
        question.PossibleAnswers.Add("None of the Above");
        return question;
    }

    // Define a method to display the quiz question dialog
    private static string DisplayQuizQuestionDialog(QuizQuestion question)
    {
        // Shuffle the order of the answers
        QuizQuestion shuffledQuestion = ShuffleAnswers(question);

        // Create a new dialog to display the question and answer options
        Form quizQuestionDialog = new Form();
        quizQuestionDialog.Text = "Question";
        quizQuestionDialog.Font = DialogFont;
        quizQuestionDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
        quizQuestionDialog.StartPosition = FormStartPosition.CenterScreen;
        quizQuestionDialog.AutoSize = true;
        quizQuestionDialog.MinimumSize = new Size(700, 0);
        quizQuestionDialog.MaximumSize = new Size(700, 1500);

        //quizQuestionDialog.BackColor = ColorTranslator.FromHtml("#f3f0ea");

        // Add the question text to the dialog
        Label questionLabel = new Label();
        questionLabel.Text = shuffledQuestion.Question;
        questionLabel.AutoSize = true;
        questionLabel.Padding = new Padding(25);
        questionLabel.MinimumSize = new Size(675, 0);
        questionLabel.MaximumSize = new Size(675, 1400);
        questionLabel.Location = new System.Drawing.Point(10, 10);
        quizQuestionDialog.Controls.Add(questionLabel);

        // Add the answer options to the dialog
        int y = questionLabel.Bottom + 10;
        List<RadioButton> answerOptions = new List<RadioButton>();
        for (int i = 0; i < shuffledQuestion.PossibleAnswers.Count; i++)
        {
            RadioButton radioButton = new RadioButton();
            string answerText = shuffledQuestion.PossibleAnswers[i];

            // Calculate the maximum line length based on the width of questionLabel
            int maxLineLength = 80;

            // Split the text into multiple lines if it's too long
            if (answerText.Length > maxLineLength)
            {
                string[] words = answerText.Split(' ');
                string line = "";
                answerText = "";
                for (int j = 0; j < words.Length; j++)
                {
                    if ((line + words[j]).Length > maxLineLength)
                    {
                        answerText += line + "\n";
                        line = "";
                    }
                    line += words[j] + " ";
                }
                answerText += line.Trim();
            }

            radioButton.Text = answerText;
            radioButton.AutoSize = true;
            radioButton.Padding = new Padding(30, 0, 0, 0);
            radioButton.MaximumSize = new Size(questionLabel.Width - 100, 0);
            radioButton.Font = new System.Drawing.Font("Segoe UI", 10);
            radioButton.Location = new System.Drawing.Point(10, y);
            quizQuestionDialog.Controls.Add(radioButton);
            answerOptions.Add(radioButton);
            y = radioButton.Bottom + 15;
        }


        // Add the OK and Cancel buttons to the dialog
        Button okButton = new Button();
        okButton.Text = "Submit";
        okButton.DialogResult = DialogResult.OK;
        okButton.Font = DialogFont;
        okButton.AutoSize = true;
        okButton.MinimumSize = new System.Drawing.Size(75, 0);
        okButton.Location = new System.Drawing.Point(10, y + 25);
        quizQuestionDialog.Controls.Add(okButton);

        // Cancel button
        Button cancelButton = new Button();
        cancelButton.Text = "Cancel";
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Font = DialogFont;
        cancelButton.AutoSize = true;
        cancelButton.MinimumSize = new System.Drawing.Size(75, 0);
        cancelButton.Location = new System.Drawing.Point(okButton.Right + 10, y + 25);
        quizQuestionDialog.Controls.Add(cancelButton);

        // Set the dialog size to fit all the controls
        quizQuestionDialog.ClientSize = new System.Drawing.Size(Math.Max(okButton.Right, cancelButton.Right) + 10, cancelButton.Bottom + 10);

        // Display the dialog and wait for the user to make a selection
        DialogResult result = quizQuestionDialog.ShowDialog();
        if (result == DialogResult.Cancel)
        {
            // If the user cancels, return null to indicate the quiz should end
            return null;
        }
        else
        {
            // If the user selects OK, determine which answer they selected
            string selectedAnswer = "";
            foreach (RadioButton answerOption in answerOptions)
            {
                if (answerOption.Checked)
                {
                    selectedAnswer = answerOption.Text.Replace("\n", "");
                    break;
                }
            }

            // Show feedback to the user about their answer
            ShowQuizQuestionFeedback(shuffledQuestion, selectedAnswer);

            return selectedAnswer;
        }
    }

    // Define a method to display feedback to the user after they answer a quiz question
    static void ShowQuizQuestionFeedback(QuizQuestion question, string selectedAnswer)
    {
        string feedbackMessage;
        int pointsEarned = 0;

        if (selectedAnswer.Equals(question.Answer))
        {
            pointsEarned = question.Points;
            CurrentScore += pointsEarned;
            CurrentCorrectAnswers++;
            feedbackMessage = $"That's Correct!\n\n{question.Context}\n\nCurrent score: {CurrentScore} points; {CurrentCorrectAnswers}/{10} ({(int)Math.Round((double)CurrentCorrectAnswers / 10 * 100)}%).";
        }
        else
        {
            feedbackMessage = $"Sorry, that is not correct. \nThe correct answer is: '{question.Answer}'.\n\n{question.Context}\n\nCurrent score: {CurrentScore} points; {CurrentCorrectAnswers}/{10} ({(int)Math.Round((double)CurrentCorrectAnswers / 10 * 100)}%).";
        }

        // Feedback dialog
        using (var feedbackDialog = new Form())
        {
            feedbackDialog.StartPosition = FormStartPosition.CenterScreen;
            feedbackDialog.AutoSize = false;
            feedbackDialog.Size = new Size(700, 600);
            feedbackDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            feedbackDialog.Text = "Feedback";

            // Feedback message
            var messageLabel = new Label();
            messageLabel.AutoSize = false;
            messageLabel.Size = new Size(700, 500);
            messageLabel.Location = new Point(0, 0);
            messageLabel.Padding = new Padding(25);
            messageLabel.Font = new System.Drawing.Font("Segoe UI", 11);
            messageLabel.Text = feedbackMessage;

            int y = feedbackDialog.Bottom - 50;

            // Read more button in the feedback form
            Button readMoreButton = new Button();
            readMoreButton.Text = "Read More";
            readMoreButton.DialogResult = DialogResult.OK;
            readMoreButton.Font = DialogFont;
            readMoreButton.AutoSize = true;
            readMoreButton.MinimumSize = new System.Drawing.Size(75, 0);
            readMoreButton.Location = new System.Drawing.Point(messageLabel.Left + 25, y - readMoreButton.Height - 10);
            readMoreButton.Click += (sender, e) =>
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = question.Link,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                };

            // Next question button in the feedback form
            var nextQuestionButton = new Button();
            nextQuestionButton.Text = "Next Question";
            nextQuestionButton.AutoSize = true;
            nextQuestionButton.MinimumSize = new System.Drawing.Size(75, 0);
            nextQuestionButton.Location = new System.Drawing.Point(messageLabel.Left + readMoreButton.Width + 60, y - nextQuestionButton.Height - 10);
            nextQuestionButton.Font = DialogFont;
            nextQuestionButton.Click += (sender, e) =>
            {
                feedbackDialog.Close();
            };

            feedbackDialog.Controls.Add(messageLabel);
            feedbackDialog.Controls.Add(readMoreButton);
            feedbackDialog.Controls.Add(nextQuestionButton);

            feedbackDialog.ShowDialog();
        }

    }

    // Define a method to calculate the quiz score
    public static void CalculateQuizScore(List<string> userAnswers)
    {
        int totalPoints = 0;
        int totalCorrect = 0;

        // Iterate over the user's answers and compare to the correct answers
        for (int i = 0; i < 10; i++)
        {
            if (userAnswers[i] == QuizQuestions[i].Answer)
            {
                totalPoints += QuizQuestions[i].Points;
                totalCorrect++;
            }
        }

        // Display feedback to the user about their final score and quiz completion
        double percentCorrect = (double)totalCorrect / 10 * 100;
        int roundedPercent = (int)Math.Round(percentCorrect);

        string finalFeedbackMessage = $"You scored {totalPoints}.\n\nYou got {totalCorrect} out of {10} questions correct ({roundedPercent}%).";
        MessageBoxIcon finalFeedbackIcon = MessageBoxIcon.Information;
        if (roundedPercent < 50)
        {
            finalFeedbackMessage += "\n\nBetter luck next time!";
            finalFeedbackIcon = MessageBoxIcon.Warning;
        }
        MessageBox.Show(finalFeedbackMessage, "Quiz Complete", MessageBoxButtons.OK, finalFeedbackIcon);
    }

    // Method to run the quiz
    public static async Task StartQuiz()
    {
        // Get the quiz questions
        await LoadQuizQuestions();

        // Shuffle the order of the questions and reset the current score and correct answer count
        List<QuizQuestion> shuffledQuestions = ShuffleQuestions(QuizQuestions);
        CurrentScore = 0;
        CurrentCorrectAnswers = 0;

        // Iterate over the shuffled questions and ask the user for their answer
        List<string> userAnswers = new List<string>();
        foreach (QuizQuestion question in shuffledQuestions)
        {
            string userAnswer = DisplayQuizQuestionDialog(question);
            if (userAnswer == null)
            {
                // If the user cancels, end the quiz
                MessageBox.Show("Quiz canceled.", "Quiz Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            userAnswers.Add(userAnswer);
        }

        // Calculate the final quiz score
        CalculateQuizScore(userAnswers);
    }
}

// Start the Quiz
Quiz.StartQuiz();