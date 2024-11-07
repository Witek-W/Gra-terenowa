using GpsApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace GpsApplication.Pages.ManageApp;

public partial class AddQuestion : ContentPage
{
	private readonly AppDbContext _context;
	private string selectedItemString;
	private CheckBox lastCheck;
	private int correctAns;
	public AddQuestion()
	{
		_context = new AppDbContext();
		InitializeComponent();
		LoadPoints();
	}
	//£adowanie wszystkich wa¿nych punktów do Pickera
	private async void LoadPoints()
	{
		var itemsString = new List<string>();
		var allPoints = await _context.GamePoints.ToListAsync();
		foreach(var point in allPoints)
		{
			itemsString.Add(point.Name);
		}
		PickerName.ItemsSource = itemsString;
	}

	//Funkcja wywo³ywana przy zmianie Picker
	private async void PickerChanged(object sender, EventArgs e)
	{
		var picker = (Picker)sender;
		int selectedIndex = picker.SelectedIndex;
		if(selectedIndex != -1)
		{
			selectedItemString = picker.Items[selectedIndex];
		} else
		{
			selectedItemString = string.Empty;
		}
	}
	//Sprawdzanie czy wszystko jest zaznaczone przed dodaniem pytania
	private void IsEverythingFilled()
	{
		if(!string.IsNullOrEmpty(QuestionEntry.Text) && !string.IsNullOrEmpty(Answer1Entry.Text) 
			&& !string.IsNullOrEmpty(Answer2Entry.Text) && !string.IsNullOrEmpty(Answer3Entry.Text)
			&& !string.IsNullOrEmpty(Answer4Entry.Text) && lastCheck != null && selectedItemString != null)
		{
			AddQuestionButton.IsEnabled = true;
		} else
		{
			AddQuestionButton.IsEnabled = false;
		}
	}
	//Funkcja która zostawia tylko jeden aktywny checkbox
	private void CheckBoxChanged(object sender, CheckedChangedEventArgs e)
	{
		var selectedCheckbox = (CheckBox)sender;
		if (!e.Value)
		{
			return;
		}
		if (lastCheck == null)
		{

		}
		else if (lastCheck != null && lastCheck != selectedCheckbox)
		{
			lastCheck.IsChecked = false;
		}
		lastCheck = selectedCheckbox;
		IsEverythingFilled();
	}
	//Zmiana entry
	private void EntryChangedQuiz(object sender, TextChangedEventArgs e)
	{
		IsEverythingFilled();
	}
	//Funkcja uruchamiana po kliknieciu w entry
	private void OnEntryFocused(object sender, FocusEventArgs e)
	{
		if (e.IsFocused)
		{
			VerticalStackLayoutMain.Padding = new Thickness(0, 0, 0, 310);
		}
	}
	private async void QuestionAdd(object sender, EventArgs e)
	{
		CheckboxTranslate();
		var question = new Quiz
		{
			PlaceName = selectedItemString,
			Question = QuestionEntry.Text,
			Answer1 = Answer1Entry.Text,
			Answer2 = Answer2Entry.Text,
			Answer3 = Answer3Entry.Text,
			Answer4 = Answer4Entry.Text,
			CorrectAnswer = correctAns
		};
		await _context.Quiz.AddAsync(question);
		await _context.SaveChangesAsync();
		await Navigation.PopAsync();
	}
	private void CheckboxTranslate()
	{
		if(lastCheck == Checkbox1)
		{
			correctAns = 1;
		} else if(lastCheck == Checkbox2)
		{
			correctAns = 2;
		} else if(lastCheck == Checkbox3)
		{
			correctAns = 3;
		} else
		{
			correctAns = 4;
		}
	}
}