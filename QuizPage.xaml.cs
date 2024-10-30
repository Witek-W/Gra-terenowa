using GpsApplication.Models;

namespace GpsApplication;

public partial class QuizPage : ContentPage
{
	private List<Quiz> _quizList;
	public QuizPage(List<Quiz> quiz)
	{
		InitializeComponent();
		_quizList = quiz;
	}
}