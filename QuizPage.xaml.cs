using GpsApplication.Models;
using Org.BouncyCastle.Math.Field;

namespace GpsApplication;

public partial class QuizPage : ContentPage
{
	private List<Quiz> _quizList;
	private int countIndex = 0;
	private CheckBox lastCheck;
	private int correctAnswer;
	private int Score = 0;
	private Auth _auth;
	private AppDbContext _context;
	public QuizPage(List<Quiz> quiz)
	{
		_context = new AppDbContext();
		_auth = new Auth(_context);
		InitializeComponent();
		_quizList = quiz;
		RenderQuiz();
	}
	private async void RenderQuiz()
	{
		string userID = await SecureStorage.GetAsync("user_id");
		var placename = _quizList[0].PlaceName;

		try
		{
			if(_auth.CheckUserQuizHistory(userID,placename))
			{
				QuizLayout.IsVisible = false;
				ErrorLayout.IsVisible = true;
				return;
			}
		}catch(Exception e)
		{

		}

		CheckBox1.IsChecked = false;
		CheckBox2.IsChecked = false;
		CheckBox3.IsChecked = false;
		CheckBox4.IsChecked = false;
		if(countIndex < _quizList.Count)
		{
			QuizLayout.IsVisible = true;
			SummaryLayout.IsVisible = false;
			Title = "Pytanie nr." + $"{countIndex + 1}";
			var currentQuiz = _quizList[countIndex];
			QuestionLabel.Text = currentQuiz.Question;
			CheckBoxOption1.Text = currentQuiz.Answer1;
			CheckBoxOption2.Text = currentQuiz.Answer2;
			CheckBoxOption3.Text = currentQuiz.Answer3;
			CheckBoxOption4.Text = currentQuiz.Answer4;
			correctAnswer = currentQuiz.CorrectAnswer;
			countIndex++;
		} else
		{
			QuizLayout.IsVisible = false;
			SummaryLayout.IsVisible = true;
			Title = "Podsumowanie";
			ScoreLabel.Text = $"{Score}" + "/" + $"{_quizList.Count}";
			int id = Convert.ToInt32(userID);
			int userPoints = _auth.ReturnUserScore(id);
			userPoints += Score;
			await _auth.UpdateUserScore(id, userPoints);
			await _auth.AddingUserToQuizHistory(id, _quizList[0].PlaceName);
		}
	}
	private async void ReturnToMainPage(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
	private void ConfirmButton(object sender, EventArgs e)
	{
		int currentAnswer = TranslateCheckBoxToAnswer(lastCheck);
		if(currentAnswer == correctAnswer)
		{
			Score++;
		}
		RenderQuiz();
	}
	private int TranslateCheckBoxToAnswer(CheckBox check)
	{
		if(check == CheckBox1) return 1;
		if(check == CheckBox2) return 2;
		if(check == CheckBox3) return 3;
		if(check == CheckBox4) return 4;
		return -1;
	}
	private void CheckBoxChanged(object sender, CheckedChangedEventArgs e)
	{
		var selectedCheckbox = (CheckBox)sender;
		if(!e.Value)
		{
			CheckBoxButton.IsEnabled = false;
			return;
		}
		
		if(lastCheck == null)
		{

		} else if(lastCheck != null && lastCheck != selectedCheckbox)
		{
			lastCheck.IsChecked = false;
		}
		lastCheck = selectedCheckbox;
		CheckBoxButton.IsEnabled = true;
	}
}