using AuthService.Data;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services
{
    public class UsersSeeder
    {
        private readonly IServiceProvider _serviceProvider;

        public UsersSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task SeedUsers()
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = new PasswordHasher<User>();

            await context.Database.EnsureCreatedAsync();

            if (!await context.Users.AnyAsync())
            {
                var users = new List<User>
                {
                    new User
                    {
                        Username = "admin",
                        Email = "admin@test.com",
                        PasswordHash = hasher.HashPassword(null, "Admin@123"),
                        Role = "Admin"
                    },
                    new User
                    {
                        Username = "user1",
                         Email = "user1@test.com",
                        PasswordHash = hasher.HashPassword(null, "User@123"),
                        Role = "User"
                    }
                };

                context.Users.AddRange(users);
                await context.SaveChangesAsync();
            }
        }
    }
}
