using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace GpsApplication.Models
{
    public class Auth
    {
        private readonly AppDbContext _context;
        public Auth(AppDbContext context)
        {
            _context = context;
        }
        public async Task Login(string login, string password)
        {
            var user = await _context.User.SingleOrDefaultAsync(p => p.Login == login);
            if(user != null)
            {
                var checkPass = BCrypt.Net.BCrypt.Verify(password, user.Password);
                if(checkPass)
                {
                    await SecureStorage.SetAsync("user_login", login);
                    await SecureStorage.SetAsync("user_id", user.ID.ToString());
                    await SecureStorage.SetAsync("user_name", user.Name);
                }
			}
        }
        public async Task Register(string name, string surname, string login, string password)
        {
			
			string salt = BCrypt.Net.BCrypt.GenerateSalt(4);
			var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, salt);

            var user = new User
            {
                Name = name,
                Surname = surname,
                Login = login,
                Password = passwordHash
            };
            await _context.User.AddAsync(user);
            await _context.SaveChangesAsync();
		}
        public async Task Logout()
        {
            SecureStorage.Remove("user_login");
            SecureStorage.Remove("user_id");
            SecureStorage.Remove("user_name");
        }
        public int ReturnUserScore(int ID)
        {
            var user = _context.User.Single(p => p.ID == ID);
            return user.AllPoints;
        }
        public async Task UpdateUserScore(int ID, int score)
        {
            var user = await _context.User.SingleAsync(p => p.ID == ID);
            user.AllPoints = score;
            await _context.SaveChangesAsync();
        }
        public bool CheckUserQuizHistory(string ID, string place)
        {
            int id = int.Parse(ID);
            return _context.QuizHistory.Any(p => p.userID == id && p.QuizPlaceName == place);
		}
        public async Task AddingUserToQuizHistory(int id, string placename) 
        {
            var quizhis = new QuizHistory
            {
                QuizPlaceName = placename,
                userID = id
            };
			await _context.QuizHistory.AddAsync(quizhis);
			await _context.SaveChangesAsync();
		}
    }
}
