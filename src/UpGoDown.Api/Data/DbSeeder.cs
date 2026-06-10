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
            Login = "student",
            Password = "123456",
            Name = "Студент Иван",
            Role = UserRoles.Student,
        });

        await auth.RegisterAsync(new RegisterRequest
        {
            Login = "teacher",
            Password = "123456",
            Name = "Преподаватель",
            Role = UserRoles.Teacher,
        });
    }
}
