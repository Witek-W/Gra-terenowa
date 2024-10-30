using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GpsApplication.Models
{
	public  class AppDbContext : DbContext
	{
		public DbSet<User> Users { get; set; }
		public DbSet<GamePoints> GamePoints { get; set; }
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			var config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.Build();
			string connectionString = config.GetConnectionString("DefaultConnection");

			optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
		}
	}
} 
