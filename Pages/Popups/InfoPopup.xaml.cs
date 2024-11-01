namespace GpsApplication.Pages.Popups;

public partial class InfoPopup : ContentPage
{
	private readonly int TimeToClose = 5;
	public InfoPopup(string error)
	{
		InitializeComponent();
		PopupLabel.Text = error;
		CloseTimer();
	}
	private async Task CloseTimer()
	{
		await Task.Delay(TimeToClose * 1000);
		Close();
	}
	private async Task Close()
	{
		await Navigation.PopModalAsync(animated: true);
	}
}