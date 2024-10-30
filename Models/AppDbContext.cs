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
		public DbSet<User> User { get; set; }
		public DbSet<GamePoints> GamePoints { get; set; }
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			//var config = new ConfigurationBuilder()
			//	.SetBasePath(Directory.GetCurrentDirectory())
			//	.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
			//	.Build();
			//string connectionString = config.GetConnectionString("DefaultConnection");
			string connectionString = "***REMOVED***"; 

			optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
		}
	}
} 
