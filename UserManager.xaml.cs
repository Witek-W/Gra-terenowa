using GpsApplication.Models;
using System.ComponentModel;

namespace GpsApplication;

public partial class UserManager : ContentPage
{
	private readonly Auth _auth;
	private readonly AppDbContext _context;
	public UserManager()
	{
		_context = new AppDbContext();
		_auth = new Auth(_context);
		InitializeComponent();
		CheckLoggedUser();
	}
	//Logowanie u�ytkownika
	private async void Login(object sender, EventArgs e)
	{
		await _auth.Login(LoginLogin.Text, PasswordLogin.Text);
		await Navigation.PopAsync();
	}
	//Rejestracja u�ytkownika
	private async void Register(object sender, EventArgs e)
	{
		await _auth.Register(NameRegister.Text, SurnameRegister.Text, LoginRegister.Text, PasswordRegister.Text);
		await Navigation.PopAsync();
	}
	//Sprawdzanie czy u�ytkownik jest zalogowany
	private async void CheckLoggedUser()
	{
		var checkUser = await SecureStorage.GetAsync("user_login");
		if(checkUser != null)
		{
			var name = await SecureStorage.GetAsync("user_name");
			LoggedUser.IsVisible = true;
			LoggedLabel.Text = "Witaj " + $"{name}";
		} else
		{
			LoginLayout.IsVisible = true;
		}
	}
	//Wylogowywanie u�ytkownika
	private async void LogoutButton(object sender, EventArgs e)
	{
		_auth.Logout();
		await Navigation.PopAsync();
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
	private bool CheckInternet()
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