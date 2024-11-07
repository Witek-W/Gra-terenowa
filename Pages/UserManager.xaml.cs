using GpsApplication.Models;
using GpsApplication.Pages.ManageApp;
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
	//Prze��czanie na stron� z dodawaniem pytania do quizu
	private async void OpenAddQuestionPage(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new AddQuestion());
	}
	//Prze��czanie na stron� z dodawaniem nowego punktu
	private async void OpenAddNewPointPage(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new AddNewPoint());
	}
	//Prze��czenia na stron� z tabel� wynik�w
	private async void LeaderboardButton(object sender, EventArgs e)
	{
		if(CheckInternet())
		{
			var page = new Leaderboard();
			await Navigation.PushAsync(page);
		} else
		{
			//UI Uzytkownika
			Leaderboard.IsEnabled = false;
			Leaderboard.Text = "Brak sieci";
			//UI admina
			LeaderboardAdmin.IsEnabled = false;
			LeaderboardAdmin.Text = "Brak sieci";
			await Task.Delay(4000);
			//UI u�ytkownika
			Leaderboard.Text = "Tabela wynik�w";
			Leaderboard.IsEnabled = true;
			//UI admina
			LeaderboardAdmin.Text = "Tabela wynik�w";
			LeaderboardAdmin.IsEnabled = true;
		}
	}
	//Logowanie u�ytkownika
	private async void Login(object sender, EventArgs e)
	{
		await _auth.Login(LoginLogin.Text, PasswordLogin.Text);
		var check = await SecureStorage.GetAsync("user_login");
		if(check != null)
		{
			await Navigation.PopAsync();
		} else
		{
			LoginButton.IsEnabled = false;
			LoginButton.Text = "B��dne dane";
			LoginLogin.Text = "";
			PasswordLogin.Text = "";
			await Task.Delay(4000);
			LoginButton.Text = "Zaloguj";
			LoginButton.IsEnabled = true;
		}
	}
	//Rejestracja u�ytkownika
	private async void Register(object sender, EventArgs e)
	{
		await _auth.Register(NameRegister.Text, SurnameRegister.Text, LoginRegister.Text, PasswordRegister.Text);
		ShowLoginForm(null,null);
	}
	//Sprawdzanie czy u�ytkownik jest zalogowany
	private async void CheckLoggedUser()
	{
		var checkUser = await SecureStorage.GetAsync("user_login");
		if(checkUser != null)
		{
			string role = await SecureStorage.GetAsync("user_role");
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
			if(role == "1")
			{
				LoggedAdmin.IsVisible = true;
			} else
			{
				LoggedUser.IsVisible = true;
			}
			Title = "Zalogowano";
			LoggedLabel.Text = "Witaj " + $"{name}";
			LoggedLabelAdmin.Text = "Witaj " + $"{name}";
		} else
		{
			LoginLayout.IsVisible = true;
			Title = "Zaloguj si�";
		}
	}
	//Wylogowywanie u�ytkownika
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
			//UI u�ytkownika
			LogoutButtonName.IsEnabled = false;
			LogoutButtonName.Text = "B��d sieci";
			//UI admina
			LogoutButtonNameAdmin.IsEnabled = false;
			LogoutButtonNameAdmin.Text = "B��d sieci";
			await Task.Delay(4000);
			//UI u�ytkownika
			LogoutButtonName.Text = "Wyloguj";
			LogoutButtonName.IsEnabled = true;
			//UI admina
			LogoutButtonNameAdmin.Text = "Wyloguj";
			LogoutButtonNameAdmin.IsEnabled = true;
		}
	}
	//Aktywowanie przycisku Rejestracji dopiero jak wszystkie pola b�d� wpisane
	private async void RegisterForm(object sender, TextChangedEventArgs e)
	{
		if(!CheckInternet())
		{
			RegisterButton.IsEnabled = false;
			RegisterButton.Text = "B��d sieci!";
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
	//Aktywowanie przycisku Logowania dopiero jak wszystkie pola b�d� wpisane
	private async void LoginForm(object sender, TextChangedEventArgs e)
	{
		if(!CheckInternet())
		{
			LoginButton.IsEnabled = false;
			LoginButton.Text = "B��d sieci!";
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
	//Prze��cznie StackLayout
	private void ShowRegisterForm(object sender, EventArgs e)
	{
		Title = "Zarejestruj si�";
		LoginLayout.IsVisible = false;
		RegisterLayout.IsVisible = true;
	}
	private void ShowLoginForm(object sender, EventArgs e)
	{
		Title = "Zaloguj si�";
		RegisterLayout.IsVisible = false;
		LoginLayout.IsVisible = true;
	}
	//Sprawdzanie po��czenia z internetem
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