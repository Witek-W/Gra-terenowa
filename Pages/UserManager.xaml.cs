using GpsApplication.Models;
using GpsApplication.Pages.Popups;
using System.ComponentModel;

namespace GpsApplication;

public partial class UserManager : ContentPage
{
	private readonly Auth _auth;
	private readonly AppDbContext _context;
	private readonly MainPage _main;
	public UserManager()
	{
		_context = new AppDbContext();
		_auth = new Auth(_context);
		_main = new MainPage();
		InitializeComponent();
		CheckLoggedUser();
	}
	//Prze³¹czenia na stronê z tabel¹ wyników
	private async void LeaderboardButton(object sender, EventArgs e)
	{
		if(CheckInternet())
		{
			var page = new Leaderboard();
			await Navigation.PushAsync(page);
		} else
		{
			Leaderboard.IsEnabled = false;
			Leaderboard.Text = "Brak sieci";
			await Task.Delay(4000);
			Leaderboard.Text = "Tabela wyników";
			Leaderboard.IsEnabled = true;
		}
	}
	//Logowanie u¿ytkownika
	private async void Login(object sender, EventArgs e)
	{
		await _auth.Login(LoginLogin.Text, PasswordLogin.Text);
		await Navigation.PopAsync();
	}
	//Rejestracja u¿ytkownika
	private async void Register(object sender, EventArgs e)
	{
		await _auth.Register(NameRegister.Text, SurnameRegister.Text, LoginRegister.Text, PasswordRegister.Text);
		ShowLoginForm(null,null);
	}
	//Sprawdzanie czy u¿ytkownik jest zalogowany
	private async void CheckLoggedUser()
	{
		var checkUser = await SecureStorage.GetAsync("user_login");
		if(checkUser != null)
		{
			var name = await SecureStorage.GetAsync("user_name");
			string idstring = await SecureStorage.GetAsync("user_id");
			int iduser = Convert.ToInt16(idstring);
			int score = 0;
			if(CheckInternet())
			{
				score = _auth.ReturnUserScore(iduser);
				ScoreUser.Text = "Zdobyte punkty: " + $"{score}";
			} else
			{
				ScoreUser.Text = "Offline";
			}
			LoggedUser.IsVisible = true;
			Title = "Zalogowano";
			LoggedLabel.Text = "Witaj " + $"{name}";
		} else
		{
			LoginLayout.IsVisible = true;
			Title = "Zaloguj siê";
		}
	}
	//Wylogowywanie u¿ytkownika
	private async void LogoutButton(object sender, EventArgs e)
	{
		if(CheckInternet())
		{

			await _main.UpdateDataBaseOfflineTxt();
			SecureStorage.Remove("user_login");
			SecureStorage.Remove("user_id");
			SecureStorage.Remove("user_name");
			await Navigation.PopAsync();
		} else
		{
			LogoutButtonName.IsEnabled = false;
			LogoutButtonName.Text = "B³¹d sieci";
			await Task.Delay(4000);
			LogoutButtonName.Text = "Wyloguj";
			LogoutButtonName.IsEnabled = true;
		}
	}
	//Aktywowanie przycisku Rejestracji dopiero jak wszystkie pola bêd¹ wpisane
	private async void RegisterForm(object sender, TextChangedEventArgs e)
	{
		if(!CheckInternet())
		{
			RegisterButton.IsEnabled = false;
			RegisterButton.Text = "B³¹d sieci!";
		}

		if (!string.IsNullOrEmpty(LoginRegister.Text) && !string.IsNullOrEmpty(PasswordRegister.Text)
			&& !string.IsNullOrEmpty(NameRegister.Text) && !string.IsNullOrEmpty(SurnameRegister.Text)
			&& CheckInternet())
		{
			RegisterButton.IsEnabled = true;
			RegisterButton.Text = "Zarejestruj";
		} else
		{
			RegisterButton.IsEnabled = false;
		}
	}
	//Aktywowanie przycisku Logowania dopiero jak wszystkie pola bêd¹ wpisane
	private async void LoginForm(object sender, TextChangedEventArgs e)
	{
		if(!CheckInternet())
		{
			LoginButton.IsEnabled = false;
			LoginButton.Text = "B³¹d sieci!";
		}

		if (!string.IsNullOrEmpty(LoginLogin.Text) && !string.IsNullOrEmpty(PasswordLogin.Text) && CheckInternet())
		{
			LoginButton.IsEnabled = true;
			LoginButton.Text = "Zaloguj";
		}
		else
		{
			LoginButton.IsEnabled = false;
		}
	}
	//Prze³¹cznie StackLayout
	private void ShowRegisterForm(object sender, EventArgs e)
	{
		Title = "Zarejestruj siê";
		LoginLayout.IsVisible = false;
		RegisterLayout.IsVisible = true;
	}
	private void ShowLoginForm(object sender, EventArgs e)
	{
		Title = "Zaloguj siê";
		RegisterLayout.IsVisible = false;
		LoginLayout.IsVisible = true;
	}
	//Sprawdzanie po³¹czenia z internetem
	public bool CheckInternet()
	{
		var network = Connectivity.Current.NetworkAccess;
		if (network == NetworkAccess.Internet)
		{
			return true;
		} else
		{
			return false;
		}
	}
}