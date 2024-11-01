using GpsApplication.Models;
using Microsoft.EntityFrameworkCore;

namespace GpsApplication.Pages.Popups;

public partial class Leaderboard : ContentPage
{
	private readonly Auth _auth;
	private readonly AppDbContext _context;
	private readonly UserManager _user;
	private readonly int UsersRanking = 10;
	public Leaderboard()
	{
		_context = new AppDbContext();
		_auth = new Auth(_context);
		_user = new UserManager();
		InitializeComponent();
		CheckUsers();
	}
	private async void CheckUsers()
	{
		if (_user.CheckInternet())
		{
			var result = await _context.User
								.OrderByDescending(p => p.AllPoints)
								.Take(UsersRanking)
								.ToListAsync();
			for(int i = 0; i < result.Count; i++)
			{
				result[i].ID = i + 1;
			}

			LeaderboardCollection.ItemsSource = result;
		}
	}
}