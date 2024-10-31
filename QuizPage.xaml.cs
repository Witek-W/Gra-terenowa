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
	public QuizPage(List<Quiz> quiz)
	{
		InitializeComponent();
		_quizList = quiz;
		RenderQuiz();
	}
	private void RenderQuiz()
	{
		if(countIndex < _quizList.Count)
		{
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
			//Okno z podsumowaniem
		}
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