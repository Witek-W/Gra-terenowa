using GpsApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace GpsApplication.Pages.ManageApp;

public partial class AddNewPoint : ContentPage
{
	private readonly AppDbContext _context;
	public AddNewPoint()
	{
		_context = new AppDbContext();
		InitializeComponent();
	}
	private async void AddNewPointButton(object sender, EventArgs e)
	{
		//Sprawdzanie czy taka nazwa ju¿ istnieje
		var checkName = await _context.GamePoints.SingleOrDefaultAsync(p => p.Name == NewPointName.Text);
		if(checkName == null)
		{
			try
			{
				string latlong = NewPointLatLong.Text;
				latlong = latlong.Trim('(', ')');
				string[] splittedLatLong = latlong.Split(",");
				double first = double.Parse(splittedLatLong[0] + "," + splittedLatLong[1]);
				double second = double.Parse(splittedLatLong[2].Trim() + "," + splittedLatLong[3]);
				var game = new GamePoints
				{

					Name = NewPointName.Text.Trim(),
					Latitude = first,
					Longitude = second
				};
				await _context.GamePoints.AddAsync(game);
				await _context.SaveChangesAsync();
				NewPointName.Text = "";
				NewPointLatLong.Text = "";
				await ClosePage();
			}catch(Exception ex)
			{
				AddPointButton.IsEnabled = false;
				AddPointButton.Text = "B³¹d";
				NewPointName.Text = "";
				NewPointLatLong.Text = "";
				await Task.Delay(4000);
				AddPointButton.Text = "Dodaj punkt";
			}
		} else
		{
			AddPointButton.IsEnabled = false;
			AddPointButton.Text = "Punkt ju¿ istnieje";
			NewPointName.Text = "";
			await Task.Delay(4000);
			AddPointButton.Text = "Dodaj punkt";
		}
	}
	private void EntryAddPointChanged(object sender, TextChangedEventArgs e)
	{
		if(!string.IsNullOrEmpty(NewPointName.Text) && !string.IsNullOrEmpty(NewPointLatLong.Text))
		{
			AddPointButton.IsEnabled = true;
		} else
		{
			AddPointButton.IsEnabled = false;
		}
	}
	private async Task ClosePage()
	{
		await Navigation.PopAsync();
	}
}