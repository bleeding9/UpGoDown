using Microsoft.EntityFrameworkCore;
using UpGoDown.Api.Services;

namespace UpGoDown.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, AuthService auth)
    {
        if (await db.Users.AnyAsync()) return;

        await auth.RegisterAsync(new RegisterRequest
        {
            Login = "player",
            Password = "123456",
            Name = "Игрок",
        });
    }
}
